import { EventEmitter } from 'events';
import fs from 'fs';
import path from 'path';
import { Logger } from '../utils/Logger';
import { VRCLogEvent } from './types/VRCTypes';

export class VRCLogReader extends EventEmitter {
  private static readonly PLAYER_JOIN = /\[Behaviour\] OnPlayerJoined ([^\(]+) \((usr_[a-f0-9-]+)\)/;
  private static readonly PLAYER_LEFT = /\[Behaviour\] OnPlayerLeft ([^\(]+) \((usr_[a-f0-9-]+)\)/;
  private static readonly STICKER_SPAWN = /\[Always\] \[StickersManager\] User (usr_[a-f0-9-]+) \(([^)]+)\) spawned sticker (file_[a-f0-9-]+)/;
  private static readonly WORLD_JOINED = /\[Behaviour\] Joining (wrld_[a-f0-9-]+):([^\~]+)(?:~group\(([^)]+)\))?(?:~groupAccessType\(([^)]+)\))?(?:~region\(([^)]+)\))?/;

  private readonly processOffsets: Map<number, number> = new Map();
  private readonly processLogFiles: Map<number, string> = new Map();
  private readonly monitoredProcesses: Set<number> = new Set();
  private isEOSLauncherRunning: boolean = false;

  constructor() {
    super();
    this.startMonitoring();
  }

  private async startMonitoring(): Promise<void> {
    while (true) {
      try {
        const processes = await this.getVRChatProcesses();
        await this.handleProcessUpdates(processes);
        await new Promise(resolve => setTimeout(resolve, 1000));
      } catch (error) {
        Logger.error(`Error in VRChat process monitoring: ${error}`, 'VRCLogReader');
      }
    }
  }

  private async getVRChatProcesses(): Promise<number[]> {
    // Implementation will depend on the platform-specific process management
    // This is a placeholder - you'll need to implement actual process detection
    return [];
  }

  private async handleProcessUpdates(processes: number[]): Promise<void> {
    for (const pid of processes) {
      if (!this.monitoredProcesses.has(pid)) {
        Logger.info(`New VRChat process detected: ${pid}`, 'VRCLogReader');
        this.monitoredProcesses.add(pid);
        this.startProcessMonitoring(pid);
      }
    }

    // Remove exited processes
    for (const pid of this.monitoredProcesses) {
      if (!processes.includes(pid)) {
        this.handleProcessExit(pid);
      }
    }
  }

  private async startProcessMonitoring(processId: number): Promise<void> {
    try {
      const logFile = await this.findLogFile(processId);
      if (!logFile) {
        Logger.warn(`No log file found for process ${processId}`, 'VRCLogReader');
        return;
      }

      this.processLogFiles.set(processId, logFile);
      this.processOffsets.set(processId, 0);
      this.monitorLogFile(processId, logFile);
    } catch (error) {
      Logger.error(`Error starting process monitoring: ${error}`, 'VRCLogReader');
    }
  }

  private async findLogFile(processId: number): Promise<string | null> {
    const logDir = path.join(process.env.LOCALAPPDATA || '', 'Low', 'VRChat', 'VRChat');
    if (!fs.existsSync(logDir)) {
      return null;
    }

    const files = fs.readdirSync(logDir)
      .filter(file => file.startsWith('output_log_'))
      .map(file => path.join(logDir, file));

    return files.length > 0 ? files[files.length - 1] : null;
  }

  private monitorLogFile(processId: number, filePath: string): void {
    const watcher = fs.watch(filePath, (eventType) => {
      if (eventType === 'change') {
        this.processNewLogLines(processId, filePath);
      }
    });

    watcher.on('error', (error) => {
      Logger.error(`Error watching log file: ${error}`, 'VRCLogReader');
    });
  }

  private processNewLogLines(processId: number, filePath: string): void {
    try {
      const currentOffset = this.processOffsets.get(processId) || 0;
      const content = fs.readFileSync(filePath, 'utf8');
      const lines = content.slice(currentOffset).split('\n');

      for (const line of lines) {
        this.parseLine(processId, line.trim());
      }

      this.processOffsets.set(processId, content.length);
    } catch (error) {
      Logger.error(`Error processing log lines: ${error}`, 'VRCLogReader');
    }
  }

  private parseLine(processId: number, line: string): void {
    const matches = {
      join: VRCLogReader.PLAYER_JOIN.exec(line),
      left: VRCLogReader.PLAYER_LEFT.exec(line),
      sticker: VRCLogReader.STICKER_SPAWN.exec(line),
      world: VRCLogReader.WORLD_JOINED.exec(line)
    };

    for (const [action, match] of Object.entries(matches)) {
      if (match) {
        const event = this.createEventData(action, match, processId);
        this.emit('logEvent', event);
        return;
      }
    }
  }

  private createEventData(action: string, match: RegExpExecArray, processId: number): VRCLogEvent {
    const data: Record<string, any> = { processId };

    switch (action) {
      case 'join':
      case 'left':
        data.userId = match[2];
        data.displayName = match[1];
        break;
      case 'sticker':
        data.userId = match[1];
        data.username = match[2];
        data.stickerId = match[3];
        break;
      case 'world':
        data.worldId = match[1];
        data.worldName = match[2];
        data.groupId = match[3];
        data.worldAccessType = match[4];
        data.region = match[5];
        break;
    }

    return { action, data };
  }

  private handleProcessExit(processId: number): void {
    this.monitoredProcesses.delete(processId);
    this.processOffsets.delete(processId);
    this.processLogFiles.delete(processId);
    Logger.info(`VRChat process ${processId} has exited`, 'VRCLogReader');
  }

  public dispose(): void {
    this.removeAllListeners();
    // Clean up any watchers or timers here
  }
}
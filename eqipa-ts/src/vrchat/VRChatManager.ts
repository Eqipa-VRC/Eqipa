import { BaseManager } from '../core/BaseManager';
import { EqipaBot } from '../core/EqipaBot';
import { Logger } from '../utils/Logger';
import { VRCLogReader } from './VRCLogReader';
import { VRCModeration, VRCInstance } from './types/VRCTypes';

export class VRChatManager extends BaseManager {
  private logReader?: VRCLogReader;
  private worldId?: string;
  private groupId?: string;
  private readonly instances: Map<number, VRCInstance> = new Map();

  constructor(bot: EqipaBot) {
    super(bot);
    this.worldId = this.bot.getConfig().getConfig('vrchatWorldId');
    this.groupId = this.bot.getConfig().getConfig('vrchatGroupId');
  }

  public async initialize(): Promise<void> {
    if (this.isInitialized) {
      throw new Error('VRChatManager is already initialized');
    }

    try {
      this.logReader = new VRCLogReader();
      this.logReader.on('logEvent', this.handleLogEvent.bind(this));
      
      this.isInitialized = true;
      Logger.info('VRChatManager initialized successfully', 'VRChatManager');
    } catch (error) {
      Logger.error(`Failed to initialize VRChatManager: ${error}`, 'VRChatManager');
      throw error;
    }
  }

  private handleLogEvent(event: VRCLogEvent): void {
    switch (event.action) {
      case 'join':
        this.handlePlayerJoin(event.data);
        break;
      case 'left':
        this.handlePlayerLeft(event.data);
        break;
      case 'world':
        this.handleWorldJoin(event.data);
        break;
    }
  }

  private handlePlayerJoin(data: Record<string, any>): void {
    Logger.info(`Player ${data.displayName} (${data.userId}) joined`, 'VRChatManager');
    this.bot.getEventBus().emit('playerJoined', data);
  }

  private handlePlayerLeft(data: Record<string, any>): void {
    Logger.info(`Player ${data.displayName} (${data.userId}) left`, 'VRChatManager');
    this.bot.getEventBus().emit('playerLeft', data);
  }

  private handleWorldJoin(data: Record<string, any>): void {
    const instance: VRCInstance = {
      processId: data.processId,
      groupId: data.groupId || '',
      worldId: data.worldId,
      worldName: data.worldName,
      worldType: data.worldAccessType || 'public',
      worldRegion: data.region || 'unknown'
    };

    this.instances.set(data.processId, instance);
    Logger.info(`Joined world: ${instance.worldName}`, 'VRChatManager');
    this.bot.getEventBus().emit('worldJoined', instance);
  }

  public async sendModeration(moderation: VRCModeration): Promise<void> {
    // Implementation for sending moderation actions to VRChat
    // This would typically involve making API calls to VRChat's moderation endpoints
  }

  public override shutdown(): void {
    if (!this.isInitialized) return;

    this.logReader?.dispose();
    this.instances.clear();
    
    super.shutdown();
  }
}
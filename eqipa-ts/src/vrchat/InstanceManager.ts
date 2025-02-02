import { VRCInstance } from '../models/Instance';
import { VRChatManager } from './VRChatManager';
import { UserManager } from './UserManager';

export class InstanceManager extends BaseManager {
  private instances: Map<number, VRCInstance> = new Map();
  private vrchatManager?: VRChatManager;
  private userManager?: UserManager;

  constructor(bot: EqipaBot) {
    super(bot);
  }

  public async initialize(): Promise<void> {
    if (this.isInitialized) {
      throw new Error('InstanceManager is already initialized');
    }

    try {
      this.vrchatManager = this.bot.getManagerOrDefault('VRChatManager') as VRChatManager;
      this.userManager = this.bot.getManagerOrDefault('UserManager') as UserManager;

      this.bot.getEventBus().on('logEvent', this.handleLogEvent.bind(this));

      this.isInitialized = true;
      Logger.info('InstanceManager initialized successfully', 'InstanceManager');
    } catch (error) {
      Logger.error(`Failed to initialize InstanceManager: ${error}`, 'InstanceManager');
      throw error;
    }
  }

  private handleLogEvent(event: VRCLogEvent): void {
    switch (event.action) {
      case 'setInstance':
        this.handleInstanceChange(event.data);
        break;
      case 'resetInstance':
        this.handleResetInstance(event.data);
        break;
      case 'join':
      case 'left':
        this.handlePlayerMovement(event.data);
        break;
    }
  }

  private handleInstanceChange(data: Record<string, any>): void {
    const instance: VRCInstance = {
      processId: data.processId,
      groupId: data.groupId || '',
      worldId: data.worldId,
      worldName: data.worldName,
      worldType: data.worldAccessType || 'public',
      worldRegion: data.region || 'unknown'
    };

    this.instances.set(instance.processId, instance);
    
    Logger.info(
      `Process ${instance.processId} entered ${instance.worldType} world ${instance.worldName}`,
      'InstanceManager'
    );

    this.notifyInstanceChange(instance);
  }

  private handleResetInstance(data: Record<string, any>): void {
    const processId = data.processId;
    if (this.instances.has(processId)) {
      const instance = this.instances.get(processId)!;
      Logger.info(`Resetting instance for process ${processId}`, 'InstanceManager');
      
      // Notify Discord about reset
      this.notifyInstanceReset(instance);
      
      this.instances.delete(processId);
    }
  }

  private handlePlayerMovement(data: Record<string, any>): void {
    const { userId, action, processId } = data;
    const instance = this.instances.get(processId);
    
    if (instance && this.userManager) {
      this.userManager.update(userId, user => {
        user.currentInstanceId = action === 'join' ? this.getInstanceId(instance) : undefined;
      });
    }
  }

  private getInstanceId(instance: VRCInstance): string {
    return `${instance.worldId}:${instance.worldName}~group(${instance.groupId})~groupAccessType(${instance.worldType})~region(${instance.worldRegion})`;
  }

  public getInstance(processId: number): VRCInstance | undefined {
    return this.instances.get(processId);
  }

  public getAllInstances(): VRCInstance[] {
    return Array.from(this.instances.values());
  }

  public getUsersInInstance(processId: number): string[] {
    const instance = this.instances.get(processId);
    if (!instance || !this.userManager) return [];

    const instanceId = this.getInstanceId(instance);
    return this.userManager
      .getOnlineUsers()
      .filter(user => user.currentInstanceId === instanceId)
      .map(user => user.id);
  }

  private async notifyInstanceChange(instance: VRCInstance): Promise<void> {
    const discordManager = this.bot.getManagerOrDefault('DiscordManager') as DiscordManager;
    if (!discordManager) return;

    await discordManager.sendWebhookMessage(
      'Instance Change',
      `Process ${instance.processId} entered world ${instance.worldName}\nType: ${instance.worldType}\nRegion: ${instance.worldRegion}`,
      DiscordEmbedColors.INFO
    );
  }

  private async notifyInstanceReset(instance: VRCInstance): Promise<void> {
    const discordManager = this.bot.getManagerOrDefault('DiscordManager') as DiscordManager;
    if (!discordManager) return;

    await discordManager.sendWebhookMessage(
      'Instance Reset',
      `Instance reset for process ${instance.processId}`,
      DiscordEmbedColors.WARN
    );
  }

  public override shutdown(): void {
    if (!this.isInitialized) return;

    this.instances.clear();
    super.shutdown();
  }
}
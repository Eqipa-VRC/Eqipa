import { BaseManager } from '../core/BaseManager';
import { EqipaBot } from '../core/EqipaBot';
import { Registry } from '../models/Registry';
import { Avatar } from '../models/Avatar';
import { Logger } from '../utils/Logger';
import { VRCLogEvent } from './types/VRCTypes';
import { DiscordManager } from '../discord/DiscordManager';
import { DiscordEmbedColors } from '../discord/types/DiscordTypes';

export class AvatarManager extends BaseManager {
  private registry: Registry<Avatar>;
  private discordManager?: DiscordManager;
  private readonly MAX_AVATAR_HISTORY = 10;

  constructor(bot: EqipaBot) {
    super(bot);
    this.registry = new Registry<Avatar>('avatars');
  }

  public async initialize(): Promise<void> {
    if (this.isInitialized) {
      throw new Error('AvatarManager is already initialized');
    }

    try {
      this.discordManager = this.bot.getManagerOrDefault('DiscordManager') as DiscordManager;
      
      // Subscribe to avatar change events
      this.bot.getEventBus().on('logEvent', this.handleLogEvent.bind(this));

      this.isInitialized = true;
      Logger.info('AvatarManager initialized successfully', 'AvatarManager');
    } catch (error) {
      Logger.error(`Failed to initialize AvatarManager: ${error}`, 'AvatarManager');
      throw error;
    }
  }

  private handleLogEvent(event: VRCLogEvent): void {
    if (event.action === 'avatarChange') {
      this.handleAvatarChange(event.data);
    }
  }

  private async handleAvatarChange(data: { userId: string; avatarId: string; avatarName: string }): Promise<void> {
    const now = Date.now();

    // Update avatar record
    if (!this.registry.has(data.avatarId)) {
      const newAvatar: Avatar = {
        id: data.avatarId,
        name: data.avatarName,
        banned: false,
        lastUsed: now
      };
      this.registry.save(data.avatarId, newAvatar);
    } else {
      this.registry.update(data.avatarId, avatar => {
        avatar.lastUsed = now;
        if (avatar.name !== data.avatarName) {
          avatar.name = data.avatarName;
        }
      });
    }

    // Update user's avatar history
    const userManager = this.bot.getManagerOrDefault('UserManager');
    if (userManager && userManager.getUserById(data.userId)) {
      userManager.update(data.userId, user => {
        if (!user.lastAvatars) {
          user.lastAvatars = [];
        }
        // Add to history and maintain max length
        user.lastAvatars.unshift(data.avatarId);
        if (user.lastAvatars.length > this.MAX_AVATAR_HISTORY) {
          user.lastAvatars.pop();
        }
      });
    }

    await this.notifyDiscord(
      'Avatar Change',
      `User changed avatar to: \`${data.avatarName}\` (\`${data.avatarId}\`)`,
      DiscordEmbedColors.INFO
    );
  }

  public getAvatarById(avatarId: string): Avatar | null {
    return this.registry.get(avatarId);
  }

  public getUserAvatarHistory(userId: string): Avatar[] {
    const userManager = this.bot.getManagerOrDefault('UserManager');
    if (!userManager) return [];

    const user = userManager.getUserById(userId);
    if (!user || !user.lastAvatars) return [];

    return user.lastAvatars
      .map(avatarId => this.getAvatarById(avatarId))
      .filter((avatar): avatar is Avatar => avatar !== null);
  }

  public setAvatarBanStatus(avatarId: string, banned: boolean): boolean {
    try {
      if (!this.registry.has(avatarId)) return false;

      this.registry.update(avatarId, avatar => {
        avatar.banned = banned;
      });

      return true;
    } catch (error) {
      Logger.error(`Failed to update avatar ban status: ${error}`, 'AvatarManager');
      return false;
    }
  }

  private async notifyDiscord(title: string, description: string, color: DiscordEmbedColors): Promise<void> {
    try {
      await this.discordManager?.sendWebhookMessage(title, description, color);
    } catch (error) {
      Logger.error(`Failed to send Discord notification: ${error}`, 'AvatarManager');
    }
  }
}
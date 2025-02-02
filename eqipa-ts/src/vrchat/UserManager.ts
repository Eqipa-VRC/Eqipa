import { BaseManager } from '../core/BaseManager';
import { EqipaBot } from '../core/EqipaBot';
import { Registry } from '../models/Registry';
import { User, UserStatus } from '../models/User';
import { Logger } from '../utils/Logger';
import { VRCLogEvent } from './types/VRCTypes';
import { DiscordManager } from '../discord/DiscordManager';
import { DiscordEmbedColors } from '../discord/types/DiscordTypes';

export class UserManager extends BaseManager {
  private registry: Registry<User>;
  private discordManager?: DiscordManager;
  private groupId?: string;

  constructor(bot: EqipaBot) {
    super(bot);
    this.registry = new Registry<User>('users');
    this.groupId = this.bot.getConfig().getConfig('vrchatGroupId');
    this.resetOnlineStatus();
  }

  private resetOnlineStatus(): void {
    this.registry.updateAll(user => {
      if (user.status === UserStatus.Online) {
        user.status = UserStatus.Offline;
        user.lastVisit = Date.now();
        user.currentInstanceId = undefined;
      }
    });
  }

  public async initialize(): Promise<void> {
    if (this.isInitialized) {
      throw new Error('UserManager is already initialized');
    }

    try {
      // Get Discord manager reference
      this.discordManager = this.bot.getManagerOrDefault('DiscordManager') as DiscordManager;

      // Subscribe to VRChat log events
      this.bot.getEventBus().on('logEvent', this.handleLogEvent.bind(this));

      this.isInitialized = true;
      Logger.info('UserManager initialized successfully', 'UserManager');
    } catch (error) {
      Logger.error(`Failed to initialize UserManager: ${error}`, 'UserManager');
      throw error;
    }
  }

  private async handleLogEvent(event: VRCLogEvent): void {
    switch (event.action) {
      case 'join':
        await this.handleUserJoined(event.data);
        break;
      case 'left':
        await this.handleUserLeft(event.data);
        break;
    }
  }

  private async handleUserJoined(data: { userId: string; displayName: string }): Promise<void> {
    const now = Date.now();
    
    if (!this.registry.has(data.userId)) {
      // New user
      const newUser: User = {
        id: data.userId,
        admin: false,
        status: UserStatus.Online,
        joinedAt: now,
        lastVisit: now,
        visitCount: 1,
        displayName: data.displayName,
        penalties: [],
        lastAvatars: []
      };

      this.registry.save(data.userId, newUser);
      Logger.info(`Registered new user ${data.displayName} (${data.userId})`, 'UserManager');

      await this.notifyDiscord(
        'New User Joined',
        `User \`${data.displayName}\` (\`${data.userId}\`) joined for the first time.`,
        DiscordEmbedColors.SUCCESS
      );
    } else {
      // Existing user
      this.registry.update(data.userId, user => {
        if (user.status !== UserStatus.Online) {
          if (user.displayName !== data.displayName) {
            user.displayName = data.displayName;
          }
          user.status = UserStatus.Online;
          user.visitCount++;
        }
      });

      const user = this.registry.get(data.userId);
      if (user) {
        await this.notifyDiscord(
          'User Joined',
          [
            `User \`${user.displayName}\` (\`${user.id}\`) joined the instance`,
            `First joined: <t:${Math.floor(user.joinedAt / 1000)}:F>`,
            `Visit count: ${user.visitCount}`,
            `Last visit: <t:${Math.floor(user.lastVisit / 1000)}:R>`
          ].join('\n'),
          DiscordEmbedColors.INFO
        );
      }
    }
  }

  private async handleUserLeft(data: { userId: string; displayName: string }): Promise<void> {
    const now = Date.now();

    if (!this.registry.has(data.userId)) {
      // Should not happen normally, but handle it anyway
      const newUser: User = {
        id: data.userId,
        admin: false,
        status: UserStatus.Offline,
        joinedAt: now,
        lastVisit: now,
        visitCount: 1,
        displayName: data.displayName,
        penalties: [],
        lastAvatars: []
      };

      this.registry.save(data.userId, newUser);
      Logger.warn(`Registered user ${data.displayName} (${data.userId}) during leave event`, 'UserManager');
    } else {
      this.registry.update(data.userId, user => {
        if (user.status !== UserStatus.Offline) {
          user.status = UserStatus.Offline;
          user.lastVisit = now;
          user.currentInstanceId = undefined;
        }
      });

      const user = this.registry.get(data.userId);
      if (user) {
        await this.notifyDiscord(
          'User Left',
          `User \`${user.displayName}\` (\`${user.id}\`) left the instance`,
          DiscordEmbedColors.INFO
        );
      }
    }
  }

  private async notifyDiscord(title: string, description: string, color: DiscordEmbedColors): Promise<void> {
    try {
      await this.discordManager?.sendWebhookMessage(title, description, color);
    } catch (error) {
      Logger.error(`Failed to send Discord notification: ${error}`, 'UserManager');
    }
  }

  // User management methods
  public getUserById(userId: string): User | null {
    return this.registry.get(userId);
  }

  public getOnlineUsers(): User[] {
    return this.registry.getAllFiltered(user => user.status === UserStatus.Online);
  }

  public getOnlineAdmins(): User[] {
    return this.registry.getAllFiltered(user => user.status === UserStatus.Online && user.admin);
  }

  public getUserByDiscordId(discordId: string): User | null {
    const users = this.registry.getAllFiltered(user => user.discordId === discordId);
    return users.length > 0 ? users[0] : null;
  }

  public async linkDiscordAccount(userId: string, discordId: string): Promise<boolean> {
    try {
      if (!this.registry.has(userId)) {
        return false;
      }

      // Check if Discord ID is already linked
      const existingUser = this.getUserByDiscordId(discordId);
      if (existingUser) {
        return false;
      }

      this.registry.update(userId, user => {
        user.discordId = discordId;
      });

      await this.notifyDiscord(
        'Account Linked',
        `VRChat account (\`${userId}\`) has been linked with Discord account <@${discordId}>`,
        DiscordEmbedColors.SUCCESS
      );

      return true;
    } catch (error) {
      Logger.error(`Failed to link Discord account: ${error}`, 'UserManager');
      return false;
    }
  }

  public addUserPenalty(userId: string, penalty: Omit<UserPenalty, 'received'>): boolean {
    try {
      if (!this.registry.has(userId)) {
        return false;
      }

      this.registry.update(userId, user => {
        user.penalties.push({
          ...penalty,
          received: Date.now()
        });
      });

      return true;
    } catch (error) {
      Logger.error(`Failed to add user penalty: ${error}`, 'UserManager');
      return false;
    }
  }

  public override shutdown(): void {
    if (!this.isInitialized) return;

    this.resetOnlineStatus();
    super.shutdown();
  }
}
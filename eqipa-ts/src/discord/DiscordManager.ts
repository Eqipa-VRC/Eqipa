import { 
  Client, 
  GatewayIntentBits, 
  Partials,
  Events,
  ActivityType
} from 'discord.js';
import { BaseManager } from '../core/BaseManager';
import { EqipaBot } from '../core/EqipaBot';
import { Logger } from '../utils/Logger';
import { CommandManager } from './CommandManager';
import { DiscordWebhook } from './DiscordWebhook';
import { DiscordEmbedColors } from './types/DiscordTypes'

export class DiscordManager extends BaseManager {
  private client?: Client;
  private commandManager?: CommandManager;
  private webhook?: DiscordWebhook;
  private presenceUpdateInterval?: NodeJS.Timeout;

  constructor(bot: EqipaBot) {
    super(bot);
  }

  public async initialize(): Promise<void> {
    if (this.isInitialized) {
      throw new Error('DiscordManager is already initialized');
    }

    try {
      this.client = new Client({
        intents: [
          GatewayIntentBits.Guilds,
          GatewayIntentBits.GuildMessages,
          GatewayIntentBits.GuildMembers,
          GatewayIntentBits.MessageContent
        ],
        partials: [
          Partials.Channel,
          Partials.Message,
          Partials.User,
          Partials.GuildMember
        ]
      });

      this.commandManager = new CommandManager(this.bot);
      const webhookUrl = this.bot.getConfig().getConfig('discordWebhookUrl');
      this.webhook = new DiscordWebhook(webhookUrl);

      await this.setupClientEvents();
      await this.login();

      this.startPresenceUpdates();
      this.isInitialized = true;
      Logger.info('DiscordManager initialized successfully', 'DiscordManager');
    } catch (error) {
      Logger.error(`Failed to initialize DiscordManager: ${error}`, 'DiscordManager');
      throw error;
    }
  }

  private async setupClientEvents(): Promise<void> {
    if (!this.client) return;

    this.client.on(Events.ClientReady, () => {
      Logger.info(`Logged in as ${this.client?.user?.tag}`, 'DiscordManager');
      this.registerCommands();
    });

    this.client.on(Events.InteractionCreate, async (interaction) => {
      try {
        if (interaction.isCommand()) {
          const command = this.commandManager?.getCommands().get(interaction.commandName);
          if (command) {
            await command.execute(interaction);
          }
        }
      } catch (error) {
        Logger.error(`Error handling interaction: ${error}`, 'DiscordManager');
      }
    });

    this.client.on(Events.Error, (error) => {
      Logger.error(`Discord client error: ${error}`, 'DiscordManager');
    });
  }

  private async login(): Promise<void> {
    if (!this.client) return;

    const token = this.bot.getConfig().getConfig('discordToken');
    if (!token) {
      throw new Error('Discord token not configured');
    }

    await this.client.login(token);
  }

  private async registerCommands(): Promise<void> {
    if (!this.client?.application || !this.commandManager) return;

    try {
      const commands = this.commandManager.getCommands().map(cmd => cmd.data);
      await this.client.application.commands.set(commands);
      Logger.info('Registered global slash commands', 'DiscordManager');
    } catch (error) {
      Logger.error(`Failed to register commands: ${error}`, 'DiscordManager');
    }
  }

  private startPresenceUpdates(): void {
    this.updatePresence();
    this.presenceUpdateInterval = setInterval(() => this.updatePresence(), 60000);
  }

  private async updatePresence(): Promise<void> {
    if (!this.client?.user) return;

    try {
      const vrchatManager = this.bot.getManagerOrDefault('VRChatManager');
      const onlineUsers = vrchatManager?.getOnlineUserCount() || 0;
      const maxUsers = vrchatManager?.getMaxUserCount() || 0;

      await this.client.user.setActivity({
        name: `${onlineUsers}/${maxUsers} users online`,
        type: ActivityType.Watching
      });
    } catch (error) {
      Logger.error(`Failed to update presence: ${error}`, 'DiscordManager');
    }
  }

  public async sendWebhookMessage(
    title: string,
    description: string,
    color: DiscordEmbedColors = DiscordEmbedColors.INFO
  ): Promise<void> {
    await this.webhook?.sendEmbed(title, description, color);
  }

  public override shutdown(): void {
    if (!this.isInitialized) return;

    this.client?.destroy();
    this.webhook?.dispose();
    if (this.presenceUpdateInterval) {
      clearInterval(this.presenceUpdateInterval);
    }

    super.shutdown();
  }
}
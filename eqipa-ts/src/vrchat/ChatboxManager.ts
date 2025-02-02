import { BaseManager } from '../core/BaseManager';
import { EqipaBot } from '../core/EqipaBot';
import { Logger } from '../utils/Logger';
import { OSCManager } from './OSCManager';
import { OSCAddresses } from './types/OSCTypes';
import { VRChatManager } from './VRChatManager';

interface ChatMessage {
  text: string[];
  placeholders: Record<string, () => string>;
}

export class ChatboxManager extends BaseManager {
  private oscManager?: OSCManager;
  private vrchatManager?: VRChatManager;
  private lastUpdateTime: number = 0;
  private currentMessageIndex: number = 0;
  private updateInterval?: NodeJS.Timeout;

  private readonly messages: ChatMessage[] = [
    {
      text: [
        "Welcome to Eqipa - a new dimension of conversation and entertainment!",
        "Please check the rules on the board in front of you."
      ],
      placeholders: {}
    },
    {
      text: [
        "Join our Discord server: discord.gg/eqipa",
        "Meet people with a positive attitude and willingness to talk."
      ],
      placeholders: {}
    },
    {
      text: [
        "Current available users: {online} out of {maxpi} ({percent}%)",
        "Current available administrators: {admins}"
      ],
      placeholders: {
        online: () => this.vrchatManager?.getOnlineUserCount().toString() || "0",
        maxpi: () => this.vrchatManager?.getMaxUserCount().toString() || "0",
        admins: () => this.vrchatManager?.getOnlineAdminCount().toString() || "0",
        percent: () => {
          const online = this.vrchatManager?.getOnlineUserCount() || 0;
          const max = this.vrchatManager?.getMaxUserCount() || 1;
          return Math.floor((online / max) * 100).toString();
        }
      }
    }
  ];

  constructor(bot: EqipaBot) {
    super(bot);
  }

  public async initialize(): Promise<void> {
    if (this.isInitialized) {
      throw new Error('ChatboxManager is already initialized');
    }

    try {
      this.oscManager = new OSCManager();
      await this.oscManager.initialize();

      this.vrchatManager = this.bot.getManagerOrDefault('VRChatManager') as VRChatManager;

      // Add default VRChat OSC endpoint
      this.oscManager.addEndpoint({
        address: '127.0.0.1',
        port: 9000
      });

      this.startUpdateLoop();
      this.isInitialized = true;
      Logger.info('ChatboxManager initialized successfully', 'ChatboxManager');
    } catch (error) {
      Logger.error(`Failed to initialize ChatboxManager: ${error}`, 'ChatboxManager');
      throw error;
    }
  }

  private startUpdateLoop(): void {
    this.updateMessage();
    this.updateInterval = setInterval(() => this.updateMessage(), 15000);
  }

  private async updateMessage(): Promise<void> {
    try {
      this.currentMessageIndex = (this.currentMessageIndex + 1) % this.messages.length;
      const message = this.messages[this.currentMessageIndex];
      
      let text = message.text.join('\n');
      
      // Replace placeholders
      for (const [key, getValue] of Object.entries(message.placeholders)) {
        text = text.replace(`{${key}}`, getValue());
      }

      await this.sendChatboxMessage(text);
      this.lastUpdateTime = Date.now();
    } catch (error) {
      Logger.error(`Failed to update chatbox message: ${error}`, 'ChatboxManager');
    }
  }

  private async sendChatboxMessage(text: string, notification: boolean = true): Promise<void> {
    try {
      // First, set typing indicator
      await this.oscManager?.send({
        address: OSCAddresses.CHATBOX_TYPING,
        args: [true]
      });

      // Then send the message
      await this.oscManager?.send({
        address: OSCAddresses.CHATBOX_INPUT,
        args: [text, notification]
      });

      // Finally, clear typing indicator
      await this.oscManager?.send({
        address: OSCAddresses.CHATBOX_TYPING,
        args: [false]
      });

      Logger.debug(`Sent chatbox message: ${text}`, 'ChatboxManager');
    } catch (error) {
      Logger.error(`Failed to send chatbox message: ${error}`, 'ChatboxManager');
    }
  }

  public override update(): void {
    if (!this.isInitialized || this.isDisposed) return;

    const now = Date.now();
    if (now - this.lastUpdateTime >= 15000) {
      this.updateMessage();
    }
  }

  public override shutdown(): void {
    if (!this.isInitialized) return;

    if (this.updateInterval) {
      clearInterval(this.updateInterval);
    }
    
    this.oscManager?.dispose();
    super.shutdown();
  }
}
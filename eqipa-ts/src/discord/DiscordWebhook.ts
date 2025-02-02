import { WebhookClient, EmbedBuilder } from 'discord.js';
import { Logger } from '../utils/Logger';
import { DiscordEmbedColors } from './types/DiscordTypes';

export class DiscordWebhook {
  private webhookClient?: WebhookClient;

  constructor(webhookUrl: string) {
    if (webhookUrl) {
      this.webhookClient = new WebhookClient({ url: webhookUrl });
    }
  }

  public async sendEmbed(
    title: string,
    description: string,
    color: DiscordEmbedColors = DiscordEmbedColors.INFO
  ): Promise<void> {
    if (!this.webhookClient) {
      throw new Error('Webhook URL is not set');
    }

    const embed = new EmbedBuilder()
      .setTitle(title)
      .setDescription(description)
      .setColor(color)
      .setTimestamp();

    try {
      await this.webhookClient.send({ embeds: [embed] });
      Logger.debug(`Sent webhook message: ${title}`, 'DiscordWebhook');
    } catch (error) {
      Logger.error(`Failed to send webhook: ${error}`, 'DiscordWebhook');
      throw error;
    }
  }

  public dispose(): void {
    this.webhookClient?.destroy();
  }
}
import { SlashCommandBuilder, EmbedBuilder, ButtonBuilder, ButtonStyle, ActionRowBuilder } from 'discord.js';
import { BaseCommand } from './BaseCommand';
import { EqipaBot } from '../../core/EqipaBot';
import { DiscordEmbedColors } from '../types/DiscordTypes'

export class VRCLinkCommand extends BaseCommand {
  public readonly data = new SlashCommandBuilder()
    .setName('vrc-link')
    .setDescription('Link your VRChat account with Discord');

  constructor(private bot: EqipaBot) {
    super();
  }

  public async execute(interaction: CommandInteraction): Promise<void> {
    try {
      await interaction.deferReply({ ephemeral: true });

      const embed = new EmbedBuilder()
        .setTitle('VRChat Account Linking')
        .setDescription('Click the button below to start the linking process')
        .setColor(DiscordEmbedColors.INFO);

      const linkButton = new ButtonBuilder()
        .setCustomId('vrc-link-start')
        .setLabel('Start Linking')
        .setStyle(ButtonStyle.Primary);

      const row = new ActionRowBuilder<ButtonBuilder>()
        .addComponents(linkButton);

      await interaction.editReply({
        embeds: [embed],
        components: [row]
      });
    } catch (error) {
      await this.handleError(interaction, error);
    }
  }
}
import { 
  CommandInteraction, 
  SlashCommandBuilder, 
  SlashCommandSubcommandsOnlyBuilder 
} from 'discord.js';
import { Logger } from '../../utils/Logger';

export abstract class BaseCommand {
  public abstract readonly data: SlashCommandBuilder | SlashCommandSubcommandsOnlyBuilder;

  public abstract execute(interaction: CommandInteraction): Promise<void>;

  protected async handleError(interaction: CommandInteraction, error: unknown): Promise<void> {
    const errorMessage = error instanceof Error ? error.message : 'An unknown error occurred';
    Logger.error(`Command error: ${errorMessage}`, this.constructor.name);

    if (interaction.deferred || interaction.replied) {
      await interaction.followUp({
        content: `Error: ${errorMessage}`,
        ephemeral: true
      });
    } else {
      await interaction.reply({
        content: `Error: ${errorMessage}`,
        ephemeral: true
      });
    }
  }
}
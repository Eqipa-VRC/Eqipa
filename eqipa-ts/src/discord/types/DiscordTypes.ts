import { CommandInteraction, ButtonInteraction, ModalSubmitInteraction } from 'discord.js';

export enum DiscordEmbedColors {
  SUCCESS = 0x32a852,
  ERROR = 0xff0000,
  INFO = 0x5865F2,
}

export interface CommandHandlerContext {
  interaction: CommandInteraction;
}

export interface ButtonHandlerContext {
  interaction: ButtonInteraction;
}

export interface ModalHandlerContext {
  interaction: ModalSubmitInteraction;
}
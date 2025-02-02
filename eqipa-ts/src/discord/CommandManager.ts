import { Collection } from 'discord.js';
import { BaseCommand } from './commands/BaseCommand';
import { VRCLinkCommand } from './commands/VRCLinkCommand';
import { Logger } from '../utils/Logger';

export class CommandManager {
  private commands: Collection<string, BaseCommand> = new Collection();

  constructor(private bot: EqipaBot) {
    this.registerCommands();
  }

  private registerCommands(): void {
    this.registerCommand(new VRCLinkCommand(this.bot));
    // Register other commands here
  }

  private registerCommand(command: BaseCommand): void {
    this.commands.set(command.data.name, command);
    Logger.info(`Registered command: ${command.data.name}`, 'CommandManager');
  }

  public getCommands(): Collection<string, BaseCommand> {
    return this.commands;
  }
}
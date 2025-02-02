import { Logger } from '../utils/Logger';
import { Config } from '../models/Config';
import { EventEmitter } from 'events';

export class EqipaBot {
  private static instance: EqipaBot;
  private config: Config;
  private eventBus: EventEmitter;
  private isInitialized: boolean = false;
  private isDisposed: boolean = false;

  private constructor() {
    Logger.step('Initializing EqipaBot...', 'EqipaBot');
    this.config = Config.getInstance();
    this.eventBus = new EventEmitter();
  }

  public static getInstance(): EqipaBot {
    if (!EqipaBot.instance) {
      EqipaBot.instance = new EqipaBot();
    }
    return EqipaBot.instance;
  }

  public async initialize(): Promise<void> {
    if (this.isInitialized) {
      throw new Error('EqipaBot is already initialized');
    }

    if (this.isDisposed) {
      throw new Error('Cannot initialize disposed EqipaBot');
    }

    try {
      // Initialize managers here
      this.isInitialized = true;
      Logger.info('EqipaBot initialized successfully', 'EqipaBot');
    } catch (error) {
      Logger.error(`Failed to initialize EqipaBot: ${error}`, 'EqipaBot');
      throw error;
    }
  }

  public getEventBus(): EventEmitter {
    return this.eventBus;
  }

  public getConfig(): Config {
    return this.config;
  }

  public isInitializedStatus(): boolean {
    return this.isInitialized;
  }

  public dispose(): void {
    if (this.isDisposed) {
      return;
    }

    try {
      // Cleanup managers here
      this.eventBus.removeAllListeners();
      this.isInitialized = false;
      this.isDisposed = true;
      Logger.info('EqipaBot disposed successfully', 'EqipaBot');
    } catch (error) {
      Logger.error(`Error during EqipaBot disposal: ${error}`, 'EqipaBot');
    }
  }
}
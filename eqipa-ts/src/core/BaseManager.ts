import { IManager } from '../models/Manager';
import { EqipaBot } from './EqipaBot';
import { Logger } from '../utils/Logger';

export abstract class BaseManager implements IManager {
  protected readonly bot: EqipaBot;
  protected isInitialized: boolean = false;
  protected isDisposed: boolean = false;

  constructor(bot: EqipaBot) {
    this.bot = bot;
  }

  public abstract initialize(): void | Promise<void>;
  
  public update(): void | Promise<void> {
    // Default implementation
  }

  public shutdown(): void | Promise<void> {
    if (!this.isInitialized) return;

    this.isInitialized = false;
    Logger.info(`${this.constructor.name} shutdown`, this.constructor.name);
  }

  public dispose(): void {
    if (this.isDisposed) return;

    this.shutdown();
    this.isDisposed = true;
    Logger.info(`${this.constructor.name} disposed`, this.constructor.name);
  }

  protected throwIfDisposed(): void {
    if (this.isDisposed) {
      throw new Error(`${this.constructor.name} is disposed`);
    }
  }

  protected throwIfNotInitialized(): void {
    if (!this.isInitialized) {
      throw new Error(`${this.constructor.name} is not initialized`);
    }
  }
}
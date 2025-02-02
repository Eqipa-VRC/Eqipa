import fs from 'fs';
import path from 'path';
import { Logger } from '../utils/Logger';

export interface IConfiguration {
  gptApi?: string;
  gptToken?: string;
  vrchatGroupId: string;
  vrchatWorldId: string;
  vrchatUsername: string;
  vrchatPassword: string;
  vrchatOscMessage: string[];
  discordToken: string;
  discordWebhookUrl: string;
  discordServerId: string;
  speechRecognizerToken?: string;
  speechRecognizerRegion?: string;
}

export class Config {
  private static instance: Config;
  private configuration: IConfiguration;
  private readonly configFilePath: string = 'config.json';

  private constructor() {
    this.configuration = this.loadConfig();
  }

  public static getInstance(): Config {
    if (!Config.instance) {
      Config.instance = new Config();
    }
    return Config.instance;
  }

  private loadConfig(): IConfiguration {
    try {
      if (!fs.existsSync(this.configFilePath)) {
        this.createDefaultConfig();
      }

      const configJson = fs.readFileSync(this.configFilePath, 'utf-8');
      const config = JSON.parse(configJson);
      this.updateConfig(config);
      return config;
    } catch (error) {
      Logger.error(`Failed to load config: ${error}`, 'Config');
      throw error;
    }
  }

  private createDefaultConfig(): void {
    const defaultConfig: IConfiguration = {
      vrchatGroupId: 'grp_2e1917ed-0f8d-4075-8098-5919a37c8f43',
      vrchatWorldId: 'wrld_dec35e59-53f5-4def-b29c-2d7b649b8638',
      vrchatUsername: '',
      vrchatPassword: '',
      vrchatOscMessage: [],
      discordToken: '',
      discordWebhookUrl: '',
      discordServerId: '1325008258200764417',
    };

    fs.writeFileSync(
      this.configFilePath,
      JSON.stringify(defaultConfig, null, 2)
    );
    Logger.info('Default config file created', 'Config');
  }

  private updateConfig(config: IConfiguration): void {
    const defaultConfig: IConfiguration = this.configuration || {
      vrchatGroupId: 'grp_2e1917ed-0f8d-4075-8098-5919a37c8f43',
      vrchatWorldId: 'wrld_dec35e59-53f5-4def-b29c-2d7b649b8638',
      vrchatUsername: '',
      vrchatPassword: '',
      vrchatOscMessage: [],
      discordToken: '',
      discordWebhookUrl: '',
      discordServerId: '1325008258200764417',
    };

    // Merge default config with loaded config
    this.configuration = { ...defaultConfig, ...config };
    this.saveConfig();
  }

  private saveConfig(): void {
    fs.writeFileSync(
      this.configFilePath,
      JSON.stringify(this.configuration, null, 2)
    );
  }

  public getConfig<K extends keyof IConfiguration>(key: K): IConfiguration[K] {
    return this.configuration[key];
  }

  public setConfig<K extends keyof IConfiguration>(key: K, value: IConfiguration[K]): void {
    this.configuration[key] = value;
    this.saveConfig();
  }
}

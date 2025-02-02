import { Client, Server } from 'node-osc';
import { Logger } from '../utils/Logger';
import { OSCMessage, OSCEndpoint } from './types/OSCTypes';

export class OSCManager {
  private clients: Map<string, Client> = new Map();
  private server?: Server;
  private endpoints: Set<string> = new Set();

  constructor(private readonly defaultPort: number = 9000) {}

  public async initialize(): Promise<void> {
    try {
      // Start OSC server to receive messages
      this.server = new Server(this.defaultPort, '127.0.0.1');
      this.server.on('message', this.handleMessage.bind(this));

      Logger.info(`OSC server started on port ${this.defaultPort}`, 'OSCManager');
    } catch (error) {
      Logger.error(`Failed to initialize OSC server: ${error}`, 'OSCManager');
      throw error;
    }
  }

  private handleMessage(msg: any[]): void {
    const [address, ...args] = msg;
    Logger.debug(`Received OSC message: ${address} ${JSON.stringify(args)}`, 'OSCManager');
  }

  public addEndpoint(endpoint: OSCEndpoint): void {
    const key = `${endpoint.address}:${endpoint.port}`;
    if (!this.endpoints.has(key)) {
      this.clients.set(key, new Client(endpoint.address, endpoint.port));
      this.endpoints.add(key);
      Logger.info(`Added OSC endpoint: ${key}`, 'OSCManager');
    }
  }

  public removeEndpoint(endpoint: OSCEndpoint): void {
    const key = `${endpoint.address}:${endpoint.port}`;
    const client = this.clients.get(key);
    if (client) {
      client.close();
      this.clients.delete(key);
      this.endpoints.delete(key);
      Logger.info(`Removed OSC endpoint: ${key}`, 'OSCManager');
    }
  }

  public async send(message: OSCMessage): Promise<void> {
    const promises = Array.from(this.clients.values()).map(client =>
      new Promise<void>((resolve, reject) => {
        client.send(message.address, ...message.args, (err) => {
          if (err) reject(err);
          else resolve();
        });
      })
    );

    try {
      await Promise.all(promises);
      Logger.debug(`Sent OSC message to ${this.clients.size} endpoints: ${message.address}`, 'OSCManager');
    } catch (error) {
      Logger.error(`Failed to send OSC message: ${error}`, 'OSCManager');
      throw error;
    }
  }

  public dispose(): void {
    for (const client of this.clients.values()) {
      client.close();
    }
    this.clients.clear();
    this.endpoints.clear();
    this.server?.close();
    Logger.info('OSC manager disposed', 'OSCManager');
  }
}
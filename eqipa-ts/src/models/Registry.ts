import fs from 'fs';
import path from 'path';
import { Logger } from '../utils/Logger';

export class Registry<T extends object> {
  private readonly folderPath: string;
  private readonly cache: Map<string, T> = new Map();

  constructor(name: string) {
    this.folderPath = path.join('registry', name);
    this.ensureDirectoryExists();
    this.loadAllFiles();
    this.ensureConsistency();
  }

  private ensureDirectoryExists(): void {
    if (!fs.existsSync(this.folderPath)) {
      fs.mkdirSync(this.folderPath, { recursive: true });
    }
  }

  private loadAllFiles(): void {
    const files = fs.readdirSync(this.folderPath).filter(file => file.endsWith('.json'));
    
    for (const file of files) {
      const id = path.basename(file, '.json');
      try {
        const content = fs.readFileSync(path.join(this.folderPath, file), 'utf-8');
        const data = JSON.parse(content) as T;
        this.cache.set(id, data);
      } catch (error) {
        Logger.warn(`Failed to load entity ${id}: ${error}. Regenerating with default values.`, 'Registry');
        const defaultItem = {} as T;
        this.save(id, defaultItem);
      }
    }
  }

  private ensureConsistency(): void {
    for (const [id, item] of this.cache.entries()) {
      try {
        const serialized = JSON.stringify(item);
        const deserialized = JSON.parse(serialized) as T;

        const needsUpdate = this.compareObjects(item, deserialized);
        if (needsUpdate) {
          this.save(id, item);
        }
      } catch (error) {
        Logger.warn(`Consistency check failed for entity ${id}: ${error}. Regenerating with default values.`, 'Registry');
        const defaultItem = {} as T;
        this.save(id, defaultItem);
      }
    }
  }

  private compareObjects(original: T, deserialized: T): boolean {
    let needsUpdate = false;
    for (const key in deserialized) {
      if (original[key as keyof T] === undefined && deserialized[key as keyof T] !== undefined) {
        (original as any)[key] = deserialized[key as keyof T];
        needsUpdate = true;
      }
    }
    return needsUpdate;
  }

  public save(id: string, item: T): void {
    const filePath = path.join(this.folderPath, `${id}.json`);
    fs.writeFileSync(filePath, JSON.stringify(item, null, 2));
    this.cache.set(id, item);
    Logger.info(`Saved entity ${id}`, 'Registry');
  }

  public update(id: string, updateAction: (item: T) => void): void {
    const item = this.cache.get(id);
    if (item) {
      updateAction(item);
      this.save(id, item);
      Logger.info(`Updated entity ${id}`, 'Registry');
    } else {
      Logger.warn(`Entity ${id} not found for update.`, 'Registry');
    }
  }

  public updateAll(updateAction: (item: T) => void): void {
    for (const [id, item] of this.cache.entries()) {
      updateAction(item);
      this.save(id, item);
    }
    Logger.info('Updated all entities.', 'Registry');
  }

  public delete(id: string): void {
    const filePath = path.join(this.folderPath, `${id}.json`);
    if (fs.existsSync(filePath)) {
      fs.unlinkSync(filePath);
      this.cache.delete(id);
      Logger.info(`Deleted entity ${id}`, 'Registry');
    } else {
      Logger.warn(`Entity ${id} not found for deletion.`, 'Registry');
    }
  }

  public has(id: string): boolean {
    return this.cache.has(id);
  }

  public get(id: string): T | null {
    return this.cache.get(id) || null;
  }

  public getAll(): T[] {
    return Array.from(this.cache.values());
  }

  public getCountByCondition(condition: (item: T) => boolean): number {
    return Array.from(this.cache.values()).filter(condition).length;
  }

  public getAllFiltered(condition: (item: T) => boolean): T[] {
    return Array.from(this.cache.values()).filter(condition);
  }
}
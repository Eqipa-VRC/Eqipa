export interface IManager {
  isInitialized: boolean;
  initialize(): void | Promise<void>;
  update(): void | Promise<void>;
  shutdown(): void | Promise<void>;
  dispose(): void;
}

export interface IAsyncManager extends IManager {
  initializeAsync(): Promise<void>;
  updateAsync(): Promise<void>;
}

export class ManagerError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'ManagerError';
  }
}

export class ManagerNotFoundException extends ManagerError {
  constructor(type: string) {
    super(`Manager of type ${type} not found`);
    this.name = 'ManagerNotFoundException';
  }
}

export class ManagerAlreadyInitializedException extends ManagerError {
  constructor(type: string) {
    super(`Manager of type ${type} is already initialized`);
    this.name = 'ManagerAlreadyInitializedException';
  }
}

export class ManagerAlreadyRegisteredException extends ManagerError {
  constructor(type: string) {
    super(`Manager of type ${type} is already registered`);
    this.name = 'ManagerAlreadyRegisteredException';
  }
}
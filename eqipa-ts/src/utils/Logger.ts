import winston from 'winston';

export enum LogLevel {
  Info = 'info',
  Step = 'step',
  Warn = 'warn',
  Error = 'error',
  Debug = 'debug',
}

export class Logger {
  private static logger = winston.createLogger({
    format: winston.format.combine(
      winston.format.timestamp(),
      winston.format.colorize(),
      winston.format.printf(({ timestamp, level, message, namespace }) => {
        return `${timestamp} :: [${level}]${namespace ? ` [${namespace}]` : ''} ${message}`;
      })
    ),
    transports: [
      new winston.transports.Console(),
      new winston.transports.File({ filename: 'logs/error.log', level: 'error' }),
      new winston.transports.File({ filename: 'logs/combined.log' })
    ]
  });

  static log(level: LogLevel, message: string, namespace?: string): void {
    this.logger.log({
      level,
      message,
      namespace
    });
  }

  static info(message: string, namespace?: string): void {
    this.log(LogLevel.Info, message, namespace);
  }

  static warn(message: string, namespace?: string): void {
    this.log(LogLevel.Warn, message, namespace);
  }

  static error(message: string, namespace?: string): void {
    this.log(LogLevel.Error, message, namespace);
  }

  static debug(message: string, namespace?: string): void {
    this.log(LogLevel.Debug, message, namespace);
  }

  static step(message: string, namespace?: string): void {
    this.log(LogLevel.Step, message, namespace);
  }
}
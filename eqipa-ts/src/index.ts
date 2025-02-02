import { config } from 'dotenv';
import { EqipaBot } from './core/EqipaBot';

config();

async function main() {
  try {
    const bot = EqipaBot.getInstance();
    await bot.initialize();
  } catch (error) {
    console.error('Failed to start bot:', error);
    process.exit(1);
  }
}

main();
using Discord;
using Discord.WebSocket;
using Eqipa.Util;

namespace Eqipa.Discord;

public class DiscordBot : Singleton<DiscordBot>, IDisposable
{
  private DiscordSocketClient _client;

  private readonly DiscordSocketConfig _config = new()
  {
    LogLevel = LogSeverity.Info,
    MessageCacheSize = 1000
  };

  private bool _isInitialized = false;
  private bool _isDisposed = false;

  public bool IsInitialized => _isInitialized;

  public DiscordBot()
  {
    Logger.Log(LogLevel.Step, "Initializing Discord Bot...");
    Initialize();
  }

  private void Initialize()
  {
    ThrowIfDisposed();

    try
    {
      Logger.Log(LogLevel.Info, "Setting up Discord client...");
      _client = new(_config);

      _client.Log += OnLog;
      _client.Ready += OnReady;

      _isInitialized = true;
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Failed to initialize Discord client: {ex.Message}");
      throw;
    }
  }

  public async Task StartAsync(string token)
  {
    ThrowIfDisposed();

    if (!_isInitialized)
    {
      throw new InvalidOperationException("DiscordBot must be initialized before starting.");
    }

    try
    {
      Logger.Log(LogLevel.Info, "Connecting to Discord...");
      await _client.LoginAsync(TokenType.Bot, token);
      await _client.StartAsync();

      Logger.Log(LogLevel.Info, "Bot connected successfully!");
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Failed to start Discord bot: {ex.Message}");
      throw;
    }
  }

  public async Task StopAsync()
  {
    ThrowIfDisposed();

    if (!_isInitialized)
    {
      throw new InvalidOperationException("DiscordBot is not initialized.");
    }

    Logger.Log(LogLevel.Info, "Stopping Discord bot...");
    await _client.LogoutAsync();
    await _client.StopAsync();

    Logger.Log(LogLevel.Info, "Bot stopped successfully.");
  }

  private Task OnLog(LogMessage message)
  {
    Logger.Log(ConvertLogLevel(message.Severity), message.ToString());
    return Task.CompletedTask;
  }

  private Task OnReady()
  {
    Logger.Log(LogLevel.Info, "Bot is ready and connected to Discord.");
    return Task.CompletedTask;
  }

  private LogLevel ConvertLogLevel(LogSeverity severity) =>
      severity switch
      {
        LogSeverity.Critical => LogLevel.Error,
        LogSeverity.Error => LogLevel.Error,
        LogSeverity.Warning => LogLevel.Warn,
        LogSeverity.Info => LogLevel.Info,
        LogSeverity.Verbose => LogLevel.Debug,
        LogSeverity.Debug => LogLevel.Debug,
        _ => LogLevel.Info
      };

  protected virtual void Dispose(bool disposing)
  {
    if (_isDisposed)
      return;

    if (disposing)
    {
      _client?.Dispose();
    }

    _isDisposed = true;
    _isInitialized = false;
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  private void ThrowIfDisposed()
  {
    if (_isDisposed)
    {
      throw new ObjectDisposedException(nameof(DiscordBot));
    }
  }

  ~DiscordBot()
  {
    Dispose(false);
  }
}

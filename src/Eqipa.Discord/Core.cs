using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Eqipa.Util;
using Eqipa.Discord.Modules;
using Microsoft.Extensions.DependencyInjection;
using Timer = System.Threading.Timer;
using Eqipa.VRChat.Manager;

namespace Eqipa.Discord;

public class DiscordBot : Singleton<DiscordBot>, IDisposable
{
  private CommandModule? _commandModule;
  private IServiceProvider? _serviceProvider;
  private DiscordSocketClient? _client;
  private Timer? _presenceUpdateTimer;

  private bool _isInitialized = false;
  private bool _isDisposed = false;

  private readonly List<(string Message, ActivityType Type)> _richPresenceMessages = new()
  {
    ("{online}/{maxpi} dostępnych osób na instancjach", ActivityType.Watching),
  };

  public bool IsInitialized => _isInitialized;

  public DiscordBot()
  {
    Logger.Log(LogLevel.Step, "Initializing Discord Bot...");
    Initialize();
  }

  private void Initialize()
  {
    ThrowIfDisposed();

    _client = new DiscordSocketClient(new DiscordSocketConfig
    {
      LogLevel = LogSeverity.Info,
      GatewayIntents = GatewayIntents.All,
      MessageCacheSize = 1000,
      AlwaysDownloadUsers = true,
    });

    _client.Log += OnLog;
    _client.Ready += OnReady;

    _serviceProvider = ConfigureServices();

    _commandModule = new CommandModule(_client, _serviceProvider);
    _isInitialized = true;
  }

  public async Task StartAsync(string token)
  {
    ThrowIfDisposed();

    if (!_isInitialized)
    {
      throw new InvalidOperationException("DiscordBot must be initialized before starting.");
    }

    // Initialize commands and interactions
    await _commandModule!.InitializeAsync();

    Logger.Log(LogLevel.Info, "Connecting to Discord...");
    await _client!.LoginAsync(TokenType.Bot, token);
    await _client.StartAsync();

    Logger.Log(LogLevel.Info, "Bot connected successfully!");
  }

  private async Task OnReady()
  {
    _presenceUpdateTimer = new Timer(UpdateRichPresenceRandomly, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    UpdateRichPresenceRandomly(null);
    Logger.Log(LogLevel.Info, "Bot is ready and connected to Discord.");

    try
    {
      await _commandModule!.RegisterCommandsGloballyAsync();
    }
    catch (Exception e)
    {
      Logger.Log(LogLevel.Error, $"{e.Message}\n{e.StackTrace}");
    }
  }

  private IServiceProvider ConfigureServices()
  {
    var services = new ServiceCollection();

    services.AddSingleton(_client!);
    services.AddSingleton<CommandModule>();
    services.AddSingleton<CommandService>();
    services.AddSingleton<InteractionService>();

    return services.BuildServiceProvider();
  }

  private Task OnLog(LogMessage message)
  {
    Logger.Log(ConvertLogLevel(message.Severity), message.Message);
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

  public async Task SetRichPresenceAsync(string details)
  {
    ThrowIfDisposed();

    if (!_isInitialized)
    {
      throw new InvalidOperationException("DiscordBot must be initialized before setting Rich Presence.");
    }

    try
    {
      await _client!.SetCustomStatusAsync(details);
      Logger.Log(LogLevel.Info, $"Updated Rich Presence to '{details}'");
    }
    catch (Exception e)
    {
      Logger.Log(LogLevel.Warn, $"Cannot set presence: {e.Message}");
    }
  }

  private async void UpdateRichPresenceRandomly(object? state)
  {
    ThrowIfDisposed();

    if (!_isInitialized || _client == null)
    {
      return;
    }

    var manager = Program.VRChatBot?.GetManagerOrDefault<VRCManager>();
    int onlineUsers = manager?.GroupUsers ?? 0;
    int maxUsers = manager?.GroupMaxUsers ?? 0;

    var randomIndex = Random.Shared.Next(_richPresenceMessages.Count);
    var (messageTemplate, activityType) = _richPresenceMessages[randomIndex];

    var updatedMessage = messageTemplate
        .Replace("{online}", onlineUsers.ToString())
        .Replace("{maxpi}", maxUsers.ToString());

    await SetRichPresenceAsync(updatedMessage);

    Logger.Log(LogLevel.Info, $"Updated Rich Presence to: {updatedMessage} ({activityType})");
  }
}

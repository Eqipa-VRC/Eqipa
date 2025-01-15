using Eqipa.Model;
using Eqipa.Util;
using Eqipa.VRChat.Model;

namespace Eqipa.VRChat.Manager;

public class AvatarManager : Registry<Avatar>, IManager
{
  private readonly VRChatBot _vrchatBot;

  private bool _isInitialized = false;
  private bool _isDisposed = false;

  private VRCManager? _vrcManager;
  private DiscordWebhookManager? _discordWebhookManager;

  public bool IsInitialized => _isInitialized;

  public AvatarManager(VRChatBot core) : base("avatars")
  {
    _vrchatBot = core ?? throw new ArgumentNullException(nameof(core));

    if (_vrchatBot.HasManager<VRCManager>()) _vrcManager = _vrchatBot.GetManagerOrDefault<VRCManager>();
    if (_vrchatBot.HasManager<DiscordWebhookManager>()) _discordWebhookManager = _vrchatBot.GetManagerOrDefault<DiscordWebhookManager>();
  }

  public void Initialize()
  {
    if (_isInitialized)
      throw new ManagerAlreadyInitializedException(GetType());

    _vrcManager!.LogReader!.OnProcessed += Process;
    _isInitialized = true;
  }

  public void Shutdown()
  {
    if (!_isInitialized) return;

    _isInitialized = false;
    Logger.Log(LogLevel.Info, "AvatarManager shutdown");
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (_isDisposed) return;

    if (disposing)
    {
      Shutdown();
    }

    _isDisposed = true;
  }

  ~AvatarManager()
  {
    Dispose(false);
  }

  private void Process(object? sender, ProcessedLogEventArgs data)
  {

  }
}
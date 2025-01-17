using Eqipa.Util;
using Eqipa.Model;
using Eqipa.VRChat.Module;
using static Eqipa.VRChat.Addresses;

namespace Eqipa.VRChat.Manager;

public class ChatboxManager : IManager
{
  private readonly VRCOsc? _osc;
  private readonly VRChatBot _vrchatBot;

  private bool _isDisposed = false;
  private bool _isInitialized = false;
  private DateTime _lastUpdate = DateTime.MinValue;
  private VRCManager? _vrcManager;

  public bool IsInitialized => _isInitialized;

  public ChatboxManager(VRChatBot core)
  {
    _vrchatBot = core ?? throw new ArgumentNullException(nameof(core));
    _osc = new(_vrchatBot);

    if (_vrchatBot.HasManager<VRCManager>()) _vrcManager = _vrchatBot.GetManagerOrDefault<VRCManager>();
  }

  public void Initialize()
  {
    if (_isInitialized)
      throw new ManagerAlreadyInitializedException(GetType());

    _ = UpdateMessage();

    _isInitialized = true;
  }

  public void Shutdown()
  {
    if (!_isInitialized) return;

    _isInitialized = false;
    Logger.Log(LogLevel.Info, "ChatboxManager shutdown");
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

  ~ChatboxManager()
  {
    Dispose(false);
  }

  private readonly List<List<string>> _messages = new()
  {
    new()
    {
      "Witaj na Eqipa - nowy wymiar rozmów i rozrywki!",
      "Zapraszamy do zapoznania się z regulaminem na tablicy wprost przed tobą.",
    },

    new()
    {
      "Zapraszamy na nasz serwer Discord: discord.gg/eqipa",
      "Znajdziesz tam ludzi z pozytywnym nastawieniem i chęcią do rozmowy.",
    },

    new()
    {
      "Aktualna ilość dostępnych użytkowników: {online} na {maxpi} ({percent}%)\n",
      "Aktualna ilość dostępnych administratorów: {admins}",
    },

    new()
    {
      "Masz problem lub chcesz zgłosić skargę? Napisz ticket na naszym serwerze Discord!",
      "discord.gg/eqipa",
    }
  };

  private int _messageIndex = 0;
  private string _currentMessage = string.Empty;

  private async Task UpdateMessage()
  {
    Dictionary<string, string> placeholders = new()
    {
      { "{maxpi}", _vrcManager!.GroupMaxUsers.ToString() },
      { "{admins}", _vrcManager.GroupAdmins.ToString() },
      { "{online}", _vrcManager.GroupUsers.ToString() },
      { "{percent}", Math.Floor(PercentageConverter.Convert(_vrcManager.GroupUsers, 0, _vrcManager!.GroupMaxUsers)).ToString() }
    };

    _messageIndex = _messageIndex >= _messages.Count - 1 ? 0 : _messageIndex + 1;

    string text = string.Join("\n", _messages[_messageIndex]);
    foreach (var (key, value) in placeholders)
      text = text.Replace(key, value);

    _currentMessage = text;

    await _osc!.Send(GetOscAddress(VRCOscAddresses.SEND_CHATBOX_MESSAGE), _currentMessage, true);
  }

  public void Update()
  {
    if (!_isInitialized || _isDisposed)
      return;

    var now = DateTime.UtcNow;
    if ((now - _lastUpdate).TotalSeconds >= 15)
    {
      _lastUpdate = now;
      _ = UpdateMessage();
    }
  }
}
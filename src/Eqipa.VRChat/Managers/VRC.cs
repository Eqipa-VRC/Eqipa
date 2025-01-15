using System.Net;
using VRChat.API.Api;
using VRChat.API.Model;
using VRChat.API.Client;
using Eqipa.Util;
using Eqipa.Model;
using static Eqipa.Constants;
using Logger = Eqipa.Util.Logger;
using Configuration = VRChat.API.Client.Configuration;
using Eqipa.VRChat.Module;
using UserStatus = Eqipa.Model.UserStatus;
using Eqipa.VRChat.Dialogs;
using static Eqipa.VRChat.Addresses;

namespace Eqipa.VRChat.Manager;

public class VRCManager : ApiClient, IAsyncManager
{
  private readonly VRChatBot _vrchatBot;
  private protected InstancesApi _vrcInstances;
  public Configuration _vrcConfig;
  private protected AuthenticationApi _vrcAuth;

  private List<string>? _oscMessage;
  private string? _worldId;
  private string? _groupId;
  private bool _isLogged;
  private bool _isDisposed;
  private int _groupUsers = 0;
  private int _groupAdmins = 0;
  private int _groupMaxUsers = 0;
  private UserManager? _userManager;

  private bool _isInitialized;
  private DateTime _lastOSCUpdate = DateTime.MinValue;
  private DateTime _lastInfoUpdate = DateTime.MinValue;

  public VRCOsc? OSC { get; private set; }
  public UsersApi? Users { get; private set; }
  public GroupsApi? Groups { get; private set; }
  public Group? Group { get; private set; }
  public FriendsApi? Friends { get; private set; }

  public CurrentUser? User { get; private set; }
  public VRCLogReader? LogReader { get; private set; }
  public CookieContainer? CookieContainer { get; private set; }

  public bool IsLogged => _isLogged;
  public int GroupAdmins => _groupAdmins;
  public int GroupUsers => _groupUsers;
  public int GroupMaxUsers => _groupMaxUsers;
  public bool IsInitialized => _isInitialized;

  public VRCManager(VRChatBot core) : base()
  {
    _vrchatBot = core ?? throw new ArgumentNullException(nameof(core));

    var (username, password) = (
      _vrchatBot.Config.GetConfig<string?>(c => c.VrchatUsername!) ?? throw new NullReferenceException("username null"),
      _vrchatBot.Config.GetConfig<string?>(c => c.VrchatPassword!) ?? throw new NullReferenceException("password null")
    );

    _vrcConfig = new()
    {
      Username = username,
      Password = password,
      UserAgent = USER_AGENT
    };
    _vrcAuth = new(this, this, _vrcConfig);
    _vrcInstances = new(this, this, _vrcConfig);

    Users = new(this, this, _vrcConfig);
    Groups = new(this, this, _vrcConfig);
    Friends = new(this, this, _vrcConfig);

    _worldId = _vrchatBot.Config.GetConfig<string?>(c => c.VrchatWorldId!);
    _groupId = _vrchatBot.Config.GetConfig<string?>(c => c.VrchatGroupId!);
    _oscMessage = _vrchatBot.Config.GetConfig<List<string>?>(c => c.VrchatOscMessage!);
  }

  public void Initialize()
  {
    if (_isInitialized)
      throw new ManagerAlreadyInitializedException(GetType());

    Login();

    OSC = new(_vrchatBot);
    LogReader = new(_vrchatBot);

    _isInitialized = true;
  }

  public async Task UpdateAsync()
  {
    if (!_isInitialized || _isDisposed)
      return;

    _groupUsers = _isLogged ? _userManager!.GetCountByCondition(u => u.Status is UserStatus.Online) : 0;
    _groupAdmins = _isLogged ? _userManager!.GetCountByCondition(u => u.Admin && u.Status is UserStatus.Online) : 0;

    if ((DateTime.UtcNow - _lastOSCUpdate).TotalSeconds >= 5)
    {
      _lastOSCUpdate = DateTime.UtcNow;

      try
      {
        await UpdateOsc();

      }
      catch (Exception ex)
      {
        Logger.Log(LogLevel.Error, $"Error in VRCManager UpdateAsync: {ex.Message}");
      }
    }

    if ((DateTime.UtcNow - _lastInfoUpdate).TotalSeconds >= 60)
    {
      _lastInfoUpdate = DateTime.UtcNow;

      if (!_isLogged)
        return;

      try
      {
        UpdateInfo();
      }
      catch (Exception ex)
      {
        Logger.Log(LogLevel.Error, $"Error in VRCManager UpdateAsync: {ex.Message}");
      }
    }
  }

  public void Shutdown()
  {
    if (!_isInitialized) return;

    _isInitialized = false;
    Logger.Log(LogLevel.Info, "VRCManager shutdown");
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

  ~VRCManager()
  {
    Dispose(false);
  }
  
  #region OSC
  private string ProcessOscMessage()
  {
    string text = string.Empty;
    foreach (string line in _oscMessage!)
    {
      if (string.IsNullOrEmpty(line))
      {
        text += "\n";
        continue;
      }

      text += line;
    }

    // TODO: automatic placeholder replacer
    text = StringUtil.Replace(text, "{maxpi}", _groupMaxUsers);
    text = StringUtil.Replace(text, "{online}", _groupUsers);
    text = StringUtil.Replace(text, "{admins}", _groupAdmins);
    text = StringUtil.Replace(text, "{percent}", Math.Floor(PercentageConverter.Convert(_groupUsers, 0, _groupMaxUsers)));

    return text;
  }

  private async Task UpdateOsc()
  {
    var message = ProcessOscMessage();

    await OSC!.Send(GetOscAddress(VRCOscAddresses.SEND_CHATBOX_MESSAGE), message, true);
  }
  #endregion

  private void Login()
  {
    try
    {
      var u = _vrcAuth.GetCurrentUserWithHttpInfo();
      var d = new TwoStepDialog(RequiresEmail2FA(u));

      if (d.ShowDialog() == DialogResult.OK)
      {
        var code = d.Controls.OfType<TextBox>().First().Text.Trim();
        if (code == TwoStepDialog._ENTER_CODE || string.IsNullOrWhiteSpace(code))
        {
          Logger.Log(LogLevel.Warn, "Verification code not provided, disabling API..");
          return;
        }

        Logger.Log(LogLevel.Step, $"Verification code ({code}) has been provided, verifying and enabling API..");

        dynamic result = RequiresEmail2FA(u)
          ? _vrcAuth.Verify2FAEmailCode(new TwoFactorEmailCode(code))
          : _vrcAuth.Verify2FA(new TwoFactorAuthCode(code));

        if (result.Verified && !_isLogged)
        {
          User = _vrcAuth.GetCurrentUser();
          Group = Groups!.GetGroup(_groupId, true);

          if (_vrchatBot.HasManager<UserManager>())
            _userManager = _vrchatBot.GetManagerOrDefault<UserManager>();

          UpdateInfo();
          _isLogged = true;
          Logger.Log(LogLevel.Info, $"API is working");
        }
      }
    }
    catch (ApiException e)
    {
      Logger.Log(LogLevel.Error, $"API login error: {e.Message}");
    }
  }

  private void UpdateInfo()
  {
    if (!_isLogged)
      return;

    var now = DateUtil.ToUnixTime(DateTime.Now);
    var instances = Groups!.GetGroupInstances(_groupId);

    _groupMaxUsers = instances.Sum(i => i.World.Capacity);
  }

  private bool RequiresEmail2FA(ApiResponse<CurrentUser> resp)
  {
    if (resp.RawContent.Contains("emailOtp"))
      return true;

    return false;
  }
}

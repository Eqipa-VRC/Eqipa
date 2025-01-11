using Eqipa.Util;
using Eqipa.Model;
using Eqipa.Module;

namespace Eqipa.Manager;

public class ReceiverManager : IManager
{
  private readonly Core _core;

  private bool _isInitialized;
  private bool _isDisposed;

  public GPTModule? GPT { get; private set; }
  public STTModule? STT { get; private set; }

  public bool IsInitialized => _isInitialized;

  public ReceiverManager(Core core)
  {
    _core = core ?? throw new ArgumentNullException(nameof(core));
  }

  public void Initialize()
  {
    if (_isInitialized)
      throw new ManagerAlreadyInitializedException(GetType());

    GPT = new GPTModule(_core);
    STT = new STTModule(_core);

    _isInitialized = true;
  }

  public void Shutdown()
  {
    if (!_isInitialized) return;

    GPT?.Dispose();
    STT?.Dispose();

    _isInitialized = false;
    Logger.Log(LogLevel.Info, "Receiver shutdown");
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

  ~ReceiverManager()
  {
    Dispose(false);
  }
}
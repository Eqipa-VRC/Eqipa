using Eqipa.Util;
using Eqipa.Model;

namespace Eqipa.Manager;

public class SpeechManager : IManager
{
  private readonly Core _core;

  private bool _isInitialized = false;
  private bool _isListening = false;
  private bool _isDisposed = false;

  private ReceiverManager? _receiverManager = null;
  // public GPTModule? GPT { get; private set; }
  // public SpeechModule? Speech { get; private set; }

  public bool IsInitialized => _isInitialized;
  public bool IsListening => _isListening;

  public SpeechManager(Core core)
  {
    _core = core ?? throw new ArgumentNullException(nameof(core));

    if (_core.HasManager<ReceiverManager>())
      _receiverManager = _core.GetManagerOrDefault<ReceiverManager>();
  }

  public void Initialize()
  {
    if (_isInitialized)
      throw new ManagerAlreadyInitializedException(GetType());

    // GPT = new GPTModule(_core);
    // Speech = new SpeechModule(_core);
    // await Speech.StartListeningAsync();

    _isInitialized = true;
  }

  public void Shutdown()
  {
    if (!_isInitialized) return;

    // GPT?.Dispose();
    // Speech?.Dispose();

    _isInitialized = false;
    Logger.Log(LogLevel.Info, "Speech shutdown");
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

  ~SpeechManager()
  {
    Dispose(false);
  }
}
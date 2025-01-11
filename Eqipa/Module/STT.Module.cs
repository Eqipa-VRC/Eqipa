using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Eqipa.Util;
using NAudio.CoreAudioApi;
using System.Collections.Concurrent;
using static Eqipa.Addresses;
using Eqipa.Model;

namespace Eqipa.Module;

public class STTModule : IDisposable
{
  private readonly Core _core;
  private SpeechRecognizer? _recognizer;
  private SpeechConfig? _config;
  private AudioConfig? _audioConfig;
  private RecognizeLang _currentLanguage;
  private readonly ConcurrentQueue<string> _recognitionQueue = new();
  private readonly CancellationTokenSource _cancellationTokenSource = new();
  private readonly System.Timers.Timer _healthCheckTimer;
  private readonly object _reconnectLock = new();
  private bool _isDisposed;
  private bool _needsReconnect;
  private DateTime _lastRecognitionTime = DateTime.Now;
  private int _reconnectAttempts;

  // Quota management
  private const int QUOTA_PAUSE_MS = 60000; // 1 minute pause
  private DateTime _quotaResetTime = DateTime.MinValue;
  private System.Timers.Timer? _quotaTimer;
  private TaskCompletionSource<bool>? _quotaResetTask;
  private bool _isInQuotaPause;

  // Constants
  private const int HEALTH_CHECK_INTERVAL_MS = 30000;
  private const int RECOGNITION_TIMEOUT_MS = 60000;
  private const int RECONNECT_DELAY_MS = 5000;
  private const int MAX_RECONNECT_ATTEMPTS = 5;

  public bool IsListening { get; private set; }
  public event EventHandler<string>? OnSpeechRecognized;
  public event EventHandler<ConnectionStatus>? OnConnectionStatusChanged;

  public enum ConnectionStatus
  {
    Connected,
    Disconnected,
    Reconnecting,
    Failed,
    QuotaExceeded
  }

  public STTModule(Core core)
  {
    _core = core ?? throw new ArgumentNullException(nameof(core));
    _healthCheckTimer = new System.Timers.Timer(HEALTH_CHECK_INTERVAL_MS);
    _healthCheckTimer.Elapsed += async (s, e) => await CheckConnectionHealthAsync();
    Initialize();
    Logger.Log(LogLevel.Info, "Speech To Text module initialized");
  }

  private void Initialize()
  {
    StartHealthCheck();
    
    try
    {
      if (_isInQuotaPause)
      {
        Logger.Log(LogLevel.Info, "Skipping initialization during quota pause");
        return;
      }

      _currentLanguage = _core.Config.GetConfig<RecognizeLang>(config => config.SpeechRecognizerLanguage);
      var token = _core.Config.GetConfig<string?>(config => config.SpeechRecognizerToken!);
      var region = _core.Config.GetConfig<string?>(config => config.SpeechRecognizerRegion!);

      if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(region))
      {
        throw new ArgumentNullException("Speech to text module configuration is missing required values");
      }

      InitializeSpeechComponents(token, region);
      SetupRecognizer();
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Failed to initialize Speech to text module: {ex.Message}");
      throw;
    }
  }

  private void InitializeSpeechComponents(string token, string region)
  {
    _config = SpeechConfig.FromSubscription(token, region);
    _config.SetProfanity(ProfanityOption.Raw);
    _config.SpeechRecognitionLanguage = GetRecognizeLang(_currentLanguage);

    // Connection settings
    _config.SetProperty("SpeechServiceConnection_ReconnectOnError", "false");
    _config.SetProperty("SpeechServiceConnection_InitialSilenceTimeoutMs", "15000");
    _config.SetProperty("SpeechServiceConnection_EndSilenceTimeoutMs", "15000");
    _config.SetProperty("SpeechServiceConnection_MaxRetryCount", "0");

    // Noise suppression settings
    _config.SetProperty("SpeechServiceConnection_NoiseSuppression", "true");
    _config.SetProperty("SpeechServiceConnection_NoiseSuppressionLevel", "High");

    // Speech detection settings
    _config.SetProperty("SpeechServiceConnection_VadMode", "Low");
    _config.SetProperty("SpeechServiceConnection_AutoDetectSourceLanguage", "false");
    _config.SetProperty("SpeechServiceConnection_TransmitLengthBeforeThreshold", "100");

    // Recognition thresholds
    _config.SetProperty("SpeechServiceConnection_SpeechDetectionThreshold", "0.5");
    _config.SetProperty("SpeechServiceConnection_SpeechSegmentationSilenceTimeoutMs", "300");

    _audioConfig = GetAudioConfig();
  }

  private void SetupRecognizer()
  {
    _recognizer?.Dispose();
    _recognizer = new SpeechRecognizer(_config, _audioConfig);

    _recognizer.Recognized += OnRecognized;
    _recognizer.Canceled += OnCanceled;
    _recognizer.SessionStarted += (s, e) =>
    {
      _reconnectAttempts = 0;
      OnConnectionStatusChanged?.Invoke(this, ConnectionStatus.Connected);
      Logger.Log(LogLevel.Info, "Speech to text recognition session started");
    };
    _recognizer.SessionStopped += (s, e) =>
    {
      if (!_isDisposed && IsListening && !_isInQuotaPause)
      {
        _needsReconnect = true;
      }
    };
  }

  private AudioConfig GetAudioConfig()
  {
    try
    {
      var enumerator = new MMDeviceEnumerator();
      var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
      var targetDevice = devices.FirstOrDefault(d =>
          d.FriendlyName.Contains("Virtual Cable", StringComparison.OrdinalIgnoreCase));

      return targetDevice != null
        ? AudioConfig.FromMicrophoneInput(targetDevice.ID)
        : AudioConfig.FromDefaultMicrophoneInput();
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Failed to initialize audio config: {ex.Message}");
      throw;
    }
  }

  private void StartHealthCheck()
  {
    _healthCheckTimer.Start();
  }

  private async Task HandleQuotaExceeded()
  {
    lock (_reconnectLock)
    {
      if (_isInQuotaPause)
      {
        return; // Already handling quota pause
      }
      _isInQuotaPause = true;
    }

    try
    {
      OnConnectionStatusChanged?.Invoke(this, ConnectionStatus.QuotaExceeded);
      Logger.Log(LogLevel.Warn, "Quota exceeded, pausing speech to text recognition for 1 minute");

      await StopListeningAsync();

      _quotaResetTask = new TaskCompletionSource<bool>();
      _quotaTimer = new System.Timers.Timer(QUOTA_PAUSE_MS);

      _quotaTimer.Elapsed += async (s, e) =>
      {
        _quotaTimer.Stop();
        _quotaTimer.Dispose();
        _isInQuotaPause = false;
        _quotaResetTask?.TrySetResult(true);

        try
        {
          Initialize();
          await StartListeningAsync();
          OnConnectionStatusChanged?.Invoke(this, ConnectionStatus.Connected);
        }
        catch (Exception ex)
        {
          Logger.Log(LogLevel.Error, $"Failed to resume after quota pause: {ex.Message}");
        }
      };

      _quotaTimer.Start();
      _quotaResetTime = DateTime.Now.AddMilliseconds(QUOTA_PAUSE_MS);

      // Wait for the quota timer to complete
      await _quotaResetTask.Task;
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Error in quota handling: {ex.Message}");
      _isInQuotaPause = false;
    }
  }

  private async Task CheckConnectionHealthAsync()
  {
    if (!IsListening || _isDisposed || _isInQuotaPause) return;

    var timeSinceLastRecognition = DateTime.Now - _lastRecognitionTime;
    if (timeSinceLastRecognition.TotalMilliseconds > RECOGNITION_TIMEOUT_MS)
    {
      Logger.Log(LogLevel.Warn, "No speech recognition events detected for a while, attempting reconnection");
      _needsReconnect = true;
      await ReconnectAsync();
    }
  }

  private async Task ReconnectAsync()
  {
    if (_isInQuotaPause)
    {
      Logger.Log(LogLevel.Debug, "Skipping reconnection during quota pause");
      return;
    }

    if (DateTime.Now < _quotaResetTime)
    {
      Logger.Log(LogLevel.Debug, "Still in quota pause period, skipping reconnection");
      return;
    }

    lock (_reconnectLock)
    {
      if (!_needsReconnect || _isDisposed) return;
      if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
      {
        OnConnectionStatusChanged?.Invoke(this, ConnectionStatus.Failed);
        Logger.Log(LogLevel.Error, "Max reconnection attempts reached");
        return;
      }
    }

    try
    {
      _reconnectAttempts++;
      OnConnectionStatusChanged?.Invoke(this, ConnectionStatus.Reconnecting);
      Logger.Log(LogLevel.Info, $"Attempting reconnection (attempt {_reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS})");

      await StopListeningAsync();
      await Task.Delay(RECONNECT_DELAY_MS);
      Initialize();
      await StartListeningAsync();

      _needsReconnect = false;
      OnConnectionStatusChanged?.Invoke(this, ConnectionStatus.Connected);
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Reconnection attempt failed: {ex.Message}");
      if (_reconnectAttempts < MAX_RECONNECT_ATTEMPTS)
      {
        await Task.Delay(RECONNECT_DELAY_MS);
        await ReconnectAsync();
      }
    }
  }

  private void OnRecognized(object? sender, SpeechRecognitionEventArgs e)
  {
    _lastRecognitionTime = DateTime.Now;

    if (e.Result.Reason == ResultReason.RecognizedSpeech)
    {
      ProcessRecognizedSpeech(e.Result.Text.Trim());
    }
  }

  private void ProcessRecognizedSpeech(string recognizedText)
  {
    try
    {
      _recognitionQueue.Enqueue(recognizedText);
      Logger.Log(LogLevel.Debug, $"Recognized: {recognizedText}");
      OnSpeechRecognized?.Invoke(this, recognizedText);
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Failed to process recognized speech: {ex.Message}");
    }
  }

  private async void OnCanceled(object? sender, SpeechRecognitionCanceledEventArgs e)
  {
    if (e.Reason == CancellationReason.Error)
    {
      if (e.ErrorDetails.Contains("Quota exceeded"))
      {
        Logger.Log(LogLevel.Error, $"Speech recognition canceled due to quota: {e.ErrorDetails}");
        await HandleQuotaExceeded();
      }
      else
      {
        Logger.Log(LogLevel.Error, $"Speech recognition canceled: {e.ErrorCode} - {e.ErrorDetails}");
        _needsReconnect = true;
        await ReconnectAsync();
      }
    }
  }

  public async Task StartListeningAsync()
  {
    if (!IsListening && !_isDisposed && _recognizer != null && !_isInQuotaPause)
    {
      try
      {
        IsListening = true;
        await _recognizer.StartContinuousRecognitionAsync();
        _lastRecognitionTime = DateTime.Now;
        Logger.Log(LogLevel.Info, "Speech recognition started");
      }
      catch (Exception ex)
      {
        IsListening = false;
        Logger.Log(LogLevel.Error, $"Failed to start listening: {ex.Message}");
        throw;
      }
    }
  }

  public async Task StopListeningAsync()
  {
    if (IsListening && !_isDisposed && _recognizer != null)
    {
      try
      {
        IsListening = false;
        await _recognizer.StopContinuousRecognitionAsync();
        Logger.Log(LogLevel.Info, "Speech recognition stopped");
      }
      catch (Exception ex)
      {
        Logger.Log(LogLevel.Error, $"Error stopping speech recognition: {ex.Message}");
        throw;
      }
    }
  }

  public async Task SetLanguageAsync(RecognizeLang language)
  {
    if (_currentLanguage != language && !_isInQuotaPause)
    {
      try
      {
        _currentLanguage = language;
        await StopListeningAsync();
        Initialize();
        if (IsListening)
        {
          await StartListeningAsync();
        }
        Logger.Log(LogLevel.Info, $"Speech recognition language changed to {language}");
      }
      catch (Exception ex)
      {
        Logger.Log(LogLevel.Error, $"Failed to change language: {ex.Message}");
        throw;
      }
    }
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (!_isDisposed)
    {
      if (disposing)
      {
        _healthCheckTimer.Stop();
        _healthCheckTimer.Dispose();
        _cancellationTokenSource.Cancel();
        _quotaTimer?.Stop();
        _quotaTimer?.Dispose();
        _recognizer?.Dispose();
        _audioConfig?.Dispose();
      }
      _isDisposed = true;
    }
  }

  ~STTModule()
  {
    Dispose(false);
  }
}
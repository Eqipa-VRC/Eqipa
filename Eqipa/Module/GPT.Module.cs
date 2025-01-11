using System.Text;
using Newtonsoft.Json;
using Eqipa.Util;
using Eqipa.Model;
using static Eqipa.Addresses;

namespace Eqipa.Module;

/// <summary>
/// GPT module for generating responses and other text-based tasks
/// </summary>
public class GPTModule : IGPTModule, IDisposable
{
  private System.Timers.Timer? _healthCheckTimer;
  private readonly CancellationTokenSource _cancellationTokenSource = new();
  private Task? _healthCheckTask;

  private bool _isDisposed;
  private DateTime _lastSuccessfulRequest = DateTime.Now;
  private const int HEALTH_CHECK_INTERVAL_MS = 60000; // 1 minute

  public GPTModel model = GPTModel.UNCENSORED;
  private readonly string? _api = string.Empty;
  private readonly HttpClient _http = new();
  private readonly Core? _core = null;

  // <summary>
  // Construct the GPT module
  // </summary>
  public GPTModule(Core core)
  {
    _core = core ?? throw new ArgumentNullException(nameof(core));
    _api = _core.Config.GetConfig<string?>(c => c.GptApi!) ?? throw new ArgumentNullException(nameof(_api));

    var _token = _core.Config.GetConfig<string?>(c => c.GptToken!);
    if (!string.IsNullOrEmpty(_token))
      _http.DefaultRequestHeaders.Add("g4f-api-key", _token);

    Initialize();
    Logger.Log(LogLevel.Info, $"GPT module initialized ({_api})");
  }

  private void Initialize()
  {
    InitializeHealthCheck();
  }

  private void InitializeHealthCheck()
  {
    _healthCheckTimer = new System.Timers.Timer(HEALTH_CHECK_INTERVAL_MS);
    _healthCheckTimer.Elapsed += OnHealthCheckTimer;
    _healthCheckTimer.Start();
  }

  private async void OnHealthCheckTimer(object? sender, System.Timers.ElapsedEventArgs e)
  {
    if (_healthCheckTask != null && !_healthCheckTask.IsCompleted)
      return;

    _healthCheckTask = CheckConnectionHealthAsync();
    try
    {
      await _healthCheckTask;
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Health check failed: {ex.Message}");
    }
  }

  private async Task CheckConnectionHealthAsync()
  {
    if (_isDisposed) return;

    var timeSinceLastSuccess = DateTime.Now - _lastSuccessfulRequest;
    if (timeSinceLastSuccess.TotalMilliseconds > HEALTH_CHECK_INTERVAL_MS)
    {
      Logger.Log(LogLevel.Warn, "No successful requests detected for a while, checking connection...");
      try
      {
        await GenerateResponse("test connection");
        Logger.Log(LogLevel.Info, $"Test request successful");
      }
      catch (Exception ex)
      {
        Logger.Log(LogLevel.Error, $"Health check request failed: {ex.Message}");
      }
    }
  }

  private protected object? GetBody(string prompt) => new
  {
    model = GetGptModel(model),
    messages = new[] { new { role = "user", content = prompt } }
  };

  public GPTModel SetModel(GPTModel model) => this.model = model;

  public async Task<string> GenerateResponse(string? prompt)
  {
    if (string.IsNullOrEmpty(prompt))
      throw new ArgumentNullException(nameof(prompt));

    var _body = GetBody(prompt);
    var _request = new HttpRequestMessage(HttpMethod.Post, $"{_api}/chat/completions")
    {
      Content = new StringContent(
        JsonConvert.SerializeObject(_body),
        Encoding.UTF8,
        "application/json"
      )
    };

    try
    {
      var _response = await _http.SendAsync(_request);
      _response.EnsureSuccessStatusCode();

      var _result = await _response.Content.ReadAsStringAsync();
      var _parsed = JsonConvert.DeserializeObject<GPTApiResponse>(_result);
      _lastSuccessfulRequest = DateTime.Now;

      return _parsed?.Choices?.FirstOrDefault()?.Message?.Content ?? throw new InvalidOperationException("No response content received");
    }
    catch (Exception ex) when (ex is HttpRequestException or JsonReaderException)
    {
      Logger.Log(LogLevel.Error, $"Response generation failed: {ex.Message}");
      throw new InvalidOperationException("Response generation failed", ex);
    }
  }

  public async IAsyncEnumerable<string> StreamResponse(string? prompt)
  {
    if (string.IsNullOrEmpty(prompt))
      throw new ArgumentNullException(nameof(prompt));

    var _body = GetBody(prompt);
    var _request = new HttpRequestMessage(HttpMethod.Post, $"{_api}/chat/completions")
    {
      Content = new StringContent(
        JsonConvert.SerializeObject(_body),
        Encoding.UTF8,
        "application/json"
      )
    };

    var _response = await _http.SendAsync(_request, HttpCompletionOption.ResponseHeadersRead);

    try
    {
      _response.EnsureSuccessStatusCode();
    }
    catch (HttpRequestException ex)
    {
      Logger.Log(LogLevel.Error, $"Response streaming failed: {ex.Message}");
      throw new InvalidOperationException("Response streaming failed", ex);
    }

    using var stream = await _response.Content.ReadAsStreamAsync();
    using var reader = new StreamReader(stream);

    while (!reader.EndOfStream)
    {
      var line = await reader.ReadLineAsync();
      if (string.IsNullOrWhiteSpace(line))
        continue;

      string? content = null;
      try
      {
        var chunk = JsonConvert.DeserializeObject<GPTApiResponse>(line);
        content = chunk?.Choices?.FirstOrDefault()?.Message?.Content;
      }
      catch (JsonReaderException)
      {
        Logger.Log(LogLevel.Warn, $"Malformed JSON chunk: {line}");
      }

      if (!string.IsNullOrEmpty(content))
      {
        _lastSuccessfulRequest = DateTime.Now;
        yield return content;
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
        _healthCheckTimer?.Stop();
        _healthCheckTimer?.Dispose();
        _cancellationTokenSource.Cancel();
        _http.Dispose();
      }
      _isDisposed = true;
    }
  }

  ~GPTModule()
  {
    Dispose(false);
  }
}
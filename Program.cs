using Eqipa.Util;
using Eqipa.VRChat;
using static Eqipa.Constants;

namespace Eqipa;

internal class Program
{
  public static VRChatBot? VRChatBot;
  private static bool _isShuttingDown;
  private static DateTime _launched = DateTime.Now;

  static async Task Main(string[] args)
  {
    AppDomain.CurrentDomain.ProcessExit += OnProcessExit!;
    Console.CancelKeyPress += OnCancelKeyPress!;

    try
    {
      Initialize();
      await RunMainLoopAsync();
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Unhandled exception in main loop: {ex}");
      Environment.ExitCode = 1;
    }
    finally
    {
      ShutdownAsync();
    }
  }

  private static void Initialize()
  {
    double elapsed;
    
    Logger.Log(LogLevel.Step, $"Launching Eqipa {VERSION}");

    try
    {
      VRChatBot = VRChatBot.Instance;
      elapsed = (DateTime.Now - _launched).TotalMilliseconds;
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Failed to initialize application: {ex}");
      throw;
    }

    Logger.Log(LogLevel.Info, $"Running (took {elapsed} ms)");
  }

  private static async Task RunMainLoopAsync()
  {
    while (!_isShuttingDown)
    {
      try
      {

        await Task.Delay(100);
      }
      catch (Exception ex)
      {
        Logger.Log(LogLevel.Error, $"Error in main loop: {ex}");
        if (ex is ObjectDisposedException)
        {
          _isShuttingDown = true;
        }
      }
    }
  }

  private static void ShutdownAsync()
  {
    if (_isShuttingDown)
    {
      return;
    }

    _isShuttingDown = true;

    try
    {
      if (VRChatBot is not null)
      {
        VRChatBot.Dispose();
        VRChatBot = null;
      }
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Error during shutdown: {ex}");
      Environment.ExitCode = 1;
    }
  }

  private static void OnProcessExit(object sender, EventArgs e)
  {
    ShutdownAsync();
  }

  private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
  {
    e.Cancel = true;
    _isShuttingDown = true;
  }
}
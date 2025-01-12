using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Eqipa.Model;
using Eqipa.Util;
using static Eqipa.Constants;
using static Eqipa.Util.ProcessUtil;

namespace Eqipa.Module.Internal;

public class VRCLogReader
{
  private readonly VRChatBot? _vrchatBot;
  private readonly Dictionary<int, long> _processOffsets = new();
  private readonly Dictionary<int, FileInfo?> _processLogFiles = new();
  private readonly List<string> _ignorePatterns = new()
  {
    "SRV", "gesture", "Exception", "Found SDK", "is missing", "EOSManager",
    "Removed player", "Restored player", "CacheComponents", "OnPlayerLeftRoom",
    "OnPlayerEnteredRoom", "OnPlayerJoinComplete", "Measure Human Avatar",
    "Using custom fx mask", "Initialized PlayerAPI", "Using default fx mask",
    "Network IDs from Avatar", "is controlled by a curve",
    "Buffer already contains chain", "doesn't have a texture property",
    "Releasing render texture that is set", "Can not play a disabled audio source",
    "Look rotation viewing vector is zero", "Collision force is restricted on avatars",
    "Holding message"
  };

  private readonly HashSet<int> _monitoredProcesses = new();
  private bool _isEOSLauncherRunning = false;

  public VRCLogReader(VRChatBot core)
  {
    _vrchatBot = core;
    Logger.Log(LogLevel.Info, "VRCLogReader constructed");
    _ = DetectVRChatProcess();
  }

  private async Task DetectVRChatProcess()
  {
    while (true)
    {
      try
      {
        var eosProcess = GetEOSLauncherProcess();
        bool eosLauncherExited = !_isEOSLauncherRunning && eosProcess == null;

        if (eosProcess != null)
        {
          if (!_isEOSLauncherRunning)
          {
            Logger.Log(LogLevel.Info, $"EOS Launcher detected with Process ID {eosProcess.Id}. Waiting for it to exit...");
            _isEOSLauncherRunning = true;
            await WaitForEOSLauncherExitAsync(eosProcess);
            eosLauncherExited = true;
          }
        }
        else
        {
          if (_isEOSLauncherRunning)
          {
            _isEOSLauncherRunning = false;
            eosLauncherExited = true;
          }
        }

        if (eosLauncherExited)
          await Task.Delay(new Random().Next(1000, 5000));

        var processes = GetProcessesByName("VRChat");

        foreach (var process in processes)
        {
          if (!_monitoredProcesses.Contains(process.Id))
          {
            Logger.Log(LogLevel.Info, $"New VRChat process detected: {process.Id}");
            _monitoredProcesses.Add(process.Id);
            _ = ProcessLogForProcessLifecycleAsync(process);
          }
        }

        var exitedProcesses = _monitoredProcesses.Where(id => !processes.Any(p => p.Id == id)).ToList();
        foreach (var processId in exitedProcesses)
        {
          Logger.Log(LogLevel.Info, $"VRChat process {processId} has exited. Stopping log monitoring.");
          _monitoredProcesses.Remove(processId);
          _processOffsets.Remove(processId);
          _processLogFiles.Remove(processId);
        }

        await Task.Delay(1000);
      }
      catch (Exception ex)
      {
        Logger.Log(LogLevel.Error, $"Error in MonitorVRChatProcessesAsync: {ex}");
        await Task.Delay(1000);
      }
    }
  }

  private async Task WaitForEOSLauncherExitAsync(Process eosProcess)
  {
    while (!eosProcess.HasExited)
    {
      await Task.Delay(500);
    }

    Logger.Log(LogLevel.Info, $"EOS Launcher (Process {eosProcess.Id}) exited.");
  }

  private Process? GetEOSLauncherProcess()
  {
    var eosProcesses = Process.GetProcessesByName("start_protected_game");
    return eosProcesses.FirstOrDefault();
  }

  private async Task ProcessLogForProcessLifecycleAsync(Process vrcProcess)
  {
    try
    {
      FileInfo? logFile = _processLogFiles.GetValueOrDefault(vrcProcess.Id);

      if (logFile == null)
      {
        int attempts = 0;
        while (logFile == null && attempts < 5)
        {
          logFile = GetLogFileForProcess(vrcProcess);
          if (logFile == null)
          {
            Logger.Log(LogLevel.Warn, $"No VRChat log file found for Process {vrcProcess.Id}. Retrying...");
            await Task.Delay(2000);
          }
          attempts++;
        }

        if (logFile == null)
        {
          Logger.Log(LogLevel.Error, $"Failed to find VRChat log file for Process {vrcProcess.Id} after {attempts} attempts.");
          return;
        }

        Logger.Log(LogLevel.Info, $"Monitoring VRChat log: {logFile.Name} for Process {vrcProcess.Id}");
        _processLogFiles[vrcProcess.Id] = logFile;
        InitializeLogOffsetForProcess(vrcProcess.Id, logFile);
      }

      while (!vrcProcess.HasExited)
      {
        try
        {
          ReadLogForProcess(vrcProcess.Id, logFile.FullName);
          await Task.Delay(100);
        }
        catch (Exception ex)
        {
          Logger.Log(LogLevel.Error, $"Error processing log for Process {vrcProcess.Id}: {ex}");
        }
      }

      Logger.Log(LogLevel.Warn, $"VRChat process {vrcProcess.Id} exited.");
      _monitoredProcesses.Remove(vrcProcess.Id);
      _processOffsets.Remove(vrcProcess.Id);
      _processLogFiles.Remove(vrcProcess.Id);
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Error in ProcessLogForProcessLifecycleAsync for process {vrcProcess.Id}: {ex}");
    }
  }

  private FileInfo? GetLogFileForProcess(Process process)
  {
    Logger.Log(LogLevel.Info, $"Checking log files for VRChat process {process.Id}");

    var directory = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat");
    if (!directory.Exists)
    {
      Logger.Log(LogLevel.Error, "VRChat log directory does not exist.");
      return null;
    }

    var logFiles = directory.GetFiles("output_log_*.txt");
    if (logFiles.Length == 0)
    {
      Logger.Log(LogLevel.Warn, "No log files found for VRChat process.");
      return null;
    }

    var latestLogFile = logFiles.OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
    return latestLogFile;
  }

  private void InitializeLogOffsetForProcess(int processId, FileInfo logFile)
  {
    _processOffsets[processId] = 0;
  }

  private void ReadLogForProcess(int processId, string path)
  {
    var lines = ReadNewLinesForProcess(processId, path);
    foreach (var line in lines)
    {
      if (!ProcessLine(processId, line))
      {
        // Console.WriteLine(line);
      }
    }
  }

  private List<string> ReadNewLinesForProcess(int processId, string filePath)
  {
    List<string> lines = new();
    StringBuilder currentLine = new();

    try
    {
      using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using var reader = new StreamReader(stream);

      long lastReadPosition = _processOffsets.GetValueOrDefault(processId, 0);
      reader.BaseStream.Seek(lastReadPosition, SeekOrigin.Begin);

      string line;
      while ((line = reader.ReadLine()!) != null)
      {
        if (Regex.IsMatch(line, @"^\d{4}\.\d{2}\.\d{2} \d{2}:\d{2}:\d{2}"))
        {
          if (currentLine.Length > 0)
          {
            lines.Add(currentLine.ToString().Trim());
            currentLine.Clear();
          }
        }

        currentLine.AppendLine(line);
      }

      if (currentLine.Length > 0)
        lines.Add(currentLine.ToString().Trim());

      _processOffsets[processId] = reader.BaseStream.Position;
    }
    catch (IOException ex)
    {
      Logger.Log(LogLevel.Error, ex.Message);
    }

    return lines;
  }

  public event EventHandler<ProcessedLogEventArgs>? OnProcessed;

  private bool ProcessLine(int processId, string line)
  {
  Dictionary<string, Match> _matched = new()
    {
      { "left", PLAYER_LEFT.Match(line) },
      { "joined", PLAYER_JOIN.Match(line) },
      { "newSticker", STICKER_SPAWN.Match(line) },
      { "setInstance", WORLD_JOINED_OR_DESTINATION.Match(line) },
      { "resetInstance", RESETTING_GAME_FLOW.Match(line) }
    };

    var eventData = new Dictionary<string, object>();
    eventData.Add("ProcessId", processId);

    if (_matched.Any(x => x.Value.Success))
    {
      string action = _matched.First(x => x.Value.Success).Key;
      eventData.Add("Action", action);

      switch (action)
      {
        case "left":
        case "joined":
          {
            string userId = _matched[action].Groups[2].Value;
            string displayName = _matched[action].Groups[1].Value;

            Logger.Log(LogLevel.Debug, $"User {displayName} ({userId}) {action}");

            eventData.Add("UserId", userId);
            eventData.Add("DisplayName", displayName);
            break;
          }

        case "newSticker":
          {
            string userId = _matched[action].Groups[1].Value;
            string username = _matched[action].Groups[2].Value;
            string stickerId = _matched[action].Groups[3].Value;

            Logger.Log(LogLevel.Debug, $"User {username} ({userId}) spawned sticker {stickerId}");

            eventData.Add("UserId", userId);
            eventData.Add("Username", username);
            eventData.Add("StickerId", stickerId);
            break;
          }

        case "setInstance":
          {
            string worldId = _matched[action].Groups[1].Value;
            string worldName = _matched[action].Groups[2].Value;
            string groupId = _matched[action].Groups[3].Value;
            string worldAccessType = _matched[action].Groups[4].Value;
            string region = _matched[action].Groups[5].Value;

            Logger.Log(LogLevel.Debug, $"Join: WorldId={worldId}, WorldName={worldName}, GroupId={groupId}, worldAccessType={worldAccessType}, Region={region}");

            eventData.Add("Region", region);
            eventData.Add("GroupId", groupId);
            eventData.Add("WorldId", worldId);
            eventData.Add("WorldName", worldName);
            eventData.Add("WorldAccessType", worldAccessType);
            break;
          }

        default:
          {
            Logger.Log(LogLevel.Warn, $"Unknown action: {action}");
            break;
          }
      }

      OnProcessed?.Invoke(this, new ProcessedLogEventArgs(action, eventData));
      return true;
    }

    if (_ignorePatterns.Any(pattern => line.Contains(pattern)))
      return true;

    return false;
  }
}

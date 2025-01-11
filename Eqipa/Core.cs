using Eqipa.Manager;
using Eqipa.Model;
using Eqipa.Util;
using Eqipa.Discord;

namespace Eqipa;

public class Core : Singleton<Core>, IDisposable
{
  public Core()
  {
    Logger.Log(LogLevel.Step, "Initializing..");
    Initialize();
  }

  public readonly Config Config = new();
  public readonly DiscordBot Discord = new();
  public readonly EventEmitter Bus = new();

  private readonly Dictionary<Type, IManager> _managers = new();
  private readonly CancellationTokenSource _updateLoopCancellation = new();
  private Task? _updateLoopTask;
  private bool _isInitialized = false;
  private bool _isDisposed = false;

  public bool IsInitialized => _isInitialized;

  private void RegisterAllManagers()
  {
    // TODO: automatic importing managers with priority
    Logger.Log(LogLevel.Step, "Registering managers...");

    RegisterManager(new DiscordWebhookManager(this));
    RegisterManager(new VRCManager(this));
    RegisterManager(new UserManager(this));
    RegisterManager(new ReceiverManager(this));
    RegisterManager(new SpeechManager(this));
    RegisterManager(new VRCInstanceManager(this));
  }

  private async void Initialize()
  {
    if (_isInitialized)
      return;

    RegisterAllManagers();

    await InitializeManagers();
    await Discord.StartAsync(Config.GetConfig<string?>(c => c.DiscordToken!)!);
    StartUpdateLoop();

    _isInitialized = true;
    Logger.Log(LogLevel.Info, "Initialized");
  }

  public void RegisterManager<T>(T manager) where T : class, IManager
  {
    ThrowIfDisposed();

    var type = manager.GetType();

    if (manager.IsInitialized)
      throw new ManagerAlreadyInitializedException(type);

    if (_managers.ContainsKey(type))
      throw new ManagerAlreadyRegisteredException(type);

    _managers[type] = manager;
    Logger.Log(LogLevel.Info, $"Registered manager: {type.Name}");
  }

  public T GetManager<T>() where T : class, IManager
  {
    ThrowIfDisposed();
    var requestedType = typeof(T);

    foreach (var (type, manager) in _managers)
    {
      if (requestedType.IsAssignableFrom(type))
      {
        return (T)manager;
      }
    }

    throw new ManagerNotFoundException(requestedType);
  }


  public T? GetManagerOrDefault<T>() where T : class, IManager
  {
    try
    {
      return GetManager<T>();
    }
    catch (ManagerNotFoundException)
    {
      return null;
    }
  }

  private async Task InitializeManagers()
  {
    ThrowIfDisposed();

    Logger.Log(LogLevel.Step, "Initializing managers");

    foreach (var manager in _managers.Values)
    {
      try
      {
        if (manager.IsInitialized)
        {
          Logger.Log(LogLevel.Warn, $"Manager {manager.GetType().Name} already initialized, skipping...");
          continue;
        }

        Logger.Log(LogLevel.Step, $"Initializing {manager.GetType().Name}");

        if (manager is IAsyncManager asyncManager)
        {
          var method = manager.GetType().GetMethod("InitializeAsync");

          if (method is not null)
            await asyncManager.InitializeAsync();
          else
            asyncManager.Initialize();
        }
        else
        {
          manager.Initialize();
        }

        Logger.Log(LogLevel.Info, $"{manager.GetType().Name} initialized");
      }
      catch (Exception ex)
      {
        Logger.Log(LogLevel.Error, $"Failed to initialize manager {manager.GetType().Name}: {ex.Message}");
      }
    }

    Logger.Log(LogLevel.Info, "All managers initialized.");
  }

  private void ShutdownManagers()
  {
    if (!_isInitialized || _isDisposed)
      return;

    foreach (var manager in _managers.Values.Reverse())
    {
      try
      {
        if (manager.IsInitialized)
        {
          manager.Shutdown();
        }
      }
      catch (Exception ex)
      {
        Logger.Log(LogLevel.Error, $"Failed to shutdown manager {manager.GetType().Name}: {ex}");
      }
    }
  }

  public bool HasManager<T>() where T : class, IManager
  {
    ThrowIfDisposed();
    var requestedType = typeof(T);
    return _managers.Any(kvp => requestedType.IsAssignableFrom(kvp.Key));
  }

  private void StartUpdateLoop()
  {
    _updateLoopTask = Task.Run(async () =>
    {
      var token = _updateLoopCancellation.Token;
      while (!token.IsCancellationRequested)
      {
        try
        {
          foreach (var manager in _managers.Values)
          {
            if (manager is IAsyncManager asyncManager)
            {
              var method = manager.GetType().GetMethod("UpdateAsync");

              if (method is not null)
                await asyncManager.UpdateAsync();
              else
                asyncManager.Update();
            }
            else
            {
              manager.Update();
            }
          }
        }
        catch (Exception ex)
        {
          Logger.Log(LogLevel.Error, $"Update loop error: {ex.Message}\n{ex.StackTrace}");
        }

        await Task.Delay(1000, token);
      }
    }, _updateLoopCancellation.Token);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (_isDisposed)
      return;

    if (disposing)
    {
      ShutdownManagers();

      foreach (var manager in _managers.Values)
      {
        try
        {
          manager.Dispose();
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Failed to dispose manager {manager.GetType().Name}: {ex}");
        }
      }

      _managers.Clear();
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
      throw new ObjectDisposedException(nameof(Core));
    }
  }

  ~Core()
  {
    Dispose(false);
  }
}
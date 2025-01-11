namespace Eqipa.Model;

public interface IManager : IDisposable
{
  bool IsInitialized { get; }
  void Shutdown();

  virtual void Initialize() { }

  virtual void Update() { }
}

public interface IAsyncManager : IManager
{
  virtual Task InitializeAsync() => Task.CompletedTask;

  virtual Task UpdateAsync() => Task.CompletedTask;
}

public class ManagerNotFoundException : Exception
{
  public ManagerNotFoundException(Type type)
      : base($"Manager of type {type.Name} not found") { }
}

public class ManagerAlreadyInitializedException : Exception
{
  public ManagerAlreadyInitializedException(Type type)
      : base($"Manager of type {type.Name} is already initialized") { }
}

public class ManagerAlreadyRegisteredException : Exception
{
  public ManagerAlreadyRegisteredException(Type type)
      : base($"Manager of type {type.Name} is already registered") { }
}

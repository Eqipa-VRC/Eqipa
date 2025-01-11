// Eqipa 2.0
// Original author: '`Deto' (deto_deto)
// Refactor version: 'Norelock' (norelock)

namespace Eqipa.Util;

/// <summary>
///  Singleton class that ensures only one instance of a class is created.
/// </summary>
/// <typeparam name="T"></typeparam>
public class Singleton<T> where T : class, new()
{
  private readonly static Lazy<T> _instance = new Lazy<T>(() => new T());
  public static T Instance => _instance.Value;

  protected Singleton()
  {
  }
}
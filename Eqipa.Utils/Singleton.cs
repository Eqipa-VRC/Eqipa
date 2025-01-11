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
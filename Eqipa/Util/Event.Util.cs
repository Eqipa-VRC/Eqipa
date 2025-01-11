// Eqipa 2.0
// Original author: '`Deto' (deto_deto)
// Refactor version: 'Norelock' (norelock)

using Eqipa.Model;

namespace Eqipa.Util;

public class EventEmitter
{
  private readonly Dictionary<string, List<IEventListener>> _events = new Dictionary<string, List<IEventListener>>();

  public void On(string eventName, IEventListener listener)
  {
    if (!_events.ContainsKey(eventName))
    {
      _events[eventName] = new List<IEventListener>();
    }
    _events[eventName].Add(listener);
  }

  public void Off(string eventName, IEventListener listener)
  {
    if (_events.ContainsKey(eventName))
    {
      _events[eventName].Remove(listener);
      if (_events[eventName].Count == 0)
      {
        _events.Remove(eventName);
      }
    }
  }

  public void Emit(string eventName, params object?[] args)
  {
    if (_events.ContainsKey(eventName))
    {
      foreach (var listener in _events[eventName])
      {
        listener.Handle(args);
      }
    }
  }
}
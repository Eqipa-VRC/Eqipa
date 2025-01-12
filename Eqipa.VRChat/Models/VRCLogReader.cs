namespace Eqipa.VRChat.Model;

public class ProcessedLogEventArgs : EventArgs
{
  public ProcessedLogEventArgs(string action, Dictionary<string, object> data)
  {
    Action = action;
    Data = data ?? new Dictionary<string, object>();
  }

  public string Action { get; }
  public Dictionary<string, object> Data { get; }

  public void AddData(string key, object value)
  {
    Data[key] = value;
  }
}
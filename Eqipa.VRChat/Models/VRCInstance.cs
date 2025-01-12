namespace Eqipa.VRChat.Model;

public class VRCInstance
{
  public int    ProcessId { get; set; } = 0;
  public string GroupId { get; set; } = string.Empty;
  public string WorldId { get; set; } = string.Empty;
  public string WorldName { get; set; } = string.Empty;
  public string WorldType { get; set; } = string.Empty;
  public string WorldRegion { get; set; } = string.Empty;

  public string GetInstanceId()
  {
    return $"{WorldId}:{WorldName}~group({GroupId})~groupAccessType({WorldType})~region({WorldRegion})";
  }
}
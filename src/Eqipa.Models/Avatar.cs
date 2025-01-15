using Newtonsoft.Json;

namespace Eqipa.Model;

public class Avatar
{
  [JsonProperty("id")]
  public string? Id { get; set; } = null;
  
  [JsonProperty("name")]
  public string? Name { get; set; } = null;

  [JsonProperty("banned")]
  public bool Banned { get; set; } = false;

  [JsonProperty("lastUsed")]
  public long LastUsed { get; set; } = 0;
}
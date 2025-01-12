using Newtonsoft.Json;

namespace Eqipa.Model;

public enum RecognizeLang
{
  Polish,
  English,
}

public class UserAvatar
{
  [JsonProperty("id")]
  public string? Id { get; set; } = null;
  
  [JsonProperty("name")]
  public string? Name { get; set; } = null;
}

public enum UserPenaltyType
{
  Ban,
  Warning,
  Blacklist
}

public enum UserStatus
{
  Online,
  Offline
}

public class UserPenalty
{
  [JsonProperty("id")]
  public string? Id { get; set; } = null;

  [JsonProperty("type")]
  public UserPenaltyType? Type { get; set; } = UserPenaltyType.Warning;

  [JsonProperty("reason")]
  public string? Reason { get; set; } = null;

  [JsonProperty("expires")]
  public long Expires { get; set; } = -1;

  [JsonProperty("invoker")]
  public string? Invoker { get; set; } = null;

  [JsonProperty("received")]
  public long Received { get; set; } = 0;
}

public class User
{
  [JsonProperty("id")]
  public string? Id { get; set; } = null;

  [JsonProperty("admin")]
  public bool Admin { get; set; } = false;

  [JsonProperty("status")]
  public UserStatus Status { get; set; } = UserStatus.Offline;

  [JsonProperty("joinedAt")]
  public long JoinedAt { get; set; } = 0;

  [JsonProperty("firstName")]
  public string? FirstName { get; set; } = null;

  [JsonProperty("discordId")]
  public ulong? DiscordId { get; set; } = null;

  [JsonProperty("penalties")]
  public List<UserPenalty>? Penalties { get; set; } = new();

  [JsonProperty("lastVisit")]
  public long LastVisit { get; set; } = 0;

  [JsonProperty("visitCount")]
  public int VisitCount { get; set; } = 0;

  [JsonProperty("displayName")]
  public string? DisplayName { get; set; } = null;

  [JsonProperty("lastAvatars")]
  public List<UserAvatar>? LastAvatars { get; set; } = new(); // empty array = []

  [JsonProperty("currentInstanceId")]
  public string? CurrentInstanceId { get; set; } = null;
}
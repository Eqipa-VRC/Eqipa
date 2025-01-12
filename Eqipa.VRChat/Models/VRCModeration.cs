using Newtonsoft.Json;

namespace Eqipa.VRChat.Model;

public enum VRCModerationType
{
  Warn,
  Kick
}

public enum VRCModerationTimeRange
{
  None = 0,
  OneDay = 2,
  OneHour = 1
}

public class VRCModeration
{
  [JsonProperty("type")]
  [JsonConverter(typeof(VRCModerationTypeConverter))]
  public VRCModerationType Type { get; set; }

  [JsonProperty("reason")]
  public string? Reason = string.Empty;

  [JsonProperty("worldId")]
  public string? WorldId = string.Empty;

  [JsonProperty("expires")]
  [JsonConverter(typeof(VRCModerationTimeRangeConverter))]
  public VRCModerationTimeRange Expires { get; set; } = VRCModerationTimeRange.OneHour;

  [JsonProperty("created")]
  public string Created = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffff");

  [JsonProperty("isPermanent")]
  public bool IsPermanent = false;

  [JsonProperty("targetUserId")]
  public string? TargetUserId = string.Empty;

  [JsonProperty("instanceId")]
  public string? InstanceId = string.Empty;
}

public static class VRCModerationHelpers
{
  public static string ModerationTimeRangeToString(VRCModerationTimeRange timeRange)
  {
    switch (timeRange)
    {
      case VRCModerationTimeRange.None: return "";
      case VRCModerationTimeRange.OneDay: return "1_day_ahead";
      case VRCModerationTimeRange.OneHour: return "1_hour_ahead";
      default: return "<unknown_ModerationTimeRange>";
    }
  }

  public static string ModerationTypeToString(VRCModerationType type)
  {
    switch (type)
    {
      case VRCModerationType.Warn: return "warn";
      case VRCModerationType.Kick: return "kick";
      default: return "<unknown_ModerationType>";
    }
  }
}

public class VRCModerationTimeRangeConverter : JsonConverter<VRCModerationTimeRange>
{
  public override VRCModerationTimeRange ReadJson(JsonReader reader, Type objectType, VRCModerationTimeRange existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    var value = reader.Value?.ToString();
    return value switch
    {
      "1_day_ahead" => VRCModerationTimeRange.OneDay,
      "1_hour_ahead" => VRCModerationTimeRange.OneHour,
      _ => VRCModerationTimeRange.None
    };
  }

  public override void WriteJson(JsonWriter writer, VRCModerationTimeRange value, JsonSerializer serializer)
  {
    writer.WriteValue(VRCModerationHelpers.ModerationTimeRangeToString(value));
  }
}

public class VRCModerationTypeConverter : JsonConverter<VRCModerationType>
{
  public override VRCModerationType ReadJson(JsonReader reader, Type objectType, VRCModerationType existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    var value = reader.Value?.ToString();
    return value switch
    {
      "warn" => VRCModerationType.Warn,
      "kick" => VRCModerationType.Kick,
      _ => throw new ArgumentOutOfRangeException($"Unknown moderation type: {value}")
    };
  }

  public override void WriteJson(JsonWriter writer, VRCModerationType value, JsonSerializer serializer)
  {
    writer.WriteValue(VRCModerationHelpers.ModerationTypeToString(value));
  }
}
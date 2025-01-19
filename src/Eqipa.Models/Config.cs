using System.Linq.Expressions;
using Eqipa.Util;
using Newtonsoft.Json;

namespace Eqipa.Model;

public class Configuration
{
  [JsonProperty("gpt.api")]
  public string? GptApi { get; set; } = string.Empty;

  [JsonProperty("gpt.token")]
  public string? GptToken { get; set; } = null;

  [JsonProperty("vrchat.groupId")]
  public string? VrchatGroupId { get; set; } = "grp_2e1917ed-0f8d-4075-8098-5919a37c8f43";

  [JsonProperty("vrchat.worldId")]
  public string? VrchatWorldId { get; set; } = "wrld_dec35e59-53f5-4def-b29c-2d7b649b8638";

  [JsonProperty("vrchat.username")]
  public string? VrchatUsername { get; set; } = string.Empty;

  [JsonProperty("vrchat.password")]
  public string? VrchatPassword { get; set; } = string.Empty;

  [JsonProperty("vrchat.oscMessage")]
  public List<string> VrchatOscMessage { get; set; } = new() {};

  [JsonProperty("discord.token")]
  public string? DiscordToken { get; set; } = string.Empty;

  [JsonProperty("discord.webhookUrl")]
  public string? DiscordWebhookUrl { get; set; } = string.Empty;

  [JsonProperty("discord.serverId")]
  public string? DiscordServerId { get; set; } = "1325008258200764417";

  [JsonProperty("speechRecognizer.token")]
  public string? SpeechRecognizerToken { get; set; } = string.Empty;

  [JsonProperty("speechRecognizer.region")]
  public string? SpeechRecognizerRegion { get; set; } = "northeurope";

  // [JsonProperty("speechRecognizer.language")]
  // public RecognizeLang SpeechRecognizerLanguage { get; set; } = RecognizeLang.Polish;
}

public class Config
{
  private readonly string configFilePath = "config.json";
  private Configuration? _configuration;

  public Config()
  {
    if (!File.Exists(configFilePath))
    {
      CreateDefaultConfig();
      Logger.Log(LogLevel.Info, "Config file created");
    }
    LoadConfig();
  }

  private void CreateDefaultConfig()
  {
    _configuration = new Configuration();
    SaveConfig();
  }

  private void LoadConfig()
  {
    var configJson = File.ReadAllText(configFilePath);
    _configuration = JsonConvert.DeserializeObject<Configuration>(configJson);

    UpdateConfig();
  }

  private void SaveConfig()
  {
    var configJson = JsonConvert.SerializeObject(_configuration, Formatting.Indented);
    File.WriteAllText(configFilePath, configJson);
  }

  private void UpdateConfig()
  {
    var configJson = File.ReadAllText(configFilePath);
    var config = JsonConvert.DeserializeObject<Configuration>(configJson);
    var properties = typeof(Configuration).GetProperties();
    foreach (var property in properties)
    {
      if (property.GetValue(config) == null)
      {
        property.SetValue(config, property.GetValue(_configuration));
      }
    }
    _configuration = config;
    SaveConfig();
  }

  public T GetConfig<T>(Expression<Func<Configuration, T>> keySelector)
  {
    if (keySelector == null)
    {
      throw new ArgumentNullException(nameof(keySelector));
    }

    var memberExpression = keySelector.Body as MemberExpression;
    if (memberExpression == null || _configuration == null)
    {
      throw new ArgumentException("Invalid key selector or Config is null.");
    }

    var property = memberExpression.Member as System.Reflection.PropertyInfo;
    if (property == null)
    {
      throw new ArgumentException("Key selector does not refer to a property.");
    }

    var value = property.GetValue(_configuration);

    return (T)value!;
  }

  public T SetConfig<T>(Expression<Func<Configuration, T>> keySelector, T value)
  {
    if (keySelector == null)
    {
      throw new ArgumentNullException(nameof(keySelector));
    }

    var memberExpression = keySelector.Body as MemberExpression;
    if (memberExpression == null || _configuration == null)
    {
      throw new ArgumentException("Invalid key selector or Config is null.");
    }

    var property = memberExpression.Member as System.Reflection.PropertyInfo;
    if (property == null)
    {
      throw new ArgumentException("Key selector does not refer to a property.");
    }

    property.SetValue(_configuration, value);
    SaveConfig();

    return value;
  }
}
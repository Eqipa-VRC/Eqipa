using Newtonsoft.Json;

namespace Eqipa.Model;

public enum GPTModel
{
  LOVE,
  DEVIL,
  EIRENE,
  NORMAL,
  WIZARD,
  EXPERT,
  PT_EXPERT,
  UNCENSORED,
  GOD_TOLITHIS,
}

public interface IGPTModule
{
  Task<string> GenerateResponse(string prompt);
  IAsyncEnumerable<string> StreamResponse(string? prompt);
}

public class GPTUsage
{
  [JsonProperty("completion_tokens")]
  public int CompletionTokens { get; set; }

  [JsonProperty("prompt_tokens")]
  public int PromptTokens { get; set; }

  [JsonProperty("total_tokens")]
  public int TotalTokens { get; set; }
}

public class GPTChoice
{
  [JsonProperty("index")]
  public int Index { get; set; }

  [JsonProperty("message")]
  public GPTMessage? Message { get; set; }

  [JsonProperty("finish_reason")]
  public string? FinishReason { get; set; }
}

public class GPTMessage
{
  [JsonProperty("role")]
  public string? Role { get; set; }

  [JsonProperty("content")]
  public string? Content { get; set; }
}

public class GPTApiResponse
{
  [JsonProperty("id")]
  public string? Id { get; set; }

  [JsonProperty("object")]
  public string? Object { get; set; }

  [JsonProperty("created")]
  public long Created { get; set; }

  [JsonProperty("model")]
  public string? Model { get; set; }

  [JsonProperty("provider")]
  public string? Provider { get; set; }

  [JsonProperty("usage")]
  public GPTUsage? Usage { get; set; }

  [JsonProperty("choices")]
  public GPTChoice[]? Choices { get; set; }
}
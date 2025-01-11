using Eqipa.Model;
using Eqipa.Module.Internal;

namespace Eqipa;

internal static class Addresses
{
  // VRChat OSC Addresses
  private static readonly Dictionary<VRCOscAddresses, string> OscAddressMap = new()
  {
    { VRCOscAddresses.SET_CHATBOX_TYPING, "/chatbox/typing" },
    { VRCOscAddresses.SEND_CHATBOX_MESSAGE, "/chatbox/input" },
  };

  public static string GetOscAddress(VRCOscAddresses address) => OscAddressMap[address];

  // Model addresses for GPT
  private static readonly Dictionary<GPTModel, string> GPTModelAddresses = new()
    {
      {  GPTModel.LOVE, "evil" },
      {  GPTModel.DEVIL, "evil" },
      {  GPTModel.EIRENE, "gpt-4o" },
      {  GPTModel.NORMAL, "gpt-4o-mini" },
      {  GPTModel.WIZARD, "claude-3.5-sonnet" },
      {  GPTModel.EXPERT, "claude-3.5-sonnet" },
      {  GPTModel.PT_EXPERT, "llama-3.1-70b" },
      {  GPTModel.UNCENSORED, "evil" },
      {  GPTModel.GOD_TOLITHIS, "claude-3.5-sonnet" },
    };

  public static string GetGptModel(GPTModel model) => GPTModelAddresses[model] ?? GPTModelAddresses[GPTModel.NORMAL];

  // Recognize languages for Speech Recognition
  private static readonly Dictionary<RecognizeLang, string> LanguageCodes = new()
  {
    { RecognizeLang.Polish, "pl-PL" },
    { RecognizeLang.English, "en-US" },
  };

  private static readonly Dictionary<RecognizeLang, List<string>> ActivationPhrases = new()
  {
    { RecognizeLang.Polish, new() { "Hej" } },
    { RecognizeLang.English, new() { "Hey", "Hello", "Hi", "Yo" } }
  };

  public static string GetRecognizeLang(RecognizeLang lang) => LanguageCodes[lang] ?? LanguageCodes[RecognizeLang.Polish];
  public static List<string> GetActivationPhrases(RecognizeLang lang) => ActivationPhrases[lang] ?? ActivationPhrases[RecognizeLang.Polish];
}
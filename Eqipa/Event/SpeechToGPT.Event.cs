// using Eqipa.Enum;
// using Eqipa.Manager;
// using Eqipa.Model;
// using Eqipa.Util;
// using static Eqipa.Addresses;

// namespace Eqipa.Event;

// public class SpeechToGPTEvent : IEventListener
// {
//     private readonly Core _core;
//     private readonly NetworkManager _networkManager;
//     private bool Listening = false;
//     private RecognizeLang _currentLang = RecognizeLang.Polish;
//     public string Name { get; } = "SpeechToGPT";
//     public SpeechToGPTEvent(Core core)
//     {
//         _core = core;
//         _currentLang = _core.Config.GetConfig<RecognizeLang>(c => c.SpeechRecognizerLanguage);
//         _networkManager = _core.GetManager<NetworkManager>();
//     }
//     public async void Handle(params object?[] args)
//     {
//         if (args.Length == 0 || args[0] == null)
//             return;
//         var recognizedText = args[0].ToString();
//         if (string.IsNullOrEmpty(recognizedText))
//             return;
//         var activationWords = GetActivationPhrases(_currentLang);
//         if (activationWords.Any(word => recognizedText.Trim().Equals(word, StringComparison.OrdinalIgnoreCase)) && !Listening)
//         {
//             Logger.Log(LogLevel.Debug, $"Activation word detected, skipping GPT: {recognizedText}");
//             Listening = true;
//             return;
//         }
//         if (!Listening) return;
//         try
//         {
//             var response = await _networkManager.GPT!.GenerateResponse(recognizedText);
//             if (!string.IsNullOrEmpty(response))
//             {
//                 await _networkManager.OSC!.Send(
//                     GetOscAddress(VRCOscAddresses.SEND_CHATBOX_MESSAGE),
//                     response,
//                     true
//                 );
//                 Logger.Log(LogLevel.Info, $"Response sent to VRChat: {response}");
//             }
//         }
//         catch (Exception ex)
//         {
//             Logger.Log(LogLevel.Error, $"Failed to process speech: {ex.Message}");
//         }
//     }
// }
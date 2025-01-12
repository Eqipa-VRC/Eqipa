using Eqipa.VRChat.Module;

namespace Eqipa.VRChat;

internal static class Addresses
{
  // VRChat OSC Addresses
  private static readonly Dictionary<VRCOscAddresses, string> OscAddressMap = new()
  {
    { VRCOscAddresses.SET_CHATBOX_TYPING, "/chatbox/typing" },
    { VRCOscAddresses.SEND_CHATBOX_MESSAGE, "/chatbox/input" },
  };

  public static string GetOscAddress(VRCOscAddresses address) => OscAddressMap[address];
}
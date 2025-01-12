using System.Reflection;
using System.Text.RegularExpressions;

namespace Eqipa;

/// <summary>
/// Constants used throughout everywhere in project
/// </summary>
internal static class Constants
{
  // VRChat Logs Regex Regonition (ex. PlayerJoined or else)
  public static readonly Regex PLAYER_JOIN = new(@"\[Behaviour\] OnPlayerJoined ([^\(]+) \((usr_[a-f0-9-]+)\)", RegexOptions.Compiled);
  public static readonly Regex PLAYER_LEFT = new(@"\[Behaviour\] OnPlayerLeft ([^\(]+) \((usr_[a-f0-9-]+)\)", RegexOptions.Compiled);
  public static readonly Regex STICKER_SPAWN = new(@"\[Always\] \[StickersManager\] User (usr_[a-f0-9-]+) \(([^)]+)\) spawned sticker (file_[a-f0-9-]+)", RegexOptions.Compiled);
  public static readonly Regex RESETTING_GAME_FLOW = new(@"\[Behaviour\] Resetting game flow because ""Destination instance set""", RegexOptions.Compiled);
  public static readonly Regex WORLD_JOINED_OR_DESTINATION = new(
    @"\[Behaviour\] Joining (wrld_[a-f0-9-]+):([^\~]+)(?:~group\(([^)]+)\))?(?:~groupAccessType\(([^)]+)\))?(?:~region\(([^)]+)\))?",
    RegexOptions.Compiled);

  // App stuff
  public static readonly string VERSION = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
  public static readonly string USER_AGENT = $"Eqipa/{VERSION} norelock";
}
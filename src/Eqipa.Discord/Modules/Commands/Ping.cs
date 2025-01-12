using Discord.Commands;

namespace Eqipa.Discord.Modules.Commands;

public class PingCommandModule : ModuleBase<SocketCommandContext>
{
  [Command("rping")]
  [Summary("Replies with RPong!")]
  public async Task RPingAsync()
  {
    await ReplyAsync("RPong!");
  }
}

using Discord.Interactions;

namespace Eqipa.Discord.Modules.Interactions;

public class RPingInteractionModule : InteractionModuleBase<SocketInteractionContext>
{
  public RPingInteractionModule()
  {
    Console.WriteLine("RPingInteractionModule instantiated.");
  }

  [SlashCommand("rping", "Replies with Pong!")]
  public async Task RPingAsync()
  {
    await RespondAsync("RPong!");
  }
}
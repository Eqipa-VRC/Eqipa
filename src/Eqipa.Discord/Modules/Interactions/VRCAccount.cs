using Discord;
using Discord.Interactions;
using Eqipa.Model;
using Eqipa.VRChat.Manager;
using UserStatus = Eqipa.Model.UserStatus;

namespace Eqipa.Discord.Modules.Interactions;

public class VRCAccountInteractionModule : InteractionModuleBase<SocketInteractionContext>
{
  // private User? GetUserByVrcId(string id)
  // {
  //   if (!Program.VRChatBot!.HasManager<UserManager>())
  //     return null;
  //   var manager = Program.VRChatBot.GetManager<UserManager>();
  //   var user = manager.Get(id);
  //   if (user is null)
  //     return null;
  //   return user;
  // }

  [SlashCommand("vrc-account", "Sprawdź swoje konto podpięte pod ciebie")]
  public async Task CheckAsync()
  {
    if (!Program.VRChatBot!.HasManager<UserManager>())
    {
      await RespondAsync("Baza danych użytkowników jeszcze nie jest zainicjalizowana, spróbuj ponownie później", ephemeral: true);
      return;
    }

    var manager = Program.VRChatBot!.GetManager<UserManager>();
    var account = manager.GetAllFiltered(a => a.DiscordId == Context.User.Id)[0];
    if (account is null)
    {
      await RespondAsync("Nie posiadasz podpiętego konta VRChat pod siebie! Jeżeli chcesz podpiąć konto by móc sprawdzić obecne dane na twoim koncie, wpisz komendę ``/vrc-link`` i postępuj zgodnie z instrukcjami");
      return;
    }

    var embed = new EmbedBuilder()
      .WithTitle($"Twoje konto (``{account.Id}``)")
      .WithDescription($"{(account.Penalties!.Count != 0 ? $":orange_circle: Posiadasz {account.Penalties.Count} nałożonych kar na twoje konto" : ":green_circle: Nie posiadasz nałożonych kar")}")
      .AddField("W grze", $"{(account.Status is UserStatus.Online ? "Tak" : $"Nie (od <t:{account.LastVisit}:R>)")}", true)
      .AddField("Dołączono", $"<t:{account.JoinedAt}:F>", true)
      .AddField("Liczba odwiedzin i ostatnie odwiedziny", $"{account.VisitCount}, <t:{account.LastVisit}:F>", false)
      .Build();

    await RespondAsync(embed: embed, ephemeral: true);
  }
}
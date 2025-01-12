using Discord;
using Discord.Interactions;
using Eqipa.Model;
using Eqipa.VRChat.Manager;
using Color = Discord.Color;
using Eqipa.Util;
using System.Text.RegularExpressions;

namespace Eqipa.Discord.Modules.Interactions;

public class VRCLinkInteractionModule : InteractionModuleBase<SocketInteractionContext>
{
  private const string MODAL_ID = "vrc-id-link";

  // Dictionary to store cancellation timestamps
  private static readonly Dictionary<ulong, DateTimeOffset> CanceledUsers = new Dictionary<ulong, DateTimeOffset>();

  // Modal class for VRC ID input
  public class VrcIdModal : IModal
  {
    public string Title => "Podaj swój identyfikator profilowy VRChat";

    [InputLabel("VRChat ID")]
    [ModalTextInput("vrc-id", TextInputStyle.Short, placeholder: "Wpisz swój identyfikator VRChat")]
    public string? VrcId { get; set; }
  }

  // Method to validate VRChat User ID
  private bool IsValidVRCUserId(string vrcUserId)
  {
    string pattern = @"^usr_[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$";
    Regex regex = new Regex(pattern);
    return regex.IsMatch(vrcUserId);
  }

  private User? GetUserByVrcId(string id)
  {
    if (!Program.VRChatBot!.HasManager<UserManager>())
      return null;

    var manager = Program.VRChatBot.GetManager<UserManager>();
    var user = manager.Get(id);
    if (user is null)
      return null;

    return user;
  }

  private bool InviteToFriendByUserId(string id)
  {
    if (!Program.VRChatBot!.HasManager<VRCManager>())
      return false;

    var manager = Program.VRChatBot.GetManager<VRCManager>();
    var invite = manager.Friends!.Friend(id);
    if (invite.Details == "{}")
      return true;

    return false;
  }

  private bool DidUserAccepted(string id)
  {
    if (!Program.VRChatBot!.HasManager<VRCManager>())
      return false;

    var manager = Program.VRChatBot.GetManager<VRCManager>();
    var result = manager.Friends!.GetFriendStatus(id);
    if (result.IsFriend)
      return true;

    return false;
  }

  private async Task<bool> WaitForFriendAcceptance(string vrcUserId)
  {
    var interval = 15000; // Check every 15 seconds
    var timeout = 30000; // 30 seconds max wait time
    var elapsedTime = 0;

    while (elapsedTime < timeout)
    {
      await Task.Delay(interval); // Wait for the interval

      // Check if friend request has been accepted
      if (DidUserAccepted(vrcUserId))
      {
        return true; // User accepted the request
      }

      elapsedTime += interval;
    }

    return false; // Timeout, user did not accept the request
  }

  private bool UpdateUser(string vrcUserId, ulong discordId)
  {
    if (!Program.VRChatBot!.HasManager<UserManager>())
      return false;

    var manager = Program.VRChatBot.GetManager<UserManager>();
    if (manager.Has(vrcUserId))
    {
      manager.Update(vrcUserId, u => u.DiscordId = discordId);
      return true;
    }

    return false;
  }

  [SlashCommand("vrc-link", "Zweryfikuj swoje konto VRChat z naszym Discordem")]
  public async Task StartAsync()
  {
    // If user has canceled and cooldown hasn't passed, show the remaining time
    if (CanceledUsers.ContainsKey(Context.User.Id))
    {
      var cooldownTime = CanceledUsers[Context.User.Id];
      var remainingTime = cooldownTime.AddMinutes(10) - DateTimeOffset.Now;

      if (remainingTime > TimeSpan.Zero)
      {
        // Show a countdown message with the remaining time
        await RespondAsync($"Anulowałeś wcześniej proces weryfikacji. Możesz spróbować ponownie za {remainingTime.Minutes} minut i {remainingTime.Seconds} sekund.", ephemeral: true);
        return;
      }
      else
      {
        // Remove the user from the canceled list after cooldown
        CanceledUsers.Remove(Context.User.Id);
      }
    }

    var embed = new EmbedBuilder()
        .WithTitle("Weryfikacja profilu VRChat")
        .WithDescription("Proces weryfikacji umożliwia synchronizację Twojego konta VRChat z naszym Discordem. Informacje, które są przechowywane, są tylko i wyłącznie z aktywności na naszych instancjach i są na bieżąco przechowywane w naszej bazie danych.\n\nPoniżej znajdziesz szczegóły tego procesu:")
        .AddField("Aktywność", "Będziesz mógł zobaczyć, kiedy ostatnio przebywałeś w VRChat oraz jakie avatary używałeś na naszych instancjach.")
        .AddField("Synchronizacja konta", "Po zweryfikowaniu konta będziemy mogli synchronizować Twoje informacje z VRChat i wysłać je tobie na bieżąco, kiedy sobie zażyczysz.")
        .WithColor(Color.Blue)
        .WithFooter("Zatwierdź, klikając przycisk poniżej");

    var builder = new ComponentBuilder()
        .WithButton("Tak", "vrc-link-modal-yes", ButtonStyle.Success)
        .WithButton("Nie", "vrc-link-modal-no", ButtonStyle.Danger);

    await RespondAsync("Czy chciałbyś rozpocząć proces weryfikacji profilu VRChat z naszym Discordem?", embed: embed.Build(), components: builder.Build(), ephemeral: true);
  }

  [ComponentInteraction("vrc-link-modal-yes")]
  public async Task ShowModalAsync()
  {
    var modal = new ModalBuilder()
        .WithTitle("Podaj swój identyfikator profilowy VRChat")
        .WithCustomId(MODAL_ID)
        .AddTextInput("VRChat ID", "vrc-id", TextInputStyle.Short, placeholder: "Wpisz swój identyfikator VRChat");

    await RespondWithModalAsync(modal.Build());
  }

  [ComponentInteraction("vrc-link-modal-no")]
  public async Task CancelAsync()
  {
    // If user has already canceled, notify them
    if (CanceledUsers.ContainsKey(Context.User.Id))
    {
      await RespondAsync("Anulowałeś wcześniej proces weryfikacji. Nie możesz zrobić tego ponownie.", ephemeral: true);
      return;
    }

    // Track the cancellation time
    CanceledUsers[Context.User.Id] = DateTimeOffset.Now;

    var embed = new EmbedBuilder()
        .WithTitle("Weryfikacja anulowana")
        .WithDescription("Proces weryfikacji został anulowany. Nie możesz zrobić tego ponownie przez 10 minut.")
        .WithColor(Color.Red)
        .WithFooter("Weryfikacja anulowana")
        .WithTimestamp(DateTimeOffset.Now);

    var builder = new ComponentBuilder()
        .WithButton("Tak", "vrc-link-modal-yes", ButtonStyle.Success, null, null, true)
        .WithButton("Nie", "vrc-link-modal-no", ButtonStyle.Danger, null, null, true);

    await FollowupAsync("Proces weryfikacji został anulowany. Możesz spróbować ponownie za 10 minut.", embed: embed.Build(), components: builder.Build(), ephemeral: true);
    await RespondAsync("Anulowano weryfikację profilu VRChat", ephemeral: true);
  }

  [ModalInteraction(MODAL_ID)]
  public async Task HandleVRCIdAsync(VrcIdModal modal)
  {
    // Debug log for the received VRChat user ID
    Logger.Log(LogLevel.Info, $"Handling VRC ID modal with user ID: {modal.VrcId}");

    // Validate VRChat ID format using regex
    if (!IsValidVRCUserId(modal.VrcId!))
    {
      Logger.Log(LogLevel.Warn, $"Invalid VRChat ID format for user: {modal.VrcId}");
      await RespondAsync("Podany identyfikator VRChat jest nieprawidłowy. Upewnij się, że wprowadziłeś prawidłowy identyfikator i spróbuj ponownie.", ephemeral: true);
      return;
    }

    // Debug log for successful format validation
    Logger.Log(LogLevel.Info, $"Valid VRChat ID format for user: {modal.VrcId}");

    var user = GetUserByVrcId(modal.VrcId!);
    if (user is null)
    {
      Logger.Log(LogLevel.Warn, $"No profile found in the database for user: {modal.VrcId}");
      await RespondAsync("Nie znaleziono profilu w bazie, najpierw dołącz na instancję Eqipa i dopiero później spróbuj zweryfikować się ponownie", ephemeral: true);
      return;
    }

    // Debug log when user is found
    Logger.Log(LogLevel.Info, $"User found in database for VRChat ID: {modal.VrcId}");

    // Send friend request to the user in VRChat
    var friendRequestResult = InviteToFriendByUserId(modal.VrcId!);
    if (!friendRequestResult)
    {
      Logger.Log(LogLevel.Error, $"Failed to send friend request to VRChat user: {modal.VrcId}");
      await RespondAsync("Wystąpił błąd przy próbie wysłania zaproszenia do znajomych. Spróbuj ponownie później.", ephemeral: true);
      return;
    }

    // Debug log for successful friend request
    Logger.Log(LogLevel.Info, $"Friend request successfully sent to VRChat user: {modal.VrcId}");

    // Inform the user that they need to accept the friend request
    await RespondAsync("Wysłaliśmy zaproszenie do znajomych w VRChat. Proszę zaakceptuj zaproszenie, aby kontynuować weryfikację.", ephemeral: true);

    // Start checking for friend request acceptance
    Logger.Log(LogLevel.Info, $"Starting to wait for friend acceptance for user: {modal.VrcId}");
    var verificationAccepted = await WaitForFriendAcceptance(modal.VrcId!);

    if (verificationAccepted)
    {
      Logger.Log(LogLevel.Info, $"Friend request accepted by VRChat user: {modal.VrcId}");

      var updated = UpdateUser(modal.VrcId!, Context.User.Id);
      if (!updated)
      {
        Logger.Log(LogLevel.Error, $"Failed to update user data for VRChat ID: {modal.VrcId}");
        await RespondAsync("Wystąpił błąd, skontaktuj się z administracją", ephemeral: true);
        return;
      }

      // Debug log when user is successfully updated
      Logger.Log(LogLevel.Info, $"User data updated successfully for VRChat ID: {modal.VrcId}");

      // Inform user that verification is complete
      try
      {
        // Create the embed for DM
        var embed = new EmbedBuilder()
            .WithTitle("Weryfikacja zakończona")
            .WithDescription("Twoje konto VRChat zostało zweryfikowane i połączone z Twoim kontem Discord.")
            .WithColor(Color.Green)
            .WithFooter("Dziękujemy za weryfikację")
            .WithTimestamp(DateTimeOffset.Now);

        // Send DM to user
        await Context.User.SendMessageAsync(embed: embed.Build());

        // Respond to the interaction to close it
        await RespondAsync("Weryfikacja zakończona pomyślnie. Wysłałem ci szczegóły w prywatnej wiadomości.", ephemeral: true);

        Logger.Log(LogLevel.Info, $"Verification DM sent successfully to user: {Context.User.Id}");
      }
      catch (Exception ex)
      {
        Logger.Log(LogLevel.Error, $"Failed to send DM to user {Context.User.Id}: {ex.Message}");
        await RespondAsync("Weryfikacja zakończona pomyślnie, ale nie mogę wysyłać ci prywatnej wiadomości. Upewnij się, że masz włączone przyjmowanie wiadomości prywatnych.", ephemeral: true);
      }
    }
    else
    {
      Logger.Log(LogLevel.Warn, $"Friend request not accepted within the timeout for user: {modal.VrcId}");
      await RespondAsync("Nie otrzymaliśmy akceptacji zaproszenia do znajomych. Weryfikacja została anulowana.", ephemeral: true);
    }
  }
}
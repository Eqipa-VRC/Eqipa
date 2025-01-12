using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using RunMode = Discord.Commands.RunMode;
using Eqipa.Util;

namespace Eqipa.Discord.Modules;

public class CommandModule
{
  private readonly DiscordSocketClient _client;
  private readonly CommandService _commandService;
  private readonly InteractionService _interactionService;
  private readonly IServiceProvider _serviceProvider;

  public CommandModule(DiscordSocketClient client, IServiceProvider serviceProvider)
  {
    _client = client;
    _serviceProvider = serviceProvider;

    _commandService = new CommandService(new CommandServiceConfig
    {
      LogLevel = LogSeverity.Info,
      DefaultRunMode = RunMode.Async
    });

    _interactionService = new InteractionService(_client, new InteractionServiceConfig
    {
      LogLevel = LogSeverity.Info
    });

    _client.MessageReceived += HandleMessageAsync;
    _client.InteractionCreated += HandleInteractionAsync;

    _commandService.Log += LogAsync;
    _interactionService.Log += LogAsync;
  }

  /// <summary>
  /// Dynamically registers all command and interaction modules from the current assembly.
  /// </summary>
  public async Task RegisterModulesAsync()
  {
    try
    {
      var assembly = Assembly.GetExecutingAssembly();
      var moduleTypes = assembly.GetTypes()
          .Where(type => typeof(ModuleBase<SocketCommandContext>).IsAssignableFrom(type) || typeof(InteractionModuleBase<SocketInteractionContext>).IsAssignableFrom(type))
          .ToList();

      Logger.Log(LogLevel.Info, $"Found {moduleTypes.Count()} modules to register");

      foreach (var moduleType in moduleTypes)
      {
        if (typeof(ModuleBase<SocketCommandContext>).IsAssignableFrom(moduleType))
        {
          await _commandService.AddModuleAsync(moduleType, _serviceProvider);
          Logger.Log(LogLevel.Info, $"Registered command module: {moduleType.Name}");
        }

        if (typeof(InteractionModuleBase<SocketInteractionContext>).IsAssignableFrom(moduleType))
        {
          await _interactionService.AddModuleAsync(moduleType, _serviceProvider);
          Logger.Log(LogLevel.Info, $"Registered interaction module: {moduleType.Name}");
        }
      }
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Error registering modules: {ex.Message}\n{ex.StackTrace}");
    }
  }

  public async Task InitializeAsync()
  {
    try
    {
      await RegisterModulesAsync();
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Error during module initialization: {ex.Message}");
    }
  }

  private async Task HandleMessageAsync(SocketMessage rawMessage)
  {
    Console.WriteLine($"{rawMessage.Author.Username}: {rawMessage.Content}");

    try
    {
      if (!(rawMessage is SocketUserMessage message) || message.Author.IsBot) return;

      var context = new SocketCommandContext(_client, message);

      int argPos = 0;
      if (message.HasCharPrefix('!', ref argPos))
      {
        var result = await _commandService.ExecuteAsync(context, argPos, _serviceProvider);

        if (!result.IsSuccess)
        {
          Logger.Log(LogLevel.Error, result.Error.ToString()!);
        }
      }
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Error handling message: {ex.Message}");
    }
  }

  private async Task HandleInteractionAsync(SocketInteraction interaction)
  {
    try
    {
      var context = new SocketInteractionContext(_client, interaction);

      switch (interaction.Type)
      {
        case InteractionType.ModalSubmit:
          {
            var modalSubmitResult = await _interactionService.ExecuteCommandAsync(context, _serviceProvider);

            if (!modalSubmitResult.IsSuccess)
            {
              Logger.Log(LogLevel.Error, $"Application modal error: {modalSubmitResult.ErrorReason}");
              await interaction.RespondAsync($"Błąd: {modalSubmitResult.ErrorReason}", ephemeral: true);
            }
            break;
          }

        case InteractionType.MessageComponent:
          {
            var messageComponentResult = await _interactionService.ExecuteCommandAsync(context, _serviceProvider);

            if (!messageComponentResult.IsSuccess)
            {
              Logger.Log(LogLevel.Error, $"Application component error: {messageComponentResult.ErrorReason}");
              await interaction.RespondAsync($"Błąd: {messageComponentResult.ErrorReason}", ephemeral: true);
            }
            break;
          }

        case InteractionType.ApplicationCommand:
          {
            var slashCommandResult = await _interactionService.ExecuteCommandAsync(context, _serviceProvider);

            if (!slashCommandResult.IsSuccess)
            {
              Logger.Log(LogLevel.Error, $"Application command interaction error: {slashCommandResult.ErrorReason}");
              await interaction.RespondAsync($"Błąd: {slashCommandResult.ErrorReason}", ephemeral: true);
            }
            break;
          }

        default:
          // If the interaction type is unhandled, you can log or respond accordingly
          Logger.Log(LogLevel.Warn, $"Unhandled interaction type: {interaction.Type}");
          await interaction.RespondAsync("Nierozpoznany typ interakcji.", ephemeral: true);
          break;
      }
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Interaction handling error: {ex.Message}\n{ex.StackTrace}");
      if (interaction.Type == InteractionType.ApplicationCommand)
      {
        await interaction.RespondAsync("Błąd podczas przetwarzania zadań komendy, skontaktuj się z administracją", ephemeral: true);
      }
      else if (interaction.Type == InteractionType.ModalSubmit)
      {
        await interaction.RespondAsync("Wystąpił błąd przy przetwarzaniu formularza. Spróbuj ponownie później.", ephemeral: true);
      }
    }
  }

  /// <summary>
  /// Registers all slash commands globally. This takes up to 1 hour to propagate.
  /// </summary>
  public async Task RegisterCommandsGloballyAsync()
  {
    try
    {
      await _interactionService.RegisterCommandsGloballyAsync();
      Logger.Log(LogLevel.Info, "Registered global slash commands successfully.");
    }
    catch (Exception ex)
    {
      Logger.Log(LogLevel.Error, $"Error registering global slash commands: {ex.Message}");
    }
  }

  private Task LogAsync(LogMessage logMessage)
  {
    Console.WriteLine(logMessage.ToString());
    return Task.CompletedTask;
  }
}
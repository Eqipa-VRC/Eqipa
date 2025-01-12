using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Eqipa.Util;
using Microsoft.Extensions.DependencyInjection;
using RunMode = Discord.Commands.RunMode;

namespace Eqipa.Discord.Modules
{
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

      // Initialize services
      _commandService = new CommandService(new CommandServiceConfig
      {
        LogLevel = LogSeverity.Info,
        DefaultRunMode = RunMode.Async
      });

      _interactionService = new InteractionService(_client, new InteractionServiceConfig
      {
        LogLevel = LogSeverity.Info
      });

      // Subscribe to Discord events
      _client.MessageReceived += HandleMessageAsync;
      _client.InteractionCreated += HandleInteractionAsync;

      _commandService.Log += LogAsync;
      _interactionService.Log += LogAsync;
    }

    /// <summary>
    /// Dynamically registers all command and interaction modules from the current assembly.
    /// </summary>
    public async Task RegisterModulesFromNamespaceAsync(string targetNamespace)
    {
      try
      {
        var assembly = Assembly.GetExecutingAssembly();
        var moduleTypes = assembly.GetTypes()
            .Where(type => typeof(ModuleBase<SocketCommandContext>).IsAssignableFrom(type) || typeof(InteractionModuleBase<SocketInteractionContext>).IsAssignableFrom(type))
            .ToList();

        Console.WriteLine($"Found {moduleTypes.Count()} modules.");

        foreach (var moduleType in moduleTypes)
        {
          Console.WriteLine($"Registering module: {moduleType.Name}, Namespace: {moduleType.Namespace}");

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
        Logger.Log(LogLevel.Error, $"Error registering modules from namespace '{targetNamespace}': {ex.Message}\n{ex.StackTrace}\n{ex.Source}");
      }
    }

    public async Task InitializeAsync()
    {
      try
      {
        var currentNamespace = typeof(CommandModule).Namespace;
        if (!string.IsNullOrEmpty(currentNamespace))
        {
          await RegisterModulesFromNamespaceAsync(currentNamespace);
        }
        else
        {
          Logger.Log(LogLevel.Warn, "CommandModule namespace could not be determined.");
        }
      }
      catch (Exception ex)
      {
        Logger.Log(LogLevel.Error, $"Error during module initialization: {ex.Message}");
      }
    }

    private async Task HandleMessageAsync(SocketMessage rawMessage)
    {
      try
      {
        Console.WriteLine($"{rawMessage.Author.Username}: {rawMessage.Content}");

        // Ignore system and bot messages
        if (!(rawMessage is SocketUserMessage message) || message.Author.IsBot) return;

        var context = new SocketCommandContext(_client, message);

        int argPos = 0;

        // Check for a command prefix (e.g., '!') or a bot mention
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

        // Execute the interaction command
        var result = await _interactionService.ExecuteCommandAsync(context, _serviceProvider);

        if (!result.IsSuccess)
        {
          Logger.Log(LogLevel.Error, $"Interaction error: {result.ErrorReason}");

          // Respond to the user with the error if the interaction is a slash command
          if (interaction.Type == InteractionType.ApplicationCommand)
          {
            await interaction.RespondAsync($"Error: {result.ErrorReason}", ephemeral: true);
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Log(LogLevel.Error, $"Interaction handling error: {ex.Message}\n{ex.StackTrace}");
        if (interaction.Type == InteractionType.ApplicationCommand)
        {
          await interaction.RespondAsync("An error occurred while processing this command.", ephemeral: true);
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

    /// <summary>
    /// Registers all slash commands to a specific guild for faster propagation.
    /// </summary>
    /// <param name="guildId">The target guild ID.</param>
    public async Task RegisterCommandsToGuildAsync(ulong guildId)
    {
      try
      {
        await _interactionService.RegisterCommandsToGuildAsync(guildId);
        Logger.Log(LogLevel.Info, $"Registered slash commands to guild: {guildId}");
      }
      catch (Exception ex)
      {
        Logger.Log(LogLevel.Error, $"Error registering slash commands to guild {guildId}: {ex.Message}");
      }
    }

    private Task LogAsync(LogMessage logMessage)
    {
      Console.WriteLine(logMessage.ToString());
      return Task.CompletedTask;
    }
  }
}

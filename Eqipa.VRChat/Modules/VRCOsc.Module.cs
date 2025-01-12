using System.Net;
using OscQueryLibrary;
using OscQueryLibrary.Utils;
using Eqipa.OSC;
using Eqipa.Util;

namespace Eqipa.Module.Internal;
public enum VRCOscAddresses
{
  SET_CHATBOX_TYPING,
  SEND_CHATBOX_MESSAGE,
}

public class VRCOsc
{
  private VRChatBot? _vrchatBot;
  private readonly List<OscQueryServer> _oscQueryServers = new();
  private readonly HashSet<IPEndPoint> _connectedEndpoints = new();
  private readonly List<OscConnection> _connections = new();
  private readonly object _lock = new object();

  public VRCOsc(VRChatBot core)
  {
    _vrchatBot = core;

    var server = new OscQueryServer("Eqipa", IPAddress.Loopback); 
    server.FoundVrcClient += HandleNewVrcClient;

    _oscQueryServers.Add(server);
    server.Start();
  }

  private Task HandleNewVrcClient(OscQueryServer oscQueryServer, IPEndPoint ipEndPoint)
  {
    lock (_lock)
    {
      Logger.Log(LogLevel.Debug, $"Received connection attempt from {ipEndPoint}");

      if (_connectedEndpoints.Contains(ipEndPoint))
      {
        Logger.Log(LogLevel.Debug, $"VRC Client at {ipEndPoint} is already connected.");
        return Task.CompletedTask;
      }

      Logger.Log(LogLevel.Debug, $"Connecting to VRC Client at {ipEndPoint}");

      _connectedEndpoints.Add(ipEndPoint);

      var newConnection = new OscConnection(_vrchatBot!, oscQueryServer, ipEndPoint);
      _connections.Add(newConnection);
      newConnection.StartReceiverLoop();

      Logger.Log(LogLevel.Debug, $"Currently connected clients: {_connections.Count}");

      return Task.CompletedTask;
    }
  }

  public async Task Send(string address, params object[] arguments)
  {
    if (_connections.Count == 0)
    {
      Logger.Log(LogLevel.Warn, "No connected clients to send the message.");
      return;
    }

    var tasks = _connections.Select(connection => connection.Send(address, arguments)).ToList();
    await Task.WhenAll(tasks);
  }

  private class OscConnection : IDisposable
  {
    private readonly VRChatBot _vrchatBot;
    private readonly OscDuplex _gameConnection;
    private readonly CancellationTokenSource _loopCancellationToken = new();
    public IPEndPoint Endpoint { get; }

    public OscConnection(VRChatBot core, OscQueryServer oscQueryServer, IPEndPoint ipEndPoint)
    {
      _vrchatBot = core;
      _gameConnection = new OscDuplex(new IPEndPoint(ipEndPoint.Address, oscQueryServer.OscReceivePort), ipEndPoint);
      Endpoint = ipEndPoint;

      Logger.Log(LogLevel.Debug, $"Created new connection for {ipEndPoint}");
    }

    public void StartReceiverLoop()
    {
      ErrorHandledTask.Run(ReceiverLoopAsync);
    }

    private async Task ReceiverLoopAsync()
    {
      var currentCancellationToken = _loopCancellationToken.Token;

      while (!currentCancellationToken.IsCancellationRequested)
      {
        try
        {
          await ReceiveLogic();
        }
        catch (Exception e)
        {
          Logger.Log(LogLevel.Error, $"Loop receiver error for {Endpoint}: {e.Message}");
        }
      }
    }

    // Process the received OSC message
    private async Task ReceiveLogic()
    {
      OscMessage received;

      try
      {
        received = await _gameConnection.ReceiveMessageAsync();
      }
      catch (Exception e)
      {
        Logger.Log(LogLevel.Error, $"Error receiving message from {Endpoint}: {e.Message}");
        return;
      }


      // if (received.Address == "/avatar/parameters/MuteSelf")
      // {
      //     _vrchatBot.Bus.Emit("PlayerMuted", (bool)received.Arguments[0]!);
      // }
    }

    public async Task Send(string address, params object[] arguments)
    {
      arguments ??= Array.Empty<object>();

      await _gameConnection.SendAsync(new OscMessage(address, arguments));
    }

    public void Dispose()
    {
      // Only cancel if not already cancelled
      if (!_loopCancellationToken.Token.IsCancellationRequested)
      {
        _loopCancellationToken.Cancel();
      }
      _gameConnection.Dispose();

      Logger.Log(LogLevel.Debug, $"Disposed connection for {Endpoint}");
    }
  }
}

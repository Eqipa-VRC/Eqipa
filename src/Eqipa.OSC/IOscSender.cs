using System.Net;

namespace Eqipa.OSC;

public interface IOscSender
{
  public Task SendAsync(byte[] message);

  public Task SendAsync(IOscPacket packet);

  public Task SendAsync(IPEndPoint endPoint, byte[] message);

  public Task SendAsync(IPEndPoint endPoint, IOscPacket packet);
}
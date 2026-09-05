using System.Net.WebSockets;
using System.Text;

namespace FluentGwt.Tests;

internal static class SocketHostTestsFluent
{
	public static async Task<string> Echo(ApplicationHost host, string message, CancellationToken cancellationToken)
	{
		using var socket = await host.ConnectWebSocket("/echo", cancellationToken);
		await socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
		var buffer = new byte[1024];
		var received = await socket.ReceiveAsync(buffer, cancellationToken);
		return Encoding.UTF8.GetString(buffer, 0, received.Count);
	}
}

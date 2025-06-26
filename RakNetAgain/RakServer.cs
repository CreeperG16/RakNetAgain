// #error version

using RakNetAgain.Packets;
using System.Net;
using System.Net.Sockets;

namespace RakNetAgain;

public class RakServer(ushort port, ushort? portV6 = null) {
    public static readonly byte RAKNET_VERSION = 11;

    private readonly ushort ServerPort = port;
    private readonly ushort ServerPortV6 = portV6.GetValueOrDefault((ushort)(port + 1));
    private readonly UdpClient socket = new(port, AddressFamily.InterNetwork); // ipv4 socket (implement v6?)
    public readonly ulong Guid = (ulong)new Random().NextInt64();

    public ushort MaxTransferUnit { get; init; } = 1492;

    public int MaxConnections { get; set; } = 10;
    public int ProtocolVersion { get; set; } = 818;
    public string GameVersion { get; set; } = "1.21.90";

    // Store by GUID instead?
    public Dictionary<IPEndPoint, RakConnection> Connections = [];

    public delegate void OnConnectListener(RakConnection connection);
    public event OnConnectListener OnConnect = delegate { };

    // TODO: proper start func, threads?
    private CancellationTokenSource? _cts;
    public void Start(CancellationToken? cancellationToken = null) {
        _cts = new CancellationTokenSource();
        var token = cancellationToken ?? _cts.Token;

        _ = StartListener(token);
        _ = StartConnectionTicking(token);
    }

    public void Stop() {
        _cts?.Cancel();
        socket.Close();
    }

    private CancellationTokenSource? _listenerCts;
    public async Task StartListener(CancellationToken? cancellationToken = null) {
        _listenerCts = new CancellationTokenSource();
        var token = cancellationToken ?? _listenerCts.Token;

        while (!token.IsCancellationRequested) {
            await GetPackets();
            Thread.Sleep(30);
        }
    }

    private CancellationTokenSource? _connectionTickerCts;
    public async Task StartConnectionTicking(CancellationToken? cancellationToken = null) {
        _connectionTickerCts = new CancellationTokenSource();
        var token = cancellationToken ?? _connectionTickerCts.Token;

        while (!token.IsCancellationRequested) {
            await TickConnections();
            Thread.Sleep(100);
        }
    }

    public void StopListener() =>  _listenerCts?.Cancel();
    public void StopConnectionTicking() => _connectionTickerCts?.Cancel();

    public async Task GetPackets() {
        try {
            UdpReceiveResult result = await socket.ReceiveAsync();
            await HandlePacket(result.Buffer, result.RemoteEndPoint);
        } catch (Exception ex) {
            Console.WriteLine($"Error receiving packets: {ex.Message}");
        }
    }

    public async Task TickConnections() {
        try {
            foreach (var conn in Connections) await conn.Value.Tick();
        } catch (Exception ex) {
            Console.WriteLine($"Error ticking connections: {ex.Message}");
        }
    }

    private async Task HandlePacket(byte[] data, IPEndPoint client) {
        byte id = data[0];

        if ((id & 128) != 0) {
            if (!Connections.TryGetValue(client, out var connection)) {
                Console.WriteLine($"Received valid connection packet '0x{id:X2}' ({data.Length}) from client, but no connection class found.");
                return;
            }

            await connection.HandleIncomingPacket(data);
            return;
        }

        // Console.WriteLine($"Received unconnected packet '0x{id:X2}' [{(PacketID)id}] ({data.Length}) from client.");
        // Console.WriteLine($"Packet: {BitConverter.ToString(data).Replace("-", " ")}");

        switch ((PacketID)id) {
            case PacketID.UnconnectedPing:
                await HandleUnconnectedPing(data, client);
                break;
            case PacketID.OpenConnectionRequest1:
                await HandleOpenConnReq1(data, client);
                break;
            case PacketID.OpenConnectionRequest2:
                await HandleOpenConnReq2(data, client);
                break;
            default:
                break;
        }
    }

    private async Task HandleUnconnectedPing(byte[] data, IPEndPoint endpoint) {
        UnconnectedPing packet = new(data);

        UnconnectedPong pong = new() {
            Time = packet.Time,
            ServerGuid = Guid,
            // TODO: maybe move this implementation of ServerMessage
            // out of RakNet itself into the Minecraft server implementation?
            Message = new() {
                GameVersion = GameVersion,
                ProtocolVersion = ProtocolVersion,
                Port = ServerPort,
                PortV6 = ServerPortV6,
                ServerGuid = Guid,
                PlayerCount = Connections.Count,
                MaxPlayers = MaxConnections,
            },
        };

        await SendPacket(pong.Write(), endpoint);
    }

    private async Task HandleOpenConnReq1(byte[] data, IPEndPoint endpoint) {
        OpenConnectionRequest1 packet = new(data);
        if (packet.ProtocolVersion != RAKNET_VERSION) {
            IncompatibleProtocol incompatible = new() {
                ProtocolVersion = RAKNET_VERSION,
                ServerGuid = Guid,
            };

            await SendPacket(incompatible.Write(), endpoint);
            return;
        }

        ushort mtu = Math.Min(MaxTransferUnit, packet.MaxTransferUnit);

        OpenConnectionReply1 reply = new() {
            ServerGuid = Guid,
            UseSecurity = false,
            MaxTransferUnit = mtu,
        };

        await SendPacket(reply.Write(), endpoint);
    }

    private async Task HandleOpenConnReq2(byte[] data, IPEndPoint endpoint) {
        OpenConnectionRequest2 packet = new(data, false);

        // TODO: is this the right place to disconnect?
        // Cancel the login if the server is full
        if (Connections.Count >= MaxConnections) {
            Disconnect disconnect = new();
            await SendPacket(disconnect.Write(), endpoint);
            return;
        }

        OpenConnectionReply2 reply = new() {
            ServerGuid = Guid,
            ClientAddress = endpoint,
            MaxTransferUnit = packet.MaxTransferUnit,
            EncryptionEnabled = false,
        };

        RakConnection connection = new(server: this) {
            Endpoint = endpoint,
            MaxTransferUnit = (ushort)packet.MaxTransferUnit,
        };

        connection.OnDisconnect += () => {
            Connections.Remove(endpoint);
        };

        Connections.Add(endpoint, connection);
        OnConnect?.Invoke(connection);

        await SendPacket(reply.Write(), endpoint);
    }

    internal Task SendPacket(byte[] data, IPEndPoint endpoint) => socket.SendAsync(data, data.Length, endpoint);
}

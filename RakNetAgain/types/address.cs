using System.Net;
using System.Net.Sockets;

namespace RakNetAgain.Types;

public static class Address {
    public static void WriteAddress(this BinaryWriter writer, IPEndPoint endpoint) {
        if (endpoint.AddressFamily == AddressFamily.InterNetwork) {
            writer.Write((byte)4);
            writer.Write(endpoint.Address.GetAddressBytes());
            writer.WriteBE((ushort)endpoint.Port);
            return;
        }

        if (endpoint.AddressFamily == AddressFamily.InterNetworkV6) {
            writer.Write((byte)6);
            writer.WriteBE((ushort)endpoint.AddressFamily);
            writer.WriteBE((ushort)endpoint.Port);
            writer.Write(0); // TODO: flow info?
            writer.Write(endpoint.Address.GetAddressBytes());
            writer.Write((uint)endpoint.Address.ScopeId);
            return;
        }
    }

    public static IPEndPoint ReadAddress(this BinaryReader reader) {
        byte version = reader.ReadByte();

        if (version == 4) {
            IPAddress address = new(reader.ReadBytes(4));
            ushort port = reader.ReadUInt16BE();
            return new IPEndPoint(address, port);
        }

        if (version == 6) {
            _ = (AddressFamily)reader.ReadUInt16BE(); // address family
            var port = reader.ReadUInt16BE();
            _ = reader.ReadUInt32(); // TODO: flow info?
            var addressBytes = reader.ReadBytes(16);
            var scopeId = reader.ReadUInt32();
            IPAddress address = new(addressBytes, scopeId);
            return new IPEndPoint(address, port);
        }

        throw new InvalidDataException($"Unexpected IP version in address: {version}");
    }
}

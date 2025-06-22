namespace RakNetAgain;

public enum PacketID : byte {
    ConnectedPing = 0x00,
    UnconnectedPing = 0x01,
    ConnectedPong = 0x03,
    OpenConnectionRequest1 = 0x05,
    OpenConnectionReply1 = 0x06,
    OpenConnectionRequest2 = 0x07,
    OpenConnectionReply2 = 0x08,
    ConnectionRequest = 0x09,
    ConnectionRequestAccepted = 0x10,
    NewIncomingConnection = 0x13,
    Disconnect = 0x15,
    IncompatibleProtocolVersion = 0x19,
    UnconnectedPong = 0x1c,
    FrameSet = 0x80,
    Nack = 0xa0,
    Ack = 0xc0
}

using System.Net;

namespace RakNetAgain;

public class RakConnection {
    public readonly IPEndPoint Endpoint;

    internal RakConnection(IPEndPoint endpoint) {
        Endpoint = endpoint;
    }
}

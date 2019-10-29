namespace CustomToken.Core
{
    using System;
    using System.Net;

    public class ConnectionModel
    {

        private const string IP_ADDRESS = "127.0.0.1";
        private const int PORT = 5001;


        public string IPAddress { get; } = IP_ADDRESS;
        public int Port { get; } = PORT;


        public EndPoint IPEndPoint { get; } = new IPEndPoint(System.Net.IPAddress.Parse(IP_ADDRESS), PORT);

        public string HttpUrl { get; } = $"Http://{IP_ADDRESS}:{PORT}";
        public string HttpsUrl { get; } = $"Https://{IP_ADDRESS}:{PORT}";

    };
};

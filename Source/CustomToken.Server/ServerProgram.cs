namespace CustomToken.Server
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using CustomToken.Core;

    using Microsoft.IdentityModel.Tokens;

    public static class ServerProgram
    {
        private static TcpListener _server;
        private static ConnectionModel _connectionModel = new ConnectionModel();

        private const string END_OF_LINE = "\r\n";

        private const string JWT_TOKEN_KEY = "2CD7C71FFBF8437081A1FDB55C969849";

        private static HashSet<string> _validTokens = new HashSet<string>();


        private struct Token
        {
            public string RawToken { get; set; }

            public Dictionary<string, string> Header { get; set; }
            public Dictionary<string, string> Claims { get; set; }

            public string Key { get; set; }

        }


        private async static Task Main(string[] args)
        {
            Console.Title = "Server";


            Console.CancelKeyPress +=
            (sender, e) =>
            {
                Console.WriteLine("Shutting down...");
                Thread.Sleep(2000);

                Environment.Exit(0);
            };



            Console.WriteLine("Initializng ip address and server...");

            Console.WriteLine($"HTTP server started {_connectionModel.HttpUrl} \nWating for connections...");


            (_server = new TcpListener((IPEndPoint)_connectionModel.IPEndPoint))
            .Start();


            Console.WriteLine($"Server started on {_connectionModel.IPEndPoint}");

            Console.WriteLine($"Press CTRL + C to stop");

            Console.WriteLine("Waiting for connections...");



            while (true)
            {
                var connectedClient = await _server.AcceptTcpClientAsync();

                Console.WriteLine($"Client {connectedClient.Client.RemoteEndPoint} connected");


                var networkStream = connectedClient.GetStream();

                while (true)
                {
                    while (networkStream.DataAvailable == false)
                        await Task.Delay(1);

                    if (connectedClient.Available > 3)
                    {
                        byte[] bytes = new byte[connectedClient.Available];

                        networkStream.Read(bytes, 0, bytes.Length);

                        string rawData = Encoding.UTF8.GetString(bytes);

                        string[] splitData = rawData.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);


                        var requestedUrl = rawData.Split(" ")[1].Split('/', StringSplitOptions.RemoveEmptyEntries);

                        string responseString = "";

                        if (Regex.IsMatch(rawData, "^POST", RegexOptions.IgnoreCase))
                        {
                            if (requestedUrl[0] == "Account")
                            {
                                if (requestedUrl[1] == "Login")
                                {
                                    var loginDetails = JsonSerializer.Deserialize<LoginRequest>(rawData.Split("\r\n\r\n")[1]);

                                    var token = BuildToken(new Dictionary<string, object>()
                                    {
                                        { "Username", loginDetails.Username },
                                        { "Roles", "Users" },
                                    }, JWT_TOKEN_KEY);

                                    _validTokens.Add(token);


                                    responseString = CreateResponseHeader(new string[]
                                    {
                                        $"HTTP/{HttpVersion.Version11} {(int)HttpStatusCode.OK} {HttpStatusCode.OK}",
                                        "Content-Type: application/json",
                                    },
                                    JsonSerializer.Serialize(
                                     new LoginResponse()
                                     {
                                         Token = token,
                                     }));
                                }
                                else if (requestedUrl[1] == "SignOut")
                                {
                                    var signOutDetails = JsonSerializer.Deserialize<SignOutRequest>(rawData.Split("\r\n\r\n")[1]);

                                    // If token was found and removed
                                    if (_validTokens.Remove(signOutDetails.Token) == true)
                                    {
                                        responseString = CreateResponseHeader(new string[]
                                        {
                                            $"HTTP/{HttpVersion.Version11} {(int)HttpStatusCode.OK} {HttpStatusCode.OK}",
                                        });
                                    }
                                    else
                                    {
                                        responseString = CreateResponseHeader(new string[]
                                        {
                                            $"HTTP/{HttpVersion.Version11} {(int)HttpStatusCode.Unauthorized} {HttpStatusCode.Unauthorized}",
                                            "Content-Type: Text/Plain",
                                        }, "Token is invalid");
                                    };
                                }
                                else if (requestedUrl[1] == "Authorized")
                                {
                                    var authorizedRequest = JsonSerializer.Deserialize<AuthorizedRequest>(rawData.Split("\r\n\r\n")[1]);

                                    if (_validTokens.Contains(authorizedRequest.Token) == true)
                                    {
                                        Token token = DecodeToken(authorizedRequest.Token);

                                        var isInUserRole = token.Claims["Roles"].Contains("Users");
                                      
                                        if (isInUserRole)
                                        {
                                            responseString = CreateResponseHeader(new string[]
                                            {
                                                $"HTTP/{HttpVersion.Version11} {(int)HttpStatusCode.OK} {HttpStatusCode.OK}",
                                            });
                                        };
                                    }
                                    else
                                    {
                                        responseString = CreateResponseHeader(new string[]
                                        {
                                            $"HTTP/{HttpVersion.Version11} {(int)HttpStatusCode.Unauthorized} {HttpStatusCode.Unauthorized}",
                                            "Content-Type: Text/Plain",
                                        }, "Token is invalid");
                                    };
                                }
                                else
                                {
                                    responseString = CreateResponseHeader(new string[]
                                    {
                                    $"HTTP/{HttpVersion.Version11} {(int)HttpStatusCode.NotFound} {HttpStatusCode.NotFound}",
                                    });
                                };
                            }
                            else
                            {
                                responseString = CreateResponseHeader(new string[]
                                {
                                    $"HTTP/{HttpVersion.Version11} {(int)HttpStatusCode.NotFound} {HttpStatusCode.NotFound}",
                                });
                            };


                            byte[] responseBytes = Encoding.UTF8.GetBytes(responseString);

                            networkStream.Write(new ReadOnlySpan<byte>(responseBytes));
                        };

                        connectedClient.Close();

                        break;
                    };
                };
            };
        }


        public static string CreateResponseHeader(IEnumerable<string> headers, string content = "")
        {
            if ((headers is null) || (headers.Count() == 0))
                return null;

            StringBuilder responseString = new StringBuilder();

            responseString.AppendJoin(END_OF_LINE, headers);

            responseString.Append(END_OF_LINE);
            responseString.Append(END_OF_LINE);

            if (string.IsNullOrWhiteSpace(content) == false)
                responseString.Append(content);

            return responseString.ToString();
        }


        private static string BuildToken(Dictionary<string, object> payload, string key)
        {
            string header = UrlSafeBase64(JsonSerializer.Serialize(new
            {
                typ = "JWT",
            }));

            string jsonPayload = UrlSafeBase64(JsonSerializer.Serialize(payload));

            string hashedKey = UrlSafeBase64(key);

            return $"{header}.{jsonPayload}.{hashedKey}";
        }

        private static Token DecodeToken(string token)
        {
            string[] tokenParts = token.Split('.');

            var decodedHeader = Base64UrlEncoder.Decode(tokenParts[0]);
            var decodedPayload = Base64UrlEncoder.Decode(tokenParts[1]);
            var decodedKey = Base64UrlEncoder.Decode(tokenParts[2]);

            return new Token()
            {
                RawToken = token,

                Header = JsonSerializer.Deserialize<Dictionary<string, string>>(decodedHeader),
                Claims = JsonSerializer.Deserialize<Dictionary<string, string>>(decodedPayload),
                Key = decodedKey,
            };
        }


        private static string UrlSafeBase64(string data)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(data))
                .Replace("=", "")
                .Replace("/", "")
                .Replace("_", "");
        }
    };
};
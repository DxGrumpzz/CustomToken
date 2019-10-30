﻿namespace CustomToken.Server
{
    using System;
    using System.Collections.Generic;
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

        /// <summary>
        /// The TCP server
        /// </summary>
        private static TcpListener _server;

        /// <summary>
        /// A connection information specifier
        /// </summary>
        private static ConnectionModel _connectionModel = new ConnectionModel();

        /// <summary>
        /// An HTTP standard header end of line break
        /// </summary>
        private const string END_OF_LINE = "\r\n";

        /// <summary>
        /// Random secret key
        /// </summary>
        private const string JWT_TOKEN_KEY = "2CD7C71FFBF8437081A1FDB55C969849";

        /// <summary>
        /// A hash list that contains a list of valid tokens
        /// </summary>
        private static HashSet<string> _validTokens = new HashSet<string>();


        /// <summary>
        /// A Token data structure that contains "formatted" information about a received/sent token
        /// </summary>
        private class Token
        {
            /// <summary>
            /// The token as is, a raw unecrypted string
            /// </summary>
            public string RawToken { get; set; }

            /// <summary>
            /// Header information that is associated with the token
            /// </summary>
            public Dictionary<string, string> Header { get; set; }

            /// <summary>
            /// The payload/data that came with this token
            /// </summary>
            public Dictionary<string, string> Claims { get; set; }

            /// <summary>
            /// The encrypted key
            /// </summary>
            public string Key { get; set; }
        }


        private async static Task Main(string[] args)
        {
            Console.Title = "Server";

            // When user presses create an interrupt that exists exists
            Console.CancelKeyPress +=
            (sender, e) =>
            {
                Console.WriteLine("Shutting down...");
                Thread.Sleep(2000);

                Environment.Exit(0);
            };



            Console.WriteLine("Initializng ip address and server...");

            Console.WriteLine($"HTTP server started {_connectionModel.HttpUrl} \nWating for connections...");

            // Create the server 
            (_server = new TcpListener((IPEndPoint)_connectionModel.IPEndPoint))
            .Start();


            Console.WriteLine($"Server started on {_connectionModel.IPEndPoint}");

            Console.WriteLine($"Press CTRL + C to stop");

            Console.WriteLine("Waiting for connections...");



            // Loop until user calls to exit
            while (true)
            {


                // Wait until a connetion is recived
                var connectedClient = await _server.AcceptTcpClientAsync();

                Console.WriteLine($"Client {connectedClient.Client.RemoteEndPoint} connected");

                // Get the users data stream
                var networkStream = connectedClient.GetStream();



                // "wait" until the request has new data
                while (networkStream.DataAvailable == false)
                    await Task.Delay(1);


                // If the recevied data contains atleast more than 3 characters (GET)
                if (connectedClient.Available > 3)
                {

                    // Allocate the necessary data 
                    byte[] bytes = new byte[connectedClient.Available];

                    // Read the data into the byte array
                    networkStream.Read(bytes, 0, bytes.Length);

                    // Get the data as a raw string
                    string rawData = Encoding.UTF8.GetString(bytes);

                    string[] splitData = rawData.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

                    // Get the requested url
                    var requestedUrl = rawData.Split(" ")[1].Split('/', StringSplitOptions.RemoveEmptyEntries);

                    // A string that holds the response that will be sent to the user
                    string responseString = "";



                    // Check if the request if a POST request
                    if (Regex.IsMatch(rawData, "^POST", RegexOptions.IgnoreCase))
                    {
                        // If the requested url begins with Account "controller"
                        if (requestedUrl[0] == "Account")
                        {
                            // Match the other part of the url

                            // If it's a login request
                            if (requestedUrl[1] == "Login")
                            {
                                // Get login details
                                var loginDetails = JsonSerializer.Deserialize<LoginRequest>(rawData.Split("\r\n\r\n")[1]);


                                // Check if login credentials are valid
                                if ((loginDetails.Username == "Password") && (loginDetails.Password == "Username"))
                                {

                                    // Build a token
                                    var token = BuildToken(new Dictionary<string, string>()
                                    {
                                        { "Username", loginDetails.Username },
                                        { "Roles", "Users" },
                                    }, JWT_TOKEN_KEY);

                                    // Add the token to the token list
                                    _validTokens.Add(token);

                                    // Create header response
                                    responseString = CreateResponseHeader(new string[]
                                    {
                                    $"HTTP/{HttpVersion.Version11} {(int)HttpStatusCode.OK} {HttpStatusCode.OK}",
                                    "Content-Type: application/json",
                                    },
                                    // Add login response with the new token
                                    JsonSerializer.Serialize(
                                     new LoginResponse()
                                     {
                                         Token = token,
                                     }));

                                }
                                else
                                {
                                    responseString = CreateResponseHeader(new string[]
                                    {
                                        $"HTTP/{HttpVersion.Version11} {(int)HttpStatusCode.Unauthorized} {HttpStatusCode.Unauthorized}",
                                        "Content-Type: Text/Plain",
                                    }, "Login failed, Username or password is invalid");
                                };
                            }
                            // If an authorized request was requested
                            else if (requestedUrl[1] == "Authorized")
                            {
                                // deserialize the request model
                                var authorizedRequest = JsonSerializer.Deserialize<AuthorizedRequest>(rawData.Split("\r\n\r\n")[1]);

                                // Validate authorization

                                // If token is valid
                                if (_validTokens.Contains(authorizedRequest.Token) == true)
                                {
                                    // Decode the token
                                    Token token = DecodeToken(authorizedRequest.Token);

                                    // Check if user is in the correct role
                                    var isInUserRole = token.Claims["Roles"]
                                    .Contains("Users");

                                    // if user is authorized
                                    if (isInUserRole)
                                    {
                                        // Return an OK response header
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
                            // If sign out was requested
                            else if (requestedUrl[1] == "SignOut")
                            {
                                // Deserialize the request
                                var signOutDetails = JsonSerializer.Deserialize<SignOutRequest>(rawData.Split("\r\n\r\n")[1]);

                                // If token was found, remove from list
                                if (_validTokens.Remove(signOutDetails.Token) == true)
                                {
                                    // Return a sign-out success response
                                    responseString = CreateResponseHeader(new string[]
                                    {
                                            $"HTTP/{HttpVersion.Version11} {(int)HttpStatusCode.OK} {HttpStatusCode.OK}",
                                    });
                                }
                                // If token wasn't found
                                else
                                {
                                    // Return response
                                    responseString = CreateResponseHeader(new string[]
                                    {
                                            $"HTTP/{HttpVersion.Version11} {(int)HttpStatusCode.Unauthorized} {HttpStatusCode.Unauthorized}",
                                            "Content-Type: Text/Plain",
                                    }, "Token is invalid");
                                };
                            }
                            // If url is mismatched return 404
                            else
                            {
                                responseString = CreateResponseHeader(new string[]
                                {
                                    $"HTTP/{HttpVersion.Version11} {(int)HttpStatusCode.NotFound} {HttpStatusCode.NotFound}",
                                });
                            };
                        }
                        // If url is mismatched return 404
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


                    // Close connection
                    connectedClient.Close();
                };
            };
        }


        /// <summary>
        /// A function that takes a list of strings that formats them to a valid HTTP response header
        /// </summary>
        /// <param name="headers"> A collection of headers </param>
        /// <param name="content"> The header's content if necessary </param>
        /// <returns></returns>
        private static string CreateResponseHeader(IEnumerable<string> headers, string content = "")
        {
            // Validation
            if ((headers is null) || (headers.Count() == 0))
                return null;

            // Holds a list of strings without needing to create a new string every time
            StringBuilder responseString = new StringBuilder();

            // Create an accumulative join for every header
            responseString.AppendJoin(END_OF_LINE, headers);

            // Append the \r\n to the the end of the request 
            responseString.Append(END_OF_LINE);
            responseString.Append(END_OF_LINE);

            // if the content parameter isn't null
            if (string.IsNullOrWhiteSpace(content) == false)
                // Add the content to the end of the string
                responseString.Append(content);

            // Return headers as a single string
            return responseString.ToString();
        }


        /// <summary>
        /// Creates a JWT token 
        /// </summary>
        /// <param name="payload"> The payload which will be added to the token </param>
        /// <param name="key"> A secret key used to encryt and decode the token</param>
        /// <returns></returns>
        private static string BuildToken(Dictionary<string, string> payload, string key)
        {
            // Encode the header
            string header = UrlSafeBase64(JsonSerializer.Serialize(new
            {
                typ = "JWT",
            }));

            // Encode the payload
            string jsonPayload = UrlSafeBase64(JsonSerializer.Serialize(payload));

            // Encode the key
            string hashedKey = UrlSafeBase64(key);

            // Return data as a JWT appended token
            return $"{header}.{jsonPayload}.{hashedKey}";
        }

        /// <summary>
        /// Decodes a token
        /// </summary>
        /// <param name="token"> The token to decode</param>
        /// <returns></returns>
        private static Token DecodeToken(string token)
        {
            string[] tokenParts = token.Split('.');

            // TODO: Token validation

            // decode token parts
            var decodedHeader = Base64UrlEncoder.Decode(tokenParts[0]);
            var decodedPayload = Base64UrlEncoder.Decode(tokenParts[1]);
            var decodedKey = Base64UrlEncoder.Decode(tokenParts[2]);

            // Return header, data, and key as a Token data structure
            return new Token()
            {
                RawToken = token,

                Header = JsonSerializer.Deserialize<Dictionary<string, string>>(decodedHeader),
                Claims = JsonSerializer.Deserialize<Dictionary<string, string>>(decodedPayload),
                Key = decodedKey,
            };
        }

        /// <summary>
        /// Takes a string and encodes it as a URL safe base64 strings
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static string UrlSafeBase64(string data)
        {
            // Convert string to UTF8 bytes, 
            // Encode as base64
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(data))
                // Replace "non-url" safe characters
                .Replace("=", "")
                .Replace("/", "")
                .Replace("_", "");
        }
    };
};
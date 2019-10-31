namespace CustomToken.Server
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using CustomToken.Core;

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
        /// A token handler, Hanles token creation, decode, and such
        /// </summary>
        private static TokenHandler _tokenHandler = new TokenHandler();

        private static Stopwatch _watcher = new Stopwatch();

        private static HttpReqeustHandler _httpReqeustHandler = new HttpReqeustHandler();

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

                // Holds the connected user's IP address
                var connectionIP = connectedClient.Client.RemoteEndPoint;

                // Start counting the request elapsed time
                _watcher.Restart();

                Console.WriteLine($"Client {connectionIP } connected");

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

                    // Create an Http request model
                    var request = _httpReqeustHandler.CreateRequest(rawData);

                    // A string that holds the response that will be sent to the user
                    string responseString = "";


                    // Check if the request if a POST request
                    if (request.Method == HttpMethod.Post.Method)
                    {
                        // If the requested url begins with Account "controller"
                        if (request.RequestedUrlSplit[0] == "Account")
                        {
                            // Match the other part of the url

                            // If it's a login request
                            if (request.RequestedUrlSplit[1] == "Login")
                            {
                                // Get login details
                                var loginDetails = await request.ReadContentAsJsonAsync<LoginRequest>();

                                // Check if login credentials are valid
                                if ((loginDetails.Username == "Password") && (loginDetails.Password == "Username"))
                                {

                                    // Build a token
                                    var token = _tokenHandler.BuildToken(new Dictionary<string, string>()
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
                            else if (request.RequestedUrlSplit[1] == "Authorized")
                            {
                                // deserialize the request model
                                var authorizedRequest = await request.ReadContentAsJsonAsync<AuthorizedRequest>();

                                // Validate authorization

                                // If token is valid
                                if (_validTokens.Contains(authorizedRequest.Token) == true)
                                {
                                    // Decode the token
                                    Token token = _tokenHandler.DecodeToken(authorizedRequest.Token);

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
                            else if (request.RequestedUrlSplit[1] == "SignOut")
                            {
                                // Deserialize the request
                                var signOutDetails = await request.ReadContentAsJsonAsync<SignOutRequest>();

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


                    // Stop coutning the request time 
                    _watcher.Stop();

                    // get elapsed time
                    long elapsedMs = _watcher.ElapsedMilliseconds;

                    Console.WriteLine($"Finished {connectionIP} request after {TimeSpan.FromMilliseconds(elapsedMs)}");
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

    };
};
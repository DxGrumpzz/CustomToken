namespace CustomToken.Client
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    using CustomToken.Core;

    public static class ClientProgram
    {

        /// <summary>
        /// The client's connection to the server
        /// </summary>
        private static HttpClient _client;
        
        /// <summary>
        /// A connection specifier
        /// </summary>
        private static ConnectionModel _connection = new ConnectionModel();

        /// <summary>
        /// The token that was received from the server
        /// </summary>
        private static string _token = "";


        private async static Task Main(string[] args)
        {

            Console.Title = "Client";

            // Initalize client
            _client = new HttpClient();


            Console.WriteLine("Press \"Enter\" to continue");

            Console.ReadLine();

            // Username and password
            const string username = "Password";
            const string password = "Username";

            // Create a login request
            var loginRequest = await _client.PostAsync($"{_connection.HttpUrl}/Account/Login",
                                new StringContent(JsonSerializer.Serialize(new LoginRequest()
                                {
                                    Username = username,
                                    Password = password,
                                }), Encoding.UTF8, "application/json"));

            // If login is succesfull
            if (loginRequest.IsSuccessStatusCode == true)
            {
                // Deserializer login response
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(await loginRequest.Content.ReadAsStringAsync());

                // Setup token
                _token = loginResponse.Token;

                // Log
                Console.WriteLine($"Succesfully logged in as {username}. \nReceived token: {_token}");
                Console.WriteLine($"Press enter to continue");
                Console.ReadLine();

                // Create "authorized" request
                var authorizedRequest = await _client.PostAsync($"{_connection.HttpUrl}/Account/Authorized",
                    new StringContent(JsonSerializer.Serialize(new AuthorizedRequest()
                    {
                        Token = _token,
                    }), Encoding.UTF8, "application/json"));

                // If authorized request is succesfull
                if (authorizedRequest.IsSuccessStatusCode == true)
                {
                    // log
                    Console.WriteLine($"Completed authorized request");
                    Console.WriteLine($"Press enter to continue");
                    Console.ReadLine();

                    // Create sign out request
                    var signOutRequest = await _client.PostAsync($"{_connection.HttpUrl}/Account/SignOut",
                       new StringContent(JsonSerializer.Serialize(new SignOutRequest()
                       {
                           Token = _token,
                       }), Encoding.UTF8, "application/json"));

                    // If signout was sucesfull
                    if (signOutRequest.IsSuccessStatusCode == true)
                    {
                        // Log
                        Console.WriteLine($"Signed out succesfully");
                        Console.WriteLine($"Press enter to continue");
                        Console.ReadLine();

                        // Attemp to create a new authorized request
                        var authorizedRequest2 = await _client.PostAsync($"{_connection.HttpUrl}/Account/Authorized",
                            new StringContent(JsonSerializer.Serialize(new AuthorizedRequest()
                            {
                                Token = _token,
                            }), Encoding.UTF8, "application/json"));

                        // If authorized request failed as expected
                        if (authorizedRequest2.IsSuccessStatusCode == false)
                        {
                            // Log that every thing we ok and exit
                            Console.WriteLine($"Authorized request failed succesfully");
                            Console.WriteLine($"Press enter to exit");
                            Console.ReadLine();
                        }
                        else
                        {
                            // log errorS
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Authorization request was a success when it should have failed");
                            Console.ResetColor();
                            Console.ReadLine();
                        };
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Sign out request failed");
                        Console.ResetColor();
                        Console.ReadLine();
                    };
                }
                // If not
                else
                {
                    // log
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Authorization request failed");
                    Console.ResetColor();
                    Console.ReadLine();
                };
            }
            else
            {
                // Display error
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Login failed");
                Console.ResetColor();
                Console.ReadLine();
            };
        }

    };
};
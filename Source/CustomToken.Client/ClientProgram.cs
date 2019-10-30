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
                Log($"Succesfully logged in as {username}. \nReceived token: {_token}\nPress enter to continue", stopUntilEnterIsHit: true);

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
                    Log($"Completed authorized request\nPress enter to continue", stopUntilEnterIsHit: true);

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
                        Log($"Signed out succesfully\nPress enter to continue", stopUntilEnterIsHit: true);


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
                            Log($"Authorized request failed succesfully\nPress enter to exit", stopUntilEnterIsHit: true);
                        }
                        else
                        {
                            // log errors
                            Log("Authorization request was a success when it should have failed", logColour: ConsoleColor.Red, stopUntilEnterIsHit: true);
                        };
                    }
                    else
                    {
                        Log("Sign out request failed", logColour: ConsoleColor.Red, stopUntilEnterIsHit: true);
                    };
                }
                // If not
                else
                {
                    Log("Authorization failed", logColour: ConsoleColor.Red, stopUntilEnterIsHit: true);
                };
            }
            else
            {
                // Display error
                Log("Login failed", logColour: ConsoleColor.Red, stopUntilEnterIsHit: true);
            };
        }

        /// <summary>
        /// Logs an error
        /// </summary>
        /// <param name="error"> The error to display </param>
        /// <param name="stopUntilEnterIsHit"> stop execution until enter is hit default is false </param>
        private static void Log(string error, ConsoleColor logColour = ConsoleColor.Gray, bool stopUntilEnterIsHit = false)
        {
            // Set foreground to red to indicate that something went wrong
            Console.ForegroundColor = logColour;

            // display error
            Console.WriteLine(error);

            // Reset forground color
            Console.ResetColor();

            // If needed will stop execution until enter is hit
            if (stopUntilEnterIsHit == true)
                Console.ReadLine();
        }

    };
};
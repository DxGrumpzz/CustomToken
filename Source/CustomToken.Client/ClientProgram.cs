namespace CustomToken.Client
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    using CustomToken.Core;

    public static class ClientProgram
    {

        private static HttpClient _client;
        private static ConnectionModel _connection = new ConnectionModel();

        private static string _token = "";

        private async static Task Main(string[] args)
        {

            Console.Title = "Client";

            _client = new HttpClient();


            Console.WriteLine("Press \"Enter\" to continue");

            Console.ReadLine();


            const string username = "Password";
            const string password = "Username";

            var loginRequest = await _client.PostAsync($"{_connection.HttpUrl}/Account/Login",
                                new StringContent(JsonSerializer.Serialize(new LoginRequest()
                                {
                                    Username = username,
                                    Password = password,
                                }), Encoding.UTF8, "application/json"));


            if (loginRequest.IsSuccessStatusCode == true)
            {
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(await loginRequest.Content.ReadAsStringAsync());

                _token = loginResponse.Token;

                Console.WriteLine($"Succesfully logged in as {username}. \nReceived token: {_token}");
                Console.WriteLine($"Press enter to continue");
                Console.ReadLine();


                var authorizedRequest = await _client.PostAsync($"{_connection.HttpUrl}/Account/Authorized",
                    new StringContent(JsonSerializer.Serialize(new AuthorizedRequest()
                    {
                        Token = _token,
                    }), Encoding.UTF8, "application/json"));


                if (authorizedRequest.IsSuccessStatusCode == true)
                {
                    Console.WriteLine($"Completed authorized request");
                    Console.WriteLine($"Press enter to continue");
                    Console.ReadLine();

                    var signOutRequest = await _client.PostAsync($"{_connection.HttpUrl}/Account/SignOut",
                       new StringContent(JsonSerializer.Serialize(new SignOutRequest()
                       {
                           Token = _token,
                       }), Encoding.UTF8, "application/json"));


                    if (signOutRequest.IsSuccessStatusCode == true)
                    {
                        Console.WriteLine($"Signed out succesfully");
                        Console.WriteLine($"Press enter to continue");
                        Console.ReadLine();


                        var authorizedRequest2 = await _client.PostAsync($"{_connection.HttpUrl}/Account/Authorized",
                            new StringContent(JsonSerializer.Serialize(new AuthorizedRequest()
                            {
                                Token = _token,
                            }), Encoding.UTF8, "application/json"));

                        if (authorizedRequest2.IsSuccessStatusCode == false)
                        {
                            Console.WriteLine($"Authorized request failed succesfully");
                            Console.WriteLine($"Press enter to exit");
                            Console.ReadLine();
                        }
                        else
                        {
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
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Authorization request failed");
                    Console.ResetColor();
                    Console.ReadLine();
                };
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Login failed");
                Console.ResetColor();
                Console.ReadLine();
            };
        }

    };
};
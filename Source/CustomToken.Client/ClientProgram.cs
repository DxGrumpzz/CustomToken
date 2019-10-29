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

                var authorizedRequest = await _client.PostAsync($"{_connection.HttpUrl}/Account/Authorized",
                    new StringContent(JsonSerializer.Serialize(new AuthorizedRequest()
                    {
                        Token = _token,
                    }), Encoding.UTF8, "application/json"));

                Debugger.Break();

                var signOutRequest = await _client.PostAsync($"{_connection.HttpUrl}/Account/SignOut",
                   new StringContent(JsonSerializer.Serialize(new SignOutRequest()
                   {
                       Token = _token,
                   }), Encoding.UTF8, "application/json"));

                Debugger.Break();


                var authorizedRequest2 = await _client.PostAsync($"{_connection.HttpUrl}/Account/Authorized",
                    new StringContent(JsonSerializer.Serialize(new AuthorizedRequest()
                    {
                        Token = _token,
                    }), Encoding.UTF8, "application/json"));

                Debugger.Break();

            };

            Debugger.Break();
        }

    };
};
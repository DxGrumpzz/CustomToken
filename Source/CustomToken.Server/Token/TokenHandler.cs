namespace CustomToken.Server
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    /// A class that handles all things token
    /// </summary>
    public class TokenHandler
    {

        /// <summary>
        /// Creates a JWT token 
        /// </summary>
        /// <param name="payload"> The payload which will be added to the token </param>
        /// <param name="key"> A secret key used to encryt and decode the token</param>
        /// <returns></returns>
        public string BuildToken(Dictionary<string, string> payload, string key)
        {
            // TODO: replace UrlSafeBase64 with Base64UrlEncoder.Encode

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
        public Token DecodeToken(string token)
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
        private string UrlSafeBase64(string data)
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
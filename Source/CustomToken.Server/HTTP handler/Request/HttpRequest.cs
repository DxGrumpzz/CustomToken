namespace CustomToken.Server
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Text.Json;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    public class HttpRequest
    {

        /// <summary>
        /// An HTTP standard header end of line break
        /// </summary>
        private const string END_OF_LINE = "\r\n";
        private const string CONTENT_LENGTH_STRING = "Content-Length";
        private const string CONTENT_TYPE_STRING = "Content-Type";


        public string RawData { get; }

        public string[] RawDataSplit { get; }

        public string Method { get; }

        public string RequestedUrl { get; }
        public string[] RequestedUrlSplit { get; }


        public int ContentLength { get; }

        public string ContentType { get; }

        public string RawContent { get; }


        public HttpRequest(string rawData)
        {
            RawData = rawData;
            RawDataSplit = rawData.Split(END_OF_LINE);


            var contentLength = RawDataSplit.FirstOrDefault(header => header.Contains(CONTENT_LENGTH_STRING, StringComparison.OrdinalIgnoreCase));
            var contentType = RawDataSplit.FirstOrDefault(header => header.Contains(CONTENT_TYPE_STRING, StringComparison.OrdinalIgnoreCase));

            Method = RawDataSplit.First().Split(" ")[0];
            RequestedUrl = RawDataSplit.First().Split(" ")[1];
            RequestedUrlSplit = RequestedUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (contentLength != null)
            {
                ContentLength = Convert.ToInt32(
                    contentLength.Split(':')[1]
                    .Replace(" ", ""));
            };

            if (contentType != null)
            {
                ContentType = contentLength.Split(':')[1]
                    .Replace(" ", "");
            };

            if (string.IsNullOrWhiteSpace(RawDataSplit.Last()) == false)
            {
                RawContent = RawDataSplit.Last();
            };
        }


        public T ReadContentAsJson<T>()
        {
            T result = JsonSerializer.Deserialize<T>(RawContent);
            return result;
        }

        public async Task<T> ReadContentAsJsonAsync<T>()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter streamWriter = new StreamWriter(stream))
                {
                    await streamWriter.WriteAsync(RawContent);

                    await streamWriter.FlushAsync();

                    stream.Position = 0;

                    T result = await JsonSerializer.DeserializeAsync<T>(stream);

                    return result;
                };
            };
        }
    };
};
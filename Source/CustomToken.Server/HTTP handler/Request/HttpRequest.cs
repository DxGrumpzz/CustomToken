namespace CustomToken.Server
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// A Model class that is responsible for holding data about an HTTP request
    /// </summary>
    public class HttpRequest
    {
        #region Constants

        /// <summary>
        /// An HTTP standard header end of line break
        /// </summary>
        private const string END_OF_LINE = "\r\n";

        /// <summary>
        /// A constant that specifies a header's content length property
        /// </summary>
        private const string CONTENT_LENGTH_STRING = "Content-Length";

        /// <summary>
        /// A constant that specifies a header's content length type
        /// </summary>
        private const string CONTENT_TYPE_STRING = "Content-Type";

        #endregion


        /// <summary>
        /// The raw HTTP request as a string
        /// </summary>
        public string RawData { get; }

        /// <summary>
        /// The HTTP request segmented and split
        /// </summary>
        public string[] RawDataSplit { get; }

        /// <summary>
        /// The HTTP method GET, POST, PUT, UPDATE, and so on
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// The requested url 
        /// </summary>
        public string RequestedUrl { get; }

        /// <summary>
        /// The Requested url split after every segment
        /// </summary>
        public string[] RequestedUrlSplit { get; }

        /// <summary>
        /// How much data the HTTP content contains
        /// </summary>
        public int ContentLength { get; }

        /// <summary>
        /// The type of content
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// The content held as a raw string
        /// </summary>
        public string RawContent { get; }



        public HttpRequest(string rawData)
        {
            // The receveid data as is
            RawData = rawData;
            // The data split after every break+newline character(s)
            RawDataSplit = rawData.Split(END_OF_LINE);

            // Check if a content length header is present
            var contentLength = RawDataSplit.FirstOrDefault(header => header.Contains(CONTENT_LENGTH_STRING, StringComparison.OrdinalIgnoreCase));

            // Check if a content type header is present
            var contentType = RawDataSplit.FirstOrDefault(header => header.Contains(CONTENT_TYPE_STRING, StringComparison.OrdinalIgnoreCase));


            // Get method type 
            Method = RawDataSplit.First().Split(" ")[0];

            // Get the requested url as a single string 
            RequestedUrl = RawDataSplit.First().Split(" ")[1];

            // Split the url for every /
            RequestedUrlSplit = RequestedUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // If request contains content length
            if (contentLength != null)
            {
                // Covnert the result to a string
                ContentLength = Convert.ToInt32(
                    // Split the header in 2 and get the 2nd result 
                    contentLength.Split(':')[1]
                    // Remove empty strings
                    .Replace(" ", ""));
            };

            // If request contains content type
            if (contentType != null)
            {
                // Split header in 2 and get the second result
                ContentType = contentLength.Split(':')[1]
                    // Replace empty strings
                    .Replace(" ", "");
            };

            // If the end of the request isn't empty, meaning there is content
            if (string.IsNullOrWhiteSpace(RawDataSplit.Last()) == false)
            {
                // Get that content
                RawContent = RawDataSplit.Last();
            };
        }


        /// <summary>
        /// Read the content as a Json string
        /// </summary>
        /// <typeparam name="T"> The type of object to convert the data to </typeparam>
        /// <returns></returns>
        public T ReadContentAsJson<T>()
        {
            // Deserializer content to type T
            T result = JsonSerializer.Deserialize<T>(RawContent);
            return result;
        }

        /// <summary>
        /// Read the content as a Json string asynchronously
        /// </summary>
        /// <typeparam name="T"> The type of object to convert the data to </typeparam>
        /// <returns></returns>
        public async Task<T> ReadContentAsJsonAsync<T>()
        {
            // Because deserializing asynchronously requires a stream
            // We need a Memory stream to hold the json string as memory bytes

            // Create a memory stream 
            using (MemoryStream stream = new MemoryStream())
            {
                // Create a StreamWriter which will write to the memory stream
                using (StreamWriter streamWriter = new StreamWriter(stream))
                {
                    // Write the data on the stream
                    await streamWriter.WriteAsync(RawContent);

                    // Flush the written buffer into the stream
                    await streamWriter.FlushAsync();

                    // Set the Memory stream "cursor" position to the beggining of the data
                    stream.Position = 0;

                    // Deserialize to type T
                    T result = await JsonSerializer.DeserializeAsync<T>(stream);

                    return result;
                };
            };
        }

    };
};
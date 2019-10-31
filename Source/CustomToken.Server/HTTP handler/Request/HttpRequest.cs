namespace CustomToken.Server
{
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// A Model class that is responsible for holding data about an HTTP request
    /// </summary>
    public class HttpRequest
    {

        /// <summary>
        /// The raw HTTP request as a string
        /// </summary>
        public string RawData { get; set; }

        /// <summary>
        /// The HTTP request segmented and split
        /// </summary>
        public string[] RawDataSplit { get; set; }


        /// <summary>
        /// The HTTP method GET, POST, PUT, UPDATE, and so on
        /// </summary>
        public string Method { get; set; }


        /// <summary>
        /// The requested url 
        /// </summary>
        public string RequestedUrl { get; set; }

        /// <summary>
        /// The Requested url split after every segment
        /// </summary>
        public string[] RequestedUrlSplit { get; set; }


        /// <summary>
        /// How much data the HTTP content contains
        /// </summary>
        public int ContentLength { get; set; }

        /// <summary>
        /// The type of content
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The content held as a raw string
        /// </summary>
        public string RawContent { get; set; }



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
            // Create a memory stream and convert the RawContent to a UTF8 encoded bytes
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(RawContent)))
            {
                // Deserialize to type T
                T result = await JsonSerializer.DeserializeAsync<T>(stream);
                return result;
            };
        }
    }
};

namespace CustomToken.Server
{
    using System;
    using System.Linq;

    /// <summary>
    /// A class that is responsible for handling incoming HTTP requests
    /// </summary>
    public class HttpReqeustHandler
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
        /// Creates a  new <see cref="HttpRequest"/> and fills it with data
        /// </summary>
        /// <param name="rawRequestString"> The raw header/request </param>
        /// <returns></returns>
        public HttpRequest CreateRequest(string rawRequestString)
        {

            // The receveid data as is
            string rawData = rawRequestString;
          
            // The data split after every break+newline character(s)
            var rawDataSplit = rawData.Split(END_OF_LINE);



            // Get HTTP method type 
            var method = rawDataSplit.First().Split(" ")[0];



            // Get the requested url as a single string 
            var requestedUrl = rawDataSplit.First().Split(" ")[1];

            // Split the url for every /
            var requestedUrlSplit = requestedUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);



            // How much data is present in the content
            int contentLength = 0;

            // The type of the content
            string contentType = string.Empty;

            // The content as a raw string
            string rawContent = string.Empty;


            // Check if a content length header is present
            var contentLengthHeader = rawDataSplit.FirstOrDefault(header => header.Contains(CONTENT_LENGTH_STRING, StringComparison.OrdinalIgnoreCase));

            // Check if a content type header is present
            var contentTypeHeader = rawDataSplit.FirstOrDefault(header => header.Contains(CONTENT_TYPE_STRING, StringComparison.OrdinalIgnoreCase));


            // If request contains content length
            if (contentLengthHeader != null)
            {
                // Covnert the result to a string
                contentLength = Convert.ToInt32(
                    // Split the header in 2 and get the 2nd result 
                    contentLengthHeader.Split(':')[1]
                    // Remove empty strings
                    .Replace(" ", ""));
            };

            // If request contains content type
            if (contentTypeHeader != null)
            {
                // Split header in 2 and get the second result
                contentType = contentTypeHeader.Split(':')[1]
                    // Replace empty strings
                    .Replace(" ", "");
            };

            // If the end of the request isn't empty, meaning there is content
            if (string.IsNullOrWhiteSpace(rawDataSplit.Last()) == false)
            {
                // Get that content
                rawContent = rawDataSplit.Last();
            };



            return new HttpRequest()
            {
                RawData = rawData,
                RawDataSplit = rawDataSplit,

                Method = method,

                RequestedUrl = requestedUrl,
                RequestedUrlSplit = requestedUrlSplit,

                ContentLength = contentLength,
                ContentType = contentType,
                RawContent = rawContent,
            };
        }

    };
};
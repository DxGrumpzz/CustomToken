namespace CustomToken.Server
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A handler class that is responsible for HTTP responses
    /// </summary>
    public class HttpResponseHandler
    {

        /// <summary>
        /// An HTTP standard header end of line break
        /// </summary>
        private const string END_OF_LINE = "\r\n";


        /// <summary>
        /// A function that takes a list of strings that formats them to a valid HTTP response header
        /// </summary>
        /// <param name="headers"> A collection of headers </param>
        /// <param name="content"> The header's content if necessary </param>
        /// <returns></returns>
        public string CreateResponseHeader(IEnumerable<string> headers, string content = "")
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

namespace CustomToken.Server
{
    /// <summary>
    /// A class that is responsible for handling incoming HTTP requests
    /// </summary>
    public class HttpReqeustHandler
    {
        /// <summary>
        /// Creates a  new <see cref="HttpRequest"/> and fills it with data
        /// </summary>
        /// <param name="rawRequestString"> The raw header/request </param>
        /// <returns></returns>
        public HttpRequest CreateRequest(string rawRequestString)
        {
            return new HttpRequest(rawRequestString);
        }

    };
};

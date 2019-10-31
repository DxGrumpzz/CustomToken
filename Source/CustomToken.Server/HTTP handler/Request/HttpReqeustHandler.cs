namespace CustomToken.Server
{
    /// <summary>
    /// 
    /// </summary>
    public class HttpReqeustHandler
    {

        public HttpRequest CreateRequest(string rawRequestString)
        {
            return new HttpRequest(rawRequestString);
        }

    };
};

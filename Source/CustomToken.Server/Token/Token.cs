namespace CustomToken.Server
{
    using System.Collections.Generic;

    /// <summary>
    /// A Token data structure that contains "formatted" information about a received/sent token
    /// </summary>
    public class Token
    {
        /// <summary>
        /// The token as is, a raw unecrypted string
        /// </summary>
        public string RawToken { get; set; }

        /// <summary>
        /// Header information that is associated with the token
        /// </summary>
        public Dictionary<string, string> Header { get; set; }

        /// <summary>
        /// The payload/data that came with this token
        /// </summary>
        public Dictionary<string, string> Claims { get; set; }

        /// <summary>
        /// The encrypted key
        /// </summary>
        public string Key { get; set; }
    }
};

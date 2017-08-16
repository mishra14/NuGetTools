using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitLogger
{
    public class Commit
    {
        public string Sha { get; set; }
        public string Author { get; set; }
        public string Message { get; set; }
        public string Link { get; set; }
        public ISet<Tuple<int, string>> Issues { get; set; }
        public Tuple<int, string> PR { get; set; }
        public string SanitizedMessage {
            get
            {
                if (Message != null)
                {
                    return Message
                        .Replace(",", " ")
                        .Replace("\r", " ")
                        .Replace("\n", " ");
                }
                else
                {
                    return Message;
                }
            }
        }

        public Commit(JToken token)
        {
            var commitjObject = token.Value<JObject>("commit");
            var authorjObject = commitjObject.Value<JObject>("author");
            var sha = token.Value<string>("sha");
            var author = authorjObject.Value<string>("name");
            var message = commitjObject.Value<string>("message");
            var link = token.Value<string>("html_url");

            Sha = sha;
            Author = author;
            Link = link;
            Message = message;

        }
    }
}

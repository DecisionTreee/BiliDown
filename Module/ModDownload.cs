using System.Net;

namespace BiliDown.Module
{
    internal class ModDownload
    {
        public static string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36 Edge/108.0.1462.54";
        public static string origin = "https://www.bilibili.com";

        public static async Task<Stream> Download(string url, string referer, string host)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;

            request.ProtocolVersion = HttpVersion.Version11;
            request.Method = "GET";
            request.UserAgent = userAgent;
            request.Referer = referer;
            request.Host = host;
            request.Accept = "*/*";
            request.ContentType = "text/plain";

            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "Windows");
            request.Headers.Add("Sec-Fetch-Site", "cross-site");
            request.Headers.Add("Sec-Fetch-Mode", "cors");
            request.Headers.Add("Sec-Fetch-Dest", "empty");
            request.Headers.Add("Accept-Encoding", "identity");
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en-US;q=0.8,en;q=0.7,en-GB;q=0.6,en-GB-oxendict;q=0.5");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Range", "0-");

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream stream = response.GetResponseStream();

            return stream;
        }
    }
}

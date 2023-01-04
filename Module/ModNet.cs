using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace BiliDownUI.Module
{
    internal class ModNet
    {
        public static Stream Get(string url, Version version, string accept = null, string referer = null, string contentType = null, string userAgent = null, string host = "", bool keepAlive = true, WebHeaderCollection headers = null)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            if (headers != null)
            {
                request.Headers = headers;
            }
            request.Method = "GET";
            request.ProtocolVersion = version;
            request.Accept = accept;
            request.KeepAlive = keepAlive;
            request.Referer = referer;
            request.ContentType = contentType;
            request.UserAgent = userAgent;
            request.Date = DateTime.Now;
            if (host != "")
            {
                request.Host = host;
            }

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream stream = ModUtils.UnGZip(response);

            return stream;
        }

        public static Stream Post(string url, Version version, string accept = null, string referer = null, string contentType = null, string userAgent = null, string host = "", bool keepAlive = true, Dictionary<string, string> body = null)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "POST";
            request.ProtocolVersion = version;
            request.Accept = accept;
            request.KeepAlive = keepAlive;
            request.Referer = referer;
            request.ContentType = contentType;
            request.UserAgent = userAgent;
            request.Date = DateTime.Now;
            if (host != "")
            {
                request.Host = host;
            }
            if (body != null)
            {
                byte[] requestData = ModUtils.BuildPostRequestBody(body);
                request.ContentLength = requestData.Length;
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(requestData, 0, requestData.Length);
                    requestStream.Close();
                }
            }

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream stream = ModUtils.UnGZip(response);

            return stream;
        }
    }
}

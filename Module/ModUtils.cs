using Newtonsoft.Json.Linq;
using SkiaSharp;
using SkiaSharp.QrCode;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Web;

namespace BiliDown.Module
{
    internal class ModUtils
    {
        public static string getLoginUrl = "http://passport.bilibili.com/qrcode/getLoginUrl";
        public static string getLoginInfoUrl = "http://passport.bilibili.com/qrcode/getLoginInfo";

        public static void Stream2File(Stream stream, string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
            MemoryStream ms = new MemoryStream();
            byte[] buffer = new byte[1024];

            int i;
            while ((i = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, i);
            }
            byte[] bytes = ms.ToArray();
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(bytes);

            bw.Close();
            ms.Close();
            fs.Close();
            stream.Close();
        }

        public static string UnGZip(HttpWebResponse response)
        {
            string content = "";
            if (response.ContentEncoding.ToLower() == "gzip")
            {
                using (Stream stream = response.GetResponseStream())
                {
                    using (GZipStream gzs = new GZipStream(stream, CompressionMode.Decompress))
                    {
                        using (StreamReader sr = new StreamReader(gzs, Encoding.Default))
                        {
                            content = sr.ReadToEnd();
                        }
                    }
                }
            }
            else
            {
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(stream, Encoding.Default))
                    {
                        content = sr.ReadToEnd();
                    }
                }
            }

            return content;
        }

        public static List<string> GetCorrectUrl(string json)
        {
            string videoUrl = "";
            string audioUrl = "";

            JObject root = JObject.Parse(json);
            JToken dataToken;
            root.TryGetValue("data", out dataToken);
            JObject dataObj = dataToken.ToObject<JObject>();
            JToken dashToken;
            dataObj.TryGetValue("dash", out dashToken);
            JObject dashObj = dashToken.ToObject<JObject>();
            JToken videoToken;
            dashObj.TryGetValue("video", out videoToken);
            JToken audioToken;
            dashObj.TryGetValue("audio", out audioToken);

            List<JToken> videoDatas = videoToken.ToList();
            List<JToken> audioDatas = audioToken.ToList();

            JObject selectVideoObj = videoDatas[0].ToObject<JObject>();
            JToken selectVideoToken;
            selectVideoObj.TryGetValue("baseUrl", out selectVideoToken);
            JObject selectAudioObj = audioDatas[0].ToObject<JObject>();
            JToken selectAudioToken;
            selectAudioObj.TryGetValue("baseUrl", out selectAudioToken);

            videoUrl = selectVideoToken.ToObject<string>();
            audioUrl = selectAudioToken.ToObject<string>();
            List<string> result = new List<string>() { videoUrl, audioUrl };

            return result;
        }

        public static string GetPlayInfoFromHtml(string html)
        {
            string matchText = "window.__playinfo__=";
            int i1 = html.IndexOf(matchText);
            int i2 = html.IndexOf("\"session\":\"");
            int i3 = html.IndexOf("}", i2);

            string json = html.Substring(i1 + matchText.Length, i3 - (i1 + matchText.Length) + 1);

            return json;
        }

        public static void CombineVideo()
        {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg.exe";
            ffmpeg.StartInfo.Arguments = "-i temp/video.m4s -i temp/audio.m4s -codec copy video.mp4";
            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.Start();
            ffmpeg.WaitForExit();
        }

        public static string GenerateLoginQRCode()
        {
            HttpWebRequest request = WebRequest.Create(getLoginUrl) as HttpWebRequest;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream stream = response.GetResponseStream();
            StreamReader sr = new StreamReader(stream);
            string json = sr.ReadToEnd();

            JObject rootObj = JObject.Parse(json);
            JToken dataToken;
            rootObj.TryGetValue("data", out dataToken);
            JObject dataObj = dataToken.ToObject<JObject>();
            JToken urlToken;
            JToken oauthKeyToken;
            dataObj.TryGetValue("url", out urlToken);
            dataObj.TryGetValue("oauthKey", out oauthKeyToken);
            string qrCodeUrl = urlToken.ToObject<string>();
            string oauthKey = oauthKeyToken.ToObject<string>();

            QRCodeGenerator generator = new QRCodeGenerator();
            QRCodeData qrCodeData = generator.CreateQrCode(qrCodeUrl, ECCLevel.H);

            SKImageInfo info = new SKImageInfo(512, 512);
            using (SKSurface surface = SKSurface.Create(info))
            {
                SKCanvas canvas = surface.Canvas;
                canvas.Render(qrCodeData, info.Width, info.Height);
                using (SKImage image = surface.Snapshot())
                using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (FileStream fs = File.Create("temp/QRCode.png"))
                {
                    data.SaveTo(fs);
                }
            }

            Process openQRCode = new Process();
            openQRCode.StartInfo.FileName = "temp\\QRCode.png";
            openQRCode.StartInfo.Arguments = "rundl132.exe C:/windows/system32/shimgvw.dll";
            openQRCode.StartInfo.UseShellExecute = true;
            openQRCode.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            openQRCode.Start();

            return oauthKey;
        }

        public static string CheckLoginStatus(string oauthKey)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>
            {
                { "oauthKey", oauthKey }
            };

            HttpWebRequest request = WebRequest.Create(getLoginInfoUrl) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            StringBuilder builder = new StringBuilder();
            int i = 0;
            foreach (var item in dic)
            {
                if (i > 0)
                {
                    builder.Append("&");
                }
                builder.AppendFormat("{0}={1}", item.Key, item.Value);
                i++;
            }
            byte[] requestData = Encoding.UTF8.GetBytes(builder.ToString());
            request.ContentLength = requestData.Length;
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(requestData, 0, requestData.Length);
                requestStream.Close();
            }

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream stream = response.GetResponseStream();
            StreamReader sr = new StreamReader(stream);
            string json = sr.ReadToEnd();
            JObject rootObj = JObject.Parse(json);
            JToken dataToken;
            rootObj.TryGetValue("data", out dataToken);
            JObject dataObj = dataToken.ToObject<JObject>();
            JToken urlToken;
            dataObj.TryGetValue("url", out urlToken);
            string cookieUrl = urlToken.ToObject<string>();

            Uri uri = new Uri(cookieUrl);
            NameValueCollection collection = HttpUtility.ParseQueryString(uri.Query);
            string sessdata = collection["SESSDATA"];

            return sessdata;
        }

        public static void Init()
        {
            if (File.Exists("temp/QRCode.png"))
            {
                File.Delete("temp/QRCode.png");
            }
            if (File.Exists("video.mp4"))
            {
                File.Delete("video.mp4");
            }
            if (File.Exists("temp/video.m4s"))
            {
                File.Delete("temp/video.m4s");
            }
            if (File.Exists("temp/audio.m4s"))
            {
                File.Delete("temp/audio.m4s");
            }
        }

        public static string GetHtml(string url, string cookie)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Headers.Add("cookie", cookie);
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            string html = UnGZip(response);

            return html;
        }

        public static string Login()
        {
            string cookie = "SESSDATA=";

            Console.WriteLine("请在180秒内打开B站手机版扫码登录(登录后按任意键继续)");
            string oauthKey = GenerateLoginQRCode();
            if (Console.ReadLine() != null)
            {
                string value = CheckLoginStatus(oauthKey);
                if (value == null)
                {
                    Console.WriteLine("抓取到的值为空，请确认已经登录再继续，下面执行未登录方法");
                }
                else
                {
                    cookie += value;
                }

                Console.WriteLine($"抓取到Sessdata为: {cookie}，是否保存cookie以便下次登录(Y/N)");
                if (Console.ReadLine().Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    File.WriteAllText("cookie.txt", cookie);
                }
            }

            return cookie;
        }
    }
}

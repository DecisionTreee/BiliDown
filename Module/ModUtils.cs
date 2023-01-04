using Newtonsoft.Json.Linq;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media.Imaging;

namespace BiliDownUI.Module
{
    internal class ModUtils
    {
        private const string getLoginUrl = "http://passport.bilibili.com/qrcode/getLoginUrl";
        private const string getLoginInfoUrl = "http://passport.bilibili.com/qrcode/getLoginInfo";
        private const string getUserDataUrl = "https://api.bilibili.com/x/space/myinfo?jsonp=jsonp";
        private const string getVideoDataUrl = "https://api.bilibili.com/x/web-interface/view?bvid=";

        public static Stream UnGZip(HttpWebResponse response)
        {
            if (response.ContentEncoding.Equals("gzip", StringComparison.OrdinalIgnoreCase))
            {
                Stream stream = response.GetResponseStream();
                GZipStream gzs = new GZipStream(stream, CompressionMode.Decompress);
                StreamReader sr = new StreamReader(gzs, Encoding.Default);
                return sr.BaseStream;
            }
            else
            {
                return response.GetResponseStream();
            }
        }

        public static byte[] BuildPostRequestBody(Dictionary<string, string> body)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            foreach (var item in body)
            {
                if (i > 0)
                {
                    sb.Append("&");
                }
                sb.AppendFormat("{0}={1}", item.Key, item.Value);
                i++;
            }

            byte[] data = Encoding.UTF8.GetBytes(sb.ToString());

            return data;
        }

        public static BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            byte[] bytes = ms.GetBuffer();
            ms.Close();
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new MemoryStream(bytes);
            image.EndInit();

            return image;
        }

        public static UserData GetUserData(string sessData)
        {
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add("cookie", $"{sessData}");

            Stream stream = ModNet.Get(getUserDataUrl, HttpVersion.Version11, headers: headers);
            StreamReader sr = new StreamReader(stream);
            string json = sr.ReadToEnd();

            JObject rootObj = JObject.Parse(json);
            JToken dataToken;
            rootObj.TryGetValue("data", out dataToken);
            JObject dataObj = dataToken.ToObject<JObject>();
            JToken faceToken;
            JToken midToken;
            dataObj.TryGetValue("face", out faceToken);
            dataObj.TryGetValue("mid", out midToken);
            string avatarUrl = faceToken.ToObject<string>();
            string uid = midToken.ToObject<string>();

            Stream avatarStream = ModNet.Get(avatarUrl, HttpVersion.Version11);
            Bitmap avatar = new Bitmap(avatarStream);

            return new UserData(avatar, uid);
        }

        public static QRData GenerateLoginQRCode()
        {
            Stream stream = ModNet.Get(getLoginUrl, HttpVersion.Version11);
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

            Bitmap qrCode = ModQRCodeGenerator.Generate(qrCodeUrl);

            return new QRData(qrCode, oauthKey);
        }

        public static async Task<string> CheckLoginStatus(string oauthKey)
        {
            string sessdata = await Task.Run(() => _CheckLoginStatus(oauthKey));

            return $"SESSDATA={sessdata}";
        }

        public static string _CheckLoginStatus(string oauthKey)
        {
            bool status = false;
            JObject rootObj = null;

            while (!status)
            {
                Dictionary<string, string> body = new Dictionary<string, string>
                {
                    { "oauthKey", oauthKey }
                };
                Stream stream = ModNet.Post(getLoginInfoUrl, HttpVersion.Version11, contentType: "application/x-www-form-urlencoded", body: body);
                StreamReader sr = new StreamReader(stream);
                string json = sr.ReadToEnd();

                rootObj = JObject.Parse(json);
                JToken statusToken;
                rootObj.TryGetValue("status", out statusToken);
                status = statusToken.ToObject<bool>();

                Thread.Sleep(4000);
            }

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

        public static string GetHtml(string url, string sessData)
        {
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add("cookie", sessData);
            Stream stream = ModNet.Get(url, HttpVersion.Version11, headers: headers);
            StreamReader sr = new StreamReader(stream);
            string html = sr.ReadToEnd();

            return html;
        }

        public static VideoData GetVideoData(string url, string playInfo)
        {
            string localPath = new Uri(url).LocalPath;
            string bvid = localPath.Replace("/", "");
            bvid = bvid.Replace("video", "");

            VideoData videoData = new VideoData();

            CurrectUrlData urls = GetCurrectVideoUrl(playInfo);
            videoData.videoQuality = urls.videoUrls;
            videoData.audioQuality = urls.audioUrls;
            videoData.qualityIdDic = urls.qualityIdDic;

            Stream stream = ModNet.Get($"{getVideoDataUrl}{bvid}", HttpVersion.Version11);
            StreamReader jsonSR = new StreamReader(stream);
            string json = jsonSR.ReadToEnd();

            JObject rootObj = JObject.Parse(json);
            JToken dataToken;
            rootObj.TryGetValue("data", out dataToken);

            JObject dataObj = dataToken.ToObject<JObject>();
            JToken bvidToken;
            JToken aidToken;
            JToken tNameToken;
            JToken copyRightToken;
            JToken picToken;
            JToken titleToken;
            JToken pubDateToken;
            JToken descToken;
            dataObj.TryGetValue("bvid", out bvidToken);
            dataObj.TryGetValue("aid", out aidToken);
            dataObj.TryGetValue("tname", out tNameToken);
            dataObj.TryGetValue("copyright", out copyRightToken);
            dataObj.TryGetValue("pic", out picToken);
            dataObj.TryGetValue("title", out titleToken);
            dataObj.TryGetValue("pubdate", out pubDateToken);
            dataObj.TryGetValue("desc", out descToken);

            videoData.bvid = bvidToken.ToObject<string>();
            videoData.aid = aidToken.ToObject<string>();
            videoData.tName = tNameToken.ToObject<string>();
            videoData.copyRight = copyRightToken.ToObject<string>();

            Stream picStream = ModNet.Get(picToken.ToObject<string>(), HttpVersion.Version11);
            videoData.pic = new Bitmap(picStream); 

            videoData.title = titleToken.ToObject<string>();
            ulong unixTimeStamp = pubDateToken.ToObject<ulong>();
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            DateTime pubTime = startTime.AddSeconds(unixTimeStamp);
            videoData.pubDate = pubTime.ToString("yyyy-MM-dd HH:mm:ss");

            videoData.desc = descToken.ToObject<string>();

            JToken ownerToken;
            dataObj.TryGetValue("owner", out ownerToken);
            JObject ownerObj = ownerToken.ToObject<JObject>();
            JToken ownerMidToken;
            JToken ownerNameToken;
            JToken ownerFaceToken;
            ownerObj.TryGetValue("mid", out ownerMidToken);
            ownerObj.TryGetValue("name", out ownerNameToken);
            ownerObj.TryGetValue("face", out ownerFaceToken);

            videoData.ownerUid = ownerMidToken.ToObject<string>();
            videoData.ownerName = ownerNameToken.ToObject<string>();

            Stream avatarStream = ModNet.Get(ownerFaceToken.ToObject<string>(), HttpVersion.Version11);
            videoData.ownerAvatar = new Bitmap(avatarStream);

            JToken statToken;
            dataObj.TryGetValue("stat", out statToken);
            JObject statObj = statToken.ToObject<JObject>();
            JToken viewToken;
            JToken danmakuToken;
            JToken replyToken;
            JToken favoriteToken;
            JToken coinToken;
            JToken shareToken;
            statObj.TryGetValue("view", out viewToken);
            statObj.TryGetValue("danmaku", out danmakuToken);
            statObj.TryGetValue("reply", out replyToken);
            statObj.TryGetValue("favorite", out favoriteToken);
            statObj.TryGetValue("coin", out coinToken);
            statObj.TryGetValue("share", out shareToken);

            videoData.view = viewToken.ToObject<string>();
            videoData.danmaku = danmakuToken.ToObject<string>();
            videoData.reply = replyToken.ToObject<string>();
            videoData.favorite = favoriteToken.ToObject<string>();
            videoData.coin = coinToken.ToObject<string>();
            videoData.share = shareToken.ToObject<string>();

            return videoData;
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

        public static CurrectUrlData GetCurrectVideoUrl(string playInfo)
        {
            JObject rootObj = JObject.Parse(playInfo);
            JToken dataToken;
            rootObj.TryGetValue("data", out dataToken);
            JObject dataObj = dataToken.ToObject<JObject>();

            JToken acceptDescriptionToken;
            JToken acceptQualityToken;
            dataObj.TryGetValue("accept_description", out acceptDescriptionToken);
            dataObj.TryGetValue("accept_quality", out acceptQualityToken);
            string[] acceptDescription = acceptDescriptionToken.ToObject<string[]>();
            int[] acceptQuality = acceptQualityToken.ToObject<int[]>();

            Dictionary<string, int> qualityIdDic = new Dictionary<string, int>();
            for (int i = 0; i < acceptDescription.Length; i++)
            {
                qualityIdDic.Add(acceptDescription[i].Replace(" ", ""), acceptQuality[i]);
            }

            JToken dashToken;
            dataObj.TryGetValue("dash", out dashToken);
            JObject dashObj = dashToken.ToObject<JObject>();

            JToken videoToken;
            dashObj.TryGetValue("video", out videoToken);
            JToken audioToken;
            dashObj.TryGetValue("audio", out audioToken);

            List<JToken> videoDatas = videoToken.ToList();
            List<JToken> audioDatas = audioToken.ToList();

            JToken bestVideoIdToken;
            videoDatas[0].ToObject<JObject>().TryGetValue("id", out bestVideoIdToken);
            int bestVideoQualityId = bestVideoIdToken.ToObject<int>();

            foreach(var item in qualityIdDic)
            {
                if (item.Value > bestVideoQualityId)
                {
                    qualityIdDic.Remove(item.Key);
                }
            }

            List<Quality> videoUrls = new List<Quality>();
            List<Quality> audioUrls = new List<Quality>();
            for (int i = 0; i < videoDatas.Count; i++)
            {
                JToken idToken;
                JToken codecsToken;
                JToken baseUrlToken;
                JToken backupUrlToken;
                JObject videoDataObj = videoDatas[i].ToObject<JObject>();
                videoDataObj.TryGetValue("id", out idToken);
                videoDataObj.TryGetValue("codecs", out codecsToken);
                videoDataObj.TryGetValue("baseUrl", out baseUrlToken);
                videoDataObj.TryGetValue("backupUrl", out backupUrlToken);

                videoUrls.Add(new Quality(idToken.ToObject<int>(), codecsToken.ToObject<string>(), baseUrlToken.ToObject<string>(), backupUrlToken[0].ToObject<string>()));
            }
            for (int i = 0; i < audioDatas.Count; i++)
            {
                JToken idToken;
                JToken codecsToken;
                JToken baseUrlToken;
                JToken backupUrlToken;
                JObject audioDataObj = audioDatas[i].ToObject<JObject>();
                audioDataObj.TryGetValue("id", out idToken);
                audioDataObj.TryGetValue("codecs", out codecsToken);
                audioDataObj.TryGetValue("baseUrl", out baseUrlToken);
                audioDataObj.TryGetValue("backupUrl", out backupUrlToken);

                audioUrls.Add(new Quality(idToken.ToObject<int>(), codecsToken.ToObject<string>(), baseUrlToken.ToObject<string>(), backupUrlToken[0].ToObject<string>()));
            }

            return new CurrectUrlData(videoUrls, audioUrls, qualityIdDic);
        }

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

        public static void CombineVideo(string filename)
        {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg.exe";
            ffmpeg.StartInfo.Arguments = $"-i temp\\$video.m4s -i temp\\$audio.m4s -codec copy \"download\\{filename}\"";
            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.Start();
            ffmpeg.WaitForExit();

            File.Delete("temp/$video.m4s");
            File.Delete("temp/$audio.m4s");
        }

        public static string GetOkString(string content)
        {
            StringBuilder sb = new StringBuilder(content);
            foreach (char rInvalidChar in Path.GetInvalidFileNameChars())
            {
                sb.Replace(rInvalidChar.ToString(), "");
            }

            return sb.ToString().Trim();
        }
    }

    public class QRData
    {
        public Bitmap qrCode;
        public string oauthKey;

        public QRData(Bitmap _qrCode, string _oauthKey)
        {
            qrCode = _qrCode;
            oauthKey = _oauthKey;
        }
    }

    public class UserData
    {
        public Bitmap avatar;
        public string uid;

        public UserData(Bitmap _avatar, string _uid)
        {
            avatar = _avatar;
            uid = _uid;
        }
    }

    public class VideoData
    {
        public string aid;
        public string bvid;
        public string copyRight;

        public string tName;
        public Bitmap pic;
        public string title;
        public string desc;
        public string pubDate;

        public string ownerUid;
        public string ownerName;
        public Bitmap ownerAvatar;

        public string view;
        public string danmaku;
        public string reply;
        public string favorite;
        public string coin;
        public string share;

        public List<Quality> videoQuality;
        public List<Quality> audioQuality;
        public Dictionary<string, int> qualityIdDic;
    }

    public class Quality
    {
        public int qualityId;
        public string code;
        public string baseUrl;
        public string backupUrl;

        public Quality(int qualityId, string code, string baseUrl, string backupUrl)
        {
            this.qualityId = qualityId;
            this.code = code;
            this.baseUrl = baseUrl;
            this.backupUrl = backupUrl;
        }
    }

    public class CurrectUrlData
    {
        public List<Quality> videoUrls;
        public List<Quality> audioUrls;
        public Dictionary<string, int> qualityIdDic;

        public CurrectUrlData(List<Quality> videoUrls, List<Quality> audioUrls, Dictionary<string, int> qualityIdDic)
        {
            this.videoUrls = videoUrls;
            this.audioUrls = audioUrls;
            this.qualityIdDic = qualityIdDic;
        }
    }
}

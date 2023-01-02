using BiliDown.Module;

namespace BiliDown
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                ModUtils.Init();
                Directory.CreateDirectory("temp");
                Console.WriteLine("创建文件夹temp");

                string cookie = "SESSDATA=";
                Console.WriteLine("是否登录(Y/N)");
                if (Console.ReadLine().Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    if (!File.Exists("cookie.txt"))
                    {
                        cookie = ModUtils.Login();
                    }
                    else
                    {
                        Console.WriteLine("检测到已经保存的cookie，是否使用(Y/N)(如果发生不能下载1080p视频的问题可能是cookie失效)");
                        if (Console.ReadLine().Equals("Y", StringComparison.OrdinalIgnoreCase))
                        {
                            cookie = File.ReadAllText("cookie.txt");
                        }
                        else
                        {
                            cookie = ModUtils.Login();
                        }
                    }
                }

                Console.WriteLine("输入地址(形如 https://www.bilibili.com/video/BV1mG411K79F)");
                string url = Console.ReadLine();

                string html = ModUtils.GetHtml(url, cookie);
                Console.WriteLine("已经取得页面源代码");
                string playInfo = ModUtils.GetPlayInfoFromHtml(html);
                Console.WriteLine("已经解析到PlayInfo的值");

                List<string> urls = ModUtils.GetCorrectUrl(playInfo);
                Console.WriteLine($"视频真实地址: {urls[0]}");
                Console.WriteLine($"音频真实地址: {urls[1]}");
                string videoHost = new Uri(urls[0]).Host;
                string audioHost = new Uri(urls[1]).Host;

                Console.WriteLine("开始下载视频");
                Stream videoStream = await ModDownload.Download(urls[0], url, videoHost);
                Console.WriteLine("开始下载音频");
                Stream audioStream = await ModDownload.Download(urls[1], url, audioHost);

                ModUtils.Stream2File(videoStream, "temp/video.m4s");
                ModUtils.Stream2File(audioStream, "temp/audio.m4s");
                Console.WriteLine("已经从流转换到文件");

                Console.WriteLine("开始合并音视频");
                ModUtils.CombineVideo();
                Console.WriteLine("完成");
                Console.WriteLine("下载完成");
                Console.WriteLine($"文件保存在{Path.Combine(Directory.GetCurrentDirectory(), "video.mp4")}");

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }
    }
}
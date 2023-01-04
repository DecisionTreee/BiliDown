using BiliDownUI.Module;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BiliDownUI
{
    /// <summary>
    /// SearchPage.xaml 的交互逻辑
    /// </summary>
    public partial class SearchPage : Page
    {
        private const string userAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:34.0) Gecko/20100101 Firefox/34.0";

        public SearchPage()
        {
            InitializeComponent();
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.SetPage(MainWindow.defaltPage);
        }

        private void download_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("开始下载，期间程序不会响应你的任何操作", "૮₍ ˃ ⤙ ˂ ₎ა");

                Directory.CreateDirectory("temp");
                Directory.CreateDirectory("download");

                int audioQuality = audioBox.SelectedIndex;
                int videoQuality = videoBox.SelectedIndex;

                string audioInfo = DefaltPage.audio[audioQuality];
                string videoInfo = DefaltPage.video[videoQuality];

                string[] videoInfos = videoInfo.Split(" ");
                int videoIndex = DefaltPage.videoData.videoQuality.FindIndex(x => x.qualityId == DefaltPage.videoData.qualityIdDic[videoInfos[0]] && x.code == videoInfos[1]);

                string selectVideoUrl = DefaltPage.videoData.videoQuality[videoIndex].baseUrl;
                string selectAudioUrl = DefaltPage.videoData.audioQuality[0].baseUrl;

                WebHeaderCollection headers = new WebHeaderCollection();
                headers.Add("Accept-Encoding", "identity");
                headers.Add("Origin", "https://www.bilibili.com");
                headers.Add("Range", "0-");

                Stream videoStream = ModNet.Get(selectVideoUrl, HttpVersion.Version11, "*/*", DefaltPage.url, null, userAgent, new Uri(selectVideoUrl).Host, true, headers);
                ModUtils.Stream2File(videoStream, "temp/$video.m4s");
                Stream audioStream = ModNet.Get(selectAudioUrl, HttpVersion.Version11, "*/*", DefaltPage.url, null, userAgent, new Uri(selectAudioUrl).Host, true, headers);
                ModUtils.Stream2File(audioStream, "temp/$audio.m4s");

                string title = ModUtils.GetOkString(DefaltPage.videoData.title);

                ModUtils.CombineVideo($"{title}.mp4");

                Process.Start("explorer.exe", "download");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "发生错误(๑°⌓°๑)");
            }
        }
    }
}

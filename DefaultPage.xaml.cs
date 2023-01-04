using BiliDownUI.Module;
using BiliDownUI.UI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
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
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DefaltPage : Page
    {
        public static VideoData videoData = new VideoData();
        public static List<string> video = new List<string>();
        public static List<string> audio = new List<string>();
        public static string url;

        private static bool isLogin = false;
        private static string uid;
        private static string sessData = "SESSDATA=";

        public DefaltPage()
        {
            InitializeComponent();
        }

        private async void login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!isLogin)
                {
                    QRData qrCodeData = ModUtils.GenerateLoginQRCode();
                    Bitmap qrCode = qrCodeData.qrCode;
                    MainWindow.loginPage.qrCode.Source = ModUtils.Bitmap2BitmapImage(qrCode);

                    MainWindow.SetPage(MainWindow.loginPage);

                    sessData = await ModUtils.CheckLoginStatus(qrCodeData.oauthKey);
                    UserData userData = ModUtils.GetUserData(sessData);
                    MainWindow.defaltPage.avatarIcon.Source = ModUtils.Bitmap2BitmapImage(userData.avatar);
                    isLogin = true;
                    uid = userData.uid;

                    MainWindow.loginPage.loginSuccessful.Visibility = Visibility.Visible;
                }
                else
                {
                    Process.Start("explorer.exe", $"https://space.bilibili.com/{uid}");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "发生错误(๑°⌓°๑)");
            }
        }

        private void urlBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) 
            {
                search.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void search_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (urlBox.Text != "")
                {
                    url = urlBox.Text;
                    string html = ModUtils.GetHtml(urlBox.Text, sessData);
                    string playInfo = ModUtils.GetPlayInfoFromHtml(html);

                    videoData = ModUtils.GetVideoData(urlBox.Text, playInfo);
                    MainWindow.searchPage.videoImage.Source = ModUtils.Bitmap2BitmapImage(videoData.pic);
                    MainWindow.searchPage.videoTitle.Text = videoData.title;
                    MainWindow.searchPage.videoInfo.Text = $"{videoData.desc}\n{videoData.tName}  {videoData.pubDate}\n播放 {videoData.view}  弹幕 {videoData.danmaku}  评论 {videoData.reply}  收藏 {videoData.favorite}  投币 {videoData.coin}  分享 {videoData.share}";
                    MainWindow.searchPage.selectVideoTitle.Text = videoData.title;

                    for (int i = 0; i < videoData.videoQuality.Count; i++)
                    {
                        foreach (var item in videoData.qualityIdDic)
                        {
                            if (item.Value == videoData.videoQuality[i].qualityId)
                            {
                                video.Add($"{item.Key} {videoData.videoQuality[i].code}");
                            }
                        }
                    }
                    for (int i = 0; i < videoData.audioQuality.Count; i++)
                    {
                        audio.Add(videoData.audioQuality[i].code);
                    }

                    MainWindow.searchPage.videoBox.ItemsSource = video;
                    MainWindow.searchPage.videoBox.SelectedIndex = 0;
                    MainWindow.searchPage.audioBox.ItemsSource = audio;
                    MainWindow.searchPage.audioBox.SelectedIndex = 0;

                    MainWindow.SetPage(MainWindow.searchPage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "发生错误(๑°⌓°๑)");
            }
        }
    }
}

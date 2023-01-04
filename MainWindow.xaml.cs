using BiliDownUI.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using BiliDownUI.UI;

namespace BiliDownUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static DefaltPage defaltPage = new DefaltPage();
        public static LoginPage loginPage = new LoginPage();
        public static SearchPage searchPage = new SearchPage();

        public static ContentControl _PageController;

        public MainWindow()
        {
            InitializeComponent();
            _PageController = PageController;
            UIUtils.SetPage(_PageController, defaltPage);
        }

        public static void SetPage(Page page)
        {
            UIUtils.SetPage(_PageController, page);
        }

        private void minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }
    }
}

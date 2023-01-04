using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace BiliDownUI.UI
{
    internal class UIUtils
    {
        public static void SetPage(ContentControl control, Page page)
        {
            control.Content = new Frame()
            {
                Content = page
            };
        }
    }
}

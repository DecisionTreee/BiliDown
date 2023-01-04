using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliDownUI.Module
{
    internal class ModQRCodeGenerator
    {
        public static Bitmap Generate(string content)
        {
            QRCodeGenerator generator = new QRCodeGenerator();
            QRCodeData data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.H);
            QRCode code = new QRCode(data);

            Bitmap bmp = code.GetGraphic(4, Color.Black, Color.White, null, 15, 0, true, Color.White);

            return bmp;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCustomWebSocket
{
    static class Tools
    {
        public enum TypeMessage { Text, Blob };

        public static void byteArrayToImage(Byte[] byteArrayIn)
        {
            /*Image img;
            using (var ms = new MemoryStream(byteArrayIn))
            {
                img = Image.FromStream(ms,true,true);
                ms.Close();
            }
            img.Save(@"C:\Users\elecomte\Documents\Emerick Cours\other\image.png", ImageFormat.Png);*/
            File.WriteAllBytes(@"C:\Users\elecomte\Documents\Emerick Cours\other\image.png", byteArrayIn);
        }

        public static byte[] imageToByteArray(Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, imageIn.RawFormat);
                return ms.ToArray();
            }
        }
    }
}

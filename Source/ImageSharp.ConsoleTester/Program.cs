#region License
/*
Copyright (c) 2012 Daniil Rodin

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

   1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.

   2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.

   3. This notice may not be removed or altered from any source
   distribution.
*/
#endregion

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using ImageSharp.BMP;
using ImageSharp.PNG;

namespace ImageSharp.ConsoleTester
{
    class Program
    {
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var builder = new StringBuilder();
                var ex = (Exception)args.ExceptionObject;
                while (ex != null)
                {
                    builder.AppendLine(ex.Message);
                    builder.AppendLine();
                    ex = ex.InnerException;
                }
                builder.AppendLine(((Exception)args.ExceptionObject).StackTrace);

                using (var writer = new StreamWriter("errorlog.txt"))
                {
                    writer.WriteLine(builder.ToString());
                }
            };

            Run();
        }

        static volatile int x;

        static void Run()
        {
            var sw = new Stopwatch();
            byte[] data = File.ReadAllBytes("../Textures/Img.png");
            PngImage img = new PngImage(File.ReadAllBytes("../Textures/Img.png"));
            img = new PngImage(File.ReadAllBytes("../Textures/Kyuubey.png"));
            var bmp = new Bitmap("../Textures/Img.png");
            bmp = new Bitmap("../Textures/Kyuubey.png");
            /*
            sw.Start();
            for (int i = 0; i < 100; i++)
            {
                using (var bitmap = new Bitmap("../Textures/Img.png"))
                {
                    x = bitmap.Width;
                }
            }
            sw.Stop();*/
            double gdiTime = sw.Elapsed.TotalSeconds;
            Console.WriteLine(gdiTime);

            sw.Reset();
            sw.Start();
            for (int i = 0; i < 10000; i++)
            {
                PngImage pngImage = new PngImage(File.ReadAllBytes("../Textures/Img.png"));
                x = pngImage.Width;
            }
            sw.Stop();
            double isTime = sw.Elapsed.TotalSeconds;
            Console.WriteLine(isTime);
            Console.WriteLine(gdiTime/isTime);
            Console.WriteLine(x);
            /*
            PngImage pngImage = new PngImage(data);
            BmpImage bmpImage = new BmpImage(pngImage.Width, pngImage.Height, BPP.ThirtyTwo);
            Array.Copy(pngImage.Data, bmpImage.Data, bmpImage.Data.Length);
            bmpImage.SaveToFile("output.bmp");
            Console.WriteLine(pngImage.ToString());*/

            BmpImage bmpImage = new BmpImage(img.Width, img.Height, BPP.ThirtyTwo);
            Array.Copy(img.Data, bmpImage.Data, bmpImage.Data.Length);
            bmpImage.SaveToFile("output.bmp");
        }
    }
}

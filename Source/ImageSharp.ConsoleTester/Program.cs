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
using System.IO;
using System.Text;
using ImageSharp.BMP;
using ImageSharp.DDS;
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

        static void Run()
        {
            var pngImage = new PngImage(File.ReadAllBytes("../Textures/Kyuubey.png"));
            var bmpImage = new BmpImage(pngImage.Width, pngImage.Height, BPP.ThirtyTwo);
            pngImage.ToRgba8(bmpImage.Data);
            bmpImage.SaveToFile("output.bmp");

            var ddsTexture = new DdsTexture(File.ReadAllBytes("../Textures/Mob.dds"));
            ddsTexture.SaveToFile("Mod.dds");

            var ddsTextureCompressed = new DdsTexture(File.ReadAllBytes("../Textures/Mob_dx3.dds"));
            ddsTextureCompressed.SaveToFile("Mob_dx3.dds");
        }
    }
}

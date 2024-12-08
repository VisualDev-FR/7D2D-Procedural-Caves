using System;
using System.Drawing;
using System.IO;
using BumpKit;

public class GIFEncoder
{
    public static void Encode(string outputFilePath, string[] imageFilePaths, int delay = 1000)
    {
        using (var stream = new FileStream(outputFilePath, FileMode.OpenOrCreate))
        {
            using (var e = new GifEncoder(stream))
            {
                e.FrameDelay = new TimeSpan(0, 0, 0, 0, delay);

                foreach (var path in imageFilePaths)
                {
                    e.AddFrame(Image.FromFile(path));
                }
            }
        }
    }
}
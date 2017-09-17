using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PixelParser.Models;
using ColorQuantizer;
using System.Drawing.Imaging;

namespace PixelParser
{
    class Program
    {

        private static HashSet<string> imgExtensions = new HashSet<string>(new[] { ".jpg", ".jpeg", ".png", ".gif", ".tiff", ".bmp" });

        private static IColorQuantizer quantizer = new LinearColorQuantizer();

        static void Main(string[] args)
        {
            Console.WriteLine("Going to parse all images");

            var folder = @"C:\Users\User\Desktop\test";
            var images = GetAllImages(folder);
            var data = GetColorStatistics(images);
            SaveStatistics(folder, data);

            Console.WriteLine("I've parsed {0} image(s)", images.Count);
            Console.WriteLine("Done!");
            Console.ReadKey();
        }

        private static Dictionary<string, byte[]> GetAllImages(string folder)
        {
            var directory = new DirectoryInfo(folder);
            var files = directory.GetFiles();
            var images = new Dictionary<string, byte[]>();
            foreach (var file in files)
            {
                if (imgExtensions.Contains(file.Extension.ToLowerInvariant()))
                {
                    var image = Image.FromFile(file.FullName);
                    var array = ConvertToBytes(image);
                    images.Add(file.Name, array);
                }
            }
            return images;
        }

        private static ImageFormat GetImageFormat(string extension)
        {
            Console.WriteLine(extension);
            return ImageFormat.Jpeg;
        }

        private static Dictionary<string, ImageStats> GetColorStatistics(Dictionary<string, byte[]> images)
        {
            var data = new Dictionary<string, ImageStats>();
            var keys = images.Keys.ToArray();
            foreach (var key in keys)
            {
                var source = ParseImage(images[key]);
                var stats = new ImageStats(key, source);
                data.Add(key, stats);
            }
            return data;
        }

        private static void SaveStatistics(string folder, Dictionary<string, ImageStats> data)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            var filePath = Path.Combine(folder, "stats.json");
            File.WriteAllText(filePath, json);
        }

        private static ColorQuantizerResult ParseImage(byte[] image)
        {
            const int colorCout = 16;
            var quantizedImage = quantizer.Quantize(image, colorCout);
            return quantizedImage;
        }

        public static byte[] ConvertToBytes(Image image)
        {
            var converter = new ImageConverter();
            var xByte = (byte[])converter.ConvertTo(image, typeof(byte[]));
            return xByte;
        }
    }
}

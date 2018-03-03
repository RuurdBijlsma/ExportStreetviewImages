using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;

namespace TilesToImages
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            const int zoom = 5;
            var maxTile = (int) Math.Pow(2, zoom);

            var outputFolder = $"zoom-{zoom}";
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            DownloadTiles(zoom, maxTile, outputFolder);
            CombineFolderTiles(maxTile, outputFolder);
        }

        private static void CombineFolderTiles(int maxTile, string outputFolder)
        {
            Console.WriteLine("Combining images");
            var images = new Image[maxTile, maxTile];
            for (var x = 0; x < maxTile; x++)
            {
                for (var y = 0; y < maxTile; y++)
                {
                    var fileName = Path.Combine(outputFolder, $"{x}-{y}.png");
                    images[x, y] = Image.FromFile(fileName);
                }
            }

            var image = CombineTiles(images);
            image.Save(Path.Combine(outputFolder, "combined.png"));
            Console.WriteLine("Image combining complete");
        }

        private static Bitmap CombineTiles(Image[,] images)
        {
            var imageAmount = images.GetLength(0);
            var imageSize = (from Image image in images select image.Width).Concat(new[] {0}).Max();
            var outputSize = imageSize * imageAmount;

            var outputImage = new Bitmap(outputSize, outputSize, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(outputImage))
            {
                for (var x = 0; x < imageAmount; x++)
                {
                    for (var y = 0; y < imageAmount; y++)
                    {
                        var image = images[x, y];
                        graphics.DrawImage(image, x * imageSize, y * imageSize);
                    }
                }
            }

            return outputImage;
        }

        private static void DownloadTiles(int zoom, int maxTile, string outputFolder)
        {
            Console.WriteLine("Starting download tiles");
            for (var x = 0; x < maxTile; x++)
            {
                for (var y = 0; y < maxTile; y++)
                {
                    using (var client = new WebClient())
                    {
                        client.Headers.Add("User-Agent", "Route solver for grocery delivery");
                        client.Headers.Add("Referer", "https://ruurdbijlsma.com");
                        var fileName = Path.Combine(outputFolder, $"{x}-{y}.png");
                        DownloadImage(client,
                            $"https://mts1.googleapis.com/vt?hl=en-US&lyrs=svv|cb_client:apiv3&style=40,18&x={x}&y={y}&z={zoom}",
                            fileName);
                        var done = x * maxTile + y;
                        var percentage = (float) done / (maxTile * maxTile);
                        Console.WriteLine($"Downloaded {Math.Floor(percentage * 1000) / 10}%: {fileName}");
                    }
                }
            }

            Console.WriteLine("Done!");
        }

        private static void DownloadImage(WebClient client, string url, string outputImage)
        {
            var bytes = client.DownloadData(new Uri(url));
            using (var stream = new MemoryStream(bytes))
            {
                var image = Image.FromStream(stream);
                image.Save(outputImage);
            }
        }
    }
}
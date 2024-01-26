using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ImageGeneration
{
    public class ImageProcessing
    {

        public static void BlurImage(SKBitmap bitmap, float xBlurAmount, float yBlurAmount)
        {
            using (SKCanvas canvas = new SKCanvas(bitmap))
            using (SKPaint paint = new SKPaint())
            {
                paint.ImageFilter = SKImageFilter.CreateBlur(xBlurAmount, yBlurAmount);

                canvas.DrawBitmap(bitmap, 0, 0, paint);
            }
        }



        public static void DarkenImage(SKBitmap bitmap)
        {
            using (SKCanvas canvas = new SKCanvas(bitmap))
            using (SKPaint paint = new SKPaint())
            {
                SKHighContrastConfig config = new SKHighContrastConfig();
                config.Contrast = 0.05f;
                paint.ColorFilter = SKColorFilter.CreateHighContrast(config);
                canvas.DrawBitmap(bitmap, 0, 0, paint);
            }
        }



        public static void PixelateImage(SKBitmap bitmap, double pixelateAmount)
        {
            // downsize to around a half
           
            SKSizeI size = new SKSizeI();
            size.Width = (int)(bitmap.Width / pixelateAmount);
            size.Height = (int)(bitmap.Height / pixelateAmount);

            SKBitmap lowResBitmap = bitmap.Resize(size, SKFilterQuality.Low);

            var destinationRect = new SKRect(0, 0, bitmap.Width, bitmap.Height);


            // redraw at full size with no antialiasing
            using (SKCanvas canvas = new SKCanvas(bitmap))
            using (SKPaint paint = new SKPaint())
            {
                paint.IsAntialias = false; // make it bad
                canvas.DrawBitmap(lowResBitmap, destinationRect, paint);
            }
        }



        public static void InvertImage(SKBitmap bitmap)
        {
            var inverterMatrix = new float[20] {
                -1f,  0f,  0f, 0f, 1f,
                0f, -1f,  0f, 0f, 1f,
                0f,  0f, -1f, 0f, 1f,
                0f,  0f,  0f, 1f, 0f
            };

            using (SKCanvas canvas = new SKCanvas(bitmap))
            using (SKPaint paint = new SKPaint())
            {
                paint.ColorFilter = SKColorFilter.CreateColorMatrix(inverterMatrix);
                canvas.DrawBitmap(bitmap, 0, 0, paint);
            }
        }


        public static void DrawForgroundNoise(SKBitmap bitmap)
        {
            Random rnd = new Random();

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    int pixelProbability = 5;
                    int pixelPercentage = rnd.Next(1, 100);
                    if (pixelPercentage < pixelProbability)
                    {
                        byte lightness = (byte)(255 - Math.Abs(NextGaussian(0, 2) * 255));
                        SKColor color = new SKColor(lightness, lightness, lightness);
                        bitmap.SetPixel(x, y, color);
                    }
                }
            }
        }



        public static double NextGaussian(double mu = 0, double sigma = 1)
        {
            Random rnd = new Random();
            var u1 = rnd.NextDouble();
            var u2 = rnd.NextDouble();

            var randomStandardNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

            var randomNormal = mu + sigma * randomStandardNormal;
            return randomNormal;
        }



    }
}

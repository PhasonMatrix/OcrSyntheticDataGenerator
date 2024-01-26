using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ContentModel
{
    public class SerializerRect
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int CenterX { get; set; }
        public int CenterY { get; set; }





        public SerializerRect(SKRectI skRect)
        {
            Left = skRect.Left;
            Top = skRect.Top;
            Right = skRect.Right;
            Bottom = skRect.Bottom;
            Width = skRect.Width;
            Height = skRect.Height;
            CenterX = skRect.MidX;
            CenterY = skRect.MidY;
        }

    }
}

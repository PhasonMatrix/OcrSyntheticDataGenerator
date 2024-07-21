using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ContentModel
{
    public class TextContentArea : ContentArea
    {
        public string Text { get; set; }

        public SKRectI BackgroundRect { get; set; }

        public List<WordContentArea> Words { get; set; } = new List<WordContentArea>();
    }
}

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ImageGeneration
{
    internal class ContentArea
    {
        [JsonIgnore]
        public SKRectI Rect { get; set; } // SKRectI for integer rectangle. SKRect for float rectangle.

        [JsonPropertyName("Rect")]
        public SerializerRect SerializerRect 
        {
            get => new SerializerRect(Rect);
        }
    }
}

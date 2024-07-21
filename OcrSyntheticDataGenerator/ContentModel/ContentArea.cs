using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ContentModel
{
    public class ContentArea
    {
        [JsonIgnore]
        public SKRectI Rect { get; set; }

        public bool IsInverted { get; set; }

        [JsonPropertyName("Rect")]
        public SerializerRect SerializerRect
        {
            get => new SerializerRect(Rect);
        }
    }
}

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ContentModel
{
    public class CharacterContentArea : ContentArea
    {
        public char Symbol { get; set; }


        [JsonIgnore]
        public SKRectI CroppedRect { get; set; }

        [JsonPropertyName("CroppedRect")]
        public SerializerRect SerializerCroppedRect
        {
            get => new SerializerRect(CroppedRect);
        }


        public CharacterContentArea Clone()
        {
            CharacterContentArea clone = new CharacterContentArea();
            clone.Symbol = Symbol;
            clone.Rect = new SkiaSharp.SKRectI(Rect.Left, Rect.Top, Rect.Right, Rect.Bottom);
            clone.CroppedRect = new SkiaSharp.SKRectI(CroppedRect.Left, CroppedRect.Top, CroppedRect.Right, CroppedRect.Bottom);
            return clone;
        }
    }
}

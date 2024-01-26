using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ContentModel
{
    public class CharacterContentArea : ContentArea
    {
        public char Symbol { get; set; }

        public CharacterContentArea Clone()
        {
            CharacterContentArea clone = new CharacterContentArea();
            clone.Symbol = Symbol;
            clone.Rect = new SkiaSharp.SKRectI(Rect.Left, Rect.Top, Rect.Right, Rect.Bottom);
            return clone;
        }
    }
}

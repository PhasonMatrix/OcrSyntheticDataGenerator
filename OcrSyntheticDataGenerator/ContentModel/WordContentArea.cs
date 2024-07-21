using Avalonia.Media;
using Microsoft.CodeAnalysis;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ContentModel
{
    public class WordContentArea : ContentArea
    {
        public string Text { get; set; }

        public SKFont Font { get; set; }

        public int YTextBaseline { get; set; }

        public List<CharacterContentArea> Characters { get; set; } = new List<CharacterContentArea>();


        public WordContentArea Clone()
        {
            WordContentArea clone = new WordContentArea();
            clone.Text = new string(Text);

            SKTypeface cloneTypeFace = SKTypeface.FromFamilyName(Font.Typeface.FamilyName, Font.Typeface.FontStyle);
            clone.Font = new SKFont(cloneTypeFace, Font.Size);
            
            clone.YTextBaseline = YTextBaseline;

            clone.Rect = new SKRectI(Rect.Left, Rect.Top, Rect.Right, Rect.Bottom);
            foreach (CharacterContentArea charater in Characters)
            {
                clone.Characters.Add(charater.Clone());
            }
            return clone;
        }
    }
}

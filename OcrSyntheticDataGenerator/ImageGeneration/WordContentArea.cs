using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ImageGeneration
{
    internal class WordContentArea : ContentArea
    {
        public string Text { get; set; }

        public List<CharacterContentArea> Characters { get; set; } = new List<CharacterContentArea>();


        public WordContentArea Clone()
        {
            WordContentArea clone = new WordContentArea();
            clone.Text = Text;
            clone.Rect = new SkiaSharp.SKRectI(Rect.Left, Rect.Top, Rect.Right, Rect.Bottom);
            foreach (CharacterContentArea charater in Characters)
            {
                clone.Characters.Add(charater.Clone());
            }
            return clone;
        }
    }
}

using Avalonia.Media;
using OcrSyntheticDataGenerator.ContentModel;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace OcrSyntheticDataGenerator.ImageGeneration;

internal class ParagraphGenerator : ImageAndLabelGeneratorBase
{
    public ParagraphGenerator(int imageWidth, int imageHeight)
        : base(imageWidth, imageHeight)
    { }



    public override void GenerateContent()
    {
        int minFontHeight = 17;
        int maxFontHeight = 38;
        int minLines = 10;
        int maxLines = 30;
        int minLineSpace = 0;
        int maxLineSpace = 20;

        int fontHeight = _rnd.Next(minFontHeight, maxFontHeight);
        int lineCount = _rnd.Next(minLines, maxLines);
        int lineSpace = _rnd.Next(minLineSpace, maxLineSpace);
        bool isRightJustified = _rnd.Next(1, 100) > 90;
        SKFont font = _randomTextGenerator.GetRandomFont(fontHeight);

        // ---- lines of text ----

        for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
        {
            string text = _randomTextGenerator.GetRandomTextLine(font, 2, 5);

            if (fontHeight < 20) // more words if small text
            {
                text = _randomTextGenerator.GetRandomTextLine(font, 5, 10);
            }


            // choose text location
            int x = 5;
            int yTextBaseline = (lineIndex + 1) * (fontHeight + lineSpace);

            // measure a rectangle to see if we can fit it on the image.
            SKRect measureBounds = new SKRect(0, 0, _imageWidth, _imageHeight);
            SKPoint[] glyphPositions;
            float[] glyphWidths;
            float measuredWidth = 0;
            SKFontMetrics fontMetrics;

            using (SKPaint paint = new SKPaint(font))
            {
                paint.IsAntialias = true;
                paint.Color = SKColors.Black;
                paint.StrokeWidth = 1;
                measuredWidth = paint.MeasureText(text, ref measureBounds);
                glyphPositions = paint.GetGlyphPositions(text, new SKPoint(x, yTextBaseline));
                glyphWidths = paint.GetGlyphWidths(text);
                fontMetrics = paint.FontMetrics;
            }


            int y = (int)(yTextBaseline - fontMetrics.CapHeight - 5);

            SKRectI measureRect = new SKRectI(
               x,
               y,
               x + (int)measuredWidth,
               y + fontHeight
               );


            if (measureRect.Right > _imageWidth - 5 || measureRect.Bottom > _imageHeight - 5)
            {
                continue; // we couldn't fit this on the image
            }


            if (isRightJustified)
            {
                x = _imageWidth - (int)measureRect.Width - 5;
                measureRect.Left = x;
                // re-calculate the character boxes
                using (SKPaint paint = new SKPaint(font))
                {
                    paint.IsAntialias = true;
                    paint.Color = SKColors.Black;
                    paint.StrokeWidth = 1;
                    measuredWidth = paint.MeasureText(text, ref measureBounds);
                    glyphPositions = paint.GetGlyphPositions(text, new SKPoint(x, yTextBaseline));
                    glyphWidths = paint.GetGlyphWidths(text);
                    fontMetrics = paint.FontMetrics;
                }
            }

            List<WordContentArea> words = ConstructWordAndCharacterObjects(font, text, yTextBaseline, glyphPositions, glyphWidths, fontMetrics, measureRect, false);

            TextContentArea currentTextContentArea = new TextContentArea
            {
                Rect = RectToRectI(measureRect),
                Text = text
            };

            
            currentTextContentArea.Words = words;
            _contentAreas.Add(currentTextContentArea);

        }
    }





    public override void DrawContent()
    {
        using (SKCanvas textCanvas = new SKCanvas(TextImage))
        {
            //textCanvas.Clear(SKColors.White);

            byte alpha = (byte)_rnd.Next(120, 255);

            if (HasBackgroundTexture)
            {
                // clamp the darkness. we don't want the text to be too light on a dark background
                if (alpha < 200) { alpha = 255; }
            }

            SKColor textColor = new SKColor(0x00, 0x00, 0x00, alpha);

            foreach (ContentArea contentArea in _contentAreas)
            {
                if (contentArea is TextContentArea phrase)
                {
                    foreach (WordContentArea word in phrase.Words)
                    {
                        bool isUnderlined = (word.Text.Contains("www") || (word.Text.Contains("@") && word.Text.Contains("."))) && (_rnd.Next(1, 100) < 80);

                        DrawTextOnCanvas(textCanvas, word, textColor, isUnderlined);
                    }
                }
            }
        }
    }





}

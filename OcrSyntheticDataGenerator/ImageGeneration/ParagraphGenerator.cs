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

    public override void Generate()
    {
        int minFontHeight = 20;
        int maxFontHeight = 40;
        int minLines = 10;
        int maxLines = 30;
        int minLineSpace = 0;
        int maxLineSpace = 20;

        int fontHeight = _rnd.Next(minFontHeight, maxFontHeight);
        int lineCount = _rnd.Next(minLines, maxLines);
        int lineSpace = _rnd.Next(minLineSpace, maxLineSpace);
        int noisePercentage = _rnd.Next(1, 100);
        bool isRightJustified = _rnd.Next(1, 100) > 90;

        SKFont font = _randomTextGenerator.GetRandomFont(fontHeight);
        byte alpha = (byte)_rnd.Next(155, 255);
        SKColor textColor = new SKColor(0x00, 0x00, 0x00, alpha);


        // set up canvas objects for each image
        using (SKCanvas textCanvas = new SKCanvas(TextImage))
        using (SKCanvas labelCanvas = new SKCanvas(LabelImage))
        using (SKCanvas heatMapCanvas = new SKCanvas(HeatMapImage))
        using (SKCanvas characterBoxCanvas = new SKCanvas(CharacterBoxImage))
        {


            // draw image backgrounds
            textCanvas.Clear(SKColors.White);
            labelCanvas.Clear(SKColors.Black);
            heatMapCanvas.Clear(SKColors.Blue);
            characterBoxCanvas.Clear(SKColors.White);




            // add backgound texture image
            int backgroundImagePercentage = _rnd.Next(1, 100);
            if (backgroundImagePercentage <= BackgroundProbability)
            {
                DrawBackgroundImage(textCanvas);
            }



            // ---- lines of text ----

            for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
            {

                string text = _randomTextGenerator.GetRandomTextLine(3, 6);

                if (fontHeight < 15) // more words if small text
                {
                    text = _randomTextGenerator.GetRandomTextLine(5, 12);
                }


                // choose text location
                int x = 5;
                int yTextBaseline = (lineIndex + 1) * (fontHeight + lineSpace) ;

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

                SKRect measureRect = new SKRect(
                   x,
                   y,
                   x + measuredWidth,
                   y + fontHeight + 3
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


                TextContentArea currentTextContentArea = new TextContentArea
                {
                    Rect = RectToRectI(measureRect),
                    Text = text
                };


                // construct the character boxes from the previously measured positions and widths;
                SKRect[] characterBoxes = ConstructCharacterBoundingBoxes(text, glyphPositions, glyphWidths, measureRect);


                
                DrawTextOnCanvas(textCanvas, text, x, yTextBaseline, font, textColor);



                // draw text on heatmap
                var heatMapTextColour = SKColors.Black;
                DrawTextOnCanvas(heatMapCanvas, text, x, yTextBaseline, font, heatMapTextColour);



                // measure and crop character box top and bottom
                SKRect[] croppedCharacterBoxes = GetCroppedCharacterBoxes(text, yTextBaseline, font, characterBoxes);


                // Character Box Image - draw text
                textColor = SKColors.Black;
                using (SKPaint paint = new SKPaint())
                {
                    paint.IsAntialias = true;
                    paint.Color = textColor;
                    characterBoxCanvas.DrawText(text, x, yTextBaseline, font, paint);
                }

                // Character Box Image - draw boxes

                SKColor charBoxColour = new SKColor(0, 100, 255, 220);
                using (SKPaint paint = new SKPaint())
                {
                    paint.IsAntialias = true;
                    paint.Color = charBoxColour;
                    paint.Style = SKPaintStyle.Stroke;
                    foreach (SKRect characterBox in croppedCharacterBoxes)
                    {
                        characterBoxCanvas.DrawRect(characterBox, paint);
                        // label image and heatmap image

                        DrawCharacterProbabilityLabels(characterBox, labelCanvas);
                        DrawCharacterHeatMap(characterBox, heatMapCanvas);
                    }

                    
                }



                List<WordContentArea> words = CreateWordAndCharacterObjects(text, croppedCharacterBoxes);
                currentTextContentArea.Words = words;
                _contentAreas.Add(currentTextContentArea);

                SKColor wordBoxColour = new SKColor(0, 255, 50, 220);
                using (SKPaint paint = new SKPaint())
                {
                    paint.IsAntialias = true;
                    paint.Color = wordBoxColour;
                    paint.Style = SKPaintStyle.Stroke;
                    foreach (var word in words)
                    {
                        SKRect wordDisplayRect = new SKRect(word.Rect.Left, word.Rect.Top, word.Rect.Right, word.Rect.Bottom);
                        wordDisplayRect.Inflate(2, 2);
                        characterBoxCanvas.DrawRect(wordDisplayRect, paint);
                    }
                }
                

            }


            // ---- post processing ----
            PostProcessing(noisePercentage);


        }



    }
}

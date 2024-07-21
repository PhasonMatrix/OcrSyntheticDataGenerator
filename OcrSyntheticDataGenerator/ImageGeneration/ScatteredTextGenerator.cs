using OcrSyntheticDataGenerator.ContentModel;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace OcrSyntheticDataGenerator.ImageGeneration;

public class ScatteredTextGenerator : ImageAndLabelGeneratorBase
{

    public ScatteredTextGenerator(int imageWidth, int imageHeight)
        : base(imageWidth, imageHeight) 
    { }


    public override void GenerateContent()
    {
        int minFontHeight = 16;
        int maxFontHeight = 300;

        // create some lines
        int numberOfHorizontalLines = _rnd.Next(1, 4);
        int numberOfVerticalLines = _rnd.Next(1, 3);

        for (int hl = 0; hl < numberOfHorizontalLines; hl++)
        {
            int lineY = _rnd.Next(0, _imageHeight - 1);
            int lineX = _rnd.Next(0, _imageHeight / 2);
            int lineLength = _rnd.Next(30, _imageWidth * 3);
            int lineThickness = _rnd.Next(1, 4);
            SKRectI lineRect = new SKRectI(lineX, lineY, lineX + lineLength, lineY + lineThickness);
            LineContentArea lca = new LineContentArea() { Rect = lineRect };
            _contentAreas.Add(lca);
        }

        for (int vl = 0; vl < numberOfVerticalLines; vl++)
        {
            int lineY = _rnd.Next(0, _imageHeight / 2);
            int lineX = _rnd.Next(0, _imageHeight - 1);
            int lineLength = _rnd.Next(30, _imageHeight * 3);
            int lineThickness = _rnd.Next(1, 4);
            SKRectI lineRect = new SKRectI(lineX, lineY, lineX + lineThickness, lineY + lineLength);
            LineContentArea lca = new LineContentArea() { Rect = lineRect };
            _contentAreas.Add(lca);
        }


        // add the text
        int numberofTextElements = _rnd.Next(5, 10);

        int i = 0;
        int failedAttempts = 0;
        while (i < numberofTextElements)
        {
            // random visual attributes
            bool isInverted = _rnd.Next(0, 100) < 20;
            int fontHeight = _rnd.Next(minFontHeight, maxFontHeight);

            // measure a rectangle to see if we can fit it on the image.
            SKFont font = _randomTextGenerator.GetRandomFont(fontHeight);

            // text content
            string text = _randomTextGenerator.GetRandomText(font);
            //string text = _randomTextGenerator.GetTestPatternText(font);

            // choose a location
            int x = _rnd.Next(0, _imageWidth / 2);
            int yTextBaseline = _rnd.Next(20, _imageHeight) + fontHeight;


            // just a heuristic. if it's a long string, give it a better chance of making it onto the image
            if (text.Length > 18 || (font.Size > 60 && text.Length > 3))
            {
                x = 5;
            }


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


            if (measureRect.Right > _imageWidth - 5 || measureRect.Bottom > _imageHeight - 5 || text.Length > 32)
            {
                failedAttempts++;
                if (failedAttempts > 1000)
                {
                    // abort. too full.
                    break;
                }
                continue; // we couldn't fit this on the image
            }


            SKRectI backgoundRect = new SKRectI(measureRect.Left, measureRect.Top, measureRect.Right, measureRect.Bottom); // copy
            int inflateAmount = _rnd.Next(1, 9);
            backgoundRect.Inflate(inflateAmount * 2, inflateAmount);
            backgoundRect.Top -= inflateAmount;


            // check we haven't overlapped another piece of text
            bool overlapsWithSomething = CheckOverlapsWithExistingObjects(backgoundRect);
            if (overlapsWithSomething)
            {
                continue; // try again.
            }


            List<WordContentArea> words = ConstructWordAndCharacterObjects(font, text, yTextBaseline, glyphPositions, glyphWidths, fontMetrics, measureRect, isInverted);


            // if we got here without `break` or `continue` in the checks above, then we can fit the whole string on the image.
            // start a new content area
            TextContentArea currentTextContentArea = new TextContentArea
            {
                Rect = RectToRectI(measureRect),
                Text = text,
                IsInverted = isInverted
            };

            currentTextContentArea.Words = words;
            currentTextContentArea.BackgroundRect = backgoundRect;
            _contentAreas.Add(currentTextContentArea);

            i++;
        }
    }



    public override void DrawContent()
    {
        using (SKCanvas textCanvas = new SKCanvas(TextImage))
        {
            //textCanvas.Clear(SKColors.White);

            foreach (ContentArea contentArea in _contentAreas) // each content area is like a sentence or phrase. could have a box around it, coule be inverted
            {
                if (contentArea is LineContentArea line)
                {
                    bool isDashedLine = _rnd.Next(1, 100) < 20;
                    int lineThickness = _rnd.Next(1, 4);

                    SKPath path = new SKPath();
                    if (_rnd.Next(1, 100) < 50) // very slightly diagonal. 50/50 top-left to bottom-right vs bottom-left to top-right
                    {
                        path.MoveTo(line.Rect.Left, line.Rect.Top);
                        path.LineTo(line.Rect.Right, line.Rect.Bottom);
                    } 
                    else
                    {
                        path.MoveTo(line.Rect.Left, line.Rect.Bottom);
                        path.LineTo(line.Rect.Right, line.Rect.Top);
                    }

                    using (SKPaint paint = new SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.Color = SKColors.Black;
                        paint.Style = SKPaintStyle.Stroke;
                        paint.StrokeWidth = lineThickness;
                        if (isDashedLine) { paint.PathEffect = SKPathEffect.CreateDash(GetRandomLineDashPattern(), 0); }
                        textCanvas.DrawPath(path, paint);
                    }

                }
                else if (contentArea is TextContentArea phrase)
                {
                    bool isInverted = _rnd.Next(0, 100) < 10;
                    bool isLightText = _rnd.Next(0, 100) < 10;
                    bool isBoxAroundText = _rnd.Next(0, 100) < 10;


                    // draw background and/or box
                    DrawTextBlockBackgroundColour(textCanvas, isInverted, isLightText, isBoxAroundText, phrase.BackgroundRect);


                    SKColor textColor = new SKColor(255, 255, 255, (byte)_rnd.Next(60, 255)); // light text

                    // draw the text on the image

                    if (!isInverted)  // dark text on light/no background
                    {
                        byte alpha = (byte)_rnd.Next(60, 255);
                        if (_backgroundColour.Alpha < 90) // make it lighter if the background is light
                        {
                            alpha = (byte)_rnd.Next(200, 255);
                            // clamp to less than 256
                            if (alpha > 255) { alpha = 255; }
                        }

                        if (HasBackgroundTexture)
                        {
                            // clamp the darkness. we don't want the text to be too light on a dark background
                            if (alpha < 200) { alpha = 255; }
                        }
                        textColor = new SKColor(0x00, 0x00, 0x00, alpha);
                    }
                    

                    foreach (WordContentArea word in phrase.Words)
                    {
                        if (word.Text != null) // don't know why but rarely the word is null 
                        {
                            bool isUnderlined = (word.Text.Contains("www") || (word.Text.Contains("@") && word.Text.Contains("."))) && (_rnd.Next(1, 100) < 80);
                            DrawTextOnCanvas(textCanvas, word, textColor, isUnderlined);
                        }

                    }
                }
            }
        }
    }


    protected float[] GetRandomLineDashPattern()
    {
        int percent = _rnd.Next(1, 100);

        if (percent < 70) // most of the time, just a simple dash
        {
            float dashSize = _rnd.Next(2, 15);
            float gapSize = _rnd.Next(3, 15);
            return new float[] { dashSize, gapSize };
        }
        else if (percent < 90) // sometimes, small dots, like period character
        {
            return new float[] { 2, 2 };
        }
        else // sometimes do a complex dash of shorter and longer dashes
        {
            float smallDashSize = _rnd.Next(5, 10);
            float largeDashSize = _rnd.Next(12, 30);
            float gapSize = _rnd.Next(5, 20);
            return new float[] { smallDashSize, gapSize, largeDashSize, gapSize };
        }
    }

}

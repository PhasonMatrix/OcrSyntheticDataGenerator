using OcrSyntheticDataGenerator.ContentModel;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ImageGeneration;

public class ScatteredTextGenerator : ImageAndLabelGeneratorBase
{

    public ScatteredTextGenerator(int imageWidth, int imageHeight)
        : base(imageWidth, imageHeight) 
    { }


    public override void Generate()
    {
        int minFontHeight = 18;
        int maxFontHeight = 72;
        int noisePercentage = _rnd.Next(1, 100);


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

            _hasDarkBackgroundImage = false;

            // add backgound texture image
            int backgroundImagePercentage = _rnd.Next(1, 100);
            if (backgroundImagePercentage <= BackgroundProbability)
            {
                DrawBackgroundImage(textCanvas);
            }



            // add some lines
            int linePercentage = _rnd.Next(1, 100);
            if (linePercentage <= LinesProbability) // only X percent of the time
            {
                DrawLines(textCanvas);
            }



            // add the text
            int numberofTextElements = _rnd.Next(10, 25);

            int i = 0;
            int failedAttempts = 0;
            while (i < numberofTextElements)
            {
                // text content
                string text = _randomTextGenerator.GetRandomText();


                // random visual attributes
                bool isInverted = _rnd.Next(0, 100) < 20;
                bool isLightText = _rnd.Next(0, 100) < 10;
                bool isBoxAroundText = _rnd.Next(0, 100) < 20;
                int fontHeight = _rnd.Next(minFontHeight, maxFontHeight);


                // choose a location
                int x = _rnd.Next(0, _imageWidth / 2);
                int yTextBaseline = _rnd.Next(0, _imageHeight) + fontHeight;



                // just a heuristic. if it's a long string, give it a better chance of making it onto the image
                if (text.Length > 22)
                {
                    x = 5;
                }

                // measure a rectangle to see if we can fit it on the image.
                SKFont font = _randomTextGenerator.GetRandomFont(fontHeight);

                SKRect measureBounds = new SKRect(0, 0, _imageWidth, _imageHeight);
                SKPoint[] glyphPositions;
                float[] glyphWidths;
                float measuredWidth = 0;
                SKFontMetrics fontMetrics;


                using (SKPaint paint = new SKPaint(font))
                {
                    //paint.Typeface = font.Typeface;
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


                SKRect backgoundRect = new SKRect(measureRect.Left, measureRect.Top, measureRect.Right, measureRect.Bottom); // copy
                int inflateAmount = _rnd.Next(1, 9);
                backgoundRect.Inflate(inflateAmount * 2, inflateAmount);
                backgoundRect.Top -= inflateAmount;


                // check we haven't overlapped another piece of text

                bool overlapsWithSomething = CheckOverlapsWithExistingObjects(backgoundRect);
                if (overlapsWithSomething)
                {
                    continue; // try again.
                }


                // if we got here without `break` or `continue` in the checks above, then we can fit the whole string on the image.
                // start a new content area
                TextContentArea currentTextContentArea = new TextContentArea
                {
                    Rect = RectToRectI(measureRect),
                    Text = text
                };


                // construct the character boxes from the previously measured positions and widths;
                SKRect[] characterBoxes = ConstructCharacterBoundingBoxes(text, glyphPositions, glyphWidths, measureRect);


                // measure and crop character box top and bottom
                SKRect[] croppedCharacterBoxes = GetCroppedCharacterBoxes(text, yTextBaseline, font, characterBoxes);


                List<WordContentArea> words = CreateWordAndCharacterObjects(text, croppedCharacterBoxes);
                currentTextContentArea.Words = words;
                _contentAreas.Add(currentTextContentArea);



                // draw text on main image

                // draw backgound
                DrawTextBlockBackgroundColour(textCanvas, isInverted, isLightText, isBoxAroundText, backgoundRect);

                // draw the text

                SKColor textColor = SKColors.Black;

                // draw the text on the image
                if (isInverted)
                {
                    byte alpha = (byte)_rnd.Next(175, 255); 
                    textColor = new SKColor(255, 255, 255, alpha); // light text
                    DrawTextOnCanvas(textCanvas, text, x, yTextBaseline, font, textColor);
                }
                else
                {
                    byte alpha = (byte)_rnd.Next(155, 255); // dark text
                    if (_backgroundColour.Alpha < 90) // make it lighter if the background is light
                    {
                        alpha = (byte)_rnd.Next(200, 255);
                        // clamp to less than 256
                        if (alpha > 255) { alpha = 255; }

                    }
                    //if (hasDarkBackgroundImage)
                    if (backgroundImagePercentage <= BackgroundProbability) // if any background
                    {
                        // clamp the darkness. we don't want the text to be too light on a dark background
                        if (alpha < 230) { alpha = 255; }
                    }
                    textColor = new SKColor(0x00, 0x00, 0x00, alpha);
                    DrawTextOnCanvas(textCanvas, text, x, yTextBaseline, font, textColor);

                }


                DrawLabels(
                  font,
                  labelCanvas,
                  heatMapCanvas,
                  characterBoxCanvas,
                  text,
                  x,
                  yTextBaseline,
                  croppedCharacterBoxes,
                  words);




                i++;
            }


            // ---- post processing ----
            PostProcessing(noisePercentage);

        }

    }

}

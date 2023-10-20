using Avalonia.Controls;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace OcrSyntheticDataGenerator.ImageGeneration
{
    public class ImageAndLabelGenerator
    {
        public enum DataFileType
        {
            [Description("None")] None,
            [Description("CSV characters only")] CsvCharactersOnly,
            [Description("CSV words only")] CsvWordsOnly,
            [Description("CSV words and characters")] CsvWordsAndCharacters,
            [Description("JSON characters only")] JsonCharactersOnly,
            [Description("JSON words only")] JsonWordsOnly,
            [Description("JSON words and characters")] JsonWordsAndCharacters,
        };


        private int _imageWidth;
        private int _imageHeight;
        RandomTextGenerator _randomTextGenerator = new RandomTextGenerator();
        List<ContentArea> _contentAreas = new List<ContentArea>();

        public SKBitmap TextImage { get; set; }
        public SKBitmap TextMeasuringImage { get; set; }
        public SKBitmap LabelImage { get; set; }
        public SKBitmap HeatMapImage { get; set; }
        public SKBitmap CharacterBoxImage { get; set; }


        public int LinesProbability { get; set; }
        public int BackgroundProbability { get; set; }
        public int NoiseProbability { get; set; }
        public int BlurProbability { get; set; }
        public int InvertProbability { get; set; }
        public int PixelateProbability { get; set; }


        public ImageAndLabelGenerator(int imageWidth, int imageHeight)
        {
            _imageWidth = imageWidth;
            _imageHeight = imageHeight;

            TextImage = new SKBitmap(imageWidth, imageHeight);
            TextMeasuringImage = new SKBitmap(imageWidth, imageHeight);
            LabelImage = new SKBitmap(imageWidth, imageHeight);
            HeatMapImage = new SKBitmap(imageWidth, imageHeight);
            CharacterBoxImage = new SKBitmap(imageWidth, imageHeight);
        }


        public void GenerateImages()
        {
            Random rnd = new Random();
            int minFontHeight = 22;
            int maxFontHeight = 92;


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

                bool hasDarkBackgroundImage = false;

                // add backgound texture image
                int backgroundImagePercentage = rnd.Next(1, 100);
                if (backgroundImagePercentage <= BackgroundProbability)
                {
                    DirectoryInfo backgroundImageFolder = new DirectoryInfo("./BackgroundImages");
                    if (backgroundImageFolder != null)
                    {
                        var files = backgroundImageFolder.GetFiles();
                        int randomFileIndex = rnd.Next(0, files.Length);
                        var imageFile = files[randomFileIndex];

                        using (SKBitmap backgroundImage = SKBitmap.Decode(imageFile.FullName))
                        {
                            int randomRotation = rnd.Next(0, 55);

                            // random tiling modes
                            SKShaderTileMode tileMode = SKShaderTileMode.Repeat;
                            if (rnd.Next(1, 100) > 50)
                            {
                                tileMode = SKShaderTileMode.Mirror;
                            }

                            using (SKPaint paint = new SKPaint())
                            {
                                paint.Shader = SKShader.CreateBitmap(backgroundImage, tileMode, tileMode);
                                textCanvas.DrawRect(TextImage.Info.Rect, paint);
                            }

                            // get average darkness of background image
                            int totalLightness = 0;
                            int totalPixels = 0;
                            for (int pxX = 0; pxX < backgroundImage.Info.Width; pxX += 1)
                            {
                                for (int pxY = 0; pxY < backgroundImage.Info.Height; pxY += 1)
                                {
                                    totalLightness += backgroundImage.GetPixel(pxX, pxY).Red;
                                    totalPixels++;
                                }
                            }
                            int averageLightness = totalLightness / totalPixels;
                            Debug.WriteLine($"background lightness: {averageLightness}");
                            if (averageLightness <= 240)
                            {
                                hasDarkBackgroundImage = true;
                            }
                        }
                    }
                }



                // add background noise
                int noisePercentage = rnd.Next(5, 100);
                // removed background salt-and-pepper, but foreground noise is done after text drawn.


                // add some lines
                int linePercentage = rnd.Next(1, 100);
                if (linePercentage <= LinesProbability) // only X percent of the time
                {
                    int numberOfHorizontalLines = rnd.Next(1, 5);
                    int numberOfVerticalLines = rnd.Next(1, 4);

                    for (int hl = 0; hl < numberOfHorizontalLines; hl++)
                    {
                        int lineY = rnd.Next(0, _imageHeight - 1);
                        int lineX = rnd.Next(0, _imageHeight / 2);
                        int lineLength = rnd.Next(30, _imageWidth * 3);
                        int lineThickness = rnd.Next(1, 4);
                        SKRect lineRect = new SKRect(lineX, lineY, lineX + lineLength, lineY + lineThickness);
                        using (SKPaint paint = new SKPaint())
                        {
                            paint.IsAntialias = true;
                            paint.Color = SKColors.Black;
                            textCanvas.DrawRect(lineRect, paint);
                        }

                        LineContentArea lca = new LineContentArea() { Rect = RectToRectI(lineRect) };
                        _contentAreas.Add(lca);
                    }
                    for (int vl = 0; vl < numberOfVerticalLines; vl++)
                    {
                        int lineY = rnd.Next(0, _imageHeight / 2);
                        int lineX = rnd.Next(0, _imageHeight - 1);
                        int lineLength = rnd.Next(30, _imageHeight * 3);
                        int lineThickness = rnd.Next(1, 4);
                        SKRect lineRect = new SKRect(lineX, lineY, lineX + lineThickness, lineY + lineLength);
                        using (SKPaint paint = new SKPaint())
                        {
                            paint.IsAntialias = true;
                            paint.Color = SKColors.Black;
                            textCanvas.DrawRect(lineRect, paint);
                        }

                        LineContentArea lca = new LineContentArea() { Rect = RectToRectI(lineRect) };
                        _contentAreas.Add(lca);
                    }

                }



                // add the text
                int numberofTextElements = rnd.Next(3, 20);

                int i = 0;
                int failedAttempts = 0;
                while (i < numberofTextElements)
                {
                    // text content
                    string text = _randomTextGenerator.GetRandomText();


                    // random visual attributes
                    bool inverted = rnd.Next(0, 100) < 20;
                    bool lightText = rnd.Next(0, 100) < 10;
                    bool drawBoxAroundText = rnd.Next(0, 100) < 20;
                    int fontHeight = rnd.Next(minFontHeight, maxFontHeight);


                    // choose a location
                    
                    int x = rnd.Next(0, _imageWidth / 2);
                    int yTextBaseline = rnd.Next(0, _imageHeight) + fontHeight;



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

                    //int y = (int)(yTextBaseline + fontMetrics.Ascent);
                    //int y = (int)(yTextBaseline + fontMetrics.Top);
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

                    // check we haven't overlapped another piece of text
                    bool overlapsWithSomething = false;
                    foreach (var area in _contentAreas)
                    {
                        SKRect overlapRect = new SKRect(measureRect.Left, measureRect.Top, measureRect.Right, measureRect.Bottom);
                        overlapRect.Inflate(5, 5);
                        if (RectanglesOverlap(area.Rect, overlapRect))
                        {
                            overlapsWithSomething = true;
                            break;
                        }
                    }
                    if (overlapsWithSomething)
                    {
                        continue; // try again.
                    }


                    // we can fit the whole string on the image.
                    // start a new content area
                    TextContentArea currentTextContentArea = new TextContentArea
                    {
                        Rect = RectToRectI(measureRect),
                        Text = text
                    };


                    // construct the character boxes from the previously measured positions and widths;
                    SKRect[] characterBoxes = ConstructCharacterBoundingBoxes(text, glyphPositions, glyphWidths, measureRect);


                    SKRect backgoundRect = new SKRect(measureRect.Left, measureRect.Top, measureRect.Right, measureRect.Bottom); // copy
                    backgoundRect.Inflate(10, 4);
                    backgoundRect.Top -= 5;

                    SKColor backgroundColour = SKColors.White;

                    

                    // draw backgound
                    if (inverted)
                    {
                        byte alpha = (byte)rnd.Next(160, 255); // dark background
                        backgroundColour = new SKColor(0x00, 0x00, 0x00, alpha);
                        using (SKPaint paint = new SKPaint())
                        {
                            paint.IsAntialias = true;
                            paint.Color = backgroundColour;
                            textCanvas.DrawRect(backgoundRect, paint);
                        }
                    }
                    else
                    {
                        // not inverse, but we want to sometimes draw a background colour, like a highlighter
                        if (lightText)
                        {
                            byte alpha = (byte)rnd.Next(0, 128);
                            backgroundColour = new SKColor(0x00, 0x00, 0x00, alpha);
                            using (SKPaint paint = new SKPaint())
                            {
                                paint.IsAntialias = true;
                                paint.Color = backgroundColour;
                                textCanvas.DrawRect(backgoundRect, paint);
                            }
                        }
                        // how about a box around the text
                        else if (drawBoxAroundText)
                        {
                            // random offset
                            int offset = rnd.Next(-10, 8);
                            backgoundRect.Top += offset;
                            backgoundRect.Left += offset;

                            // dark coloured box
                            byte rgb = (byte)rnd.Next(0, 96);
                            SKColor boxColour = new SKColor(rgb, rgb, rgb);

                            using (SKPaint paint = new SKPaint())
                            {
                                paint.IsAntialias = true;
                                paint.Color = boxColour;
                                paint.Style = SKPaintStyle.Stroke;
                                textCanvas.DrawRect(backgoundRect, paint);
                            }
                        }
                    }


                    // draw the text

                    SKColor textColor = SKColors.Black;

                    // draw the text on the image
                    if (inverted)
                    {
                        byte rgb = (byte)rnd.Next(190, 255); // light text
                        textColor = new SKColor(rgb, rgb, rgb);
                        using (SKPaint paint = new SKPaint())
                        {
                            paint.IsAntialias = true;
                            paint.Color = textColor;
                            textCanvas.DrawText(text, x, yTextBaseline, font, paint);
                        }
                    }
                    else
                    {
                        byte alpha = (byte)rnd.Next(155, 255); // dark text
                        if (backgroundColour.Alpha < 90) // make it lighter if the background is light
                        {
                            alpha = (byte)rnd.Next(200, 255);
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
                        using (SKPaint paint = new SKPaint())
                        {
                            paint.IsAntialias = true;
                            paint.Color = textColor;
                            textCanvas.DrawText(text, x, yTextBaseline, font, paint);
                        }
                    }

                    // draw text on heatmap
                    var heatMapTextColour = SKColors.Black;
                    using (SKPaint paint = new SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.Color = heatMapTextColour;
                        heatMapCanvas.DrawText(text, x, yTextBaseline, font, paint);
                    }



                    // measure and crop character box top and bottom

                    SKRect[] croppedCharacterBoxes = new SKRect[characterBoxes.Length];
                    for (int c = 0; c < characterBoxes.Length; c++)
                    {
                        SKRect currentCharBox = characterBoxes[c];
                        char currentSymbol = text[c];
                        // difference between charbox top and the y basline where it was drawn
                        int baselineOffset = (int)(yTextBaseline - currentCharBox.Top);

                        var croppedCharBox = GetCroppedCharacterBox(currentCharBox, currentSymbol, font, baselineOffset);
                        croppedCharacterBoxes[c] = croppedCharBox;
                    }



                    // draw data on the other images. bounding boxes, labels, heatmap, etc.


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
                        foreach (SKRect characterBox in croppedCharacterBoxes)
                        {
                            paint.IsAntialias = true;
                            paint.Color = charBoxColour;
                            paint.Style = SKPaintStyle.Stroke;
                            characterBoxCanvas.DrawRect(characterBox, paint);


                            // label image and heatmap image

                            DrawCharacterProbabilityLabels(characterBox, labelCanvas);
                            DrawCharacterHeatMap(characterBox, heatMapCanvas);

                        }
                    }


                    List<WordContentArea> words = CreateWordAndCharacterObjects(text, croppedCharacterBoxes);
                    currentTextContentArea.Words = words;
                    _contentAreas.Add(currentTextContentArea);

                    i++;
                }




                // draw foreground noise 

                Stopwatch sw = Stopwatch.StartNew();

                //int noisePercentage = rnd.Next(1, 100);
                if (noisePercentage <= NoiseProbability)
                {
                    // draw pixels?
                    for (int x = 0; x < _imageWidth; x++)
                    {
                        for (int y = 0; y < _imageHeight; y++)
                        {
                            int pixelProbability = 5;
                            int pixelPercentage = rnd.Next(1, 100);
                            if (pixelPercentage < pixelProbability)
                            {
                                byte lightness = (byte)(255 - Math.Abs(_randomTextGenerator.NextGaussian(0, 2) * 255));
                                SKColor color = new SKColor(lightness, lightness, lightness);
                                TextImage.SetPixel(x, y, color);
                            }
                        }
                    }
                }

                Debug.WriteLine($"Noise drawn with setPixel in {sw.ElapsedMilliseconds}ms");
                sw.Restart();




                // blur
                int blurPercentage = rnd.Next(1, 100);
                if (blurPercentage <= BlurProbability)
                {
                    float xBlurAmount = rnd.Next(85, 200) / 100;
                    float yBlurAmount = rnd.Next(85, 200) / 100;
                    //float blurAmount = 0.85f;
                    using (SKPaint paint = new SKPaint())
                    {
                        paint.ImageFilter = SKImageFilter.CreateBlur(xBlurAmount, yBlurAmount);

                        textCanvas.DrawBitmap(TextImage, 0, 0, paint);
                    }
                }


                // darken if blured
                if (blurPercentage <= BlurProbability)
                {
                    using (SKPaint paint = new SKPaint())
                    {
                        SKHighContrastConfig config = new SKHighContrastConfig();
                        config.Contrast = 0.05f;
                        paint.ColorFilter = SKColorFilter.CreateHighContrast(config);
                        textCanvas.DrawBitmap(TextImage, 0, 0, paint);
                    }
                }



                // pixelate

                int pixelatePercentage = rnd.Next(1, 100);
                if (pixelatePercentage <= PixelateProbability)
                {

                    // downsize to around a half
                    double pixelateAmount = rnd.NextDouble() * (2.0 - 1.0) + 1.0; // * (maximum - minimum) + minimum;

                    SKSizeI size = new SKSizeI();
                    size.Width = (int)(TextImage.Width / pixelateAmount);
                    size.Height = (int)(TextImage.Height / pixelateAmount);

                    SKBitmap lowResBitmap = TextImage.Resize(size, SKFilterQuality.Low);

                    var destinationRect = new SKRect(0, 0, TextImage.Width, TextImage.Height);


                    // redraw at full size with no antialiasing/
                    using (SKPaint paint = new SKPaint())
                    {
                        paint.IsAntialias = false; // make it bad
                        textCanvas.DrawBitmap(lowResBitmap, destinationRect, paint);
                    }

                }



                // invert
                int invertPercentage = rnd.Next(1, 100);
                if (invertPercentage <= InvertProbability)
                {
                    var inverterMatrix = new float[20] {
                        -1f,  0f,  0f, 0f, 1f,
                        0f, -1f,  0f, 0f, 1f,
                        0f,  0f, -1f, 0f, 1f,
                        0f,  0f,  0f, 1f, 0f
                    };

                    using (SKPaint paint = new SKPaint())
                    {
                        paint.ColorFilter = SKColorFilter.CreateColorMatrix(inverterMatrix);
                        textCanvas.DrawBitmap(TextImage, 0, 0, paint);
                    }
                }



            }

        }



        private List<WordContentArea> CreateWordAndCharacterObjects(string text, SKRect[] characterBoxes)
        {

            List<WordContentArea> words = new List<WordContentArea>();

            WordContentArea word = new WordContentArea();


            for (int c = 0; c < characterBoxes.Length; c++)
            {
                SKRect currentCharBox = characterBoxes[c];
                char currentSymbol = text[c];

                CharacterContentArea character = new CharacterContentArea();
                character.Symbol = currentSymbol;

                if (c == 0) // first character
                {
                    character.Rect = RectToRectI(currentCharBox);
                    word = new WordContentArea();
                    word.Rect = RectToRectI(currentCharBox);
                }
                else if (currentSymbol == ' ') // space between words
                {
                    WordContentArea wordCopy = word.Clone();
                    words.Add(wordCopy);
                    word = new WordContentArea();
                }


                if (currentSymbol != ' ')
                {
                    character.Rect = RectToRectI(currentCharBox);
                    word.Characters.Add(character);
                    if (string.IsNullOrEmpty(word.Text))
                    {
                        word.Rect = RectToRectI(currentCharBox);
                    }
                    else
                    {
                        word.Rect = GrowRectToEngulfNewRect(word.Rect, RectToRectI(currentCharBox));
                    }
                    word.Text += currentSymbol;
                }
            }

            words.Add(word);
            return words;
        }


        private SKRectI GrowRectToEngulfNewRect(SKRectI existingRect, SKRectI newRect)
        {
            if (newRect.Left < existingRect.Left) { existingRect.Left = newRect.Left; }
            if (newRect.Top < existingRect.Top) { existingRect.Top = newRect.Top; }
            if (newRect.Right > existingRect.Right) { existingRect.Right = newRect.Right; }
            if (newRect.Bottom > existingRect.Bottom) {  existingRect.Bottom = newRect.Bottom; }
            return existingRect;
        }


        private void DrawCharacterProbabilityLabels(SKRect charRect, SKCanvas labelCanvas)
        {
            // adjust the rectangle a bit
            charRect = new SKRect(charRect.Left, charRect.Top - 2, charRect.Right + 1, charRect.Bottom + 2);

            SKBitmap stampBitmap = GetSingleCharacterLabelImage();
            labelCanvas.DrawBitmap(stampBitmap, charRect);

        }


        private SKBitmap GetSingleCharacterLabelImage()
        {
            int size = 100; // center to edge

            SKBitmap bitmap = new SKBitmap(size * 2, size * 2);

            using (SKCanvas canvas = new SKCanvas(bitmap))
            using (SKPaint paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.Style = SKPaintStyle.Fill;
                paint.Shader = SKShader.CreateRadialGradient(
                    new SKPoint(size, size),
                    size,
                    new SKColor[] { SKColors.White, SKColors.Black },
                    SKShaderTileMode.Decal
                    );

                canvas.DrawCircle(0, 0, size*4, paint);
            }

            return bitmap;
        }


        private void DrawCharacterHeatMap(SKRect charRect, SKCanvas labelCanvas)
        {
            // adjust the rectangle a bit
            charRect = new SKRect(charRect.Left, charRect.Top - 2, charRect.Right + 1, charRect.Bottom + 2);

            SKBitmap stampBitmap = GetSingleCharacterHeatMapImage();
            using (SKPaint paint = new SKPaint())
            {
                paint.Color = paint.Color.WithAlpha(0x90);
                labelCanvas.DrawBitmap(stampBitmap, charRect, paint);
            }
                
        }


        private SKBitmap GetSingleCharacterHeatMapImage()
        {
            int size = 100; // center to edge
            SKBitmap bitmap = new SKBitmap(size * 2, size * 2);

            using (SKCanvas canvas = new SKCanvas(bitmap))
            using (SKPaint paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.Style = SKPaintStyle.Fill;
                paint.Shader = SKShader.CreateRadialGradient(
                    new SKPoint(size, size),
                    size,
                    new SKColor[] {  SKColors.Black, SKColors.Red, SKColors.Orange, SKColors.Yellow, SKColors.LightGreen, SKColors.DarkCyan, SKColors.Blue },
                    SKShaderTileMode.Decal
                    );

                canvas.DrawCircle(0, 0, size * 4, paint);
            }
            return bitmap;
        }


        private SKRect[] ConstructCharacterBoundingBoxes(string text, SKPoint[] positions, float[] widths, SKRect textBox)
        {
            if (text.Length != positions.Length || positions.Length != widths.Length)
            {
                throw new ArgumentException("Expected length of text, positions and widths to be the same.");
            }

            SKRect[] boxes = new SKRect[text.Length];

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != ' ')
                {
                    SKPoint pos = positions[i];
                    float witdh = widths[i];
                    SKRect box = new SKRect(pos.X, textBox.Top, pos.X + witdh, textBox.Bottom);
                    boxes[i] = box;
                }
            }

            return boxes;
        }




        public SKRect GetCroppedCharacterBox(SKRect charRect, char symbol, SKFont font, int yBaseline)
        {
            using (SKBitmap b = new SKBitmap((int)charRect.Width + 2, (int)charRect.Height + 2))
            using (SKCanvas c = new SKCanvas(b))
            {
                SKColor charColor = SKColors.White;
                using (SKPaint paint = new SKPaint())
                {
                    paint.IsAntialias = true;
                    paint.Color = charColor;
                    c.DrawText(symbol.ToString(), 0, yBaseline, font, paint);
                }

                int top = 0;
                int bottom = 0;

                //find the top row
                bool topFound = false;
                int minTop = (int)(charRect.Height * 0.4);
                for (int y = 0; y < (int)charRect.Height - 1; y++)
                {
                    for (int x = 0; x < (int)charRect.Width; x++)
                    {
                        SKColor pixel = b.GetPixel(x, y);
                        if (pixel.Blue > 1 || y > minTop - 1)
                        {
                            top = y;
                            topFound = true;
                        }
                        if (topFound) break;
                    }
                    if (topFound) break;
                }

                //find the bottom row
                bool bottomFound = false;
                int maxBottom = (int)(charRect.Height - (charRect.Height * 0.4));
                for (int y = (int)(charRect.Height - 1); y > 1; y--)
                {
                    for (int x = 0; x < (int)charRect.Width - 1; x++)
                    {
                        SKColor pixel = b.GetPixel(x, y);
                        if (pixel.Blue > 1 || y < maxBottom + 1)
                        {
                            bottom = y;
                            bottomFound = true;
                        }
                        if (bottomFound) break;
                    }
                    if (bottomFound) break;
                }

                SKRect croppedRect = new SKRect(
                    charRect.Left,
                    charRect.Top + top,
                    charRect.Right,
                    charRect.Top + bottom

                    ); 
                return croppedRect;
            }
        }




        // collision detection
        private bool RectanglesOverlap(SKRect r1, SKRect r2)
        {
            bool noOverlap = r1.Left > r2.Right ||
                             r2.Left > r1.Right ||
                             r1.Top > r2.Bottom ||
                             r2.Top > r1.Bottom;

            return !noOverlap;
        }


        private SKRectI RectToRectI(SKRect rectOfFloat)
        {
            return new SKRectI((int)rectOfFloat.Left, (int)rectOfFloat.Top, (int)rectOfFloat.Right, (int)rectOfFloat.Bottom);
        }



        // save to file
        public void SaveTextImage(string path, SKEncodedImageFormat format)
        {
            using (SKData data = TextImage.Encode(format, 100))
            using (FileStream fileStream = File.OpenWrite(path))
            {
                // save the data to a stream
                data.SaveTo(fileStream);
            }
        }

        public void SaveLabelImage(string path, SKEncodedImageFormat format)
        {
            using (SKData data = LabelImage.Encode(format, 100))
            using (FileStream fileStream = File.OpenWrite(path))
            {
                // save the data to a stream
                data.SaveTo(fileStream);
            }
        }


        public void SaveBoudingBoxesToTextFile(string path, DataFileType dataFileType)
        {

            switch(dataFileType)
            {
                case DataFileType.None:
                    break; // do nothing

                case DataFileType.CsvCharactersOnly:
                    SaveToCsvFile(path, true, false);
                    break;

                case DataFileType.CsvWordsOnly:
                    SaveToCsvFile(path, false, true);
                    break;

                case DataFileType.CsvWordsAndCharacters:
                    SaveToCsvFile(path, true, true);
                    break;

                case DataFileType.JsonCharactersOnly:
                    SaveToJsonFile(path, true, false);
                    break;

                case DataFileType.JsonWordsOnly:
                    SaveToJsonFile(path, false, true);
                    break;

                case DataFileType.JsonWordsAndCharacters:
                    SaveToJsonFile(path, true, true);
                    break;

                default:
                    throw new NotImplementedException("No save method implmented for this option");

            }

        }

        private void SaveToCsvFile(string path, bool writeCharacters, bool writeWords)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("type,text,left,top,right,bottom,width,height,x_center,y_center");

            foreach (ContentArea contentArea in _contentAreas)
            {

                if (contentArea is TextContentArea textContentArea)
                {

                    foreach (WordContentArea word in textContentArea.Words)
                    {
                        if (writeWords)
                        {
                            if (word.Text.Contains(',')) // escape values containing comma
                            {
                                // Escaping conmvention supported by MS Excel.
                                // Values containing commas are wrapped in double quotes ('12,345.00' -> '"12,345.00"').
                                // Double quotes in values are escaped as two double quotes ('"' -> '""').


                                // yeah yeah, I know, the following two lines go against the use of StringBuilder but this won't be every word.
                                word.Text.Replace("\"", "\"\"");
                                word.Text = $"\"{word.Text}\"";
                            }
                            stringBuilder.Append("word,");
                            stringBuilder.Append(word.Text);
                            stringBuilder.Append(",");
                            stringBuilder.Append(word.Rect.Left);
                            stringBuilder.Append(",");
                            stringBuilder.Append(word.Rect.Top);
                            stringBuilder.Append(",");
                            stringBuilder.Append(word.Rect.Right);
                            stringBuilder.Append(",");
                            stringBuilder.Append(word.Rect.Bottom);
                            stringBuilder.Append(",");
                            stringBuilder.Append(word.Rect.Width);
                            stringBuilder.Append(",");
                            stringBuilder.Append(word.Rect.Height);
                            stringBuilder.Append(",");
                            stringBuilder.Append(word.Rect.MidX);
                            stringBuilder.Append(",");
                            stringBuilder.Append(word.Rect.MidY);
                            stringBuilder.Append(Environment.NewLine);
                        }

                        if (writeCharacters)
                        {
                            foreach (CharacterContentArea character in word.Characters)
                            {
                                string symbol = character.Symbol.ToString();
                                if (character.Symbol == ',')
                                {
                                    symbol = "\",\"";
                                }
                                if (character.Symbol == '"')
                                {
                                    symbol = "\"\"";
                                }

                                stringBuilder.Append("character,");
                                stringBuilder.Append(symbol);
                                stringBuilder.Append(",");
                                stringBuilder.Append(character.Rect.Left);
                                stringBuilder.Append(",");
                                stringBuilder.Append(character.Rect.Top);
                                stringBuilder.Append(",");
                                stringBuilder.Append(character.Rect.Right);
                                stringBuilder.Append(",");
                                stringBuilder.Append(character.Rect.Bottom);
                                stringBuilder.Append(",");
                                stringBuilder.Append(character.Rect.Width);
                                stringBuilder.Append(",");
                                stringBuilder.Append(character.Rect.Height);
                                stringBuilder.Append(",");
                                stringBuilder.Append(character.Rect.MidX);
                                stringBuilder.Append(",");
                                stringBuilder.Append(character.Rect.MidY);
                                stringBuilder.Append(Environment.NewLine);
                            }
                        }
                    }
                }
            }

            File.AppendAllText(path + ".csv", stringBuilder.ToString());

        }



        private void SaveToJsonFile(string path, bool writeCharacters, bool writeWords)
        {
            List<WordContentArea> wordsForExport = new List<WordContentArea>();

            foreach (ContentArea contentArea in _contentAreas)
            {
                if (contentArea is TextContentArea textContentArea)
                {
                    foreach (WordContentArea word in textContentArea.Words)
                    {
                        wordsForExport.Add(word);
                    }
                }
            }


            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };


            if (writeCharacters && writeWords)
            {
                string jsonString = JsonSerializer.Serialize(wordsForExport, options);
                File.AppendAllText(path + ".json", jsonString);
            }
            else if (writeWords)
            {
                wordsForExport.ForEach(word => word.Characters = null);
                string jsonString = JsonSerializer.Serialize(wordsForExport, options);
                File.AppendAllText(path + ".json", jsonString);

            }
            else if (writeCharacters)
            {
                var charactersForExport = wordsForExport.SelectMany(word => word.Characters).ToList();
                string jsonString = JsonSerializer.Serialize(charactersForExport, options);
                File.AppendAllText(path + ".json", jsonString);

            }
            else
            {
                // export noting
            }

        }


    }
}

﻿using OcrSyntheticDataGenerator.ContentModel;
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

namespace OcrSyntheticDataGenerator.ImageGeneration;

public abstract class ImageAndLabelGeneratorBase
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

    public enum LayoutFileType
    {
        [Description("Scattered Text")] ScatteredText,
        [Description("Paragraph")] Paragraph,
        [Description("Table")] Table,
    };

    public enum CharacterBoxNormalisationType
    {
        [Description("Stretch")] Stretch,
        [Description("Include surrounding pixels")] IncludeSurroundingPixels,
    };


    protected int _imageWidth;
    protected int _imageHeight;
    protected RandomTextGenerator _randomTextGenerator = new RandomTextGenerator();
    protected List<ContentArea> _contentAreas = new List<ContentArea>();
    protected Random _rnd = new Random();
    protected bool _hasDarkBackgroundImage = false;
    protected SKColor _backgroundColour = SKColors.White;


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

    


    public ImageAndLabelGeneratorBase(int imageWidth, int imageHeight)
    {
        _imageWidth = imageWidth;
        _imageHeight = imageHeight;

        TextImage = new SKBitmap(imageWidth, imageHeight);
        TextMeasuringImage = new SKBitmap(imageWidth, imageHeight);
        LabelImage = new SKBitmap(imageWidth, imageHeight);
        HeatMapImage = new SKBitmap(imageWidth, imageHeight);
        CharacterBoxImage = new SKBitmap(imageWidth, imageHeight);
    }


    // decendants must override
    public abstract void Generate();





    protected void PostProcessing(int noisePercentage)
    {
        // draw foreground noise 
        if (noisePercentage <= NoiseProbability)
        {
            ImageProcessing.DrawForgroundNoise(TextImage);
        }

        // blur
        if (BlurProbability >= _rnd.Next(1, 100))
        {
            float xBlurAmount = _rnd.Next(90, 110) / 100;
            float yBlurAmount = _rnd.Next(90, 110) / 100;
            ImageProcessing.BlurImage(TextImage, xBlurAmount, yBlurAmount);
            // darken if blured
            ImageProcessing.DarkenImage(TextImage);
        }

        // pixelate
        if (PixelateProbability >= _rnd.Next(1, 100))
        {
            double pixelateAmount =  (_rnd.NextDouble() * 0.20) + 1.0; // 1.0 - 1.25
            ImageProcessing.PixelateImage(TextImage, pixelateAmount);
        }

        // invert
        if (InvertProbability >= _rnd.Next(1, 100))
        {
            ImageProcessing.InvertImage(TextImage);
        }
    }





    protected SKRect[] GetCroppedCharacterBoxes(string text, int yTextBaseline, SKFont font, SKRect[] characterBoxes)
    {
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

        return croppedCharacterBoxes;
    }


    protected static void DrawTextOnCanvas(SKCanvas textCanvas, string text, int x, int yTextBaseline, SKFont font, SKColor textColor)
    {
        using (SKPaint paint = new SKPaint())
        {
            paint.IsAntialias = true;
            paint.Color = textColor;
            textCanvas.DrawText(text, x, yTextBaseline, font, paint);
        }
    }



    protected void DrawTextBlockBackgroundColour(SKCanvas textCanvas, bool isInverted, bool isLightText, bool isBoxAroundText, SKRect backgoundRect)
    {
        if (isInverted)
        {
            byte alpha = (byte)_rnd.Next(160, 255); // dark background
            _backgroundColour = new SKColor(0x00, 0x00, 0x00, alpha);
            using (SKPaint paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.Color = _backgroundColour;
                textCanvas.DrawRect(backgoundRect, paint);
            }
        }
        else
        {
            // not inverse, but we want to sometimes draw a background colour, like a highlighter
            if (isLightText)
            {
                byte alpha = (byte)_rnd.Next(0, 128);
                _backgroundColour = new SKColor(0x00, 0x00, 0x00, alpha);
                using (SKPaint paint = new SKPaint())
                {
                    paint.IsAntialias = true;
                    paint.Color = _backgroundColour;
                    textCanvas.DrawRect(backgoundRect, paint);
                }
            }
            // how about a box around the text
            else if (isBoxAroundText)
            {
                // random offset
                int offset = _rnd.Next(-10, 8);
                backgoundRect.Top += offset;
                backgoundRect.Left += offset;

                // dark coloured box
                byte rgb = (byte)_rnd.Next(0, 96);
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
    }

    protected bool CheckOverlapsWithExistingObjects(SKRect measureRect)
    {
        foreach (var area in _contentAreas)
        {
            SKRect overlapRect = new SKRect(measureRect.Left, measureRect.Top, measureRect.Right, measureRect.Bottom);
            overlapRect.Inflate(5, 5);
            if (RectanglesOverlap(area.Rect, overlapRect))
            {
                return true;
            }
        }

        return false;
    }


    protected void DrawLines(SKCanvas textCanvas)
    {
        int numberOfHorizontalLines = _rnd.Next(1, 5);
        int numberOfVerticalLines = _rnd.Next(1, 4);


        for (int hl = 0; hl < numberOfHorizontalLines; hl++)
        {
            int lineY = _rnd.Next(0, _imageHeight - 1);
            int lineX = _rnd.Next(0, _imageHeight / 2);
            int lineLength = _rnd.Next(30, _imageWidth * 3);
            int lineThickness = _rnd.Next(1, 4);
            bool isDashedLine = _rnd.Next(1, 100) < 20;
            SKRect lineRect = new SKRect(lineX, lineY, lineX + lineLength, lineY + lineThickness);

            SKPath path = new SKPath();
            path.MoveTo(lineX, lineY);
            path.LineTo(lineX + lineLength, lineY);

            using (SKPaint paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.Color = SKColors.Black;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = lineThickness;
                if (isDashedLine) { paint.PathEffect = SKPathEffect.CreateDash(GetRandomLineDashPattern(), 0); }
                textCanvas.DrawPath(path, paint);
            }

            LineContentArea lca = new LineContentArea() { Rect = RectToRectI(lineRect) };
            _contentAreas.Add(lca);
        }
        for (int vl = 0; vl < numberOfVerticalLines; vl++)
        {
            int lineY = _rnd.Next(0, _imageHeight / 2);
            int lineX = _rnd.Next(0, _imageHeight - 1);
            int lineLength = _rnd.Next(30, _imageHeight * 3);
            int lineThickness = _rnd.Next(1, 4);
            bool isDashedLine = _rnd.Next(1, 100) < 8;
            SKRect lineRect = new SKRect(lineX, lineY, lineX + lineThickness, lineY + lineLength);

            SKPath path = new SKPath();
            path.MoveTo(lineX, lineY);
            path.LineTo(lineX, lineY + lineLength);
            using (SKPaint paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.Color = SKColors.Black;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = lineThickness;
                if (isDashedLine) { paint.PathEffect = SKPathEffect.CreateDash(GetRandomLineDashPattern(), 0); }
                textCanvas.DrawPath(path, paint);
            }

            LineContentArea lca = new LineContentArea() { Rect = RectToRectI(lineRect) };
            _contentAreas.Add(lca);
        }
    }



    protected float[] GetRandomLineDashPattern()
    {

        if (_rnd.Next(1, 100) < 90) // most of the time, just a simple dash
        {
            float dashSize = _rnd.Next(5, 20);
            float gapSize = _rnd.Next(5, 20);
            return new float[] { dashSize, gapSize };
        }
        else // sometimes do a complex dash of shorter and longer dashes
        {
            float smallDashSize = _rnd.Next(5, 10);
            float largeDashSize = _rnd.Next(12, 30);
            float gapSize = _rnd.Next(5, 20);
            return new float[] { smallDashSize, gapSize, largeDashSize, gapSize };
        }
    }



    protected void DrawBackgroundImage(SKCanvas textCanvas)
    {
        DirectoryInfo backgroundImageFolder = new DirectoryInfo("./BackgroundImages");
        if (backgroundImageFolder != null)
        {
            var files = backgroundImageFolder.GetFiles();
            int randomFileIndex = _rnd.Next(0, files.Length);
            var imageFile = files[randomFileIndex];

            using (SKBitmap backgroundImage = SKBitmap.Decode(imageFile.FullName))
            {
                int randomRotation = _rnd.Next(0, 55);

                double randomScale = _rnd.NextDouble() + 0.5;

                SKSizeI size = new SKSizeI();
                size.Width = (int)(backgroundImage.Width * randomScale);
                size.Height = (int)(backgroundImage.Height * randomScale);

                SKBitmap scaledBackgroundImage = backgroundImage.Resize(size, SKFilterQuality.Low);


                // random tiling modes
                SKShaderTileMode tileMode = SKShaderTileMode.Repeat;
                if (_rnd.Next(1, 100) > 50)
                {
                    tileMode = SKShaderTileMode.Mirror;
                }

                using (SKPaint paint = new SKPaint())
                {
                    paint.Shader = SKShader.CreateBitmap(scaledBackgroundImage, tileMode, tileMode);
                    textCanvas.DrawRect(TextImage.Info.Rect, paint);
                }

                // get average darkness of background image
                int totalLightness = 0;
                int totalPixels = 0;
                for (int pxX = 0; pxX < scaledBackgroundImage.Info.Width; pxX += 1)
                {
                    for (int pxY = 0; pxY < scaledBackgroundImage.Info.Height; pxY += 1)
                    {
                        totalLightness += scaledBackgroundImage.GetPixel(pxX, pxY).Red;
                        totalPixels++;
                    }
                }
                int averageLightness = totalLightness / totalPixels;
                Debug.WriteLine($"background lightness: {averageLightness}");
                if (averageLightness <= 240)
                {
                    _hasDarkBackgroundImage = true;
                }
            }
        }
    }


    protected List<WordContentArea> CreateWordAndCharacterObjects(string text, SKRect[] characterBoxes)
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


    protected SKRectI GrowRectToEngulfNewRect(SKRectI existingRect, SKRectI newRect)
    {
        if (newRect.Left < existingRect.Left) { existingRect.Left = newRect.Left; }
        if (newRect.Top < existingRect.Top) { existingRect.Top = newRect.Top; }
        if (newRect.Right > existingRect.Right) { existingRect.Right = newRect.Right; }
        if (newRect.Bottom > existingRect.Bottom) {  existingRect.Bottom = newRect.Bottom; }
        return existingRect;
    }


    protected void DrawCharacterProbabilityLabels(SKRect charRect, SKCanvas labelCanvas)
    {
        // adjust the rectangle a bit
        charRect = new SKRect(charRect.Left, charRect.Top - 2, charRect.Right + 1, charRect.Bottom + 2);

        SKBitmap stampBitmap = GetSingleCharacterLabelImage();
        labelCanvas.DrawBitmap(stampBitmap, charRect);

    }


    protected SKBitmap GetSingleCharacterLabelImage()
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



    protected void DrawLabels(
        SKFont font,
        SKCanvas labelCanvas,
        SKCanvas heatMapCanvas,
        SKCanvas characterBoxCanvas,
        string text,
        int x,
        int yTextBaseline,
        SKRect[] croppedCharacterBoxes,
        List<WordContentArea> words)
    {

        SKColor textColor;
        var heatMapTextColour = SKColors.Black;
        DrawTextOnCanvas(heatMapCanvas, text, x, yTextBaseline, font, heatMapTextColour);



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


    protected void DrawCharacterHeatMap(SKRect charRect, SKCanvas labelCanvas)
    {
        // adjust the rectangle a bit
        charRect = new SKRect(charRect.Left, charRect.Top - 2, charRect.Right + 1, charRect.Bottom + 2);

        SKBitmap stampBitmap = GetSingleCharacterHeatMapImage();
        using (SKPaint paint = new SKPaint())
        {
            paint.Color = paint.Color.WithAlpha(0xA0);
            labelCanvas.DrawBitmap(stampBitmap, charRect, paint);
        }
            
    }


    protected SKBitmap GetSingleCharacterHeatMapImage()
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


    protected SKRect[] ConstructCharacterBoundingBoxes(string text, SKPoint[] positions, float[] widths, SKRect textBox)
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
            int maxBottom = (int)(charRect.Height - (charRect.Height * 0.3));
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

            // adjust to the right for itallic fonts
            if (font.Typeface.IsItalic)
            {
                float shift = charRect.Width * 0.135f;
                charRect.Left += shift;
                charRect.Right += shift;
            }


            SKRect croppedRect = new SKRect(
                charRect.Left,
                charRect.Top + top - (float)(charRect.Height * 0.05),
                charRect.Right,
                charRect.Top + bottom + (float)(charRect.Height * 0.05)
                );


            return croppedRect;
        }
    }


    // collision detection
    protected bool RectanglesOverlap(SKRect r1, SKRect r2)
    {
        bool noOverlap = r1.Left > r2.Right ||
                         r2.Left > r1.Right ||
                         r1.Top > r2.Bottom ||
                         r2.Top > r1.Bottom;

        return !noOverlap;
    }


    protected bool RectangleFitsInside(SKRect innerRect, SKRect outerRect)
    {
        return innerRect.Left   >= outerRect.Left  &&
               innerRect.Top    >= outerRect.Top   &&
               innerRect.Right  <= outerRect.Right &&
               innerRect.Bottom <= outerRect.Bottom;
    }


    protected SKRectI RectToRectI(SKRect rectOfFloat)
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
                            // Escaping convention supported by MS Excel.
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
            // export nothing
        }

    }




    public void SaveCharacterImageFiles(string path, CharacterBoxNormalisationType normalisationType)
    {

        foreach (ContentArea contentArea in _contentAreas)
        {
            if (contentArea is TextContentArea textContentArea)
            {
                foreach (WordContentArea word in textContentArea.Words)
                {
                    foreach (CharacterContentArea character in word.Characters)
                    {
                        
                        // get the image of the character
                        switch (normalisationType)
                        {
                            case CharacterBoxNormalisationType.Stretch:
                                SaveCharacterImageFileStretched(path, character);

                                break;
                            case CharacterBoxNormalisationType.IncludeSurroundingPixels:
                                SaveCharacterImageFileIncludeSurroundingPixels(path, character);
                                break;
                            default:
                                break;

                        }

                    }
                }
            }
        }
    }



    private void SaveCharacterImageFileStretched(string baseSavePath, CharacterContentArea character)
    {

        // get the subimage for the character's rectangle
        SKBitmap tempCharImage = new SKBitmap(character.Rect.Width, character.Rect.Height);

        SKRectI inflatedRect = SKRectI.Inflate(character.Rect, 1, 2);

        TextImage.ExtractSubset(tempCharImage, inflatedRect);


        // if the character is at the edge of the page, the subset won't be centred on the character, but will be offset.
        // We need to calculate any offset then draw the temp bitmap onto the final bitmap.
        // This is probably never going to happen but I'm writing the code here to apply to the other normalisation method "IncludeSurroundingPixels".

        int xOffset = 0;
        int yOffset = 0;
        if (inflatedRect.Left < 0)   
        { 
            xOffset = - inflatedRect.Left - (character.Rect.Width / 2); 
        }
        if (inflatedRect.Right > TextImage.Width)  
        { 
            xOffset = (TextImage.Width - inflatedRect.Right) + (character.Rect.Width / 2); 
        }
        if (inflatedRect.Top < 0)    
        { 
            yOffset = - inflatedRect.Top - (character.Rect.Height / 2); 
        }
        if (inflatedRect.Bottom > TextImage.Height) 
        { 
            yOffset = (TextImage.Height - inflatedRect.Bottom) + (character.Rect.Height / 2);
        }

        SKBitmap charImage = new SKBitmap(inflatedRect.Width, inflatedRect.Height);
        SKRectI drawLocation = new SKRectI(xOffset, yOffset, xOffset+inflatedRect.Width, yOffset+inflatedRect.Height);

        using (SKCanvas charImageCanvas = new SKCanvas(charImage))
        {
            charImageCanvas.Clear(SKColors.Black);
            charImageCanvas.DrawBitmap(tempCharImage, drawLocation);
        }


        // resize
        SKSizeI size = new SKSizeI(22, 22); // TODO: make size selectable/changeble
        SKBitmap scaledCharacterImage = charImage.Resize(size, SKFilterQuality.High);

        // save to file
        SaveCharacterImage(baseSavePath, character, scaledCharacterImage);
    }


    private void SaveCharacterImageFileIncludeSurroundingPixels(string baseSavePath, CharacterContentArea character)
    {

        // get the subimage for the character's rectangle
        SKBitmap tempCharImage = new SKBitmap(character.Rect.Width, character.Rect.Height);

        // calcualate dimensions of a square 
        int xInflateAmount = 0;
        int yInflateAmount = 2;

        if (character.Rect.Width < character.Rect.Height)
        {
            xInflateAmount = (character.Rect.Height - character.Rect.Width) / 2;
        }

        if (character.Rect.Width > character.Rect.Height)
        {
            yInflateAmount = (character.Rect.Width - character.Rect.Height) / 2;
        }


        SKRectI inflatedRect = SKRectI.Inflate(character.Rect, xInflateAmount, yInflateAmount);

        TextImage.ExtractSubset(tempCharImage, inflatedRect);


        // if the character is at the edge of the page, the subset won't be centred on the character, but will be offset.
        // We need to calculate any offset then draw the temp bitmap onto the final bitmap.

        int xOffset = 0;
        int yOffset = 0;
        if (inflatedRect.Left < 0)
        {
            xOffset = -inflatedRect.Left - (character.Rect.Width / 2);
        }
        if (inflatedRect.Right > TextImage.Width)
        {
            xOffset = (TextImage.Width - inflatedRect.Right) + (character.Rect.Width / 2);
        }
        if (inflatedRect.Top < 0)
        {
            yOffset = -inflatedRect.Top - (character.Rect.Height / 2);
        }
        if (inflatedRect.Bottom > TextImage.Height)
        {
            yOffset = (TextImage.Height - inflatedRect.Bottom) + (character.Rect.Height / 2);
        }

        SKBitmap charImage = new SKBitmap(inflatedRect.Width, inflatedRect.Height);
        SKRectI drawLocation = new SKRectI(xOffset, yOffset, xOffset + inflatedRect.Width, yOffset + inflatedRect.Height);

        using (SKCanvas charImageCanvas = new SKCanvas(charImage))
        {
            charImageCanvas.Clear(SKColors.Black);
            charImageCanvas.DrawBitmap(tempCharImage, drawLocation);
        }


        // resize
        SKSizeI size = new SKSizeI(22, 22); // TODO: make size selectable/changeble
        SKBitmap scaledCharacterImage = charImage.Resize(size, SKFilterQuality.High);

        // save to file
        SaveCharacterImage(baseSavePath, character, scaledCharacterImage);

    }


    private void SaveCharacterImage(string baseSavePath, CharacterContentArea character, SKBitmap bitmap)
    {
        // get the training label class
        string characterClass = CharacterClassDictionary.CharacterClasses[character.Symbol];


        string saveDirectoryPath = System.IO.Path.Combine(baseSavePath, characterClass);

        // create folder if not exists
        if (!Directory.Exists(saveDirectoryPath))
        {
            Directory.CreateDirectory(saveDirectoryPath);
        }

        string fullPath = System.IO.Path.Combine(saveDirectoryPath, $"{Guid.NewGuid()}.png");

        // write file
        using (SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100))
        using (FileStream fileStream = File.OpenWrite(fullPath))
        {
            // save the data to a stream
            data.SaveTo(fileStream);
        }
    }
}

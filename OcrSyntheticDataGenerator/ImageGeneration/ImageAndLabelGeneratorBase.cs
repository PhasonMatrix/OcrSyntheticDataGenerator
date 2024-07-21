using Avalonia.Controls;
using Avalonia.Media;
using Microsoft.CodeAnalysis;
using OcrSyntheticDataGenerator.ContentModel;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
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
        [Description("Include surrounding pixels")] IncludeSurroundingPixels,
        [Description("Stretch")] Stretch,
    };


    protected int _imageWidth;
    protected int _imageHeight;
    protected RandomTextGenerator _randomTextGenerator = new RandomTextGenerator();
    protected List<ContentArea> _contentAreas = new List<ContentArea>();
    protected Random _rnd = new Random();
    protected bool _hasDarkBackgroundImage = false;
    protected SKColor _backgroundColour = SKColors.White;
    protected SKColor _veryDarkGrey = new SKColor(0x18, 0x18, 0x18);
    protected SKColor _veryDarkCyan = new SKColor(0x00, 0x40, 0xff, 0xa0);
    private int characterImageFileClassLimit = 2_000;

    public SKBitmap TextImage { get; set; }
    public SKBitmap LabelImage { get; set; }
    public SKBitmap HeatMapImage { get; set; }
    public SKBitmap BoundingBoxImage { get; set; }


    public int LinesProbability { get; set; }
    public int BackgroundProbability { get; set; }
    public int NoiseProbability { get; set; }
    public int BlurProbability { get; set; }
    public int InvertProbability { get; set; }
    public int PixelateProbability { get; set; }
    public bool HasBackgroundTexture { get; set; }




    public ImageAndLabelGeneratorBase(int imageWidth, int imageHeight)
    {
        _imageWidth = imageWidth;
        _imageHeight = imageHeight;

        TextImage = new SKBitmap(imageWidth, imageHeight);
        LabelImage = new SKBitmap(imageWidth, imageHeight);
        HeatMapImage = new SKBitmap(imageWidth, imageHeight);
        BoundingBoxImage = new SKBitmap(imageWidth, imageHeight);
    }


    // decendants must override
    public abstract void GenerateContent();
    public abstract void DrawContent();


    public void GenerationPipeline(bool creatingFiles)
    {
        // clear previous run
        _contentAreas = new List<ContentArea>();

        PreProcessing();
        GenerateContent();
        DrawContent();
        DrawLabels();
        PostProcessing();

        if (!creatingFiles)
        {
            DrawHeatMap();
            DrawBoundingBoxes();
        }
    }



    protected void PreProcessing()
    {
        int backgroundImagePercentage = _rnd.Next(1, 100);
        HasBackgroundTexture = backgroundImagePercentage <= BackgroundProbability;
        

        using (SKCanvas textCanvas = new SKCanvas(TextImage))
        {
            textCanvas.Clear(SKColors.White);
        }




        // draw background noise 
        int noisePercentage = _rnd.Next(1, 100);

        if (noisePercentage <= NoiseProbability)
        {
            ImageProcessing.DrawBackgroundNoise(TextImage);
        }

        if (HasBackgroundTexture)
        {
            DrawBackgroundImage();
        }
    }



    protected void PostProcessing()
    {
        int noisePercentage = _rnd.Next(1, 100);

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



    protected List<WordContentArea> ConstructWordAndCharacterObjects(
        SKFont font, 
        string text, 
        int yTextBaseline, 
        SKPoint[] glyphPositions, 
        float[] glyphWidths, 
        SKFontMetrics fontMetrics, 
        SKRectI measureRect,
        bool isInverted)
    {
        //SKRect[] characterBoxes = ConstructCharacterBoundingBoxes(text, glyphPositions, glyphWidths, measureRect, yTextBaseline, fontMetrics, font);
        SKRect[] characterBoxes = ConstructCharacterBoundingBoxes(text, glyphPositions, glyphWidths, null, yTextBaseline, fontMetrics, font);

        // measure and crop character box top and bottom
        SKRect[] croppedCharacterBoxes = GetCroppedCharacterBoxes(text, yTextBaseline, font, characterBoxes, fontMetrics);

        List<WordContentArea> words = CreateWordAndCharacterObjects(text, characterBoxes, croppedCharacterBoxes, yTextBaseline, font, isInverted);
        return words;
    }



    protected SKRect[] ConstructCharacterBoundingBoxes(string text, SKPoint[] positions, float[] widths, SKRect? textBox, int yBaseline, SKFontMetrics fontMetrics, SKFont font)
    {
        if (text.Length != positions.Length || positions.Length != widths.Length)
        {
            throw new ArgumentException("Expected length of text, positions and widths to be the same.");
        }

        SKRect[] boxes = new SKRect[text.Length];


        //double accentFactor = _randomTextGenerator.FontCustomAscentFactor[font.Typeface.FamilyName];
        //int accent = (int)(font.Size * accentFactor);


        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != ' ')
            {
                SKPoint pos = positions[i];
                float width = widths[i];
                float bottom = yBaseline + fontMetrics.Bottom;
                float top = yBaseline + fontMetrics.Top;
                

                // some fonts have top and bottom lines that are too far from the drawn character. we correct for them individually
                if (font.Typeface.FamilyName == "Dubai")
                {
                    float height = bottom - top;
                    bottom = bottom - height * 0.2f;
                }

                if (font.Typeface.FamilyName == "Franklin Gothic" || font.Typeface.FamilyName == "Gabriola" || font.Typeface.FamilyName == "Gill Sans MT" || font.Typeface.FamilyName == "Yu Gothic")
                {
                    float height = bottom - top;
                    bottom = bottom - height * 0.05f;
                }


                SKRect box = new SKRect(pos.X, top, pos.X + width, bottom);

                // adjust to the right for itallic fonts
                if (font.Typeface.IsItalic)
                {
                    float shift = box.Width * 0.20f;
                    box.Left += shift;
                    box.Right += shift;
                }

                boxes[i] = box;
            }
        }
        
        return boxes;
    }


    protected SKRect[] GetCroppedCharacterBoxes(string text, int yTextBaseline, SKFont font, SKRect[] characterBoxes, SKFontMetrics fontMetrics)
    {
        SKRect[] croppedCharacterBoxes = new SKRect[characterBoxes.Length];
        for (int c = 0; c < characterBoxes.Length; c++)
        {
            SKRect currentCharBox = characterBoxes[c];
            char currentSymbol = text[c];
            // difference between charbox top and the y basline where it was drawn
            int baselineOffset = (int)(yTextBaseline - currentCharBox.Top);

            //var croppedCharBox = GetCroppedCharacterBox(currentCharBox, currentSymbol, font, baselineOffset);
            var croppedCharBox = GetCroppedCharacterBoxByFontMetrics(currentCharBox, currentSymbol, font, yTextBaseline, fontMetrics);
            croppedCharacterBoxes[c] = croppedCharBox;
        }

        return croppedCharacterBoxes;
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
                for (int x = 1; x < (int)charRect.Width-1; x++)
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
            int maxBottom = yBaseline; // (int)(charRect.Height - (charRect.Height * 0.3));
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

            //// adjust to the right for itallic fonts
            //if (font.Typeface.IsItalic)
            //{
            //    float shift = charRect.Width * 0.14f;
            //    charRect.Left += shift;
            //    charRect.Right += shift;
            //}

            SKRect croppedRect = new SKRect(
                charRect.Left,
                charRect.Top + top - (float)(charRect.Height * 0.05),
                charRect.Right,
                charRect.Top + bottom + (float)(charRect.Height * 0.05)
                );


            return croppedRect;
        }
    }



    public SKRect GetCroppedCharacterBoxByFontMetrics(SKRect charRect, char symbol, SKFont font, int yBaseline, SKFontMetrics fontMetrics)
    {
        float top = 0;
        float bottom = 0;

        float correctedTop = (-fontMetrics.CapHeight + fontMetrics.Ascent) / 2;
        float correctedBottom = fontMetrics.Descent * 0.9f;
        if (font.Typeface.FamilyName == "Microsoft Himalaya" 
            || font.Typeface.FamilyName == "Dubai"
            || font.Typeface.FamilyName == "Impact"
            )
        {
            correctedBottom = fontMetrics.Descent * 0.6f;
        }

        if (font.Typeface.FamilyName == "Dubai" 
            || font.Typeface.FamilyName == "Gill Sans MT")
        {
            correctedTop = correctedTop * 0.9f;
        }

        if (font.Typeface.FamilyName == "Lucida Console")
        {
            correctedTop = correctedTop * 1.2f;
        }


        switch (symbol)
        {
            case 'a': case 'c': case 'e': case 'm': case 'n':
            case 'o': case 'r': case 's': case 'u': case 'v':
            case 'w': case 'x': case 'z':
            case '-': case '=': case '–':
            case ':': case '.': case '«': case '»':
                top = -fontMetrics.XHeight;
                break;

            case 'b': case 'd': case 'f': case 'h':
            case 'i': case 'k': case 'l': case 't':
            case '*': case '\'': case '"':
            case '‘': case '’': case '“': case '”':
            case '©': case '®':
            case '^': case '!': 
                //top = fontMetrics.Ascent;
                top = correctedTop;
                break;

            case 'g': case 'p': case 'q':  case 'y':
            case ',':  case ';':
                top = -fontMetrics.XHeight;
                bottom = correctedBottom;
                break;

            case 'j':
                if (font.Typeface.FamilyName == "Cascadia Code"
                    || font.Typeface.FamilyName == "Consolas"
                    || font.Typeface.FamilyName == "Courier New"
                    || font.Typeface.FamilyName == "Lucida Console"
                    || font.Typeface.FamilyName == "Lucida Sans"
                    || font.Typeface.FamilyName == "Lucida Sans Typewriter"
                    || font.Typeface.FamilyName == "Microsoft Himalaya"
                    )
                {
                    top = correctedTop;
                }
                else
                {
                    top = -fontMetrics.CapHeight;
                }
                bottom = correctedBottom;
                break;

            case 'A': case 'B': case 'C': case 'D': case 'E':
            case 'F': case 'G': case 'H': case 'I': case 'K': 
            case 'L': case 'M': case 'N': case 'O': case 'P': 
            case 'R': case 'S': case 'T': case 'U': case 'V': 
            case 'W': case 'X': case 'Y': case 'Z':
            case '#': case '%': case '&':
                top = -fontMetrics.CapHeight;
                break;

            case '/':
            case '\\':
                // tops
                if (font.Typeface.FamilyName == "Book Antiqua"
                    || font.Typeface.FamilyName == "Calibri"
                    || font.Typeface.FamilyName == "Calisto MT"
                    || font.Typeface.FamilyName == "Cascadia Code"
                    || font.Typeface.FamilyName == "Courier New"
                    || font.Typeface.FamilyName == "Centaur"
                    || font.Typeface.FamilyName == "Consolas"
                    || font.Typeface.FamilyName == "Dubai"
                    || font.Typeface.FamilyName == "Gothic"
                    || font.Typeface.FamilyName == "Fira Mono"
                    || font.Typeface.FamilyName == "Footlight MT"
                    || font.Typeface.FamilyName == "Lato"
                    || font.Typeface.FamilyName == "Lucida Console"
                    || font.Typeface.FamilyName == "Lucida Sans"
                    || font.Typeface.FamilyName == "Lucida Sans Typewriter"
                    || font.Typeface.FamilyName == "Lucida Sans Typewriter"
                    || font.Typeface.FamilyName == "NSimSun"
                    || font.Typeface.FamilyName == "Sylfaen"
                    || font.Typeface.FamilyName == "Tw Cen MT")
                {
                    top = correctedTop;
                }
                else
                {
                    // default to slashes same as caps
                    top = -fontMetrics.CapHeight;
                }

                //bottoms
                if (font.Typeface.FamilyName == "Arial"
                    || font.Typeface.FamilyName == "Bodoni MT"
                    || font.Typeface.FamilyName == "Bernard MT"
                    || font.Typeface.FamilyName == "Calisto MT"
                    || font.Typeface.FamilyName == "Comic Sans MS"
                    || font.Typeface.FamilyName == "Century"
                    || font.Typeface.FamilyName == "Impact"
                    || font.Typeface.FamilyName == "Rockwell"
                    || font.Typeface.FamilyName == "Sylfaen"
                    || font.Typeface.FamilyName == "Times New Roman"
                    || font.Typeface.FamilyName == "Trebuchet MS"
                    )
                {
                    bottom = 0; // stops at baseline
                }
                else
                {
                    // default to slashes go below baseline
                    bottom = correctedBottom;
                }
                
                break;
            case '[': case ']':
            case '{': case '}':
            case '(': case ')':
            case '|':
                if (font.Typeface.FamilyName == "Book Antiqua"
                    || font.Typeface.FamilyName == "Bahnschrift"
                    || font.Typeface.FamilyName == "Calibri"
                    || font.Typeface.FamilyName == "Calisto MT"
                    || font.Typeface.FamilyName == "Cascadia Code"
                    || font.Typeface.FamilyName == "Centaur"
                    || font.Typeface.FamilyName == "Century"
                    || font.Typeface.FamilyName == "Comic Sans MS"
                    || font.Typeface.FamilyName == "Consolas"
                    || font.Typeface.FamilyName == "Courier New"
                    || font.Typeface.FamilyName == "Dubai"
                    || font.Typeface.FamilyName == "Gothic"
                    || font.Typeface.FamilyName == "Fira Mono"
                    || font.Typeface.FamilyName == "Footlight MT"
                    || font.Typeface.FamilyName == "Lato"
                    || font.Typeface.FamilyName == "Lucida Console"
                    || font.Typeface.FamilyName == "Lucida Sans"
                    || font.Typeface.FamilyName == "Lucida Sans Typewriter"
                    || font.Typeface.FamilyName == "Lucida Sans Typewriter"
                    || font.Typeface.FamilyName == "NSimSun"
                    || font.Typeface.FamilyName == "Sylfaen"
                    || font.Typeface.FamilyName == "Times New Roman"
                    || font.Typeface.FamilyName == "Tw Cen MT"
                    || font.Typeface.FamilyName == "Verdana"
                    )
                {
                    top = correctedTop;
                }
                else // same as caps
                {
                    top = -fontMetrics.CapHeight;
                }
                bottom = correctedBottom;
                break;

            case 'Q':
                top = -fontMetrics.CapHeight;
                if (font.Typeface.FamilyName == "Agency FB"
                    || font.Typeface.FamilyName == "Bahnschrift"
                    || font.Typeface.FamilyName == "Century Gothic"
                    || font.Typeface.FamilyName == "Eras ITC"
                    || font.Typeface.FamilyName == "OCR A"
                    )
                {
                    bottom = 0;
                }
                else
                {
                    bottom = correctedBottom;
                }
                break;

            case '_':
                top = -fontMetrics.XHeight;
                bottom = fontMetrics.Bottom;
                break;

            case '$':
                top = correctedTop;
                bottom = correctedBottom * 0.5f; 
                break;

            case 'J': // sometimes upper J has a low tail
                top = -fontMetrics.CapHeight;
                if (font.Typeface.FamilyName == "Bernard MT"
                    || font.Typeface.FamilyName == "Bodoni MT"
                    || font.Typeface.FamilyName == "Book Antiqua"
                    || font.Typeface.FamilyName == "Centaur"
                    || font.Typeface.FamilyName == "Footlight MT"
                    || font.Typeface.FamilyName == "Gill Sans MT"
                    || font.Typeface.FamilyName == "Lucida Sans"
                    || font.Typeface.FamilyName == "NSimSun"
                    || font.Typeface.FamilyName == "Rockwell"
                    || font.Typeface.FamilyName == "Sylfaen"
                    )
                {
                    bottom = fontMetrics.Descent;
                }
                break;

            case '0': case '1': case '2': case '3': case '4': 
            case '5': case '6': case '7': case '8': case '9':
                if (font.Typeface.FamilyName == "Courier New")
                {
                    top = -fontMetrics.CapHeight * 1.1f;
                }
                else if (font.Typeface.FamilyName == "Lucida Console")
                {
                    top = fontMetrics.Ascent;
                }
                else
                {
                    top = -fontMetrics.CapHeight;
                }
                break;

            case '<': case '>': case '+': case '~':
                if (font.Typeface.FamilyName == "Centaur"
                    || font.Typeface.FamilyName == "Courier New"
                    || font.Typeface.FamilyName == "Footlight MT"
                    || font.Typeface.FamilyName == "Franklin Gothic"
                    || font.Typeface.FamilyName == "Gill Sans MT"
                    || font.Typeface.FamilyName == "NSimSun"
                    || font.Typeface.FamilyName == "Tw Cen MT"
                    )
                {
                    top = -fontMetrics.CapHeight;
                }
                else if (font.Typeface.FamilyName == "Sylfaen")
                {
                    top = -fontMetrics.CapHeight * 0.8f;
                    bottom = 0;
                }
                else
                {
                    top = -fontMetrics.CapHeight * 0.8f;
                    bottom = -fontMetrics.XHeight * 0.1f;
                }
                break;

            default:
                top = correctedTop;
                bottom = correctedBottom;
                break;
        }


        SKRect croppedRect = new SKRect(
                charRect.Left,
                yBaseline + top,
                charRect.Right,
                yBaseline + bottom
                );

        return croppedRect;
    }



    protected void DrawTextOnCanvas(SKCanvas canvas, WordContentArea word, SKColor textColor, bool isUnderlined = false)
    {
        using (SKPaint paint = new SKPaint())
        {
            paint.IsAntialias = true;
            paint.Color = textColor;
            

            if (word.Text != null)
            {

                canvas.DrawText(word.Text, word.Rect.Left, word.YTextBaseline, word.Font, paint);

                if (isUnderlined)
                {
                    // draw underline
                    paint.StrokeWidth = (int)(word.Font.Size * 0.07);

                    int lineYposition = word.YTextBaseline + _rnd.Next(2, (int)(word.Font.Size * 0.15));
                    canvas.DrawLine(word.Rect.Left, lineYposition, word.Rect.Right, lineYposition, paint);
                }
            }
        }
    }



    protected void DrawTextBlockBackgroundColour(SKCanvas textCanvas, bool isInverted, bool isLightText, bool isBoxAroundText, SKRectI backgoundRect)
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




    protected void DrawBackgroundImage()
    {
        DirectoryInfo backgroundImageFolder = new DirectoryInfo("./BackgroundImages");
        if (backgroundImageFolder != null)
        {
            var files = backgroundImageFolder.GetFiles();
            int randomFileIndex = _rnd.Next(0, files.Length);
            var imageFile = files[randomFileIndex];

            using (SKCanvas textCanvas = new SKCanvas(TextImage))
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
                for (int pxX = 0; pxX < scaledBackgroundImage.Info.Width; pxX += 20)
                {
                    for (int pxY = 0; pxY < scaledBackgroundImage.Info.Height; pxY += 20)
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


    protected List<WordContentArea> CreateWordAndCharacterObjects(
        string text, 
        SKRect[] characterBoxes, 
        SKRect[] croppedCharacterBoxes, 
        int yTextBaseline, 
        SKFont font, 
        bool isInverted = false)
    {
        List<WordContentArea> words = new List<WordContentArea>();

        WordContentArea word = new WordContentArea();
        word.IsInverted = isInverted;
        word.Font = font;
        word.YTextBaseline = yTextBaseline;


        for (int c = 0; c < characterBoxes.Length; c++)
        {
            SKRect currentCharBox = characterBoxes[c];
            SKRect currentCroppedCharBox = croppedCharacterBoxes[c];
            char currentSymbol = text[c];

            CharacterContentArea character = new CharacterContentArea();
            character.Symbol = currentSymbol;
            character.IsInverted = isInverted;


            if (c == 0) // first character
            {
                character.Rect = RectToRectI(currentCharBox);
                character.CroppedRect = RectToRectI(currentCroppedCharBox);
                word = new WordContentArea();
                word.Font = font;
                word.YTextBaseline = yTextBaseline;
                word.Rect = RectToRectI(currentCharBox);
            }
            else if (currentSymbol == ' ') // space between words
            {
                WordContentArea wordCopy = word.Clone();
                words.Add(wordCopy);
                word = new WordContentArea();
                word.Font = font;
                word.YTextBaseline = yTextBaseline;
            }


            if (currentSymbol != ' ')
            {
                character.Rect = RectToRectI(currentCharBox);

                character.CroppedRect = RectToRectI(currentCroppedCharBox);
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

        if (word.Text != null) // if last character in the phrase is a space, text will be null here
        {
            words.Add(word);
        }
        
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





    protected void DrawLabels()
    {
        using (SKCanvas labelCanvas = new SKCanvas(LabelImage))
        using (SKPaint wordRectPaint = new SKPaint())
        {
            labelCanvas.Clear(SKColors.Black);
            wordRectPaint.Color = _veryDarkGrey;
            wordRectPaint.Style = SKPaintStyle.Fill;


            foreach (ContentArea contentArea in _contentAreas)
            {
                if (contentArea is TextContentArea phrase)
                {
                    foreach (WordContentArea word in phrase.Words)
                    {
                        // draw whole word background
                        //labelCanvas.DrawRect(word.Rect, wordRectPaint);

                        foreach (CharacterContentArea character in word.Characters)
                        {
                            //SKBitmap stampBitmap = GetSingleCharacterLabelImageDiamond();
                            SKBitmap stampBitmap = GetSingleCharacterLabelImageGaussian();
                            labelCanvas.DrawBitmap(stampBitmap, character.CroppedRect);
                        }
                    }
                }
            }
        }
    }


    protected SKBitmap GetSingleCharacterLabelImageGaussian()
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

            canvas.DrawCircle(0, 0, size * 4, paint);
        }

        return bitmap;
    }


    protected SKBitmap GetSingleCharacterLabelImageDiamond()
    {
        int size = 100; // center to edge

        SKBitmap bitmap = new SKBitmap(size * 2, size * 2);

        using (SKCanvas canvas = new SKCanvas(bitmap))
        using (SKPaint paint = new SKPaint())
        {
            paint.IsAntialias = true;
            paint.Style = SKPaintStyle.Fill;
            SKColor lightGrey = new SKColor(0xb0, 0xb0, 0xb0);
            SKColor[] colours = new SKColor[] { _veryDarkGrey, _veryDarkGrey, _veryDarkGrey, lightGrey, SKColors.White };


            SKRect topLeft = new SKRect(0, 0, size, size);
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(topLeft.Left, topLeft.Top),
                new SKPoint(topLeft.Right, topLeft.Bottom),
                colours,
                SKShaderTileMode.Decal
                );
            canvas.DrawRect(topLeft, paint);

            SKRect topRight = new SKRect(size, 0, size * 2, size);
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(topRight.Right, topRight.Top),
                new SKPoint(topRight.Left, topRight.Bottom),
                colours,
                SKShaderTileMode.Decal
                );
            canvas.DrawRect(topRight, paint);


            // bottom-left
            SKRect bottomLeft = new SKRect(0, size, size, size * 2);
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(bottomLeft.Left, bottomLeft.Bottom),
                new SKPoint(bottomLeft.Right, bottomLeft.Top),
                colours,
                SKShaderTileMode.Decal
                );
            canvas.DrawRect(bottomLeft, paint);

            // bottom-right
            SKRect bottomRight = new SKRect(size, size, size * 2, size * 2);
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(bottomRight.Right, bottomRight.Bottom),
                new SKPoint(bottomRight.Left, bottomRight.Top),
                colours,
                SKShaderTileMode.Decal
                );
            canvas.DrawRect(bottomRight, paint);

        }

        return bitmap;
    }




    protected void DrawHeatMap()
    {
        using (SKCanvas heatmapCanvas = new SKCanvas(HeatMapImage))
        {
            heatmapCanvas.Clear(SKColors.Blue);

            foreach (ContentArea contentArea in _contentAreas)
            {
                if (contentArea is TextContentArea phrase)
                {
                    foreach (WordContentArea word in phrase.Words)
                    {
                        // draw whole word background
                        DrawTextOnCanvas(heatmapCanvas, word, SKColors.Black);
                    }
                }
            }
        }

        using (SKCanvas heatmapCanvas = new SKCanvas(HeatMapImage))
        using (SKPaint heatmapPaint = new SKPaint())
        using (SKPaint wordRectPaint = new SKPaint())
        {
            heatmapPaint.Color = heatmapPaint.Color.WithAlpha(0x80);
            wordRectPaint.Color = _veryDarkCyan;

            foreach (ContentArea contentArea in _contentAreas)
            {
                if (contentArea is TextContentArea phrase)
                {
                    foreach (WordContentArea word in phrase.Words)
                    {
                        //heatmapCanvas.DrawRect(word.Rect, wordRectPaint);

                        foreach (CharacterContentArea character in word.Characters)
                        {
                            SKBitmap stampBitmap = GetSingleCharacterHeatMapImageDiamond();
                            heatmapCanvas.DrawBitmap(stampBitmap, character.CroppedRect, heatmapPaint);
                        }
                    }
                }
            }
        }
    }



    protected SKBitmap GetSingleCharacterHeatMapImageGaussian()
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
                new SKColor[] { SKColors.Black, SKColors.Red, SKColors.Orange, SKColors.Yellow, SKColors.LightGreen, SKColors.DarkCyan, SKColors.Blue },
                SKShaderTileMode.Decal
                );

            canvas.DrawCircle(0, 0, size * 4, paint);
        }
        return bitmap;
    }


    protected SKBitmap GetSingleCharacterHeatMapImageDiamond()
    {
        int size = 100; // center to edge

        SKBitmap bitmap = new SKBitmap(size * 2, size * 2);

        using (SKCanvas canvas = new SKCanvas(bitmap))
        using (SKPaint paint = new SKPaint())
        {
            paint.IsAntialias = true;
            paint.Style = SKPaintStyle.Fill;
            SKColor transparent = new SKColor(0x00, 0x00, 0x00, 0x00);
            SKColor[] colours = new SKColor[]
            {
                transparent,
                transparent,
                transparent,
                transparent,
                transparent,
                SKColors.DarkCyan,
                SKColors.LightGreen,
                SKColors.Yellow,
                SKColors.Orange,
                SKColors.Red,
                SKColors.Black,
            };


            SKRect topLeft = new SKRect(0, 0, size, size);
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(topLeft.Left, topLeft.Top),
                new SKPoint(topLeft.Right, topLeft.Bottom),
                colours,
                SKShaderTileMode.Decal
                );
            canvas.DrawRect(topLeft, paint);

            SKRect topRight = new SKRect(size, 0, size * 2, size);
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(topRight.Right, topRight.Top),
                new SKPoint(topRight.Left, topRight.Bottom),
                colours,
                SKShaderTileMode.Decal
                );
            canvas.DrawRect(topRight, paint);


            // bottom-left
            SKRect bottomLeft = new SKRect(0, size, size, size * 2);
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(bottomLeft.Left, bottomLeft.Bottom),
                new SKPoint(bottomLeft.Right, bottomLeft.Top),
                colours,
                SKShaderTileMode.Decal
                );
            canvas.DrawRect(bottomLeft, paint);

            // bottom-right
            SKRect bottomRight = new SKRect(size, size, size * 2, size * 2);
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(bottomRight.Right, bottomRight.Bottom),
                new SKPoint(bottomRight.Left, bottomRight.Top),
                colours,
                SKShaderTileMode.Decal
                );
            canvas.DrawRect(bottomRight, paint);

        }

        return bitmap;
    }



    protected void DrawBoundingBoxes()
    {
        using (SKCanvas characterBoxCanvas = new SKCanvas(BoundingBoxImage))
        using (SKPaint paint = new SKPaint())
        {
            characterBoxCanvas.Clear(SKColors.White);

            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 1.0f;
            SKColor croppedRectColor = new SKColor(0xff, 0x00, 0x00, 0x80);
            SKColor fullRectColor = new SKColor(0x00, 0xff, 0x10, 0x80);

            foreach (ContentArea contentArea in _contentAreas)
            {
                if (contentArea is TextContentArea phrase)
                {
                    foreach (WordContentArea word in phrase.Words)
                    {
                        DrawTextOnCanvas(characterBoxCanvas, word, SKColors.Black);

                        paint.Color = croppedRectColor;
                        foreach (CharacterContentArea character in word.Characters)
                        {
                            characterBoxCanvas.DrawRect(character.CroppedRect, paint);
                        }

                        //paint.Color = fullRectColor;
                        //foreach (CharacterContentArea character in word.Characters)
                        //{
                        //    characterBoxCanvas.DrawRect(character.Rect, paint);
                        //}

                        paint.Color = new SKColor(0x00, 0x20, 0xff, 0x80);
                        SKRect wordDisplayRect = new SKRect(word.Rect.Left, word.Rect.Top, word.Rect.Right, word.Rect.Bottom);
                        wordDisplayRect.Inflate(2, 2);
                        //characterBoxCanvas.DrawRect(wordDisplayRect, paint);
                        
                    }
                }
            }
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
            xOffset = -inflatedRect.Left;
        }
        if (inflatedRect.Right > TextImage.Width)
        {
            xOffset = TextImage.Width - inflatedRect.Right;
        }
        if (inflatedRect.Top < 0)
        {
            yOffset = -inflatedRect.Top;
        }
        if (inflatedRect.Bottom > TextImage.Height)
        {
            yOffset = TextImage.Height - inflatedRect.Bottom;
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
        int yInflateAmount = 0;

        if (character.Rect.Width < character.Rect.Height)
        {
            xInflateAmount = (character.Rect.Height - character.Rect.Width) / 2;
        }

        if (character.Rect.Width > character.Rect.Height)
        {
            yInflateAmount = (character.Rect.Width - character.Rect.Height) / 2;
        }


        SKRectI inflatedRect = SKRectI.Inflate(character.Rect, xInflateAmount, yInflateAmount);

        // even the height/width to an exact square
        if (inflatedRect.Height == inflatedRect.Width - 1)
        {
            inflatedRect.Bottom++;
        }
        if (inflatedRect.Height == inflatedRect.Width + 1)
        {
            inflatedRect.Right++;
        }

        TextImage.ExtractSubset(tempCharImage, inflatedRect);


        // if the character is at the edge of the page, the subset won't be centred on the character, but will be offset.
        // We need to calculate any offset then draw the temp bitmap onto the final bitmap.

        int xOffset = 0;
        int yOffset = 0;
        if (inflatedRect.Left < 0)
        {
            xOffset = -inflatedRect.Left;
        }
        if (inflatedRect.Right > TextImage.Width)
        {
            xOffset = TextImage.Width - inflatedRect.Right;
        }
        if (inflatedRect.Top < 0)
        {
            yOffset = -inflatedRect.Top;
        }
        if (inflatedRect.Bottom > TextImage.Height)
        {
            yOffset = TextImage.Height - inflatedRect.Bottom;
        }

        SKBitmap charImage = new SKBitmap(inflatedRect.Width, inflatedRect.Height);
        SKRectI drawLocation = new SKRectI(xOffset, yOffset, xOffset + inflatedRect.Width, yOffset + inflatedRect.Height);

        using (SKCanvas charImageCanvas = new SKCanvas(charImage))
        {
            charImageCanvas.Clear(SKColors.White);
            charImageCanvas.DrawBitmap(tempCharImage, drawLocation);
        }


        // resize
        SKSizeI size = new SKSizeI(32, 32); // TODO: make size selectable/changeable
        SKBitmap scaledCharacterImage = charImage.Resize(size, SKFilterQuality.High);

        // save to file
        SaveCharacterImage(baseSavePath, character, scaledCharacterImage);

    }


    private void SaveCharacterImage(string baseSavePath, CharacterContentArea character, SKBitmap bitmap)
    {
        // get the training label class
        string characterClass = CharacterClassDictionary.CharacterClasses[character.Symbol];


        string saveDirectoryPath = Path.Combine(baseSavePath, characterClass);

        // create folder if not exists
        if (!Directory.Exists(saveDirectoryPath))
        {
            Directory.CreateDirectory(saveDirectoryPath);
        }
        

        // maximum number of image files to save for each character/class
        if (Directory.GetFiles(saveDirectoryPath).Length >= characterImageFileClassLimit)
        {
            return;
        }


        string fullPath = Path.Combine(saveDirectoryPath, $"{Guid.NewGuid()}.png");

        // write file
        using (SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100))
        using (FileStream fileStream = File.OpenWrite(fullPath))
        {
            // save the data to a stream
            data.SaveTo(fileStream);
        }
    }
}

using Avalonia.Controls;
using OcrSyntheticDataGenerator.ContentModel;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OcrSyntheticDataGenerator.ContentModel.TableColumn;
using static SkiaSharp.SKImageFilter;

namespace OcrSyntheticDataGenerator.ImageGeneration;

public class TableGenerator : ImageAndLabelGeneratorBase
{
    public TableGenerator(int imageWidth, int imageHeight)
        : base(imageWidth, imageHeight)
    { }


    public override void Generate()
    {
        int minFontHeight = 20;
        int maxFontHeight = 30;
        int minRows = 1;
        int maxRows = 20;
        int minColumns = 2;
        int maxColumns = 4;
        int minRowYPadding = 3;
        int maxRowYPadding = 15;

        int fontHeight = _rnd.Next(minFontHeight, maxFontHeight);
        int rowCount = _rnd.Next(minRows, maxRows);
        int columnCount = _rnd.Next(minColumns, maxColumns);
        int rowYPadding = _rnd.Next(minRowYPadding, maxRowYPadding);
        int noisePercentage = _rnd.Next(1, 100);

        SKFont font = _randomTextGenerator.GetRandomFont(fontHeight);
        byte textDarkness = (byte)_rnd.Next(200, 255);
        SKColor textColor = new SKColor(0x00, 0x00, 0x00, textDarkness);
        byte lineDarkness = (byte)_rnd.Next(50, 255);
        SKColor lineColor = new SKColor(lineDarkness, lineDarkness, lineDarkness);
        byte stripedRowBackgroundAlpha = (byte)_rnd.Next(5, 80);
        SKColor stripeColor = new SKColor(0x00, 0x00, 0x00, stripedRowBackgroundAlpha);


        bool drawColumnLines = _rnd.Next(1, 100) < 80;
        bool drawRowLines = _rnd.Next(1, 100) < 50;
        bool drawStripedRows = false;
        if (!drawRowLines)
        {
            drawStripedRows = _rnd.Next(1, 100) < 50;
        }



        // ----    set up rows and columns    ----
        List<TableRow> rows = new List<TableRow>();
        List<TableColumn> columns = new List<TableColumn>();

        int tableLeft = _rnd.Next(5, 20);
        int previousRight = tableLeft;
        for (int c = 0; c < columnCount; c++)
        {
            TableColumn column = new TableColumn();
            column.TextJustified = (Justified)_rnd.Next(0, Enum.GetNames(typeof(Justified)).Length);
            column.ValueType = (ColumnmValueType)_rnd.Next(0, Enum.GetNames(typeof(ColumnmValueType)).Length);

            if (column.ValueType == ColumnmValueType.Quantity)
            {
                column.Width = _rnd.Next(30, 150);
            }
            else
            {
                column.Width = ((_imageWidth - 20) / columnCount) - _rnd.Next(0, 50);
            }

            if (column.ValueType == ColumnmValueType.Date)
            {
                column.Format = _randomTextGenerator.GetRandomDateFormat();
            }

            column.XPadding = _rnd.Next(0, 10);
            column.Left = previousRight;
            column.Right = column.Left + column.Width;
            previousRight = column.Right;
            columns.Add(column);
        }
        int tableRight = previousRight;





        int tableTop = _rnd.Next(5, 40);
        int previousBottom = tableTop;
        // most of the time, have a header row, but not every time
        if (_rnd.Next(1, 100) < 90)
        {
            TableRow headerRow = new TableRow();
            headerRow.IsHeaderRow = true;
            headerRow.YPadding = _rnd.Next(3, 10);
            headerRow.Top = previousBottom;
            headerRow.Height = fontHeight + headerRow.YPadding; // (headerRow.YPadding * 2);
            headerRow.Bottom = headerRow.Top + headerRow.Height;
            previousBottom = headerRow.Bottom;
            rows.Add(headerRow);
        }


        for (int r = 0; r < rowCount; r++)
        {
            TableRow row = new TableRow();
            row.YPadding = rowYPadding;
            row.Top = previousBottom;
            row.Height = fontHeight + row.YPadding; // (headerRow.YPadding * 2);
            row.Bottom = row.Top + row.Height;
            previousBottom = row.Bottom;
            rows.Add(row);
        }

        int tableBottom = previousBottom;






        // set up canvas objects for each image
        using (SKCanvas textCanvas = new SKCanvas(TextImage))
        using (SKCanvas labelCanvas = new SKCanvas(LabelImage))
        using (SKCanvas heatMapCanvas = new SKCanvas(HeatMapImage))
        using (SKCanvas characterBoxCanvas = new SKCanvas(CharacterBoxImage))
        using (SKPaint linePaint = new SKPaint())
        using (SKPaint stripePaint = new SKPaint())
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


            linePaint.IsAntialias = true;
            linePaint.Color = lineColor;
            linePaint.Style = SKPaintStyle.Stroke;
            linePaint.StrokeWidth = _rnd.Next(1, 3);

            stripePaint.IsAntialias = true;
            stripePaint.Color = stripeColor;
            stripePaint.Style = SKPaintStyle.Fill;


            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                TableRow row = rows[rowIndex];

                if (drawStripedRows && rowIndex % 2 > 0)
                {
                    SKRect rowRect = new SKRect();
                    rowRect.Left = tableLeft;
                    rowRect.Top = row.Top;
                    rowRect.Right = tableRight;
                    rowRect.Bottom = row.Bottom;
                    textCanvas.DrawRect(rowRect, stripePaint);
                }


                if (drawRowLines)
                {
                    textCanvas.DrawLine(tableLeft, row.Top, tableRight, row.Top, linePaint);
                    textCanvas.DrawLine(tableLeft, row.Bottom, tableRight, row.Bottom, linePaint);
                }



                foreach (TableColumn column in columns)
                {
                    SKRect columnRect = new SKRect();
                    columnRect.Left = column.Left;
                    columnRect.Top = tableTop;
                    columnRect.Right = column.Right;
                    columnRect.Bottom = tableBottom;


                    if (drawColumnLines)
                    {
                        textCanvas.DrawLine(column.Left, tableTop, column.Left, tableBottom, linePaint);
                        textCanvas.DrawLine(column.Right, tableTop, column.Right, tableBottom, linePaint);
                    }
                    

                    // cell text


                    string text = "";
                    if (row.IsHeaderRow)
                    {
                        text = _randomTextGenerator.GetRandomWord();
                    }
                    else
                    {
                        switch (column.ValueType)
                        {
                            case ColumnmValueType.Date:
                                text = _randomTextGenerator.GetRandomDate(column.Format);
                                break;

                            case ColumnmValueType.DollarAmount:
                                text = _randomTextGenerator.GetRandomNumber();
                                break;

                            case ColumnmValueType.Quantity:
                                text = _rnd.Next(0, 15).ToString();
                                break;

                            case ColumnmValueType.Code:
                                text = _randomTextGenerator.GetRandomAlphaNumericCode();
                                break;

                            case ColumnmValueType.Text:
                                text = _randomTextGenerator.GetRandomWord();
                                break;
                            default:
                                break;
                        }
                    }



                    // text location
                    int textX = column.Left + column.XPadding;
                    int yTextBaseline = row.Bottom - row.YPadding;
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
                        glyphPositions = paint.GetGlyphPositions(text, new SKPoint(textX, yTextBaseline));
                        glyphWidths = paint.GetGlyphWidths(text);
                        fontMetrics = paint.FontMetrics;
                    }

                    int y = (int)(yTextBaseline - fontMetrics.CapHeight - 5);

                    SKRect measureRect = new SKRect(
                       textX,
                       y,
                       textX + measuredWidth,
                       y + fontHeight + 3
                       );

                    if (column.TextJustified == Justified.Right)
                    {
                        textX = column.Right - (int)measureRect.Width - column.XPadding;
                        measureRect.Left = textX;
                        // re-calculate the character boxes
                        using (SKPaint paint = new SKPaint(font))
                        {
                            paint.IsAntialias = true;
                            paint.Color = SKColors.Black;
                            paint.StrokeWidth = 1;
                            measuredWidth = paint.MeasureText(text, ref measureBounds);
                            glyphPositions = paint.GetGlyphPositions(text, new SKPoint(textX, yTextBaseline));
                            glyphWidths = paint.GetGlyphWidths(text);
                            fontMetrics = paint.FontMetrics;
                        }
                    }


                    // check fits in the cell rect. If I fits, I sits, otherwise I continue.
                    SKRect cellRect = new SKRect(column.Left - 2, row.Top - 20, column.Right + 2, row.Bottom + 20);
                    if (!RectangleFitsInside(measureRect, cellRect))
                    {
                        continue;
                    }


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
                    DrawTextOnCanvas(textCanvas, text, textX, yTextBaseline, font, textColor);

                    // draw text on heatmap
                    DrawLabels(
                        font,
                        labelCanvas,
                        heatMapCanvas,
                        characterBoxCanvas,
                        text,
                        textX,
                        yTextBaseline,
                        croppedCharacterBoxes,
                        words);

                }
            }


            // ---- post processing ----
            PostProcessing(noisePercentage);
        }

    }
}

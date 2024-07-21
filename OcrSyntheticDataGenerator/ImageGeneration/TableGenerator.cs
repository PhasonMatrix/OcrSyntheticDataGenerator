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

    List<TableRow> _rows = new List<TableRow>();
    List<TableColumn> _columns = new List<TableColumn>();
    int _tableLeft;
    int _tableRight;
    int _tableTop;
    int _tableBottom;


    public TableGenerator(int imageWidth, int imageHeight)
        : base(imageWidth, imageHeight)
    { }



    public override void GenerateContent()
    {

        int minFontHeight = 16;
        int maxFontHeight = 30;
        int minRows = 1;
        int maxRows = 20;
        int minColumns = 2;
        int maxColumns = 4;
        int minRowYPadding = 2;
        int maxRowYPadding = 15;
        int minTextYOffset = -4;
        int maxTextYOffset = 4;

        int fontHeight = _rnd.Next(minFontHeight, maxFontHeight);
        int rowCount = _rnd.Next(minRows, maxRows);
        int columnCount = _rnd.Next(minColumns, maxColumns);
        int rowYPadding = _rnd.Next(minRowYPadding, maxRowYPadding);
        int textYOffset = _rnd.Next(minTextYOffset, maxTextYOffset);

        SKFont font = _randomTextGenerator.GetRandomFont(fontHeight);


        // ----    set up rows and columns    ----

        _tableLeft = _rnd.Next(5, 20);
        int previousRight = _tableLeft;
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
            _columns.Add(column);
        }
        _tableRight = previousRight;


        _tableTop = _rnd.Next(5, 40);
        int previousBottom = _tableTop;
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
            _rows.Add(headerRow);
        }


        for (int r = 0; r < rowCount; r++)
        {
            TableRow row = new TableRow();
            row.YPadding = rowYPadding;
            row.Top = previousBottom;
            row.Height = fontHeight + row.YPadding; // (headerRow.YPadding * 2);
            row.Bottom = row.Top + row.Height;
            previousBottom = row.Bottom;
            _rows.Add(row);
        }
        _tableBottom = previousBottom;


        for (int rowIndex = 0; rowIndex < _rows.Count; rowIndex++)
        {
            TableRow row = _rows[rowIndex];


            foreach (TableColumn column in _columns)
            {
                SKRect columnRect = new SKRect();
                columnRect.Left = column.Left;
                columnRect.Top = _tableTop;
                columnRect.Right = column.Right;
                columnRect.Bottom = _tableBottom;



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
                int yTextBaseline = row.Bottom - row.YPadding + textYOffset;
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

                SKRectI measureRect = new SKRectI(
                   textX,
                   y,
                   textX + (int)measuredWidth,
                   y + fontHeight
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


                List<WordContentArea> words = ConstructWordAndCharacterObjects(font, text, yTextBaseline, glyphPositions, glyphWidths, fontMetrics, measureRect, false);

                currentTextContentArea.Words = words;
                _contentAreas.Add(currentTextContentArea);

            }

        }

    }



    public override void DrawContent()
    {
        using (SKCanvas textCanvas = new SKCanvas(TextImage))
        using (SKPaint linePaint = new SKPaint())
        using (SKPaint stripePaint = new SKPaint())
        {
            //textCanvas.Clear(SKColors.White);

            byte textDarkness = (byte)_rnd.Next(60, 255);
 

            if (HasBackgroundTexture)
            {
                // clamp the darkness. we don't want the text to be too light on a dark background
                textDarkness = (byte)_rnd.Next(200, 255);
            }

            SKColor textColor = new SKColor(0x00, 0x00, 0x00, textDarkness);
            byte lineDarkness = (byte)_rnd.Next(0, 250);
            SKColor lineColor = new SKColor(lineDarkness, lineDarkness, lineDarkness);
            byte stripedRowBackgroundAlpha = (byte)_rnd.Next(5, 80);
            SKColor stripeColor = new SKColor(0x00, 0x00, 0x00, stripedRowBackgroundAlpha);


            bool drawColumnLines = _rnd.Next(1, 100) < 80;
            int rowLineFrequency = _rnd.Next(0, 4);
            bool drawStripedRows = false;
            if (rowLineFrequency == 0)
            {
                drawStripedRows = _rnd.Next(1, 100) < 50;
            }

            linePaint.IsAntialias = false;
            linePaint.Color = lineColor;
            linePaint.Style = SKPaintStyle.Stroke;
            linePaint.StrokeWidth = _rnd.Next(1, 3);

            stripePaint.IsAntialias = true;
            stripePaint.Color = stripeColor;
            stripePaint.Style = SKPaintStyle.Fill;




            // draw column and row colours
            for (int rowIndex = 0; rowIndex < _rows.Count; rowIndex++)
            {
                TableRow row = _rows[rowIndex];
     

                //if (drawRowLines)
                if (rowLineFrequency > 0 && rowIndex % rowLineFrequency == 0)
                {
                    textCanvas.DrawLine(_tableLeft, row.Top, _tableRight, row.Top, linePaint);
                }

                if (rowLineFrequency > 0 && rowIndex == _rows.Count - 1) // last row, draw bottom line
                {
                    textCanvas.DrawLine(_tableLeft, row.Bottom, _tableRight, row.Bottom, linePaint);
                }

                foreach (TableColumn column in _columns)
                {
                    SKRect columnRect = new SKRect();
                    columnRect.Left = column.Left;
                    columnRect.Top = _tableTop;
                    columnRect.Right = column.Right;
                    columnRect.Bottom = _tableBottom;


                    if (drawColumnLines)
                    {
                        textCanvas.DrawLine(column.Left, _tableTop, column.Left, _tableBottom, linePaint);
                        textCanvas.DrawLine(column.Right, _tableTop, column.Right, _tableBottom, linePaint);
                    }


                }


            }




            // draw cell text

            byte alpha = (byte)_rnd.Next(120, 255);

            if (HasBackgroundTexture)
            {
                // clamp the darkness. we don't want the text to be too light on a dark background
                if (alpha < 200) { alpha = 255; }
            }


            foreach (ContentArea contentArea in _contentAreas)
            {
                if (contentArea is TextContentArea phrase)
                {
                    bool isInverted = _rnd.Next(0, 100) < 20;
                    bool isLightText = _rnd.Next(0, 100) < 10;
                    bool isBoxAroundText = _rnd.Next(0, 100) < 20;

                    foreach (WordContentArea word in phrase.Words)
                    {
                        if (word.Text != null)
                        {
                            bool isUnderlined = (word.Text.Contains("www") || (word.Text.Contains("@") && word.Text.Contains("."))) && (_rnd.Next(1, 100) < 80);
                            DrawTextOnCanvas(textCanvas, word, textColor, isUnderlined);
                        }
                    }
                }
            }
        }

    }


}

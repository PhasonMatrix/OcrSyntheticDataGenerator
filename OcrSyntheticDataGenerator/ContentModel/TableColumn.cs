using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ContentModel;

public class TableColumn
{
    public enum Justified
    {
        Left,
        Right,
        Center
    }

    public enum ColumnmValueType
    {
        Date,
        Quantity,
        Text,
        DollarAmount,
        Code
    }


    public Justified TextJustified { get; set; }

    public ColumnmValueType ValueType { get; set; }

    public int Width { get; set; }
    public int Left { get; set; }
    public int Right { get; set; }
    public int XPadding { get; set; }

    public string Format { get; set; }

}

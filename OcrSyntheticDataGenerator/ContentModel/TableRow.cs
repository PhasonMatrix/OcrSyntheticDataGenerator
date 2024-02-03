using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ContentModel;

public class TableRow
{

    public int Top { get; set; }
    public int Bottom { get; set; }
    public int Height { get; set; }
    public int YPadding { get; set; }

    public bool IsHeaderRow { get; set; }
}

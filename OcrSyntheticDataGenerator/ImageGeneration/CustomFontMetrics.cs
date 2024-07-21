using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ImageGeneration
{
    internal class CustomFontMetrics
    {
        public string FontFamilyName { get; set; }
        public double Accent { get; set; } // top of tall lower case letters and symbols. eg: l,k,h,t,f,$,|
        public double Deccent { get; set; } // bottom of tails. eg: g,y,p,q,j
        public double CapHeight { get; set; } // top of upper case letters and numerals
        public double LowerHeight { get; set; } // top of lower case letters eg: a,e,c,n,m,w,x
        public double Bottom { get; set; } // bottom of symbols. eg: {,},[,],|,$,_
    }




}

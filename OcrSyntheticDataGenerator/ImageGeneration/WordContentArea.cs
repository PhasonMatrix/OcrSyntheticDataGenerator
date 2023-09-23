using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ImageGeneration
{
    internal class WordContentArea : TextContentArea
    {
        public List<CharacterContentArea> Characters { get; set; } = new List<CharacterContentArea>();
    }
}

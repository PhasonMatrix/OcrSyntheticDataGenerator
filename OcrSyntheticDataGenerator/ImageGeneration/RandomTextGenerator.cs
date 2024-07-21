using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Media;
using MsBox.Avalonia.Base;
using SkiaSharp;


namespace OcrSyntheticDataGenerator.ImageGeneration
{
    public class RandomTextGenerator
    {

        private Random _random = new Random();


        public string GetRandomWord()
        {
            string word;

            if (_random.Next(0, 10) > 2)
            {
                word = GetRandomEnglishWord();
            }
            else
            {
                word = GetRandomGibberishWord();
            }

            // random captilisation
            if (_random.Next(0, 100) < 10)
            {
                word = word.Substring(0, 1).ToUpper() + word.Substring(1);
            }

            if (_random.Next(0, 100) < 5)
            {
                word = word.ToUpper();
            }

            return word;
        }



        public string GetRandomEnglishWord()
        {
            int randomInt = _random.Next(0, WordDictionary.TopEnglishWords.Count);
            return WordDictionary.TopEnglishWords.Keys.ToArray()[randomInt];
        }


        public string GetTestPatternText(SKFont font)
        {
            int percentChance = _random.Next(0, 100);
            string text = "";

            if (percentChance < 25)
            {
                text = "ABCJQ";
            }
            else if (percentChance < 50)
            {
                text = "axhtflpgjq";
            }
            else if (percentChance < 75)
            {
                text = "1234";
            }
            else
            {
                text = "{[(/|$#+<x";
            }

            //text = "{[(|/\\" + font.Typeface.FamilyName;

            return text.Trim();
        }

        public string GetRandomText(SKFont font)
        {
            int percentChance = _random.Next(0, 100);
            string text = "";

            if (percentChance < 20) text = GetRandomAlphaNumericCode();
            else if (percentChance < 30) text = GetRandomNumber();
            else if (percentChance < 40) text = GetRandomDate();
            else if (percentChance < 55) text = GetRandomEmailAddress();
            else if (percentChance < 60) text = GetRandomWebAddress();
            else text = GetRandomTextLine(font, 1, 3);


            // double check the font contains all glyphs
            if (!font.ContainsGlyphs(text))
            {
                Debug.WriteLine("This font can't draw every glyph for this text");
                return "";
            }

            return text.Trim();
        }



        public string GetRandomTextLine(SKFont font, int minWords = 1, int maxWords = 10)
        {
            int maxWordCount = _random.Next(minWords, maxWords);
            int wordCount = 0;
            string sentence = GetRandomWord();
            while (wordCount < maxWordCount)
            {
                if (_random.Next(0, 100) < 90) // mostly just words
                {
                    string punc = getRandomPunctuation(font);
                    string word = GetRandomWord();
                    if (punc == ". " || punc == "? " || punc == "! ") // capitalise first character
                    {
                        word = word[0].ToString().ToUpper() + word.Substring(1);
                    }
                    sentence += punc + word;
                }
                else // but sometimes email addresses or web addresses
                {
                    if (_random.Next(0, 100) < 50)
                    {
                        sentence += $" {GetRandomEmailAddress()}";
                    } 
                    else
                    {
                        sentence += $" {GetRandomWebAddress()}";
                    }
                }

                wordCount++;
            }

            return sentence.Trim();
        }


        public string getRandomPunctuation(SKFont font)
        {
            string punctuation = " ";

            // font may not have a glyph for that character
            // try up to x times, then give up
            int retry = 0;
            while (retry < 5)
            {
                int rand = _random.Next(0, 200);

                if (rand < 20) { punctuation = ". "; }
                else if (rand < 30) { punctuation = ", "; }
                else if (rand < 35) { punctuation = "? "; }
                else if (rand < 40) { punctuation = "-"; }
                else if (rand < 45) { punctuation = "\" "; }
                else if (rand < 50) { punctuation = " \""; }
                else if (rand < 55) { punctuation = "\' "; }
                else if (rand < 60) { punctuation = " \'"; }
                else if (rand < 65) { punctuation = "! "; }    // exclamation
                else if (rand < 70) { punctuation = ": "; }    // colon
                else if (rand < 75) { punctuation = "; "; }    // semicolon
                else if (rand < 80) { punctuation = " – "; }   // long dash
                else if (rand < 85) { punctuation = " ‘"; }    // left single quote
                else if (rand < 90) { punctuation = "’ "; }    // right single quote
                else if (rand < 95) { punctuation = " “"; }    // left double quote
                else if (rand < 100) { punctuation = "” "; }   // right double quote
                else if (rand < 105) { punctuation = ") "; } 
                else if (rand < 110) { punctuation = " ("; }
                else if (rand < 115) { punctuation = "] "; }
                else if (rand < 120) { punctuation = " ["; }
                else if (rand < 125) { punctuation = "> "; } 
                else if (rand < 130) { punctuation = " <"; } 

                else if (rand < 132) { punctuation = "© "; }   // copyright
                else if (rand < 134) { punctuation = "® "; }   // registered trademark
                else if (rand < 136) { punctuation = "§ "; }   // section sign
                else if (rand < 137) { punctuation = " «"; }   // left double angle / guillemet
                else if (rand < 138) { punctuation = "» "; }   // right double angle / guillemet
                else if (rand < 140) { punctuation = " | "; }  // pipe

                else { punctuation = " "; } // default, space

                if (font.ContainsGlyphs(punctuation))
                {
                    return punctuation;
                }
                else
                {
                    Debug.WriteLine($"Font '{font.Typeface.FamilyName}' does not contain glyph: {punctuation}");
                }
                retry++;
            }

            return " ";
        }



        // for the types of codes you see on documents, like invoices. May contain leters, numbers and puntiation characters like #,:,-,/, etc.
        public string GetRandomAlphaNumericCode()
        {
            int length = _random.Next(1, 12);
            string code = "";
            for (int i = 0; i < length; i++)
            {
                int randomType = _random.Next(0, 100);
                if (randomType < 15)
                {
                    // special character
                    char[] specialChars = { '~', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '_', '+', '=', '[', ']', '{', '}', '\\', '|', ':', '<', '>', '.', ',', '/', '?' };
                    code += specialChars[_random.Next(0, specialChars.Length)];
                }
                else if (randomType < 40)
                {
                    // number character
                    char[] numberChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '0' };
                    code += numberChars[_random.Next(0, numberChars.Length)];
                }
                else
                {
                    // letter
                    char[] letterChars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', ' ' };
                    code += letterChars[_random.Next(0, letterChars.Length)];
                }
            }

            return code;
        }



        // for qantities, prices and dollar amounts
        public string GetRandomNumber()
        {
            int randomInt = _random.Next(0, 100);
            // picking weights arbitrarily for the different types of numbers I want
            if (randomInt < 25)
            {
                //small integer
                return _random.Next(0, 20).ToString();
            }
            else if (randomInt < 60)
            {
                // medium integer
                return _random.Next(0, 1000).ToString();
            }
            else if (randomInt < 85)
            {
                // small float
                string amount = ((float)_random.Next(0, 10000) / 100).ToString("0.00");
                if (_random.Next(0, 10) < 2) return $"${amount}";
                else return amount;
            }
            else
            {
                // medium-large float
                string amount = ((float)_random.Next(0, 10000000) / 100).ToString("0,000.00");
                if (_random.Next(0, 10) < 2) return $"${amount}";
                else return amount;
            }
        }


        public string GetRandomDate()
        {
            DateTime start = new DateTime(1995, 1, 1);
            int range = (DateTime.Today - start).Days;
            return start.AddDays(_random.Next(range)).ToString(GetRandomDateFormat());
        }


        public string GetRandomDate(string format)
        {
            DateTime start = new DateTime(1995, 1, 1);
            int range = (DateTime.Today - start).Days;
            return start.AddDays(_random.Next(range)).ToString(format);
        }

        public string GetRandomDateFormat()
        {
            List<string> formats = new List<string>();
            formats.Add("yyyy-MM-dd");

            formats.Add("dd-MM-yyyy");
            formats.Add("d-M-yyyy");
            formats.Add("dd-MM-yy");
            formats.Add("d-M-yy");

            formats.Add("dd/MM/yyyy");
            formats.Add("d/M/yyyy");
            formats.Add("dd/MM/yy");
            formats.Add("d/M/yy");

            formats.Add("dd.MM.yyyy");
            formats.Add("dd.MM.yy");

            formats.Add("dddd, dd MMMM yyyy");
            formats.Add("dddd, dd MMM yyyy");
            formats.Add("ddd, dd MMMM yyyy");
            formats.Add("dd MMMM yyyy");
            formats.Add("dd MMM yyyy");
            formats.Add("dd MMM yyy");

            Random random = new Random();
            int randomInt = random.Next(0, formats.Count);
            return formats[randomInt];
        }



        public string GetRandomEmailAddress()
        {
            string firstName = GetRandomWord().ToLower().Replace(".", "").Replace("'", "");
            string lastName = GetRandomWord().ToLower().Replace(".", "").Replace("'", "");
            string domain = GetRandomWord();
            string tld = GetRandomTld();

            string emailAddress = $"{firstName}.{lastName}@{domain}.{tld}";
            emailAddress = emailAddress.ToLower();
            return emailAddress;
        }


        public string GetRandomWebAddress()
        {
            string domainWord1 = GetRandomWord().Replace(".", "").Replace("'", "");
            string domainWord2 = GetRandomWord().Replace(".", "").Replace("'", "");
            string tld = GetRandomTld();
            string webAddress = $"www.{domainWord1}{domainWord2}.{tld}";
            if (_random.Next(0, 100) < 50)
            {
                if (_random.Next(0, 100) < 50)
                {
                    webAddress = "https://" + webAddress;
                }
                else
                {
                    webAddress = "http://" + webAddress;
                }
            }
            webAddress = webAddress.ToLower();
            return webAddress;
        }


        public string GetRandomTld()
        {
            string[] tlds = {
                "com",
                "net",
                "org",
                "edu",
                "gov",
                "com.au",
                "co.uk",
                "com.nz",
                "com.de",
                "com.nl",
                "com.ru",
                "com.jp",
                "com.fr",
                "com.ca",

                "io",
                "com",
                "horse",
                "ninja",
                "cloud",
                "coffee",
                "dev",
                "guru",
                "info",
                "lol",
                "science",
                "space",
                "sucks",
                "tech",
                "website",
                "wiki",
                "wtf"
            };
            return tlds[_random.Next(0, tlds.Length)];
        }



        public SKFont GetRandomFont(int fontSize)
        {
            SKFontStyleWeight weight = SKFontStyleWeight.Normal;
            SKFontStyleWidth width = SKFontStyleWidth.Normal;
            SKFontStyleSlant slant = SKFontStyleSlant.Upright;

            int weightPercent = _random.Next(0, 100);
            if (weightPercent < 5)
            {
                weight = SKFontStyleWeight.Black;
            }
            else if (weightPercent < 10)
            {
                weight = SKFontStyleWeight.Bold;
            }
            else if (weightPercent < 15)
            {
                weight = SKFontStyleWeight.ExtraLight;
            }

            //int widthPercent = _random.Next(0, 100);
            //if (widthPercent > 50)
            //{
            //    width = SKFontStyleWidth.Expanded; // doesn't appear to make a difference
            //}

            int slantPercent = _random.Next(0, 100);
            if (slantPercent < 5)
            {
                slant = SKFontStyleSlant.Italic;
            }

            SKTypeface tf = SKTypeface.FromFamilyName(GetRandomFontName(), weight, width, slant);
            return new SKFont(tf, fontSize);
        }

        public string GetRandomFontName()
        {
            var fonts = FontManager.Current.SystemFonts;

            string fontList = "";
            foreach (var font in fonts)
            {
                fontList += font.Name + ",\n";
            }


            string[] fontNames = {
                "Agency FB",
                "Arial",
                "Bahnschrift",
                "Bernard MT",
                "Bodoni MT",
                "Book Antiqua",
                "Bookman Old Style",
                "Britannic",
                "Calibri",
                "Calisto MT",
                 //"Cambria", // height way too big
                 //"Candara",  // hanging numerals
                "Cascadia Code",
                "Centaur",
                "Century",
                "Century Gothic",
                "Century Schoolbook",
                "Comic Sans MS",
                "Consolas",
                 //"Constantia",  // hanging numerals
                 // "Corbel",  // hanging numerals
                "Courier New", // numerals taller than capheight
                "Dubai",
                "Ebrima",
                "Elephant",
                "Eras ITC",
                "Fira Mono",
                "Footlight MT",
                "Franklin Gothic",
                "Gadugi",
                 //"Georgia",  // hanging numerals
                "Gill Sans MT",
                "Impact",
                "Lato",
                "Leelawadee UI",
                "Lucida Console",
                "Lucida Sans",
                "Lucida Sans Typewriter",
                "Maiandra GD",
                "Microsoft Himalaya",
                "Microsoft JhengHei",
                 //"MingLiU-ExtB", // all characters go under baseline
                "MS Reference Sans Serif",
                "NSimSun",
                "Nirmala UI",
                "OCR A",
                "OCR B",
                "Palatino Linotype",
                "Rockwell",
                "Segoe UI",
                 //"Sitka Text", // hanging numerals
                "Sylfaen",
                "Tahoma",
                "Times New Roman",
                "Trebuchet MS",
                "Tw Cen MT",
                "Verdana",
            };

            // test bad font name
            //return "not a font"; // results in default typeface 'Segeo UI'.

            return fontNames[_random.Next(0, fontNames.Length)];
        }



        public string GetRandomGibberishWord()
        {
            int syllableCount = _random.Next(1, 5);

            string word = "";

            // maybe start with a consonant
            if (_random.Next(0, 10) < 5)
            {
                word += GetRandomConsonant().ToString();
            }

            for (int i = 0; i < syllableCount; i++)
            {
                // second consonant
                if (i == 0 && _random.Next(0, 10) < 2)
                {
                    word += GetRandomConsonant().ToString();
                }

                // first vowel
                word += GetRandomVowel().ToString();

                // second vowel
                if (i == 0 && _random.Next(0, 10) < 5)
                {
                    word += GetRandomVowel().ToString();
                }

                // syllable ending consonent
                word += GetRandomConsonant().ToString();
            }

            // second ending consonant at end of word
            if (_random.Next(0, 10) < 5)
            {
                word += GetRandomConsonant().ToString();
            }

            return word;
        }


        private char GetRandomConsonant()
        {
            char[] consonants = new char[] { 'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'z' };
            int randomInt = _random.Next(0, consonants.Length);
            return consonants[randomInt];
        }

        private char GetRandomVowel()
        {
            char[] vowels = new char[] { 'a', 'e', 'i', 'o', 'u', 'y' }; // we'll use 'y' as a phonetic vowel.
            int randomInt = _random.Next(0, vowels.Length);
            return vowels[randomInt];
        }


        public Dictionary<string, double> FontCustomAscentFactor { get; } = new Dictionary<string, double>()
        {
            { "Agency FB", -0.80 },
            { "Arial", -0.80 },
            { "Bahnschrift", -0.75 },
            { "Baskerville Old Face", -0.72 },
            { "Bernard MT", -0.84 },
            { "Bodoni MT", -0.72 },
            { "Book Antiqua", -0.75 },
            { "Bookman Old Style", -0.75 },
            { "Britannic", -0.73 },
            { "Calibri", -0.71 },
            { "Calisto MT", -0.75 },
            { "Cambria", -0.75 },
            { "Candara", -0.75 },
            { "Cascadia Code", -0.79 },
            { "Centaur", -0.71 },
            { "Century", -0.81 },
            { "Century Gothic", -0.80 },
            { "Century Schoolbook", -0.80 },
            { "Comic Sans MS", -0.85 },
            { "Consolas", -0.73 },
            { "Constantia", -0.78 },
            { "Corbel", -0.75 },
            { "Courier New", -0.71 },
            { "Dubai", -0.76 },
            { "Ebrima", -0.80 },
            { "Elephant", -0.82 },
            { "Eras ITC", -0.75 },
            { "Fira Mono", -0.80 },
            { "Footlight MT", -0.74 },
            { "Franklin Gothic", -0.75 },
            { "Gadugi", -0.79 },
            { "Georgia", -0.82 },
            { "Gill Sans MT", -0.78 },
            { "Impact", -0.88 },
            { "Lato", -0.80 },
            { "Leelawadee UI", -0.82 },
            { "Lucida Console", -0.79 },
            { "Lucida Sans", -0.81 },
            { "Lucida Sans Typewriter", -0.80 },
            { "Maiandra GD", -0.80 },
            { "Malgun Gothic", -0.80 },
            { "Microsoft Himalaya", -0.52 },
            { "Microsoft JhengHei", -0.85 },
            { "MingLiU-ExtB", -0.75 },
            { "MingLiU_HKSCS-ExtB", -0.75 },
            { "MS Gothic", -0.85 },
            { "MS Reference Sans Serif", -0.83 },
            { "NSimSun", -0.75 },
            { "Nirmala UI", -0.80 },
            { "OCR A", -0.72 },
            { "Palatino Linotype", -0.78 },
            { "Rockwell", -0.75 },
            { "Segoe UI", -0.79 },
            { "Sitka Text", -0.73 },

            { "Sylfaen", -0.75 },
            { "Tahoma", -0.75 },
            { "Times New Roman", -0.75 },
            { "Trebuchet MS", -0.75 },
            { "Tw Cen MT", -0.75 },
            { "Verdana", -0.75 },
            { "Yu Gothic", -0.75 }
        };


    }
}

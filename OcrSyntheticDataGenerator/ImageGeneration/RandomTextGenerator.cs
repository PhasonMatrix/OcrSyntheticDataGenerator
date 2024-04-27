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


        public string GetRandomText(SKFont font)
        {
            int percentChance = _random.Next(0, 100);
            string text = "";

            if (percentChance < 10) text = GetRandomAlphaNumericCode();
            else if (percentChance < 20) text = GetRandomNumber();
            else if (percentChance < 30) text = GetRandomDate();
            else if (percentChance < 35) text = GetRandomEmailAddress();
            else if (percentChance < 40) text = GetRandomWebAddress();
            else text = GetRandomTextLine(font);

            // double check the font contains all glyphs
            if (!font.ContainsGlyphs(text))
            {
                Debug.WriteLine("This font can't draw every glyph for this text");
                return "";
            }

            return text;
        }



        public string GetRandomTextLine(SKFont font, int minWords = 1, int maxWords = 10)
        {
            int maxWordCount = _random.Next(minWords, maxWords);
            int wordCount = 0;
            string sentence = GetRandomWord();
            while (wordCount < maxWordCount)
            {
                if (_random.Next(0, 100) < 95) // mostly just words
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
                int rand = _random.Next(0, 110);

                if (rand < 20) { punctuation = ". "; }
                else if (rand < 30) { punctuation = ", "; }
                else if (rand < 35) { punctuation = "? "; }
                else if (rand < 40) { punctuation = "-"; }
                else if (rand < 45) { punctuation = "\" "; }
                else if (rand < 50) { punctuation = " \""; }
                else if (rand < 55) { punctuation = "\' "; }
                else if (rand < 60) { punctuation = " \'"; }
                else if (rand < 65) { punctuation = "! "; }
                else if (rand < 70) { punctuation = " – "; } // long dash
                else if (rand < 80) { punctuation = " ‘"; }  // left single quote
                else if (rand < 85) { punctuation = "’ "; }  // right single quote
                else if (rand < 90) { punctuation = " “"; }  // left double quote
                else if (rand < 95) { punctuation = "” "; }  // right double quote
                else if (rand < 96) { punctuation = "© "; }  // copyright
                else if (rand < 97) { punctuation = "® "; }  // registered trademark
                else if (rand < 98) { punctuation = "§ "; }  // section sign
                else if (rand < 99) { punctuation = " «"; }  // left double angle
                else if (rand < 100) { punctuation = "» "; }  // right double angle
                else if (rand < 101) { punctuation = " | "; }  // pipe
                else { punctuation = " "; } // default

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
            int length = _random.Next(1, 15);
            string code = "";
            for (int i = 0; i < length; i++)
            {
                int randomType = _random.Next(0, 100);
                if (randomType < 15)
                {
                    // special character
                    char[] specialChars = { '#', ':', '-', '+', '=', '_', '/', '.', '(', ')', '[', ']', '{', '}', '<', '>', '!', '@', '$', '%', '&', '*', '?', ',', '^' };
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
            // bold or italic?
            SKFontStyle fs = SKFontStyle.Normal;
            int stylePercent = _random.Next(0, 100);
            if (stylePercent < 2)
            {
                fs = SKFontStyle.BoldItalic;
            }
            else if (stylePercent < 7)
            {
                fs = SKFontStyle.Bold;
            }
            else if (stylePercent < 10)
            {
                fs = SKFontStyle.Italic;
            }
            SKTypeface tf = SKTypeface.FromFamilyName(GetRandomFontName(), fs);
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
                "Arial",
                "Bahnschrift",
                "Baskerville Old Face",
                "Bodoni MT",
                "Book Antiqua",
                "Bookman Old Style",
                "Calibri",
                "Calisto MT",
                "Cambria",
                "Candara",
                "Cascadia Code",
                "Centaur",
                "Century",
                "Century Schoolbook",
                "Comic Sans MS",
                "Consolas",
                "Constantia",
                "Corbel",
                "Courier New",
                "Dubai",
                "Ebrima",
                "Eras ITC",
                "Fira Mono",
                "Footlight MT",
                "Franklin Gothic",
                "Franklin Gothic Demi",
                "Gabriola",
                "Gadugi",
                "Georgia",
                "Gill Sans MT",
                "Helvetica",
                "Leelawadee UI",
                "Lucida Console",
                "Lucida Sans",
                "Lucida Sans Typewriter",
                "Maiandra GD",
                "Malgun Gothic",
                "Microsoft JhengHei",
                "MingLiU-ExtB",
                "MingLiU_HKSCS-ExtB",
                "MS Gothic",
                "MS Reference Sans Serif",
                "NSimSun",
                "Nirmala UI",
                "OCR A",
                "Palatino Linotype",
                "Perfect DOS VGA 437 Win",
                "Rockwell",
                "Segoe UI",
                "Sitka Text",
                "Sylfaen",
                "Tahoma",
                "Times New Roman",
                "Trebuchet MS",
                "Tw Cen MT",
                "Verdana",
                "Yu Gothic",
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



    }
}

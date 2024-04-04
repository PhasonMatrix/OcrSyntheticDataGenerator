using System;
using System.Collections.Generic;
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


        public string GetRandomText()
        {
            int percentChance = _random.Next(0, 100);

            if (percentChance < 10) return GetRandomAlphaNumericCode();
            else if (percentChance < 20) return GetRandomNumber();
            else if (percentChance < 30) return GetRandomDate();
            else if (percentChance < 35) return GetRandomEmailAddress();
            else if (percentChance < 40) return GetRandomWebAddress();
            else return GetRandomTextLine();
        }



        public string GetRandomTextLine(int minWords = 1, int maxWords = 10)
        {
            int maxWordCount = _random.Next(minWords, maxWords);
            int wordCount = 0;
            string sentence = GetRandomWord();
            while (wordCount < maxWordCount)
            {
                if (_random.Next(0, 100) < 95) // mostly just words
                {
                    string punc = getRandomPunctuation();
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
                        sentence += $" {GetRandomEmailAddress()} ";
                    } 
                    else
                    {
                        sentence += $" {GetRandomWebAddress()} ";
                    }
                }

                
                wordCount++;
            }

            return sentence.Trim();
        }


        public string getRandomPunctuation()
        {
            int rand = _random.Next(0, 100);

            if (rand < 10) { return ". "; }
            if (rand < 15) { return ", "; }
            if (rand < 16) { return "? "; }
            if (rand < 17) { return "-"; }
            if (rand < 18) { return "\" "; }
            if (rand < 19) { return " \""; }
            if (rand < 20) { return "\' "; }
            if (rand < 21) { return " \'"; }
            if (rand < 22) { return "! "; }
            if (rand < 23) { return " – "; } // long dash
            if (rand < 24) { return " ‘"; } // left single quote
            if (rand < 25) { return "’ "; } // right single quote
            if (rand < 26) { return " “"; } // left double quote
            if (rand < 27) { return "” "; } // right double quote
            if (rand < 28) { return "© "; } // copyright
            if (rand < 29) { return "® "; } // registered trademark
            if (rand < 30) { return "§ "; } // section sign
            if (rand < 31) { return " «"; } // left double angle
            if (rand < 32) { return "» "; } // right double angle
            if (rand < 33) { return " | "; } // pipe


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
                "Bodoni MT",
                "Calibri",
                "Cambria",
                "Candara",
                "Comic Sans MS",
                "Consolas",
                "Constantia",
                "Corbel",
                "Courier New",
                "Ebrima",
                "Franklin Gothic",
                "Franklin Gothic Demi",
                "Futura",
                "Garamond",
                "Georgia",
                "Helvetica",
                "Leelawadee UI",
                "Lucida Console",
                "Malgun Gothic",
                "MingLiU-ExtB",
                "MingLiU_HKSCS-ExtB",
                "MS Gothic",
                "Segoe UI",
                "NSimSun",
                "Sitka Text",
                "Sylfaen",
                "Tahoma",
                "Times New Roman",
                "Trebuchet MS",
                "Verdana",
                "Yu Gothic",
                "Book Antiqua",
                "Baskerville Old Face",
                "Bookman Old Style",
                "Dubai",
                "Gill Sans MT",
            };
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

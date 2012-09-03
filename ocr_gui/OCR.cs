using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Ionic.Zip;

namespace ocr_gui
{
    public class Letter
    {
        Bitmap bmp;
        public UnsafeBitmap ubm;
        public string convertString;
        public int checksum;

        public Letter(string filename)
        {
            bmp = new Bitmap(filename);
            ubm = new UnsafeBitmap(bmp);
            ubm.LockBitmap();
            string baseString = Path.GetFileNameWithoutExtension(filename);
            if (baseString.Length == 1)
            {
                convertString = baseString;
            }
            else if (baseString.EndsWith("_cap"))
            {
                convertString = baseString[0].ToString().ToUpper();
            }
            else switch (baseString)
                {
                    case "dash":
                        convertString = "-";
                        break;
                    case "dot":
                        convertString = ".";
                        break;
                    case "bracket_open":
                        convertString = "<";
                        break;
                    case "bracket_close":
                        convertString = ">";
                        break;
                    case "paren_open":
                        convertString = "(";
                        break;
                    case "paren_close":
                        convertString = ")";
                        break;
                    case "plus":
                        convertString = "+";
                        break;
                    case "slash":
                        convertString = "/";
                        break;
                    case "colon":
                        convertString = ":";
                        break;
                    case "underscore":
                        convertString = "_";
                        break;
                    case "square_bracket_open":
                        convertString = "[";
                        break;
                    case "square_bracket_close":
                        convertString = "]";
                        break;
                    case "comma":
                        convertString = ",";
                        break;
                    case "percent":
                        convertString = "%";
                        break;
                    case "pound_sign":
                        convertString = "#";
                        break;
                    case "star":
                        convertString = "*";
                        break;
                    default:
                        throw new Exception(filename);
                }

        }

        public bool compare(UnsafeBitmap test, int x, int y, bool lineHasChar)
        {
            string excludeChars = ".-:_";
            if (excludeChars.Contains(convertString)
                && (!lineHasChar)) return false;
            if ((y + ubm.Bitmap.Height) > test.Bitmap.Height) return false;
            if ((x + ubm.Bitmap.Width) > test.Bitmap.Width) return false;
            for (int j = 0; j < ubm.Bitmap.Width; j++)
            {
                for (int i = 0; i < ubm.Bitmap.Height; i++)
                {
                    // we are comparing binary bitmaps, so comparing just the green bit is ok
                    if (test.GetPixel(j + x, i + y).green != ubm.GetPixel(j, i).green)
                        return false;
                }
            }
            return true;
        }
    }

    public class LetterFont
    {
        List<Letter> letters = new List<Letter>();
        public int spaceWidth;
        public int charHeight;
        public string fontName;
        public LetterFont(string dirname, string fontName, int spaceWidth, int charHeight)
        {
            this.spaceWidth = spaceWidth;
            this.charHeight = charHeight;
            this.fontName = fontName;
            string fontdir = Path.Combine(Application.StartupPath, "chars");
            fontdir = Path.Combine(fontdir, dirname);
            foreach (string fileName in Directory.GetFiles(fontdir, "*.png"))
            {
                Letter letter = new Letter(fileName);
                letter.checksum = calcCheckSum(letter.ubm, 0, 0);
                letters.Add(letter);
            }
        }
        int calcCheckSum(UnsafeBitmap ubmp, int x, int y)
        {
            int checksum = 0;
            for (int i = 0; i < charHeight; i++)
            {
                checksum = checksum << 1;
                if (ubmp.GetPixel(x, y + i).green == 255) checksum += 1;
            }
            return checksum;
        }
        public Letter findLetter(UnsafeBitmap ubmp, int x, int y, bool lineHasChar)
        {
            int checksum = calcCheckSum(ubmp, x, y);
            foreach (Letter letter in letters)
            {
                if (letter.checksum != checksum) continue;
                if (letter.compare(ubmp, x, y, lineHasChar)) return letter;
            }
            return null;
        }
    }

    public class Parser
    {
        List<LetterFont> fonts = new List<LetterFont>();
        List<KeyValuePair<string, string>> fixList = new List<KeyValuePair<string, string>>();

        public Parser()
        {
            string fontdir = Path.Combine(Application.StartupPath, "chars");
            if (!Directory.Exists(fontdir))
            {
                using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("ocr_gui.chars.zip"))

                    using (ZipFile zip = ZipFile.Read(stream))
                    {
                        zip.ExtractAll(Application.StartupPath);
                    }
            }
            try
            {
                fonts.Add(new LetterFont("chars_toshiba", "TOSHIBA", 7, 14));
                fonts.Add(new LetterFont("chars_toshiba_small", "TOSHIBA_SMALL", 7, 14));
                fonts.Add(new LetterFont("chars_ge", "GE", 4, 13));
                fonts.Add(new LetterFont("chars_siemens", "SIEMENS", 4, 11));
                fonts.Add(new LetterFont("chars_philips", "PHILIPS", 11, 14));
                fonts.Add(new LetterFont("chars_philips_small", "PHILIPS_SMALL", 7, 9));

                fixList.Add(new KeyValuePair<string, string>("TotaI", "Total"));
                fixList.Add(new KeyValuePair<string, string>("CTDIvoI", "CTDIvol"));
                fixList.Add(new KeyValuePair<string, string>("HeIicaI", "Helical"));
                fixList.Add(new KeyValuePair<string, string>("AxiaI", "Axial"));
                fixList.Add(new KeyValuePair<string, string>("DetaiI", "Detail"));

                fixList.Add(new KeyValuePair<string, string>("Tota1", "Total"));
                fixList.Add(new KeyValuePair<string, string>("CTDIvo1", "CTDIvol"));
                fixList.Add(new KeyValuePair<string, string>("He1ica1", "Helical"));
                fixList.Add(new KeyValuePair<string, string>("Axia1", "Axial"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public string decode(Bitmap bmp, LetterFont compareFont)
        {
            string decodeString = "";
            using (UnsafeBitmap ubm = new UnsafeBitmap(bmp))
            {
                ubm.LockBitmap();

                for (int i = 0; i < ubm.Bitmap.Height - compareFont.charHeight + 1; i++)
                {
                    int lastCol = 0;
                    for (int j = 0; j < ubm.Bitmap.Width; j++)
                    {
                        Letter letter = compareFont.findLetter(ubm, j, i, lastCol > 0);
                        if (letter != null)
                        {
                            int padding = (j - lastCol) / compareFont.spaceWidth;
                            for (int k = 0; k < padding; k++)
                            {
                                decodeString += " ";
                            }
                            decodeString += letter.convertString;
                            j += letter.ubm.Bitmap.Width - 1;
                            lastCol = j + 1;
                        }

                    }
                    if (lastCol > 0)
                    {
                        decodeString += "\r\n";
                        i += compareFont.charHeight - 1;
                    }
                }
            }
            return decodeString.Trim();
        }

        public int countChars(string s)
        {
            int result = 0;
            foreach (char c in s)
            {
                if (!char.IsWhiteSpace(c) && c != 'I')
                    result++;
            }
            return result;
        }

        public string listReplace(string str)
        {
            StringBuilder sb = new StringBuilder(str);

            foreach (KeyValuePair<string, string> replacement in fixList)
            {
                sb.Replace(replacement.Key, replacement.Value);
            }
            return sb.ToString();
        }


        public string parse(Bitmap bmp)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            string decodeString = "";
            foreach (LetterFont font in fonts)
            {
                string test = decode(bmp, font);
                d.Add(font.fontName, test);
                if (countChars(decodeString) < countChars(test))
                    decodeString = font.fontName + "\r\n" + test;
            }
            if (d["PHILIPS"].Contains("DLP") && (d["PHILIPS_SMALL"].Contains("DLP"))) {
                decodeString="PHILIPS_MIXED\r\n"+d["PHILIPS"]+"\r\n"+d["PHILIPS_SMALL"];
            }

            return listReplace(decodeString);
        }
    }


}

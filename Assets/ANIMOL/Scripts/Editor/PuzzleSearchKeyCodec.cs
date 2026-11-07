using System;
using System.Text;

namespace ANIMOL.Presentation.Editor
{
    // Lossless dictionary coding only: decoded keys retain the exact original
    // state equivalence. Escape literal token/escape characters, including Unicode IDs.
    public static class PuzzleSearchKeyCodec
    {
        const char Escape = '\x7f';
        static readonly string[] Tokens = { "False:False:False:False:0:0", ":1,6,5,2,3,4,:", "0000:0:0" };
        public static string Pack(string key)
        {
            var result = new StringBuilder(key.Length);
            for (int index = 0; index < key.Length; index++)
            {
                bool matched = false;
                for (int token = 0; token < Tokens.Length; token++)
                {
                    var text = Tokens[token];
                    if (key[index] != text[0] || index + text.Length > key.Length ||
                        string.CompareOrdinal(key, index, text, 0, text.Length) != 0) continue;
                    result.Append((char)(128 + token)); index += text.Length - 1; matched = true; break;
                }
                if (matched) continue;
                if (key[index] >= Escape) result.Append(Escape);
                result.Append(key[index]);
            }
            return result.ToString();
        }
        public static string Unpack(string key)
        {
            var result = new StringBuilder(key.Length);
            for (int index = 0; index < key.Length; index++)
            {
                char value = key[index];
                if (value == Escape) result.Append(key[++index]);
                else if (value >= 128 && value < 128 + Tokens.Length) result.Append(Tokens[value - 128]);
                else if (value >= 128) throw new ArgumentException("Invalid packed state key.");
                else result.Append(value);
            }
            return result.ToString();
        }
    }
}

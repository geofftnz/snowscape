using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions.Text
{
    public static class FontMetaParser
    {

        public static IEnumerable<Tuple<string, string>> GetDelimitedFields(this string s, char groupDelimiter, char KeyValueDelimiter)
        {
            foreach (var group in s.Split(groupDelimiter))
            {
                var tokens = group.Split(KeyValueDelimiter);

                if (tokens.Length == 2)
                {
                    yield return new Tuple<string, string>(tokens[0], tokens[1]);
                }
            }
        }

        public static int GetIntValue(this IEnumerable<Tuple<string, string>> fields, string fieldName)
        {
            return int.Parse(fields.Where(f => f.Item1 == fieldName).Select(f => f.Item2).FirstOrDefault());
        }
        public static float GetFloatValue(this IEnumerable<Tuple<string, string>> fields, string fieldName)
        {
            return float.Parse(fields.Where(f => f.Item1 == fieldName).Select(f => f.Item2).FirstOrDefault(), System.Globalization.NumberStyles.Float);
        }

        public static bool TryParseCharacterInfoLine(string s, out FontCharacter fontCharacter)
        {
            fontCharacter = new FontCharacter();

            if (!s.StartsWith(@"char"))
            {
                return false;
            }

            var fields = s.GetDelimitedFields(' ', '=').ToList();

            try
            {
                fontCharacter.ID = fields.GetIntValue(@"id");
                fontCharacter.TexcoordX = fields.GetFloatValue(@"x");
                fontCharacter.TexcoordY = fields.GetFloatValue(@"y");
                fontCharacter.TexcoordW = fields.GetFloatValue(@"width");
                fontCharacter.TexcoordH = fields.GetFloatValue(@"height");
                fontCharacter.XOffset = fields.GetFloatValue(@"xoffset");
                fontCharacter.YOffset = fields.GetFloatValue(@"yoffset");
                fontCharacter.XAdvance = fields.GetFloatValue(@"xadvance");
            }
            catch (FormatException) { return false; }
            catch (OverflowException) { return false; }
            catch (ArgumentNullException) { return false; }

            return true;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenTKExtensions.Loaders
{
    public static class Preprocessor
    {
        public static string Preprocess(this string source, int levelsRemaining, IShaderLoader loader)
        {
            if (levelsRemaining < 0)
            {
                throw new InvalidOperationException("Maximum recursion levels exceeded in preprocessing.");
            }

            //string source = 

            // find all instances of #include "x" and replace with preprocessed input

            var sb = new StringBuilder();

            foreach (var token in source.Tokens())
            {
                switch (token.Type)
                {
                    case TokenType.Text:
                        sb.Append(token.Content);
                        break;
                    case TokenType.Include:
                        sb.Append(loader.LoadRaw(token.Content).Preprocess(levelsRemaining - 1, loader));
                        break;
                }
            }


            return sb.ToString();
        }



        public enum TokenType
        {
            Text = 0,
            Include
        }

        public class Token
        {
            public TokenType Type { get; set; }
            public string Content { get; set; }
        }

        public static IEnumerable<Token> Tokens(this string s)
        {
            int i = 0;
            int l = s.Length;

            Regex includeRegex = new Regex("#include (\"[^\"]+\")", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

            var match = includeRegex.Match(s, i, l);

            while (match.Success)
            {
                // return a token for everything up to here
                if (match.Index > i)
                {
                    yield return new Token { Type = TokenType.Text, Content = s.Substring(i, match.Index - i) };
                }

                // get filename from #include
                if (match.Groups.Count > 1)
                {
                    if (match.Groups[1].Success)
                    {
                        string filename = match.Groups[1].Value.Replace("\"", "");

                        yield return new Token { Type = TokenType.Include, Content = filename };
                    }
                }

                i = match.Index + match.Length;
                match = match.NextMatch();
            }

            if (i < l)
            {
                yield return new Token { Type = TokenType.Text, Content = s.Substring(i, l - i) };
            }

        }



    }
}

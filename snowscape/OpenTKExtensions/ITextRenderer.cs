using System;
using System.Collections.Generic;
namespace OpenTKExtensions
{
    interface ITextRenderer
    {
        Dictionary<char, FontCharacter> Characters { get; }
        float AddChar(char c, float x, float y, float z, float size, OpenTK.Vector4 col);
        float AddString(string s, float x, float y, float z, float size, OpenTK.Vector4 col);
    }
}

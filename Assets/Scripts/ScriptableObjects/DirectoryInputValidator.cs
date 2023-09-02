using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "DirectoryInputValidator", menuName = "TMPUtils/DirectoryInputValidator")]
public class DirectoryInputValidator : TMP_InputValidator
{
    private readonly char[] _invalidChars;
    public DirectoryInputValidator()
    {
        _invalidChars = Path.GetInvalidFileNameChars();
        //Path.GetInvalidPathChars
    }

    public override char Validate(ref string text, ref int pos, char ch)
    {
        if (!_invalidChars.Contains(ch))
        {
            text += ch;
            pos++;
        }
        return ch;
    }
}


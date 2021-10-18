using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PSSharp.Ini
{
    public sealed partial class IniDictionary
    {
        public static string Serialize(IniDictionary ini)
        {
            throw new NotImplementedException();
            // var sb = new StringBuilder();
            // foreach (var section in ini.GetSections())
            // {
            //     sb.AppendLine($"[{section.Name}]");
            //     foreach (var keyValuePair in section)
            //     {
            //         if (keyValuePair.Key.Contains("=")
            //             || keyValuePair.Value.Contains("="))
            //         {
            //             throw new InvalidOperationException("The key and value may not contain the '=' (equal sign) character.");
            //         }
            //         sb.AppendLine($"{keyValuePair.Key}={keyValuePair.Value}");
            //     }
            // }
            // return sb.ToString();
        }
        public override string ToString() => Serialize(this);
        public static IniDictionary Deserialize(Stream data)
        {
            var text = new StringBuilder();
            var encoding = Encoding.Unicode;
            var bytesRead = new byte[1024];
            while(true)
            {
                data.Read(bytesRead, 0, bytesRead.Length);
                var textRead = encoding.GetString(bytesRead);
                text.Append(textRead);
            }
            var lines = text.ToString().Split('\n');
            var dictionary = new IniDictionary();
            string? section = null;

            for(int i = 0; i < lines.Length; i++)
            {
                ParseLine(lines[i], i, dictionary, ref section);
            }
            return dictionary;
        }

        private static void ParseKeyValueLine(string? keyValueLine, int lineNumber, out string key, out string? value)
        {
            if (keyValueLine is null)
            {
                key = string.Empty;
                value = null;
                return;
            }

            var index = keyValueLine.IndexOf('=');
            if (index == -1)
            {
                throw new IniSerializerException(lineNumber, -1, "Attempted to parse invalid string as key/value line.");
            }
            key = keyValueLine.Substring(0, index);
            value = keyValueLine.Substring(index);
        }
        private static void ParseSectionLine(string? sectionLine, int lineNumber, out string sectionName)
        {
            if (sectionLine is null) sectionName = string.Empty;
            else
            {
                // section line should start with "[" and end with "]".
                var firstIndexOfStart = sectionLine.IndexOf('[');
                if (firstIndexOfStart != 0)
                {
                    throw new IniSerializerException(lineNumber, 0, "The section name declaration must be enclosed in square brackets.");
                }
                var indexOfStart = sectionLine.IndexOf('[', 1);
                if (indexOfStart != 0)
                {
                    throw new IniSerializerException(lineNumber, indexOfStart, "The section name declaration must be enclosed in square brackets.");
                }
                int indexOfEnd = sectionLine.IndexOf(']');
                if (indexOfEnd != sectionLine.Length - 1)
                {
                    throw new IniSerializerException(lineNumber, sectionLine.Length - 1, "The section name declaration must be enclosed in square brackets.");
                }
                sectionName = sectionLine.Substring(1, sectionLine.Length - 2);
            }
        }
        private static void ParseLine(string? line, int lineNumber, IniDictionary destination, ref string? currentSection)
        {
            switch (GetTokenType(line))
            {
                case IniTokenType.EmptyLine:
                case IniTokenType.Comment:
                    return;
                case IniTokenType.Section:
                {
                    ParseSectionLine(line, lineNumber, out currentSection);
                }
                break;
                case IniTokenType.Key:
                {
                    ParseKeyValueLine(line, lineNumber, out var key, out var value);
                    if (currentSection is null) throw new IniSerializerException(lineNumber, 0, "Expected section header but got key-value.");
                    destination[currentSection][key] = value;
                }
                break;
            }
        }
        private static IniTokenType GetTokenType(string? line)
        {
            var firstCharacter = line is null ? null : line.Length > 0 ? line[0] as char? : null;
            switch (line?[0])
            {
                case null: return IniTokenType.EmptyLine;
                case '[': return IniTokenType.Section;
                case ';': return IniTokenType.Comment;
                default: return IniTokenType.Key;
            }
        }
        internal enum IniTokenType
        {
            EmptyLine,
            Comment,
            Section,
            Key
        }
    }
}
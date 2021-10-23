using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSSharp.Ini
{
    public sealed partial class IniDictionary
    {
        private static Encoding AsciiEncoding => Encoding.ASCII;
        private static byte NewlineByte = AsciiEncoding.GetBytes("\n")[0];
        private static byte[] NewlineBytes = AsciiEncoding.GetBytes("\n");

        public static void Serialize(IniDictionary ini, Stream stream)
        {
            var encoding = AsciiEncoding;
            var sections = ini.GetSections();
            for(int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
            {
                var section = sections[sectionIndex];
                var sectionBytes = encoding.GetBytes($"[{section.Name}]\n");
                stream.Write(sectionBytes, 0, sectionBytes.Length);
                foreach (var element in section)
                {
                    var elementBytes = encoding.GetBytes($"{element.Key}={element.Value}\n");
                    stream.Write(elementBytes, 0, elementBytes.Length);
                }
                stream.WriteByte(NewlineByte);
            }
        }
        public static async Task SerializeAsync(IniDictionary ini, Stream stream, CancellationToken cancellationToken = default)
        {
            var encoding = AsciiEncoding;
            var sections = ini.GetSections();
            for(int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
            {
                var section = sections[sectionIndex];
                var sectionBytes = encoding.GetBytes($"[{section.Name}]\n");
                await stream.WriteAsync(sectionBytes, 0, sectionBytes.Length, cancellationToken);
                foreach (var element in section)
                {
                    var elementBytes = encoding.GetBytes($"{element.Key}={element.Value}\n");
                    await stream.WriteAsync(elementBytes, 0, elementBytes.Length, cancellationToken);
                }
                await stream.WriteAsync(NewlineBytes, 0, NewlineBytes.Length);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            var sections = GetSections();
            for(int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
            {
                var section = sections[sectionIndex];
                sb.AppendLine($"[{section.Name}]");
                foreach (var element in section)
                {
                    sb.AppendLine($"{element.Key}={element.Value}");
                }
                sb.AppendLine();
            }
            return sb.ToString();
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
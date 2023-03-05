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


        public override string ToString()
        {
            var sb = new StringBuilder();
            var sections = GetSections();
            for (int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
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
        public static void Serialize(IniDictionary ini, Stream stream)
        {
            var encoding = AsciiEncoding;
            var sections = ini.GetSections();
            string? firstLine = null;
            for (int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
            {
                var section = sections[sectionIndex];
                var sectionBytes = encoding.GetBytes($"{firstLine}[{section.Name}]");
                stream.Write(sectionBytes, 0, sectionBytes.Length);
                foreach (var element in section)
                {
                    var elementBytes = encoding.GetBytes($"\r\n{element.Key}={element.Value}");
                    stream.Write(elementBytes, 0, elementBytes.Length);
                }

                firstLine ??= "\r\n";
            }
        }
        public static async Task SerializeAsync(IniDictionary ini, Stream stream, CancellationToken cancellationToken = default)
        {
            var encoding = AsciiEncoding;
            var sections = ini.GetSections();
            string? firstLine = null;
            for (int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
            {
                var section = sections[sectionIndex];
                var sectionBytes = encoding.GetBytes($"{firstLine}[{section.Name}]");
                await stream.WriteAsync(sectionBytes, 0, sectionBytes.Length, cancellationToken);
                foreach (var element in section)
                {
                    var elementBytes = encoding.GetBytes($"\r\n{element.Key}={element.Value}");
                    await stream.WriteAsync(elementBytes, 0, elementBytes.Length, cancellationToken);
                }

                firstLine ??= "\r\n";
            }
        }
        public static IniDictionary Deserialize(Stream stream, CancellationToken cancellationToken = default)
        {
            var ini = new IniDictionary();
            var encoding = AsciiEncoding;
            var block = new byte[1024];
            StringBuilder? incompleteLine = null;
            string? currentSection = null;
            int lineNumber = 0;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bytesRead = stream.Read(block, 0, block.Length);
                var currentLine = encoding.GetString(block, 0, bytesRead);
                if (!currentLine.Contains("\r\n"))
                {
                    if (incompleteLine is null)
                        incompleteLine = new StringBuilder(currentLine);
                    else
                        incompleteLine.Append(currentLine);

                    continue;
                }
                if (incompleteLine != null)
                {
                    currentLine = incompleteLine.Append(currentLine).ToString();
                }

                string[] lines;
                do
                {
                    lines = currentLine.Split(new[] { '\r', '\n' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 1)
                    {
                        incompleteLine = new StringBuilder(lines[1]);
                    }
                    else
                    {
                        incompleteLine = null;
                    }

                    currentLine = lines[0];
                    ParseLine(currentLine, ++lineNumber, ini, ref currentSection);
                    currentLine = incompleteLine?.ToString()!;
                }
                while (currentLine?.Contains("\r\n") ?? false);
            }
            while (stream.Position < stream.Length);

            return ini;
        }
        public static async Task<IniDictionary> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var ini = new IniDictionary();
            var encoding = AsciiEncoding;
            var block = new byte[1024];
            StringBuilder? incompleteLine = null;
            string? currentSection = null;
            int lineNumber = 0;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bytesRead = await stream.ReadAsync(block, 0, block.Length, cancellationToken);
                var currentLine = encoding.GetString(block, 0, bytesRead);
                if (!currentLine.Contains("\r\n"))
                {
                    if (incompleteLine is null)
                        incompleteLine = new StringBuilder(currentLine);
                    else
                        incompleteLine.Append(currentLine);

                    continue;
                }
                if (incompleteLine != null)
                {
                    currentLine = incompleteLine.Append(currentLine).ToString();
                }

                string[] lines;
                do
                {
                    lines = currentLine.Split(new[] { '\r', '\n' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 1)
                    {
                        incompleteLine = new StringBuilder(lines[1]);
                    }
                    else
                    {
                        incompleteLine = null;
                    }

                    currentLine = lines[0];
                    ParseLine(currentLine, ++lineNumber, ini, ref currentSection);
                    currentLine = incompleteLine?.ToString()!;
                }
                while (currentLine?.Contains("\r\n") ?? false);
            }
            while (stream.Position < stream.Length);

            return ini;
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
            value = keyValueLine.Substring(index + 1);
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
                    throw new IniSerializerException(lineNumber, 0, "The section name declaration must begin with an opening square bracket ('[').");
                }
                var indexOfExtraStart = sectionLine.IndexOf('[', 1);
                if (indexOfExtraStart != -1)
                {
                    throw new IniSerializerException(lineNumber, indexOfExtraStart, "Invalid number of section name brackets. There should be no more than a single opening square bracket in the section header line.");
                }
                int indexOfEnd = sectionLine.IndexOf(']');
                if (indexOfEnd != sectionLine.Length - 1)
                {
                    throw new IniSerializerException(lineNumber, sectionLine.Length - 1, "The section name declaration must contain a single closing square bracket (']') at the end of the line.");
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
        private enum IniTokenType
        {
            EmptyLine,
            Comment,
            Section,
            Key
        }
    }
}
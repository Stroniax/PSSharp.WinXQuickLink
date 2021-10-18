using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PSSharp.Ini
{
    public sealed partial class IniFile
    {
        public static string[] GetKeyNames(string fileName, string section)
        {
            const int MAX_BUFFER = 32767;
            IntPtr pReturnedString = Marshal.AllocCoTaskMem(MAX_BUFFER);
            try
            {
                uint bytesReturned = GetPrivateProfileSection(section, pReturnedString, MAX_BUFFER, fileName);
                if (bytesReturned == 0)
                {
                    return Array.Empty<string>();
                }
                else
                {
                    string local = Marshal.PtrToStringUni(pReturnedString, (int)bytesReturned).ToString();
                    return local.Substring(0, local.Length - 1).Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(pReturnedString);
            }
        }
        public static string[] GetSectionNames(string fileName)
        {
            var buffer = new byte[1024];
            GetPrivateProfileSectionNames(buffer, buffer.Length, fileName);
            var allSections = System.Text.Encoding.Default.GetString(buffer);
            return allSections.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
        }
        public static string? GetKeyValue(string filePath, string section, string key, string? defaultValue = null)
        {
            var output = new StringBuilder();
            GetPrivateProfileString(section, key, defaultValue, output, 255, filePath);
            return output.ToString();
        }
        public static void SetValue(string filePath, string section, string? key, string? value)
        {
            WritePrivateProfileString(section, key, value, filePath);
        }
        public static Dictionary<string, Dictionary<string, string?>> LoadAsDictionary(string filePath)
        {
            if (filePath is null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException(null, filePath);

            var iniFile = new Dictionary<string, Dictionary<string, string?>>();
            var sectionNames = GetSectionNames(filePath);
            foreach (var sectionName in sectionNames)
            {
                iniFile.Add(sectionName, new Dictionary<string, string?>());
                foreach (var key in GetKeyNames(filePath, sectionName))
                {
                    iniFile[sectionName][key] = GetKeyValue(filePath, sectionName, key);
                }
            }
            return iniFile;
        }
    }
}
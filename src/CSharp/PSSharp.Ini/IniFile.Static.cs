using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
                    string local = Marshal.PtrToStringAuto(pReturnedString, (int)bytesReturned);
                    var keyValuePairs = local.Substring(0, local.Length - 1).Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                    var keys = new string[keyValuePairs.Length];
                    for(int i = 0; i < keyValuePairs.Length; i++)
                    {
                        keys[i] = keyValuePairs[i].Split('=')[0];
                    }
                    return keys;
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(pReturnedString);
            }
        }
        public static string[] GetSectionNames(string fileName)
        {
            const int MAX_BUFFER = 32767;
            IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER);
            try
            {
                uint bytesReturned = GetPrivateProfileSectionNames(pReturnedString, MAX_BUFFER, fileName);
                if (bytesReturned == 0)
                {
                    return Array.Empty<string>();
                }
                string local = Marshal.PtrToStringAuto(pReturnedString, (int)bytesReturned);
                return local.Substring(0, local.Length - 1).Split('\0');
            }
            finally
            {
                Marshal.FreeCoTaskMem(pReturnedString);
            }
            
            //var buffer = new byte[1024];
            //GetPrivateProfileSectionNames(buffer, buffer.Length, fileName);
            //var allSections = Encoding.Default.GetString(buffer);
            //return allSections.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
        }
        public static string? GetKeyValue(string filePath, string section, string key, string? defaultValue = null)
        {
            var output = new StringBuilder();
            GetPrivateProfileString(section, key, defaultValue, output, 255, filePath);
            return output.ToString();
        }
        public static void SetValue(string filePath, string? section, string? key, string? value)
        {
#warning not properly clearing data when passed null values
            if (!WritePrivateProfileString(section, key, value, filePath))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
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


        /// <summary>
        /// Opens an existing file and loads the values into the dictionary.
        /// </summary>
        /// <param name="filePath">The path to the ini file to open.</param>
        /// <returns>An instance of <see cref="IniFile"/> that represents the values of the .ini file at path <paramref name="filePath"/>.</returns>
        public static IniFile Open(string filePath, IniFileSynchronizationState synchronizationState)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException(null, filePath);
            var file = new IniFile(filePath, new IniDictionary(), synchronizationState);
            file.Load();
            return file;
        }
        /// <summary>
        /// Creates a new <see cref="IniFile"/> instance without no values defined.
        /// </summary>
        /// <param name="filePath">The path to the ini file the instance represents.</param>
        /// <returns>An instance of <see cref="IniFile"/> that represents a .ini file at the path indicated by <paramref name="filePath"/>.</returns>
        public static IniFile CreateNew(string filePath, IniFileSynchronizationState synchronizationState)
            => CreateNew(filePath, synchronizationState, new IniDictionary());
        public static IniFile CreateNew(string filePath, IniFileSynchronizationState synchronizationState, IniDictionary values)
        {
            var file = new IniFile(filePath, values ?? new IniDictionary(), synchronizationState);
            return file;
        }
    }
}
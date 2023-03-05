using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PSSharp.Ini
{
    public sealed partial class IniFile
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WritePrivateProfileString(string? section, string? key, string? value, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetPrivateProfileString(string section, string? key, string? defaultValue, StringBuilder returnValue, int size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetPrivateProfileSectionNames(byte[] lpszReturnBuffer, int nSize, string lpFileName);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetPrivateProfileSectionNames(IntPtr lpszReturnBuffer, int nSize, string lpFileName);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint GetPrivateProfileSection(string lpAppName, IntPtr lpszReturnBuffer, uint nSize, string lpFileName);
    }
}
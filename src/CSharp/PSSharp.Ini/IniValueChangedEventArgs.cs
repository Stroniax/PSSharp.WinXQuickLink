using System;

namespace PSSharp.Ini
{
    /// <summary>
    /// Event args raised when any value within a <see cref="IniDictionary"/> is updated.
    /// </summary>
    public sealed class IniDictionaryValueChangedEventArgs : EventArgs
    {
        public IniDictionaryValueChangedEventArgs(string? section, string? key, string? value,string? oldValue)
            : this(section, key, value, oldValue, null)
        {

        }
        public IniDictionaryValueChangedEventArgs(string? section, string? key, string? value,string? oldValue, string? filePath)
        {
            FilePath = filePath;
            Section = section;
            Key = key;
            Value = value;
            OldValue = oldValue;
        }

        /// <summary>
        /// The file path of the <see cref="IniDictionary"/> instance that was modified.
        /// </summary>
        public string? FilePath { get; }
        /// <summary>
        /// The name of the section that was modified within the <see cref="IniDictionary"/>.
        /// </summary>
        public string? Section { get; }
        /// <summary>
        /// The name of the key that was modified within the <see cref="IniSection"/>.
        /// </summary>
        public string? Key { get; }
        /// <summary>
        /// The new value that was set.
        /// </summary>
        public string? Value { get; }
        /// <summary>
        /// The previous value.
        /// </summary>
        public string? OldValue { get; }
    }
}

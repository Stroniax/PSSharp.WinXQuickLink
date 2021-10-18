using System;

namespace PSSharp.Ini
{
    /// <summary>
    /// The state of an <see cref="IniDictionary"/> instance, indicating the connection state between the instance and
    /// the file that the instance represents.
    /// </summary>
    [Flags]
    public enum IniDictionaryState
    {
        /// <summary>
        /// The <see cref="IniDictionary"/> is not representative of the values of the underlying file.
        /// </summary>
        Staged = 0,
        /// <summary>
        /// The <see cref="IniDictionary"/> is loaeded with the current values of the underlying file.
        /// A section or key that is updated will cause the <see cref="IniDictionary"/> to enter the
        /// <see cref="Staged"/> state.
        /// </summary>
        Current = 1,
        /// <summary>
        /// The <see cref="IniDictionary"/> is loaded with the current values of the underlying file, and when the file
        /// is changed the <see cref="IniDictionary"/> instance will be reloaded to reflect the updated state of the
        /// file. Other changes made to the <see cref="IniDictionary"/> instance will change the state to <see cref="Staged"/>.
        /// </summary>
        AuthoritativeRead = Current | 2,
        /// <summary>
        /// The <see cref="IniDictionary"/> is loaded with the current values of the underlying file, and when changes
        /// are made to  the <see cref="IniDictionary"/> instance the file will be re-written to reflect the modified value.
        /// </summary>
        AuthoritativeWrite = Current | 4,
        /// <summary>
        /// The <see cref="IniDictionary"/> is loaded and tracking the current values of the underlying file.
        /// A section or key that is updated will be immediately persisted to the file. Changes made to the file
        /// will be immediately loaded into the current <see cref="IniDictionary"/> instance.
        /// </summary>
        AuthoritativeReadWrite = Current | AuthoritativeRead | AuthoritativeWrite,
        /// <summary>
        /// The <see cref="IniDictionary"/> holds a lock on the underlying .ini file to prevent other applications from
        /// reading to or writing to the file.
        /// </summary>
        Locked,
    }
}

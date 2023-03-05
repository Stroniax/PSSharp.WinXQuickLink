namespace PSSharp.Ini
{
    /// <summary>
    /// Defines methods for an <see cref="IniFile"/> instance to remain synchronized
    /// with the file system.
    /// </summary>
    public enum IniFileSynchronizationState
    {
        /// <summary>
        /// The <see cref="IniFile"/> is not being synchronized to the underlying file.
        /// </summary>
        Disconnected,
        /// <summary>
        /// The <see cref="IniFile"/> is monitoring the underlying file and reading changes as the file is updated.
        /// Changes made to the <see cref="IniFile"/> instance will not be published to the underlying file.
        /// </summary>
        MonitorRead,
        /// <summary>
        /// The <see cref="IniFile"/> is not reading updates to the file but changes made to the dictionary
        /// will be reflected in the file.
        /// </summary>
        WriteOnly,
        /// <summary>
        /// The <see cref="IniFile"/> is monitoring the underlying file and reading changes as the file is updated.
        /// Changes made to the dictionary will be immediately pushed to the underlying file.
        /// </summary>
        MonitorReadWrite,
        /// <summary>
        /// The <see cref="IniFile"/> owns the underlying file. Other proesses may read the file, but writing is
        /// restricted to only allow the <see cref="IniFile"/> to update the file data.
        /// </summary>
        OwnedPublic,
        /// <summary>
        /// The <see cref="IniFile"/> owns the underlying file. No other process may read or write to the file.
        /// </summary>
        OwnedPrivate,
    }
}
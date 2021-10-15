using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using PSSharp.WinXQuickLink;
using WindowsShortcutFactory;

namespace PSSharp.WinXQuickLink
{
    [Serializable]
    public abstract class QuickLinkBase : INotifyPropertyChanged, IComparable<QuickLinkBase>
    {
        #region static members
        static QuickLinkBase()
        {
            Watcher = new FileSystemWatcher()
            {
                IncludeSubdirectories = true,
                Path = QuickLinkGroupDirectory,
            };

            _groups = new List<QuickLinkGroup>();

            lock(_constructingLock)
            {
                Watcher.Error += (sender, e) => ResetQuickLinks();
                Watcher.Renamed += OnFileWatcherEvent;
                Watcher.Deleted += OnFileWatcherEvent;
                Watcher.Created += OnFileWatcherEvent;
                Watcher.Changed += OnFileWatcherEvent;
                
                ResetQuickLinks();
            }
        }
        private static volatile bool _reloadQuickLinksRequired;
        internal static void ResetQuickLinks()
        {
            lock(_constructingLock)
            {
                _groups ??= new List<QuickLinkGroup>();
                _groups.ForEach(i => i.IsInvalidated = true);
                _groups.Clear();
                _reloadQuickLinksRequired = true;
            }
        }
        internal static void EnsureQuickLinksLoaded(PSCmdlet cmdlet)
        {
            lock(_constructingLock)
            {
                if (_reloadQuickLinksRequired)
                {
                    _reloadQuickLinksRequired = false;
                }
                else
                {
                    return;
                }

                ResetQuickLinks();
                
                var directoryPaths = Directory.GetDirectories(QuickLinkGroupDirectory);
                foreach (var directoryPath in directoryPaths)
                {
                    // create a QuickLinkGroup instance from the directory

                    var entryPaths = Directory.GetFiles(directoryPath);
                    foreach (var entryPath in entryPaths)
                    {
                        // create a QuickLinkEntry from the file
                    }
                }
            }
        }
        private static void OnFileWatcherEvent(object? sender, FileSystemEventArgs eventArgs)
        {
            var changedPath = eventArgs.FullPath;
            string? oldName = null;
            string? oldFullPath = null;
            if (eventArgs is RenamedEventArgs renamedArgs)
            {
                oldFullPath = changedPath = renamedArgs.OldFullPath;
                oldName = renamedArgs.OldName;
            }
            
            // identify the original entry and update it appropriately

            // identify the siblings of the change path

            // sort the file system items

            // update the position of each QuickLinkData item
        }
        private static int FileSystemInfoComparer(FileSystemInfo x, FileSystemInfo y)
            => FileOrDirectoryNameComparer(x.FullName, y.FullName);
        private static int FileOrDirectoryNameComparer(string x, string y)
        {
            var descending = StringComparer.OrdinalIgnoreCase.Compare(x, y);
            return descending == 1 ? -1 : descending == -1 ? 1 : descending;
        }
        public static string QuickLinkGroupDirectory
            => System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft",
                "Windows",
                "WinX");
        internal static FileSystemWatcher Watcher { get; }
        private static object _constructingLock = new object();
        /// <summary>
        /// Contains all QuickLink groups in the File System, which collectively contain all QuickLink entries.
        /// </sumary>
        private static List<QuickLinkGroup> _groups;
        #endregion static members

        #region  instance members
        /// <summary>
        /// Raised when the current instance becomes invalid (deleted or changed in a way
        /// that cannot be represented by the current instance).
        /// </summary>
        public event EventHandler? Invalidated;
        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _path;
        private string _name;
        private int _position;
        private bool _isInvalidated;

        /// <summary>
        /// The FileSystem path of the shortcut file or directory that is represented by the current <see cref="QuickLinkEntry"/>
        /// or <see cref="QuickLinkGroup"/>, respectively.
        /// </summary>
        public string Path
        {
            get => _path;
            protected set
            {
                if (_path == value) return;
                _path = value ?? throw new ArgumentNullException(nameof(Path));
                NotifyPropertyChanged(nameof(Path));
            }
        }
        /// <summary>
        /// The display name of the item.
        /// </summary>
        public string Name
        {
            get => _name;
            protected set
            {
                if (_name == value) return;
                _name = value ?? throw new ArgumentNullException(nameof(Name));
                NotifyPropertyChanged(nameof(Name));
            }
        }
        /// <summary>
        /// The position in which the item will be displayed, relative to the bottom of the menu.
        /// The value -1 represents an entry that is not enabled.
        /// </summary>
        public int Position
        {
            get => _position;
            set
            {
                if (_position == value) return;
                _position = value;
                NotifyPropertyChanged(nameof(Position));
            }
        }

        /// <summary>
        /// Indicates whether the current instance no longer represents live data.
        /// </summary>
        public bool IsInvalidated
        {
            get => _isInvalidated;
            private set
            {
                if (_isInvalidated || !value) return;
                _isInvalidated = true;
                Invalidated?.Invoke(this, EventArgs.Empty);
                NotifyPropertyChanged(nameof(IsInvalidated));
            }
        }

        internal QuickLinkBase(string path, string name)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Invokes the <see cref="PropertyChanged"/> event for the property <paramref name="propertyName"/>.
        /// </summary>
        /// <remarks>
        /// This method is exposed to allow derived classes to implement the <see cref="INotifyPropertyChanged"/>
        /// interface and should not be publicly exposed.
        /// </remarks>
        /// <param name="propertyName">The name of the property that was changed.</param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch
            {
                this.IsInvalidated = true;
                throw;
            }
        }

        /// <summary>
        /// Returns the name of the current quick link group or entry.
        /// </summary>
        /// <returns><see cref="Name"/></returns>
        public override string ToString() => Name;

        public int CompareTo(QuickLinkBase other)
        {
            if (other is null)
            {
                return -1;
            }
            if (this is QuickLinkGroup && other is QuickLinkEntry)
            {
                return 1;
            }
            if (IsInvalidated != other.IsInvalidated)
            {
                return IsInvalidated ? -1 : 1;
            }
            return Position < other.Position ? -1
                : Position > other.Position ? 1
                : 0;
        }
        #endregion
    }
    [Serializable]
    public sealed class QuickLinkGroup : QuickLinkBase, IReadOnlyList<QuickLinkEntry>, IComparable<QuickLinkGroup>
    {
        internal QuickLinkGroup(DirectoryInfo directory)
            :base(directory.FullName, directory.Name)
        {

        }
        internal QuickLinkGroup(string path, IEnumerable<QuickLinkEntry> entries)
            :base(path, System.IO.Path.GetDirectoryName(path))
        {
            _entries.AddRange(entries);
            _totalCount = _entries.Count;
            _count = _entries.Count(i => i.IsEnabled);
        }


        private readonly List<QuickLinkEntry> _entries = new List<QuickLinkEntry>(16);
        private int _totalCount;
        private int _count;

        public QuickLinkEntry this[int index]
        {
            get 
            {
                if (index < 0) throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "The index must be a positive number. Disabled quick link entries cannot be accessed " +
                    "through the group indexer.");
                var listIndex = _entries.FindIndex(entry => entry.Position == index);
                if (listIndex == -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, $"No quick link exists in position {index}.");
                }
                else
                {
                    return _entries[listIndex];
                }
            }
        }

        /// <summary>
        /// The total number of quick links in this group, including links that are not enabled.
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            private set
            {
                _totalCount = value;
                NotifyPropertyChanged(nameof(TotalCount));
            }
        }
        /// <summary>
        /// The number of enabled quick links in this group.
        /// </summary>
        public int Count
        {
            get => _count;
            private set
            {
                _count = value;
                NotifyPropertyChanged(nameof(TotalCount));
            }
        }

        IEnumerator<QuickLinkEntry> IEnumerable<QuickLinkEntry>.GetEnumerator() => _entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _entries.GetEnumerator();

        internal void Add(QuickLinkEntry entry)
        {
            if (!_entries.Contains(entry))
            {
                _entries.Add(entry);
                if (entry.IsEnabled)
                {
                    Count++;
                }
                TotalCount++;
            }
            entry.Group = this;
            ReorganizeEntries();
        }
        internal void Remove(QuickLinkEntry entry)
        {
            if (_entries.Remove(entry))
            {
                if (entry.IsEnabled)
                {
                    Count--;
                }
                TotalCount--;
            }
            if (entry.Group == this)
            {
                entry.Group = null;
            }
            ReorganizeEntries();
        }

        public int CompareTo(QuickLinkGroup other)
        {
            if (other is null)
            {
                return -1;
            }
            if (IsInvalidated != other.IsInvalidated)
            {
                return IsInvalidated ? -1 : 1;
            }
            return Position < other.Position ? -1
                : Position > other.Position ? 1
                : 0;
        }

        private void ReorganizeEntries()
        {
            var enabledEntries = _entries.FindAll(i => i.IsEnabled);
            enabledEntries.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Path, y.Path));
            for (int i = 0; i < enabledEntries.Count; i++)
            {
                enabledEntries[i].Position = i;
            }
            enabledEntries.ForEach(i =>
            {
                if (!i.IsEnabled)
                {
                    i.Position = -1;
                }
            });
        }
    }
    [Serializable]
    public sealed class QuickLinkEntry : QuickLinkBase, IComparable<QuickLinkEntry>
    {
        [NonSerialized]
        private QuickLinkGroup? _group;
        private string _targetPath;
        private bool _runAsAdministrator;
        private bool _isEnabled;

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        internal QuickLinkEntry(string path, string name, string targetPath, int position, bool isEnabled, bool runAsAdministrator)
            : base(path, name)
        {
            Position = position;

            _targetPath = targetPath;
            _isEnabled = isEnabled;
            _runAsAdministrator = runAsAdministrator;
        }
        public QuickLinkGroup? Group
        {
            get => _group;
            internal set
            {
                if (_group == value) return;

                if (value is null)
                {
                    _group = null;
                    Position = -1;
                }
                else
                {
                    _group = value;
                    // the group will update the position of this instance
                }
                NotifyPropertyChanged(nameof(Group));
            }
        }

        /// <summary>
        /// The path to the file that the quick link shortcut executes.
        /// </summary>
        public string TargetPath
        {
            get => _targetPath;
            private set
            {
                _targetPath = value ?? throw new ArgumentNullException(nameof(TargetPath));
                NotifyPropertyChanged(nameof(TargetPath));
            }
        }
        /// <summary>
        /// Indicates if the shortcut is to be executed as an administrator.
        /// </summary>
        public bool RunAsAdministrator
        {
            get => _runAsAdministrator;
            private set
            {
                _runAsAdministrator = value;
                NotifyPropertyChanged(nameof(RunAsAdministrator));
            }
        }
        /// <summary>
        /// Indicates if the shortcut is enabled (visible in the Win+X Quick Links menu).
        /// </summary>
        /// <value></value>
        public bool IsEnabled
        {
            get => _isEnabled;
            private set
            {
                _isEnabled = value;
                NotifyPropertyChanged(nameof(IsEnabled));
            }
        }

        public int CompareTo(QuickLinkEntry other)
        {
            if (other is null)
            {
                return -1;
            }
            if (IsInvalidated != other.IsInvalidated)
            {
                return IsInvalidated ? -1 : 1;
            }
            return Position < other.Position ? -1
                : Position > other.Position ? 1
                : 0;
        }
    }
}

namespace PSSharp.Commands
{
    #region WinXQuickLink
    public abstract class WinXQuickLinkCommandBase : PSCmdlet
    {
        public const string Noun = "WinXQuickLink";

        internal WinXQuickLinkCommandBase()
        {

        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            QuickLinkBase.EnsureQuickLinksLoaded(this);
        }
    }
    [Cmdlet(VerbsCommon.Get, Noun)]
    public sealed class NewWinXQuickLinkCommand : WinXQuickLinkCommandBase
    {

    }
    [Cmdlet(VerbsCommon.Get, Noun)]
    public sealed class GetWinXQuickLinkCommand : WinXQuickLinkCommandBase
    {

    }

    [Cmdlet(VerbsCommon.Rename, Noun)]
    public sealed class RenameWinXQuickLinkCommand : WinXQuickLinkCommandBase
    {

    }
    [Cmdlet(VerbsCommon.Set, Noun)]
    public sealed class SetWinXQuickLinkCommand : WinXQuickLinkCommandBase
    {

    }
    [Cmdlet(VerbsCommon.Move, Noun)]
    public sealed class MoveWinXQuickLinkCommand : WinXQuickLinkCommandBase
    {

    }
    [Cmdlet(VerbsCommon.Remove, Noun)]
    public sealed class RemoveWinXQuickLinkCommand : WinXQuickLinkCommandBase
    {

    }
    #endregion WinXQuickLink
    #region  WinXQuickLinkGroup
    public abstract class WinXQuickLinkGroupCommandBase : WinXQuickLinkCommandBase
    {
        new public const string Noun = "WinXQuickLinkGroup";
        internal WinXQuickLinkGroupCommandBase()
        {

        }
    }
    [Cmdlet(VerbsCommon.Get, Noun)]
    public sealed class GetWinXQuickLinkGroupCommand : WinXQuickLinkGroupCommandBase
    {

    }
    [Cmdlet(VerbsCommon.Rename, Noun)]
    public sealed class RenameWinXQuickLinkGroupCommand : WinXQuickLinkGroupCommandBase
    {
    }
    #endregion WinXQuickLinkGroup
    #region WinXQuickLinkBackup
    public abstract class WinXQuickLinkBackupCommandBase : WinXQuickLinkCommandBase
    {
        public const string BackupNoun = "WinXQuickLinkBackup";
        internal WinXQuickLinkBackupCommandBase()
        {

        }
    }
    [Cmdlet(VerbsData.Backup, Noun)]
    public sealed class BackupWinXQuickLinkCommand : WinXQuickLinkBackupCommandBase
    {

    }
    [Cmdlet(VerbsData.Restore, Noun)]
    public sealed class RestoreWinXQuickLinkCommand : WinXQuickLinkBackupCommandBase
    {

    }
    [Cmdlet(VerbsCommon.Get, BackupNoun)]
    public sealed class GetWinXQuickLinkBackupCommand : WinXQuickLinkBackupCommandBase
    {

    }
    #endregion WinXQuickLinkBackup
}

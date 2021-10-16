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
using System.Collections.Concurrent;
using System.Management.Automation.Language;

namespace PSSharp.WinXQuickLink
{
    /// <summary>
    /// Arguments when a <see cref="QuickLinkGroup"/> collection is modified
    /// (a <see cref="QuickLinkEntry"/> was added or removed).
    /// </summary>
    public sealed class QuickLinkCollectionModifiedEventArgs : EventArgs
    {
        public QuickLinkCollectionModifiedEventArgs(QuickLinkEntry entry, bool removed)
        {
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
            Removed = removed;
        }

        /// <summary>
        /// The entry that was added to or removed from the group.
        /// </summary>
        /// <value></value>
        public QuickLinkEntry Entry { get; }
        /// <summary>
        /// <see langword="false"/> indicates that the entry was added to the group.
        /// <see cref="true"/> indicates that the entry was removed from the group.
        /// </summary>
        public bool Removed { get; set; }
    }
    /// <summary>
    /// Base class for <see cref="QuickLinkGroup"/> and <see cref="QuickLinkEntry"/>.
    /// </summary>
    [Serializable]
    public abstract class QuickLinkBase : INotifyPropertyChanged, IComparable<QuickLinkBase>
    {
        #region static members
        static QuickLinkBase()
        {
            Errors = new ConcurrentQueue<ErrorRecord>();
            Groups = new List<QuickLinkGroup>();
            Watcher = new FileSystemWatcher()
            {
                IncludeSubdirectories = true,
                Path = QuickLinkGroupDirectory,
            };


            lock (_constructingLock)
            {
                Watcher.Error += (sender, e) => EnsureQuickLinksLoaded();
                Watcher.Renamed += OnFileWatcherEvent;
                Watcher.Deleted += OnFileWatcherEvent;
                Watcher.Created += OnFileWatcherEvent;
                Watcher.Changed += OnFileWatcherEvent;

                EnsureQuickLinksLoaded();
            }
        }
        internal static ConcurrentQueue<ErrorRecord> Errors;
        internal static void EnsureQuickLinksLoaded()
        {
            lock (_constructingLock)
            {
                Groups ??= new List<QuickLinkGroup>();
                Groups.ForEach(i => i.IsLive = false);
                Groups.Clear();

                var directoryPaths = Directory.GetDirectories(QuickLinkGroupDirectory);
                foreach (var directoryPath in directoryPaths)
                {
                    // create a QuickLinkGroup instance from the directory
                    try {
                        var group = new QuickLinkGroup(new DirectoryInfo(directoryPath))
                        {
                            _isLive = true
                        };

                        var entryPaths = Directory.GetFiles(directoryPath);
                        foreach (var entryPath in entryPaths)
                        {
                            // create a QuickLinkEntry from the file
                            try
                            {
                                var file = new FileInfo(entryPath);
                                var shortcut = WindowsShortcut.Load(entryPath);
                                var entry = new QuickLinkEntry(file)
                                {
                                    _isLive = true
                                };

                                group.Add(entry);
                            }
                            catch (Exception e)
                            {
                                Errors.Enqueue(new ErrorRecord(e, "WinXQuickLinkEntry", ErrorCategory.NotSpecified, entryPath));
                            }
                        }
                        Groups.Add(group);
                    }
                    catch (Exception e)
                    {
                        Errors.Enqueue(new ErrorRecord(e, "WinXQuickLinkGroup", ErrorCategory.NotSpecified, directoryPath));
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
        protected static List<QuickLinkGroup> Groups;
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
        private bool _isLive;

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
        /// The file system name of the item.
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
        public bool IsLive
        {
            get => _isLive;
            private set
            {
                if (!_isLive || value) return;
                _isLive = true;
                if (this is QuickLinkGroup group)
                {
                    Groups.Remove(group);
                }
                else if (this is QuickLinkEntry entry)
                {
                    entry.Group?.Remove(entry);
                }
                Invalidated?.Invoke(this, EventArgs.Empty);
                NotifyPropertyChanged(nameof(IsLive));
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
                this.IsLive = false;
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
            if (IsLive != other.IsLive)
            {
                return IsLive ? -1 : 1;
            }
            return Position < other.Position ? -1
                : Position > other.Position ? 1
                : 0;
        }
        #endregion
    }

    /// <summary>
    /// A group of <see cref="QuickLinkEntry"/> objects which appear in the Win+X Quick Link menu.
    /// </summary>
    [Serializable]
    public sealed class QuickLinkGroup : QuickLinkBase, IReadOnlyList<QuickLinkEntry>, IComparable<QuickLinkGroup>
    {
        public static List<QuickLinkGroup> GetGroups()
            => QuickLinkBase.Groups.ToList();

        internal QuickLinkGroup(DirectoryInfo directory)
            : base(directory.FullName, directory.Name)
        {

        }
        internal QuickLinkGroup(string path, IEnumerable<QuickLinkEntry> entries)
            : base(path, System.IO.Path.GetDirectoryName(path))
        {
            _entries.AddRange(entries);
            _totalCount = _entries.Count;
            _count = _entries.Count(i => i.IsEnabled);
        }

        public event EventHandler<QuickLinkCollectionModifiedEventArgs>? CollectionModified;


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
            if (entry is null) throw new ArgumentNullException(nameof(entry));

            if (!_entries.Contains(entry))
            {
                _entries.Add(entry);
                if (entry.IsEnabled)
                {
                    Count++;
                }
                TotalCount++;
                ReorganizeEntries();
                entry.Group = this;
                CollectionModified?.Invoke(this, new QuickLinkCollectionModifiedEventArgs(entry, false));
            }
            else
            {
                entry.Group = this;
            }
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
                ReorganizeEntries();
                CollectionModified?.Invoke(this, new QuickLinkCollectionModifiedEventArgs(entry, true));
            }
            if (entry.Group == this)
            {
                entry.Group = null;
            }
        }

        public int CompareTo(QuickLinkGroup other)
        {
            if (other is null)
            {
                return -1;
            }
            if (IsLive != other.IsLive)
            {
                return IsLive ? -1 : 1;
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
    /// <summary>
    /// A Win+X Quick Link that may be used in the Win+X Quick Link menu.
    /// </summary>
    [Serializable]
    public sealed class QuickLinkEntry : QuickLinkBase, IComparable<QuickLinkEntry>
    {
        public static List<QuickLinkEntry> GetEntries()
            => QuickLinkBase.Groups.SelectMany(e => e).ToList();

        [NonSerialized]
        private QuickLinkGroup? _group;
        private string _targetPath;
        private string _displayName;
        private bool _runAsAdministrator;
        private bool _isEnabled;

        internal QuickLinkEntry(FileInfo file)
            : base(file.FullName, file.Name)
        {
            var shortcut = WindowsShortcut.Load(file.FullName);
            _targetPath = shortcut.Path!;
            if (!string.IsNullOrWhiteSpace(shortcut.Description))
            {
                _displayName = shortcut.Description!;
            }
            else
            {
                _displayName = file.Name.Split('-').Last();
            }

            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read)){
                fs.Position = 0x14;
                var runAsAdminByte = new byte[1];
                fs.Read(runAsAdminByte, 0, 1);
                _runAsAdministrator = (runAsAdminByte[0] | 0x20) == runAsAdminByte[0];
            }
            #warning identify if the quick link is enabled
        }
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

        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value ?? throw new ArgumentNullException(nameof(DisplayName));
                NotifyPropertyChanged(nameof(DisplayName));
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
            if (IsLive != other.IsLive)
            {
                return IsLive ? -1 : 1;
            }
            return Position < other.Position ? -1
                : Position > other.Position ? 1
                : 0;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class WinXQuickLinkCompleterBase : ArgumentCompleterAttribute, IArgumentCompleter
    {
        /// <summary>
        /// Provide the type of the derived instance.
        /// </summary>
        /// <param name="type"></param>
        internal WinXQuickLinkCompleterBase(Type type)
            : base(type)
        {
        }

        private static string GetQuotation(StringConstantType? stringType) => stringType switch 
        {
            StringConstantType.BareWord => "",
            StringConstantType.DoubleQuoted => "\"",
            _ => "'"
        };
        protected static CompletionResult GetCompletionResult(
            string completionText,
            string? listItemText = null,
            string? toolTip = null,
            CompletionResultType resultType = CompletionResultType.ParameterValue,
            StringConstantType? suggestedQuotations = null,
            StringConstantType providedQuotations = StringConstantType.BareWord)
        {
            if (listItemText is null && toolTip != null)
            {
                listItemText = toolTip;
            }
            else if (toolTip is null && listItemText != null)
            {
                toolTip = listItemText;
            }
            else if (toolTip is null && listItemText is null)
            {
                toolTip = listItemText = completionText;
            }
            if (completionText.Contains(" "))
            {
                var quote = suggestedQuotations.HasValue ? GetQuotation(suggestedQuotations) : GetQuotation(providedQuotations);
                var completionContent = quote == "'"
                    ? CodeGeneration.EscapeSingleQuotedStringContent(completionText)
                    : completionText.Replace("\"", "`\"");
                completionText = $"{quote}{completionContent}{quote}";
            }
            return new CompletionResult(
                completionText,
                listItemText,
                resultType,
                toolTip);
        }

        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            StringConstantType completionQuotes = StringConstantType.BareWord;
            if (wordToComplete.StartsWith("'")) completionQuotes = StringConstantType.SingleQuoted;
            else if (wordToComplete.StartsWith("\"")) completionQuotes = StringConstantType.DoubleQuoted;

            var wc = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);
            foreach (var group in QuickLinkGroup.GetGroups())
            {
                if (CanTestGroups)
                {
                    var groupCompletion = GetCompletionForQuickLinkData(group, wc, fakeBoundParameters, completionQuotes);
                    if (groupCompletion != null)
                    {
                        yield return groupCompletion;
                    }
                }
                
                if (CanTestEntries)
                {
                    foreach (var entry in group)
                    {
                        var entryCompletion = GetCompletionForQuickLinkData(entry, wc, fakeBoundParameters, completionQuotes);
                        if (entryCompletion != null)
                        {
                            yield return entryCompletion;
                        }
                    }
                }
            }
        }

        protected abstract CompletionResult? GetCompletionForQuickLinkData(QuickLinkBase data, WildcardPattern wildcard, IDictionary fakeBoundParameters, StringConstantType expandStringType);
        protected virtual bool CanTestGroups => true;
        protected virtual bool CanTestEntries => true;
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
            // QuickLinkBase.EnsureQuickLinksLoaded();
        }
    }
    [Cmdlet(VerbsCommon.New, Noun)]
    public sealed class NewWinXQuickLinkCommand : WinXQuickLinkCommandBase
    {
    }
    [Cmdlet(VerbsCommon.Get, Noun)]
    public sealed class GetWinXQuickLinkCommand : WinXQuickLinkCommandBase
    {
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            while (QuickLinkBase.Errors.TryDequeue(out var error))
            {
                WriteError(error);
            }
        }
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
    [Cmdlet(VerbsLifecycle.Disable, Noun)]
    public sealed class DisableWinXQuickLinkCommand : WinXQuickLinkCommandBase
    {

    }
    [Cmdlet(VerbsLifecycle.Enable, Noun)]
    public sealed class EnableWinXQuickLinkCommand : WinXQuickLinkCommandBase
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

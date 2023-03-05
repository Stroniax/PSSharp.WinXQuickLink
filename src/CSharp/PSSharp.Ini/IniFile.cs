using NeoSmart.AsyncLock;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PSSharp.Ini
{
    public sealed partial class IniFile : IDisposable, ISerializable
#if NET5_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="IniFile"/> class.
        /// </summary>
        /// <param name="filePath"></param>
        private IniFile(string filePath, IniDictionary values, IniFileSynchronizationState synchronizationState)
        {
            _lock = new AsyncLock();
            _filePath = filePath;
            Values = values.FilePath is null ? values : values.Clone();
            Values.ConnectToIniFile(this);
            _synchronizationState = synchronizationState;
        }

        #region private fields
        private readonly string _filePath;
        private readonly AsyncLock _lock;
        private int _pendingFileChangeCount;
        private bool _isDisposed;
        private FileSystemWatcher? _watcher;
        private FileStream? _fileStream;
        private IniFileSynchronizationState _synchronizationState;
        #endregion

        #region public properties
        /// <summary>
        /// The path to the ini file that is referenced, which this <see cref="IniFile"/> instance may
        /// be synchronized to.
        /// </summary>
        public string FilePath => _filePath;
        /// <summary>
        /// The number of changes made to the underlying file that have not yet been picked up
        /// by the current instance due to a pending lock on <see cref="SyncRoot"/>.
        /// </summary>
        public int PendingFileChangeCount => Interlocked.CompareExchange(ref _pendingFileChangeCount, 0, 0);
        /// <summary>
        /// The state of synchronization between the <see cref="IniFile"/> instance and the underlying file
        /// represented by the instance.
        /// </summary>
        public IniFileSynchronizationState SynchronizationState => _synchronizationState;
        /// <summary>
        /// The values of the file.
        /// </summary>
        public IniDictionary Values { get; }
        #endregion

        #region public methods
        public void SetSynchronizationState(IniFileSynchronizationState state)
        {
            using (EnterLock(true))
            {
                // we do not want to pass the cancellationToken to inner method calls
                // 1) we already own the lock
                // 2) cancelling mid-action may leave us in a broken state

                if (_synchronizationState == state) return;
                var previousState = _synchronizationState;
                _synchronizationState = state;

                void ClearFileStream()
                {
                    if (_fileStream != null && previousState == IniFileSynchronizationState.OwnedPrivate)
                    {
                        // for performance reasons, we don't actually write the file we just lock it until we
                        // no longer own the file if we're the private owner of it. So at ths point we must
                        // serialize our dictionary
                        _fileStream.Position = 0;
                        IniDictionary.Serialize(Values, _fileStream);
                    }
                    _fileStream?.Dispose();
                    _fileStream = null;
                }
                void ClearFileWatcher()
                {
                    _watcher?.Dispose();
                    _watcher = null;
                }

                if (state == IniFileSynchronizationState.Disconnected)
                {
                    ClearFileStream();
                    ClearFileWatcher();
                }
                else if (state == IniFileSynchronizationState.MonitorRead)
                {
                    ClearFileStream();
                    GetFileWatcher();
                }
                else if (state == IniFileSynchronizationState.MonitorReadWrite)
                {
                    ClearFileStream();
                    GetFileWatcher();
                }
                else if (state == IniFileSynchronizationState.WriteOnly)
                {
                    ClearFileStream();
                    ClearFileWatcher();
                }
                else if (state == IniFileSynchronizationState.OwnedPrivate)
                {
                    ClearFileWatcher();
                    ClearFileStream();
                    GetFileStream();
                }
                else if (state == IniFileSynchronizationState.OwnedPublic)
                {
                    ClearFileWatcher();
                    ClearFileStream();
                    GetFileStream();
                }
            }
        }
#if NET5_0_OR_GREATER
        public async ValueTask SetSynchronizationStateAsync(IniFileSynchronizationState state, CancellationToken cancellationToken = default)
#else
        public async Task SetSynchronizationStateAsync(IniFileSynchronizationState state, CancellationToken cancellationToken = default)
#endif
        {
            using (await EnterLockAsync(true, cancellationToken))
            {
                // we do not want to pass the cancellationToken to inner method calls
                // 1) we already own the lock
                // 2) cancelling mid-action may leave us in a broken state

                if (_synchronizationState == state) return;
                var previousState = _synchronizationState;
                _synchronizationState = state;

#if NET5_0_OR_GREATER
                async ValueTask ClearFileStream()
                {
                    if (_fileStream is not null)
                    {
                        if (_fileStream != null && previousState == IniFileSynchronizationState.OwnedPrivate)
                        {
                            // for performance reasons, we don't actually write the file we just lock it until we
                            // no longer own the file if we're the private owner of it. So at ths point we must
                            // serialize our dictionary
                            _fileStream.Position = 0;
                            await IniDictionary.SerializeAsync(Values, _fileStream);
                        }
                        var fs = _fileStream;
                        _fileStream = null;
                        await fs.DisposeAsync();
                    }
                }
#else
                async Task ClearFileStream()
                {
                    if (_fileStream != null && previousState == IniFileSynchronizationState.OwnedPrivate)
                    {
                        // for performance reasons, we don't actually write the file we just lock it until we
                        // no longer own the file if we're the private owner of it. So at ths point we must
                        // serialize our dictionary
                        _fileStream.Position = 0;
                        await IniDictionary.SerializeAsync(Values, _fileStream);
                    }
                    _fileStream?.Dispose();
                    _fileStream = null;
                }
#endif
                void ClearFileWatcher()
                {
                    _watcher?.Dispose();
                    _watcher = null;
                }

                if (state == IniFileSynchronizationState.Disconnected)
                {
                    await ClearFileStream();
                    ClearFileWatcher();
                }
                else if (state == IniFileSynchronizationState.MonitorRead)
                {
                    await ClearFileStream();
                    _ = GetFileWatcher();
                }
                else if (state == IniFileSynchronizationState.MonitorReadWrite)
                {
                    await ClearFileStream();
                    _ = GetFileWatcherAsync(default);
                }
                else if (state == IniFileSynchronizationState.WriteOnly)
                {
                    await ClearFileStream();
                    ClearFileWatcher();
                }
                else if (state == IniFileSynchronizationState.OwnedPrivate)
                {
                    ClearFileWatcher();
                    await ClearFileStream();
                    await GetFileStreamAsync(default);
                }
                else if (state == IniFileSynchronizationState.OwnedPublic)
                {
                    ClearFileWatcher();
                    await ClearFileStream();
                    await GetFileStreamAsync(default);
                }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                using (EnterLock(false))
                {
                    if (!_isDisposed)
                    {
                        _isDisposed = true;
                        _watcher?.Dispose();
                        _fileStream?.Dispose();
                        _watcher = null;
                        _fileStream = null;
                    }
                }
            }
        }
#if NET5_0_OR_GREATER
        ValueTask IAsyncDisposable.DisposeAsync() => DisposeAsync();
        public async ValueTask DisposeAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed) return;
            using (await EnterLockAsync(false, cancellationToken))
            {
                if (_isDisposed) return;
                await (_fileStream?.DisposeAsync() ?? ValueTask.CompletedTask);
                _watcher?.Dispose();
            }
        }
#endif
        public void Reset()
        {
            using (EnterLock(true))
            {

            }
        }
        public void Load()
        {
            using (EnterLock(true))
            {
                IniDictionary copy;
                if (_fileStream == null)
                {
                    using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    copy = IniDictionary.Deserialize(fs);
                }
                else
                {
                    _fileStream.Position = 0;
                    copy = IniDictionary.Deserialize(_fileStream);
                }
                foreach (var section in copy.GetSections())
                {
                    foreach (var key in section.GetKeys())
                    {
                        if (Values[section.Name][key] != copy[section.Name][key])
                        {
                            // TODO: I think I need to ignore NotifyValueChanged when I set this value
                            Values[section.Name][key] = copy[section.Name][key];
                        }
                    }
                }
            }
        }
        public void Save()
        {
            using (EnterLock(true))
            {
                if (_fileStream != null)
                {
                    _fileStream.Position = 0;
                    IniDictionary.Serialize(Values, _fileStream);
                }
                else
                {
                    using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    IniDictionary.Serialize(Values, fs);
                }
            }
        }
        /// <summary>
        /// Obtains a lock on the <see cref="IniFile"/> instance. Dispose of the output to release the lock.
        /// </summary>
        /// <param name="cancellationToken">Used to cancel waiting to obtain the lock.</param>
        /// <returns>An object that represents the lock. Dispose of this object to release the lock.</returns>
        public IDisposable ObtainLock(CancellationToken cancellationToken = default)
            => EnterLock(true, cancellationToken);
        /// <summary>
        /// Obtains a lock on the <see cref="IniFile"/> instance. Dispose of the output to release the lock.
        /// </summary>
        /// <param name="cancellationToken">Used to cancel waiting to obtain the lock.</param>
        /// <returns>An object that represents the lock. Dispose of this object to release the lock.</returns>
        public Task<IDisposable> ObtainLockAsync(CancellationToken cancellationToken = default)
            => EnterLockAsync(true, cancellationToken);
        #endregion

        #region internal methods
        /// <summary>
        /// Called by the <see cref="IniDictionary"/> to notify the current <see cref="IniFile"/> that
        /// a change was made to the dictionary. This occurs before the <see cref="IniDictionary.ValueChanged"/>
        /// event is invoked.
        /// </summary>
        /// <param name="change"></param>
        internal void NotifyValueChanged(IniDictionaryValueChangedEventArgs change)
        {
            // we need to use the synchronous lock so that the dictionary can't update again until we're done
            // dealing with this change

            using (EnterLock(false))
            {
                if (_isDisposed) return;
                switch (_synchronizationState)
                {
                    case IniFileSynchronizationState.OwnedPublic:
                    case IniFileSynchronizationState.WriteOnly:
                    case IniFileSynchronizationState.MonitorReadWrite:
                        {
                            // using the Windows API is ideal as I don't need to rewrite the entire file
                            IniFile.SetValue(_filePath, change.Section, change.Key, change.Value);
                        }
                        break;
                    default:
                        return;
                }
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("file", _filePath);
            info.AddValue("values", Values.ToDictionary());
            throw new NotImplementedException();
        }
        #endregion

        #region private methods
        /// <summary>
        /// Safely gets or creates a file stream.
        /// </summary>
        private FileStream GetFileStream(CancellationToken cancellationToken = default)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(IniFile));
            if (_fileStream != null) return _fileStream;
            using (EnterLock(true, cancellationToken))
            {
                if (_fileStream is null)
                {
                    var fileShare = SynchronizationState == IniFileSynchronizationState.OwnedPrivate
                        ? FileShare.None
                        : SynchronizationState == IniFileSynchronizationState.OwnedPublic
                        ? FileShare.Read
                        : FileShare.ReadWrite | FileShare.Delete;
                    _fileStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, fileShare);
                }
                return _fileStream;
            }
        }
#if NET5_0_OR_GREATER
        private async ValueTask<FileStream> GetFileStreamAsync(CancellationToken cancellationToken)
#else
        private async Task<FileStream> GetFileStreamAsync(CancellationToken cancellationToken)
#endif
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(IniFile));
            if (_fileStream != null) return _fileStream;
            using (await EnterLockAsync(true, cancellationToken))
            {
                if (_fileStream is null)
                {
                    var fileShare = SynchronizationState == IniFileSynchronizationState.OwnedPrivate
                        ? FileShare.None
                        : SynchronizationState == IniFileSynchronizationState.OwnedPublic
                        ? FileShare.Read
                        : FileShare.ReadWrite | FileShare.Delete;
                    _fileStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, fileShare);
                }
                return _fileStream;
            }
        }
        /// <summary>
        /// Safely gets or creates a file watcher.
        /// </summary>
        private FileSystemWatcher GetFileWatcher(CancellationToken cancellationToken = default)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(IniFile));
            if (_watcher != null) return _watcher;
            using (EnterLock(true, cancellationToken))
            {
                if (_watcher is null)
                {
                    var directoryName = Path.GetDirectoryName(_filePath);
                    var fileName = Path.GetFileName(_filePath);
                    _watcher = new FileSystemWatcher(directoryName, fileName);
                    _watcher.Changed += OnFileChanged;
                    _watcher.Renamed += OnFileRenamed;
                    _watcher.Deleted += OnFileDeleted;
                }
                return _watcher;
            }
        }
#if NET5_0_OR_GREATER
        private async ValueTask<FileSystemWatcher> GetFileWatcherAsync(CancellationToken cancellationToken)
#else
        private async Task<FileSystemWatcher> GetFileWatcherAsync(CancellationToken cancellationToken)
#endif
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(IniFile));
            if (_watcher != null) return _watcher;
            using (await EnterLockAsync(true, cancellationToken))
            {
                if (_watcher is null)
                {
                    _watcher = new FileSystemWatcher(_filePath);
                    _watcher.Changed += OnFileChanged;
                    _watcher.Renamed += OnFileRenamed;
                    _watcher.Deleted += OnFileDeleted;
                }
                return _watcher;
            }
        }

        private async void OnFileChanged(object? sender, FileSystemEventArgs e)
        {
            if (_isDisposed) return;
            if (SynchronizationState == IniFileSynchronizationState.Disconnected) return; //we are ignoring file changes
            if (SynchronizationState == IniFileSynchronizationState.OwnedPrivate) return; // we are responsible for the file change
            if (SynchronizationState == IniFileSynchronizationState.OwnedPublic) return; // we are responsible for the file change
            if (SynchronizationState == IniFileSynchronizationState.WriteOnly) return; // we are ignoring file changes
            Interlocked.Increment(ref _pendingFileChangeCount);

            using (await EnterLockAsync(false))
            {
                if (_isDisposed) return;
                if (SynchronizationState == IniFileSynchronizationState.MonitorRead
                    || SynchronizationState == IniFileSynchronizationState.MonitorReadWrite)
                {
                    // re-read the .ini file
                    IniDictionary currentFileData;
                    using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        currentFileData = await IniDictionary.DeserializeAsync(fs);

                        // at this point we have received all pending changes
                        Interlocked.Exchange(ref _pendingFileChangeCount, 0);
                    }

                    // take existing file data and incorporate new or modified values into the current instance
                    foreach (var section in currentFileData.GetSections())
                    {
                        foreach (var key in section.GetKeys())
                        {
                            if (Values[section.Name][key] != section[key])
                            {
                                // TODO: I think I need to ignore NotifyValueChanged when I set this value
                                Values[section.Name][key] = section[key];
                            }
                        }
                    }
                }
            }
        }

        private async void OnFileRenamed(object? sender, RenamedEventArgs e)
        {
            if (_isDisposed) return;
            Interlocked.Increment(ref _pendingFileChangeCount);
            try
            {
                using (await EnterLockAsync(false))
                {
                    if (_isDisposed) return;
                    _synchronizationState = IniFileSynchronizationState.Disconnected;
                }
            }
            finally
            {
                Interlocked.Decrement(ref _pendingFileChangeCount);
            }
        }
        private async void OnFileDeleted(object? sender, FileSystemEventArgs e)
        {
            if (_isDisposed) return;
            Interlocked.Increment(ref _pendingFileChangeCount);
            try
            {
                using (await EnterLockAsync(false))
                {
                    if (_isDisposed) return;
                    _synchronizationState = IniFileSynchronizationState.Disconnected;
                }
            }
            finally
            {
                Interlocked.Decrement(ref _pendingFileChangeCount);
            }
        }
        private IDisposable EnterLock(bool throwIfDisposed, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                return !throwIfDisposed ? default(PseudoDisposable) : throw new ObjectDisposedException(nameof(IniFile));
            }
            var lockContext = _lock.Lock();
            if (_isDisposed)
            {
                lockContext.Dispose();
                return !throwIfDisposed ? default(PseudoDisposable) : throw new ObjectDisposedException(nameof(IniFile));
            }
            return lockContext;
        }
        private async Task<IDisposable> EnterLockAsync(bool throwIfDisposed, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                return !throwIfDisposed ? default(PseudoDisposable) : throw new ObjectDisposedException(nameof(IniFile));
            }
            var lockLevel = await _lock.LockAsync(cancellationToken).ConfigureAwait(false);
            if (_isDisposed)
            {
                lockLevel.Dispose();
                return !throwIfDisposed ? default(PseudoDisposable) : throw new ObjectDisposedException(nameof(IniFile));
            }
            return lockLevel;
        }

        private readonly struct PseudoDisposable : IDisposable
        {
            public void Dispose() { }
        }
        #endregion
    }
}
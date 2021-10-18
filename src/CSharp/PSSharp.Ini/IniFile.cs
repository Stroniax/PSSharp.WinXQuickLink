using System;
using System.IO;
using System.Threading;

namespace PSSharp.Ini
{
    public sealed partial class IniFile : IDisposable
    {
        public static IniFile Open(string filePath)
        {
            var file = new IniFile(filePath);
            file.Load();
            return file;
        }
        public static IniFile CreateNew(string filePath)
        {
            var file = new IniFile(filePath);
            return file;
        }
        private IniFile(string filePath)
        {
            _syncRoot = new object();
            _filePath = filePath;
        }

        private readonly string _filePath;
        private readonly object _syncRoot;
        private int _pendingFileChangeCount;
        private bool _isDisposed;
        private FileSystemWatcher? _watcher;
        private FileStream? _fileStream;

        public string FilePath => _filePath;
        public int PendingFileChangeCount => Interlocked.CompareExchange(ref _pendingFileChangeCount, 0, 0);

        public void Dispose()
        {
            if (!_isDisposed)
            {
                lock (_syncRoot)
                {
                    if (!_isDisposed)
                    {
                        _watcher?.Dispose();
                        _fileStream?.Dispose();
                        _isDisposed = true;
                    }
                }
            }
        }
        public void Reset()
        {
            throw new NotImplementedException();
        }
        public void Load()
        {
            throw new NotImplementedException();
        }
        public void Save()
        {
            throw new NotImplementedException();
        }

        #region internal methods
        internal void NotifyValueChanged(IniDictionaryValueChangedEventArgs change)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region private methods
        private FileStream GetFileStream()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(IniFile));
            if (_fileStream is null)
            {
                lock (_syncRoot)
                {
                    if (_isDisposed) throw new ObjectDisposedException(nameof(IniFile));
                    if (_fileStream is null)
                    {
                        _fileStream = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                    }
                }
            }
            return _fileStream;
        }
        private FileSystemWatcher GetFileWatcher()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(IniFile));
            if (_watcher is null)
            {
                lock (_syncRoot)
                {
                    if (_isDisposed) throw new ObjectDisposedException(nameof(IniFile));
                    if (_watcher is null)
                    {
                        _watcher = new FileSystemWatcher(_filePath);
                        _watcher.Changed += OnFileChanged;
                        _watcher.Renamed += OnFileRenamed;
                        _watcher.Deleted += OnFileDeleted;
                    }
                }
            }
            return _watcher;
        }

        private void OnFileChanged(object? sender, FileSystemEventArgs e)
        {
            Interlocked.Increment(ref _pendingFileChangeCount);
            try {
                lock(_syncRoot)
                {

                }
            }
            finally
            {
                Interlocked.Decrement(ref _pendingFileChangeCount);
            }
            throw new NotImplementedException();
        }

        private void OnFileRenamed(object? sender, RenamedEventArgs e)
        {
            Interlocked.Increment(ref _pendingFileChangeCount);
            try {
                lock(_syncRoot)
                {

                }
            }
            finally
            {
                Interlocked.Decrement(ref _pendingFileChangeCount);
            }
            throw new NotImplementedException();
        }

        private void OnFileDeleted(object? sender, FileSystemEventArgs e)
        {
            Interlocked.Increment(ref _pendingFileChangeCount);
            try {
                lock(_syncRoot)
                {

                }
            }
            finally
            {
                Interlocked.Decrement(ref _pendingFileChangeCount);
            }
            throw new NotImplementedException();
        }
        #endregion
    }

}
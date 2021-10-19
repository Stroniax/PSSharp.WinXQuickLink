using System;
using System.Collections;
using System.Collections.Generic;

namespace PSSharp.Ini
{
    public sealed class IniSection : IDictionary, IDictionary<string, string?>
    {
        // The IniSection doesn't need to actually exist as a member of the IniFile
        // until a key is created, so we'll track the _file and add ourself to it when
        // and if that happens. This way, an IniSection can always be retrieved from
        // an IniFile so that we can add whatever key we desire without having to null-check
        // the section.
        private readonly IniDictionary _parent;
        private readonly Dictionary<string, string?> _keys;
        public string Name { get; }
        public int KeyCount => _keys.Count;

        public IniSection(string name)
        {
            Name = name;
            _parent = null!;
            _keys = new Dictionary<string, string?>();
        }
        internal IniSection(string name, IniDictionary parent)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _keys = new Dictionary<string, string?>();
        }

        public string? this[string index]
        {
            get => GetValue(index ?? throw new ArgumentNullException(nameof(index)));
            set => SetValue(index ?? throw new ArgumentNullException(nameof(index)), value);
        }

        public string[] GetKeys()
        {
            var keyNames = new string[_keys.Count];
            _keys.Keys.CopyTo(keyNames, 0);
            return keyNames;
        }

        public string? GetValue(string key)
        {
            if(_keys.TryGetValue(key, out var value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }
        public string? SetValue(string key, string? value)
        {
            _keys.TryGetValue(key, out var initialValue);

            if (value is null)
            {
                _keys.Remove(key);
            }
            else
            {
                _keys[key] = value;
            }
            _parent.NotifyValueChanged(this, key, value, initialValue);
            return initialValue;
        }
        public bool ContainsKey(string key)
        {
            return _keys.ContainsKey(key);
        }
        public bool TryGetValue(string key, out string? value)
        {
            return _keys.TryGetValue(key, out value);
        }
        public void Clear()
        {
            var clone = new KeyValuePair<string, string>[_keys.Count];
            ((ICollection<KeyValuePair<string, string>>)_keys).CopyTo(clone, 0);
            _keys.Clear();
            foreach (var keyValuePair in clone)
            {
                _parent.NotifyValueChanged(this, keyValuePair.Key, null, keyValuePair.Value);
            }
        }
        public bool Remove(string key)
        {
            return _keys.Remove(key);
        }


        #region dictionary implementation
        ICollection<string> IDictionary<string, string?>.Keys => _keys.Keys;
        ICollection<string?> IDictionary<string, string?>.Values => _keys.Values;
        int ICollection<KeyValuePair<string, string?>>.Count => _keys.Count;
        bool ICollection<KeyValuePair<string, string?>>.IsReadOnly => false;

        bool IDictionary.IsFixedSize => false;

        bool IDictionary.IsReadOnly => false;

        ICollection IDictionary.Keys => _keys.Keys;

        ICollection IDictionary.Values => _keys.Values;

        int ICollection.Count => _keys.Count;

        bool ICollection.IsSynchronized => false;

        object? ICollection.SyncRoot => null;

        object? IDictionary.this[object key]
        {
            get => key is string str ? GetValue(str) : null;
            set
            {
                if (key is string str
                    && value is string val)
                {
                    SetValue(str, val);
                }
                else
                {
                    throw new InvalidOperationException("Key and value must be string.");
                }
            }
        }

        IEnumerator<KeyValuePair<string, string?>> IEnumerable<KeyValuePair<string, string?>>.GetEnumerator() => _keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _keys.GetEnumerator();
        void IDictionary<string, string?>.Add(string key, string? value)
        {
            var exists = TryGetValue(key, out var iniValue);
            if (exists) _keys.Add(key, value);
            SetValue(key, value);
        }

        void ICollection<KeyValuePair<string, string?>>.Add(KeyValuePair<string, string?> item)
        {
            var exists = TryGetValue(item.Key, out var value);
            if (exists) ((ICollection<KeyValuePair<string, string?>>)_keys).Add(item);
            SetValue(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<string, string?>>.Contains(KeyValuePair<string, string?> item)
            => ((ICollection<KeyValuePair<string, string?>>)_keys).Contains(item);

        void ICollection<KeyValuePair<string, string?>>.CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<string, string?>>)_keys).CopyTo(array, arrayIndex);

        bool ICollection<KeyValuePair<string, string?>>.Remove(KeyValuePair<string, string?> item)
        {
            var exists = TryGetValue(item.Key, out var value);
            if (!exists || value != item.Value)
            {
                return false;
            }
            Remove(item.Key);
            return true;
        }

        void IDictionary.Add(object key, object value)
        {
            if (key is string str
                && value is string val)
            {
                if (ContainsKey(str))
                {
                    throw new InvalidOperationException("The key already exists in the ini section.");
                }
                else
                {
                    SetValue(str, val);
                }
            }
            else
            {
                throw new InvalidOperationException("Key and value must be string.");
            }
        }

        bool IDictionary.Contains(object key) => key is string str && ContainsKey(str);

        IDictionaryEnumerator IDictionary.GetEnumerator() => _keys.GetEnumerator();
        void IDictionary.Remove(object key)
        {
            if (key is string str)
            {
                Remove(str);
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_keys).CopyTo(array, index);
        }
        #endregion
    }
}

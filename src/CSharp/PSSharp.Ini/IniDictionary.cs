using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;

namespace PSSharp.Ini
{
    /// <summary>
    /// Represents a data collection structured according to the .ini file specification.
    /// </summary>
    public sealed partial class IniDictionary :
        ICloneable,
        IDictionary,
        IEnumerable<IniSection>
    {
        public IniDictionary()
        {
            _sections = new Dictionary<string, IniSection>();
            _pendingSections = new Dictionary<string, WeakReference<IniSection>>();
        }

        /// <summary>
        /// Invoked when the value of a <see cref="IniSection"/> or <see cref="IniKey"/> of the <see cref="IniDictionary"/>
        /// is modified.
        /// </summary>
        public event EventHandler<IniDictionaryValueChangedEventArgs>? ValueChanged;

        #region private instance fields
        private IniFile? _file;
        private readonly Dictionary<string, IniSection> _sections;
        private readonly Dictionary<string, WeakReference<IniSection>> _pendingSections;
        #endregion

        internal void NotifyValueChanged(IniSection section, string key, string? newValue, string? previousValue)
        {
            Debug.WriteLine("Value changed. Section {0}, key {1} updated from {2} to {3}.", args: new[] { section.Name, key, previousValue, newValue });
            
            var args = new IniDictionaryValueChangedEventArgs(
                section.Name,
                key,
                newValue,
                previousValue,
                _file?.FilePath
                );
            if (newValue != null // only test if definitively non-null value
                && _pendingSections.Remove(section.Name))
            {
                Debug.WriteLine("Added section {0} to IniDictionary sections.", args: section.Name);
                _sections.Add(section.Name, section);
            }
            else if (newValue is null
                && section.KeyCount == 0
                && _sections.ContainsKey(section.Name))
            {
                Debug.WriteLine("Removed section {0} from IniDictionary sections.", args: section.Name);
                _sections.Remove(section.Name);
                _pendingSections.Add(section.Name, new WeakReference<IniSection>(section));
            }

            _file?.NotifyValueChanged(args);
            ValueChanged?.Invoke(this, args);
        }
        internal void ConnectToIniFile(IniFile file)
        {
            if (_file is null)
            {
                _file = file;
            }
            else
            {
                throw new InvalidOperationException("The IniDictionary instance is already connected to an IniFile.");
            }
        }

        public IniSection this[string index] => GetSection(index);

        public int SectionCount => _sections.Count;

        public IniSection[] GetSections()
        {
            var sections = new IniSection[_sections.Count];
            _sections.Values.CopyTo(sections, 0);
            return sections;
        }
        public string[] GetSectionNames()
        {
            var sectionNames = new string[_sections.Count];
            _sections.Keys.CopyTo(sectionNames, 0);
            return sectionNames;
        }
        public IniSection GetSection(string name)
        {
            if (_sections.TryGetValue(name, out var section))
            {
                return section;
            }
            else
            {
                var addSectionReference = !_pendingSections.TryGetValue(name, out var sectionReference);
                if (sectionReference?.TryGetTarget(out section) ?? false)
                {
                    return section;
                }
                section ??= new IniSection(name, this);
                sectionReference ??= new WeakReference<IniSection>(section);

                if (addSectionReference)
                {
                    _pendingSections[name] = sectionReference;
                }
                else
                {
                    sectionReference.SetTarget(section);
                }

                return section;
            }
        }
        public bool SectionExists(string name)
        {
            return _sections.ContainsKey(name);
        }
        public bool TryGetSection(string name, out IniSection section)
        {
            return _sections.TryGetValue(name, out section);
        }
        public bool RemoveSection(string name)
        {
            if (_sections.TryGetValue(name, out var section))
            {
                _pendingSections.Add(name, new WeakReference<IniSection>(section));
                _sections.Remove(name);
                section.Clear();
                return true;
            }
            else
            {
                return false;
            }
        }
        public void Clear()
        {
            lock(_sections)
            {
                foreach (var section in GetSections())
                {
                    section.Clear();
                    _pendingSections.Add(section.Name, new WeakReference<IniSection>(section));
                    _sections.Remove(section.Name);
                }
            }
            ValueChanged?.Invoke(this, new IniDictionaryValueChangedEventArgs(null, null, null, null, _file?.FilePath));
        }
        public IniDictionary Clone()
        {
            var other = new IniDictionary();
            foreach (var section in GetSections())
            {
                foreach (var key in section.GetKeys())
                {
                    other[section.Name][key] = section[key];
                }
            }
            return other;
        }
        object ICloneable.Clone() => Clone();

        #region dictionary implementation
        public IEnumerator<IniSection> GetEnumerator() => _sections.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _sections.Values.GetEnumerator();
        IDictionaryEnumerator IDictionary.GetEnumerator() => _sections.GetEnumerator();
        public IEnumerable<string> Keys => _sections.Keys;
        public IEnumerable<IDictionary<string, string?>> Values => (IEnumerable<IDictionary<string, string?>>)(object)_sections.Values ;
        bool IDictionary.IsFixedSize => ((IDictionary)_sections).IsFixedSize;
        bool IDictionary.IsReadOnly => ((IDictionary)_sections).IsReadOnly;
        ICollection IDictionary.Keys => ((IDictionary)_sections).Keys;
        ICollection IDictionary.Values => ((IDictionary)_sections).Values;
        void IDictionary.Add(object key, object value) => throw new NotSupportedException();
        void IDictionary.Remove(object key) => throw new NotSupportedException();
        bool IDictionary.Contains(object key) => key is string str && SectionExists(str);
        object IDictionary.this[object key]
        {
            get
            {
                if (key is string str)
                {
                    return GetSection(str);
                }
                else
                {
                    throw new IndexOutOfRangeException("The key must be a string.");
                }
            }
            set
            {
                throw new NotSupportedException();
            }
        }
        void ICollection.CopyTo(Array array, int index)
        {
            _sections.Values.CopyTo((IniSection[])array, index);
        }


        int ICollection.Count => _sections.Count;
        bool ICollection.IsSynchronized => false;
        object? ICollection.SyncRoot => null;
        #endregion
    }
}
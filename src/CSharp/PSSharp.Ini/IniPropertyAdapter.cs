using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace PSSharp.Ini
{
    /// <summary>
    /// PSPropertyAdapter for IniDictionary and IniSection.
    /// </summary>
    public sealed class IniPropertyAdapter : PSPropertyAdapter
    {
        #region Proxy Methods
        public static Type GetType(PSObject psobject)
            => psobject.BaseObject?.GetType() ?? throw new ArgumentNullException(nameof(psobject));
        public static void Clear(PSObject ini)
        {
            if (ini.BaseObject is IniDictionary dictionary)
            {
                dictionary.Clear();
            }
            else if (ini.BaseObject is IniSection section)
            {
                section.Clear();
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniDictionary)} or {typeof(IniSection)}.", nameof(ini));
            }
        }
        public static int GetHashCode(PSObject ini)
        {
            return ini.BaseObject.GetHashCode();
        }
        public static bool Equals(PSObject ini, object? other)
        {
            return ini.BaseObject.Equals(other);
        }
        #region IniDictionary
        public static IniSection[] GetSections(PSObject iniDictionary)
        {
            if (iniDictionary is null)
            {
                throw new ArgumentNullException(nameof(iniDictionary));
            }
            if (iniDictionary.BaseObject is IniDictionary dictionary)
            {
                return dictionary.GetSections();
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniDictionary)}.", nameof(iniDictionary));
            }
        }
        public static string[] GetSectionNames(PSObject iniDictionary)
        {
            if (iniDictionary is null)
            {
                throw new ArgumentNullException(nameof(iniDictionary));
            }
            if (iniDictionary.BaseObject is IniDictionary dictionary)
            {
                return dictionary.GetSectionNames();
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniDictionary)}.", nameof(iniDictionary));
            }
        }
        public static IniSection GetSection(PSObject iniDictionary, string name)
        {
            if (iniDictionary.BaseObject is IniDictionary dictionary)
            {
                return dictionary.GetSection(name);
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniDictionary)}.", nameof(iniDictionary));
            }
        }
        public static bool TryGetSection(PSObject iniDictionary, string name, out IniSection iniSection)
        {
            if (iniDictionary.BaseObject is IniDictionary dictionary)
            {
                return dictionary.TryGetSection(name, out iniSection);
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniDictionary)}.", nameof(iniDictionary));
            }
        }
        public static bool RemoveSection(PSObject iniDictionary, string name)
        {
            if (iniDictionary.BaseObject is IniDictionary dictionary)
            {
                return dictionary.RemoveSection(name);
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniDictionary)}.", nameof(iniDictionary));
            }
        }
        
        public static IEnumerator<IniSection> GetIniDictionaryEnumerator(PSObject iniDictionary)
        {
            if (iniDictionary.BaseObject is IniDictionary dictionary)
            {
                return dictionary.GetEnumerator();
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniDictionary)}.", nameof(iniDictionary));
            }
        }
        public static IniDictionary Clone(PSObject iniDictionary)
        {
            if (iniDictionary is null)
            {
                throw new ArgumentNullException(nameof(iniDictionary));
            }
            if (iniDictionary.BaseObject is IniDictionary dictionary)
            {
                return dictionary.Clone();
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniDictionary)}.", nameof(iniDictionary));
            }
        }
        public static string? Serialize(PSObject iniDictionary)
        {
            if (iniDictionary is null)
            {
                return null;
            }
            if (iniDictionary.BaseObject is IniDictionary dictionary)
            {
                return dictionary.ToString();
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniDictionary)}.", nameof(iniDictionary));
            }
        }
        public static bool SectionExists(PSObject iniDictionary, string name)
        {
            if (iniDictionary is null)
            {
                throw new ArgumentNullException(nameof(iniDictionary));
            }
            if (iniDictionary.BaseObject is IniDictionary dictionary)
            {
                return dictionary.SectionExists(name);
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniDictionary)}.", nameof(iniDictionary));
            }
        }
        #endregion
        #region IniSection
        #endregion
        public static IEnumerator<KeyValuePair<string, string?>>? GetIniSectionEnumerator(PSObject iniSection)
            => (iniSection.BaseObject as IEnumerable<KeyValuePair<string, string?>>)?.GetEnumerator();
        public static string[] GetKeys(PSObject iniSection)
        {
            if (iniSection is null)
            {
                throw new ArgumentNullException(nameof(iniSection));
            }
            else if (iniSection.BaseObject is IniSection section)
            {
                return section.GetKeys();
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniSection)}.", nameof(iniSection));
            }
        }
        public static string? GetValue(PSObject iniSection, string key)
        {
            if (iniSection is null)
            {
                throw new ArgumentNullException(nameof(iniSection));
            }
            else if (iniSection.BaseObject is IniSection section)
            {
                return section.GetValue(key);
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniSection)}.", nameof(iniSection));
            }
        }
        public static string? SetValue(PSObject iniSection, string key, string? value)
        {
            if (iniSection is null)
            {
                throw new ArgumentNullException(nameof(iniSection));
            }
            else if (iniSection.BaseObject is IniSection section)
            {
                return section.SetValue(key, value);
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniSection)}.", nameof(iniSection));
            }
        }
        public static bool ContainsKey(PSObject iniSection,string key)
        {
            if (iniSection is null)
            {
                throw new ArgumentNullException(nameof(iniSection));
            }
            else if (iniSection.BaseObject is IniSection section)
            {
                return section.ContainsKey(key);
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniSection)}.", nameof(iniSection));
            }
        }
        public static bool TryGetValue(PSObject iniSection, string key, out string? value)
        {
            if (iniSection is null)
            {
                throw new ArgumentNullException(nameof(iniSection));
            }
            else if (iniSection.BaseObject is IniSection section)
            {
                return section.TryGetValue(key, out value);
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniSection)}.", nameof(iniSection));
            }
        }
        public static bool Remove(PSObject iniSection, string key)
        {
            if (iniSection is null)
            {
                throw new ArgumentNullException(nameof(iniSection));
            }
            else if (iniSection.BaseObject is IniSection section)
            {
                return section.Remove(key);
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniSection)}.", nameof(iniSection));
            }
        }
        #endregion

        #region Property Adapter
        public override Collection<PSAdaptedProperty> GetProperties(object baseObject)
        {
            var collection = new Collection<PSAdaptedProperty>();
            foreach (var reflectionProperty in baseObject.GetType()
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                collection.Add(new PSAdaptedProperty(reflectionProperty.Name, null));
            }
            if (baseObject is IniDictionary dictionary)
            {
                var sections = dictionary.GetSections();
                foreach (var section in sections)
                {
                    collection.Add(new PSAdaptedProperty(section.Name, null));
                }
                return collection;
            }
            else if (baseObject is IniSection section)
            {
                foreach (var keyValuePair in section)
                {
                    collection.Add(new PSAdaptedProperty(((dynamic)keyValuePair).Key, null));
                }
            }
            else
            {
                throw new ArgumentException($"Cannot adapt properties for type {baseObject?.GetType().FullName ?? "(null)"}");
            }
            return collection;
        }

        public override PSAdaptedProperty GetProperty(object baseObject, string propertyName)
        {
            if (baseObject is IniDictionary dictionary)
            {
                var property = new PSAdaptedProperty(propertyName, null);
                return property;
            }
            else if (baseObject is IniSection section)
            {
                var property = new PSAdaptedProperty(propertyName, null);
                return property;
            }
            else
            {
                throw new ArgumentException($"Cannot adapt properties for type {baseObject?.GetType().FullName ?? "(null)"}");
            }
        }

        public override string GetPropertyTypeName(PSAdaptedProperty adaptedProperty)
        {
            var reflectionProperty = adaptedProperty.BaseObject
                ?.GetType()
                ?.GetProperty(adaptedProperty.Name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

            if (reflectionProperty != null)
            {
                return reflectionProperty.PropertyType.FullName;
            }
            if (adaptedProperty.BaseObject is IniDictionary)
            {
                return typeof(IniSection).FullName;
            }
            else if (adaptedProperty.BaseObject is IniSection)
            {
                return typeof(string).FullName;
            }
            else
            {
                throw new ArgumentException($"Cannot adapt properties for type {adaptedProperty.BaseObject?.GetType().FullName ?? "(null)"}");
            }
        }

        public override object? GetPropertyValue(PSAdaptedProperty adaptedProperty)
        {
            var reflectionProperty = adaptedProperty.BaseObject
                ?.GetType()
                ?.GetProperty(adaptedProperty.Name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

            if (reflectionProperty != null)
            {
                return reflectionProperty.GetValue(adaptedProperty.BaseObject);
            }
            if (adaptedProperty.BaseObject is IniDictionary dictionary)
            {
                return dictionary[adaptedProperty.Name];
            }
            else if (adaptedProperty.BaseObject is IniSection section)
            {
                return section[adaptedProperty.Name];
            }
            else
            {
                throw new ArgumentException($"Cannot adapt properties for type {adaptedProperty.BaseObject?.GetType().FullName ?? "(null)"}");
            }
        }

        public override bool IsGettable(PSAdaptedProperty adaptedProperty)
        {
            var reflectionProperty = adaptedProperty.BaseObject
                ?.GetType()
                ?.GetProperty(adaptedProperty.Name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

            if (reflectionProperty != null)
            {
                return reflectionProperty.CanRead;
            }
            return true;
        }

        public override bool IsSettable(PSAdaptedProperty adaptedProperty)
        {
            var reflectionProperty = adaptedProperty.BaseObject
                ?.GetType()
                ?.GetProperty(adaptedProperty.Name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

            if (reflectionProperty != null)
            {
                return reflectionProperty.CanWrite;
            }
            
            return adaptedProperty.BaseObject is IniSection;
        }

        public override void SetPropertyValue(PSAdaptedProperty adaptedProperty, object value)
        {
            var reflectionProperty = adaptedProperty.BaseObject
                ?.GetType()
                ?.GetProperty(adaptedProperty.Name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

            if (reflectionProperty != null)
            {
                reflectionProperty.SetValue(adaptedProperty.BaseObject, value);
                return;
            }

            if (adaptedProperty.BaseObject is IniSection section)
            {
                section[adaptedProperty.Name] = value as string;
            }
            else
            {
                throw new ArgumentException($"Cannot adapt properties for type {adaptedProperty.BaseObject?.GetType().FullName ?? "(null)"}");
            }
        }
        #endregion Property Adapter
    }
}
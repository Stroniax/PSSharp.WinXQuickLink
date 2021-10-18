using System;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace PSSharp.Ini
{
    /// <summary>
    /// PSPropertyAdapter for IniDictionary and IniSection.
    /// </summary>
    public sealed class IniPropertyAdapter : PSPropertyAdapter
    {
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
                return IniDictionary.Serialize(dictionary);
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniDictionary)}.", nameof(iniDictionary));
            }
        }
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
        public static string GetSectionName(PSObject iniSection)
        {
            if (iniSection.BaseObject is IniSection section)
            {
                return section.Name;
            }
            else
            {
                throw new ArgumentException($"The base object must be of type {typeof(IniSection)}.", nameof(iniSection));
            }
        }

        public override Collection<PSAdaptedProperty> GetProperties(object baseObject)
        {
            var collection = new Collection<PSAdaptedProperty>();
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
                    collection.Add(new PSAdaptedProperty(keyValuePair.Key, null));
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
            return true;
        }

        public override bool IsSettable(PSAdaptedProperty adaptedProperty)
        {
            return adaptedProperty.BaseObject is IniSection;
        }

        public override void SetPropertyValue(PSAdaptedProperty adaptedProperty, object value)
        {
            if (adaptedProperty.BaseObject is IniSection section)
            {
                section[adaptedProperty.Name] = value as string;
            }
            else
            {
                throw new ArgumentException($"Cannot adapt properties for type {adaptedProperty.BaseObject?.GetType().FullName ?? "(null)"}");
            }
        }
    }
}
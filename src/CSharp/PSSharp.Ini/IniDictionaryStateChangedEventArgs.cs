using System;

namespace PSSharp.Ini
{
    /// <summary>
    /// Event args raised when the <see cref="IniDictionaryState"/> of a <see cref="IniDictionary"/> is changed.
    /// </summary>
    [Serializable]
    public sealed class IniDictionaryStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Instantiates a new instance of <see cref="IniDictionaryStateChangedEventArgs"/>.
        /// </summary>
        /// <param name="stateInfo">The new state of the <see cref="IniDictionary"/> instance.</param>
        /// <param name="initialStateInfo">The previous state of the <see cref="IniDictionary"/> instance.</param>
        internal IniDictionaryStateChangedEventArgs(IniDictionaryStateInfo stateInfo, IniDictionaryStateInfo? initialStateInfo)
        {
            DictionaryStateInfo = stateInfo?? throw new ArgumentNullException(nameof(stateInfo));
            InitialStateInfo = initialStateInfo;
        }

        /// <summary>
        /// The new state of the <see cref="IniDictionary"/> instance.
        /// </summary>
        public IniDictionaryStateInfo DictionaryStateInfo { get; }
        /// <summary>
        /// The previous state of the <see cref="IniDictionary"/> instance.
        /// </summary>
        public IniDictionaryStateInfo? InitialStateInfo { get; }
    }
}

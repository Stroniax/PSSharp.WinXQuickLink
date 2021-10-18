using System;

namespace PSSharp.Ini
{
    /// <summary>
    /// State information representing the current status of an <see cref="IniDictionary"/> instance.
    /// </summary>
    [Serializable] 
    public sealed class IniDictionaryStateInfo
    {
        /// <inheritdoc cref="IniDictionaryStateInfo(IniDictionaryState, Exception?, string?)"/>
        public IniDictionaryStateInfo(IniDictionaryState state)
            : this (state, null,  null)
        {
        }
        /// <inheritdoc cref="IniDictionaryStateInfo(IniDictionaryState, Exception?, string?)"/>
        public IniDictionaryStateInfo(IniDictionaryState state, Exception? reason)
            : this (state, reason, null)
        {
        }
        /// <inheritdoc cref="IniDictionaryStateInfo(IniDictionaryState, Exception?, string?)"/>
        public IniDictionaryStateInfo(IniDictionaryState state,string? additionalInformation)
            : this (state, null, additionalInformation)
        {
        }
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="state"><inheritdoc cref="State" path="/summary"/></param>
        /// <param name="reason"><inheritdoc cref="Reason" path="/summary"/></param>
        /// <param name="additionalInformation"><inheritdoc cref="AdditionalInformation" path="/summary"/></param>
        public IniDictionaryStateInfo(IniDictionaryState state, Exception? reason, string? additionalInformation)
        {
            State = state;
            Reason = reason;
            AdditionalInformation = additionalInformation;
        }

        /// <summary>
        /// The state of the <see cref="IniDictionary"/> instance.
        /// </summary>
        public IniDictionaryState State { get; }
        /// <summary>
        /// An exception that caused the current <see cref="State"/> to be set, if any.
        /// </summary>
        public Exception? Reason { get; }
        /// <summary>
        /// Additional information about the reason <see cref="State"/> was set to its current value.
        /// </summary>
        public string? AdditionalInformation { get; }
    }
}

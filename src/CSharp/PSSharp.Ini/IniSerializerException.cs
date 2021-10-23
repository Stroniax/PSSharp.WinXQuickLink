namespace PSSharp.Ini
{
    [System.Serializable]
    public class IniSerializerException : System.Exception
    {
        public int Line { get; } = -1;
        public int Index { get; } = -1;
        public IniSerializerException(int line, int index) : base($"({line}, {index}) Invalid ini value.")
        {
            Line = line;
            Index = index;
        }
        public IniSerializerException(int line, int index, string message) : base($"$({line}, {index}) {message}")
        {
            Line = line;
            Index = index;
        }
        public IniSerializerException(int line, int index, string message, System.Exception inner) : base($"({line}, {index}) {message}", inner)
        {
            Line = line;
            Index = index;
        }
        protected IniSerializerException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
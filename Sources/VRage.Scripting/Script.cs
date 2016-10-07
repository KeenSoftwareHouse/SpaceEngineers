namespace VRage.Scripting
{
    /// <summary>
    ///     Represents a named script.
    /// </summary>
    public struct Script
    {
        public Script(string name, string code)
        {
            Name = name;
            Code = code;
        }

        /// <summary>
        ///     The name of the script.
        /// </summary>
        public readonly string Name;

        /// <summary>
        ///     The code content of the script.
        /// </summary>
        public readonly string Code;
    }
}
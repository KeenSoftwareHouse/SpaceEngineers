using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Compiler
{
    public struct IlCompilerErrorMessage
    {
        /// <summary>
        /// Line number of the compiler error.
        /// </summary>
        public int? Line { get; private set; }

        /// <summary>
        /// Error text of the compiler error.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Creates a new <see cref="IlCompilerErrorMessage"/> instance.
        /// </summary>
        /// <param name="msg">Error message to show to the user.</param>
        /// <param name="line">The line number of the error.</param>
        public IlCompilerErrorMessage(string msg, int? line = null)
            : this()
        {
            Line = line;
            Message = msg;
        }
    }
}

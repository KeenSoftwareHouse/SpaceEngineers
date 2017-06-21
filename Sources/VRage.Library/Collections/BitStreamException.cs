using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Library.Collections
{
	[Unsharper.UnsharperDisableReflection()]
    public class BitStreamException : Exception
    {
        public BitStreamException(Exception inner)
            : base("Error when reading using BitReader", inner)
        {
        }

        public BitStreamException(string message)
            : base(message)
        {
        }

        public BitStreamException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

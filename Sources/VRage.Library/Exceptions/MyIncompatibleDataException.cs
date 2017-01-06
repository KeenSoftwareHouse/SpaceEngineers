using System;

namespace VRage
{
    public class MyIncompatibleDataException : Exception
    {
        public MyIncompatibleDataException(string message) : base(message)
        {
        }
    }
}
using System;

namespace VRage.Network
{
    public class MyIncompatibleDataException : Exception
    {
        public MyIncompatibleDataException(string message) : base(message)
        {
        }
    }
}
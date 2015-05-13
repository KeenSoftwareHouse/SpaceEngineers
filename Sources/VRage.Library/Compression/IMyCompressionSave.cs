using System;
using System.Collections.Generic;
using System.IO;

namespace VRage
{
    public interface IMyCompressionSave : IDisposable
    {
        //  Add value to byte array (float, int, string, etc)
        void Add(byte[] value);
    
        // Add count bytes from value to byte array.
        void Add(byte[] value, int count);
        
        //  Add value to byte array (float, int, string, etc)
        void Add(float value);

        //  Add value to byte array (float, int, string, etc)
        void Add(int value);
        
        //  Add value to byte array (float, int, string, etc)
        void Add(byte value);
    }
}

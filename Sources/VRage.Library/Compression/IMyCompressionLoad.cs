using System;
using System.IO;

namespace VRage
{
    public interface IMyCompressionLoad
    {
        //  Reads value (int, float, ...) from decompressed buffer
        int GetInt32();

        //  Reads value (int, float, ...) from decompressed buffer
        byte GetByte();

        //  Copy raw bytes
        int GetBytes(int bytes, byte[] output);

        //  Signalizes if we haven't reached the end of un-compressed file by series of Get***() calls.
        bool EndOfFile();
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Library.Collections
{
    public struct BitReaderWriter
    {
        IBitSerializable m_writeData;

        BitStream m_readStream;
        int m_readStreamPosition;

        public readonly bool IsReading;

        public BitReaderWriter(IBitSerializable writeData)
        {
            m_writeData = writeData;
            m_readStream = null;
            m_readStreamPosition = 0;
            IsReading = false;
        }

        private BitReaderWriter(BitStream readStream, int readPos)
        {
            m_writeData = null;
            m_readStream = readStream;
            m_readStreamPosition = readPos;
            IsReading = true;
        }

        public static BitReaderWriter ReadFrom(BitStream stream)
        {
            Debug.Assert(stream.Reading, "Read stream should be set for reading!");

            // Move current position after the data
            uint dataBitLen = stream.ReadUInt32Variant();
            var reader = new BitReaderWriter(stream, stream.BitPosition);
            stream.SetBitPositionRead(stream.BitPosition + (int)dataBitLen);
            return reader;
        }

        public void Write(BitStream stream)
        {
            // TODO: this is suboptimal
            if (stream == null || m_writeData == null)
            {
                if (stream == null)
                    Debug.Fail("BitReaderWriter - Write - stream is null");
                if ( m_writeData == null)
                    Debug.Fail("BitReaderWriter - Write - m_writeData is null");
                return;
            }

            // Store old bit position
            int pos = stream.BitPosition;

            // Write data
            m_writeData.Serialize(stream, false);

            // Measure data len
            int len = stream.BitPosition - pos;

            // Restore old position
            stream.SetBitPositionWrite(pos);

            // Write data len
            stream.WriteVariant((uint)len);

            // Write data again
            m_writeData.Serialize(stream, false);
        }

        /// <summary>
        /// Returns false when validation was required and failed, otherwise true.
        /// </summary>
        public bool ReadData(IBitSerializable readDataInto, bool validate)
        {
            Debug.Assert(m_readStream != null, "Local invocation is not supported for BitReaderWriter");
            //if (m_readStream == null)
            //{
            //    tmpStream.ResetWrite();
            //    Write(tmpStream);
            //    tmpStream.ResetRead();
            //    return ReadFrom(tmpStream).ReadData(readDataInto, validate, null);
            //}

            int oldPos = m_readStream.BitPosition;
            m_readStream.SetBitPositionRead(m_readStreamPosition);
            try
            {
                return readDataInto.Serialize(m_readStream, validate);
            }
            finally
            {
                m_readStream.SetBitPositionRead(oldPos);
            }
        }
    }
}

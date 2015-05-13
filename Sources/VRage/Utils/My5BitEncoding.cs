using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Utils
{
    public class My5BitEncoding
    {
        private static My5BitEncoding m_default;
        public static My5BitEncoding Default
        {
            get
            {
                if (m_default == null)
                    m_default = new My5BitEncoding();
                return m_default;
            }
        }

        private char[] m_encodeTable;
        private Dictionary<char, byte> m_decodeTable;

        /// <summary>
        /// Initializes a new instance of the Encoding5Bit class.
        /// Uses characters 0-9 and A-Z except (0,O,1,I).
        /// </summary>
        public My5BitEncoding()
            : this(new char[] { '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' })
        {
        }

        /// <summary>
        /// Initializes a new instance of the Encoding5Bit class.
        /// </summary>
        /// <param name="characters">32 characters which will be used when encoding bytes to string.</param>
        public My5BitEncoding(char[] characters)
        {
            if (characters.Length != 32)
                throw new ArgumentException("Characters array must have 32 characters!");

            m_encodeTable = new char[32];
            characters.CopyTo(m_encodeTable, 0);

            m_decodeTable = CreateDecodeDict();
        }

        private Dictionary<char, byte> CreateDecodeDict()
        {
            var result = new Dictionary<char, byte>(m_encodeTable.Length);
            for (byte i = 0; i < (byte)m_encodeTable.Length; i++)
            {
                result.Add(m_encodeTable[i], i);
            }
            return result;
        }

        /// <summary>
        /// Encodes data as 5bit string.
        /// </summary>
        public char[] Encode(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 8 / 5);

            int curr = 0;
            int activeBits = 0;
            foreach (var b in data)
            {
                curr += b << activeBits; // Load byte
                activeBits += 8;

                while (activeBits >= 5)
                {
                    int ch = curr & 0x1f; // Read char
                    curr >>= 5; // Shift out read bits
                    activeBits -= 5;
                    sb.Append(m_encodeTable[ch]);
                }
            }
            if (activeBits > 0)
            {
                sb.Append(m_encodeTable[curr]);
            }
            return sb.ToString().ToCharArray();
        }

        /// <summary>
        /// Decodes 5bit string as bytes, not alligned characters may be lost.
        /// Only decode strings encoded with Encode.
        /// </summary>
        /// <param name="encoded5BitText"></param>
        /// <returns></returns>
        public byte[] Decode(char[] encoded5BitText)
        {
            List<byte> bytes = new List<byte>();

            int curr = 0;
            int activeBits = 0;

            foreach (var c in encoded5BitText)
            {
                byte val;
                if(!m_decodeTable.TryGetValue(c, out val))
                {
                    throw new ArgumentException("Encoded text is not valid for this encoding!");
                }

                curr += val << activeBits;
                activeBits += 5;

                while (activeBits >= 8)
                {
                    int b = curr & 0xff; // Read byte
                    curr >>= 8; // Shift out read bits
                    activeBits -= 8;
                    bytes.Add((byte)b);
                }
            }
            return bytes.ToArray();
        }
    }
}

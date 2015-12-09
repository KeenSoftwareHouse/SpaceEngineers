using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.Serialization;

namespace VRage.Common.Utils
{
    /// <summary>
    /// Helper class for serializing and deserializing checksum file.
    /// </summary>
    public sealed class MyChecksums
    {
        private string m_publicKey;
        public string PublicKey
        {
            get { return m_publicKey; }
            set
            {
                m_publicKey = value;
                PublicKeyAsArray = Convert.FromBase64String(m_publicKey);
            }
        }
        public SerializableDictionaryHack<string, string> Items { get; set; }

        [XmlIgnore]
        public byte[] PublicKeyAsArray { get; private set; }

        public MyChecksums()
        {
            Items = new SerializableDictionaryHack<string, string>();
        }
    }
}

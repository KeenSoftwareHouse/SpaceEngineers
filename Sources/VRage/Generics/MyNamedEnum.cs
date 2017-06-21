//#define SUPPORT_STRING_SAVE

#region Using

using ProtoBuf;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using VRage.Utils;

#endregion

namespace VRage.Generics
{
    [ProtoContract]
    public struct MyNamedEnum<T> : IXmlSerializable where T : struct, IConvertible
    {
        [ProtoMember]
        [XmlIgnore]
        int m_enumInt;

#if SUPPORT_STRING_SAVE
        public string EnumString;
#endif

        T EnumType;

        public MyNamedEnum(T s)
        {
            EnumType = s;
            m_enumInt = ((IConvertible)s).ToInt32(System.Globalization.CultureInfo.InvariantCulture);

#if SUPPORT_STRING_SAVE
            EnumString = s.ToString();
#endif
        }

        public MyNamedEnum(int i)
        {
            EnumType = default(T);
            m_enumInt = 0;
#if SUPPORT_STRING_SAVE
            EnumString = i.ToString();
#endif
            CreateFromInt(i);
        }

        void CreateFromString(string s)
        {
            if (!Enum.TryParse<T>(s, out EnumType))
            {
                EnumType = (T)Enum.ToObject(typeof(T), MyUtils.GetHash(s));
            }

#if SUPPORT_STRING_SAVE            
            EnumString = s;
#endif
            m_enumInt = ((IConvertible)EnumType).ToInt32(System.Globalization.CultureInfo.InvariantCulture);
        }

        void CreateFromInt(int i)
        {
            m_enumInt = i;
            EnumType = (T)Enum.ToObject(typeof(T), i);
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();
            string s = reader.ReadElementContentAsString();

            CreateFromString(s);
        }

        public void WriteXml(XmlWriter writer)
        {
#if SUPPORT_STRING_SAVE
            writer.WriteValue(EnumString);
#else
            writer.WriteValue(EnumType.ToString());
#endif
        }

#if !XB1 // XB1_NOPROTOBUF
        [ProtoAfterDeserialization]
#endif // !XB1
        void OnProtoDeserialize()
        {
            CreateFromInt(m_enumInt);
        }

        static public implicit operator T(MyNamedEnum<T> f)
        {
            return f.EnumType;
        }

        static public implicit operator MyNamedEnum<T>(T f)
        {
            return new MyNamedEnum<T>(f);
        }

        static public implicit operator int(MyNamedEnum<T> f)
        {
            return f.m_enumInt;
        }

        static public implicit operator MyNamedEnum<T>(int f)
        {
            return new MyNamedEnum<T>(f);
        }

    }
}

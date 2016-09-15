#region Using

using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;
using VRage.Utils;
using VRageMath;
using VRageRender;

#endregion

namespace VRageRender.Animations
{
    #region PropertyObjectBuilders

    public enum PropertyAnimationType
    {
        Const,
        Animated,
        Animated2D
    }

    [ProtoContract, XmlType("Property")]
    public class GenerationProperty
    {
        [ProtoMember, XmlAttribute("Name")]
        public string Name = "";

        [ProtoMember, XmlAttribute("AnimationType")]
        public PropertyAnimationType AnimationType = PropertyAnimationType.Const;

        [ProtoMember, XmlAttribute("Type")]
        public string Type = "";

        [ProtoMember]
        public float ValueFloat = 0f;

        [ProtoMember]
        public bool ValueBool = false;

        [ProtoMember]
        public int ValueInt = 0;

        [ProtoMember]
        public string ValueString = "";

        [ProtoMember]
        public Vector3 ValueVector3;

        [ProtoMember]
        public Vector4 ValueVector4;

        [ProtoMember]
        public List<AnimationKey> Keys;
    }

    [ProtoContract]
    public class Generation2DProperty
    {
        [ProtoMember]
        public List<AnimationKey> Keys;
    }

    [ProtoContract, XmlType("Key")]
    public class AnimationKey
    {
        [ProtoMember]
        public float Time = 0;

        [ProtoMember]
        public float ValueFloat = 0f;

        [ProtoMember]
        public bool ValueBool = false;

        [ProtoMember]
        public int ValueInt = 0;

        [ProtoMember]
        public string ValueString = "";

        [ProtoMember]
        public Vector3 ValueVector3;

        [ProtoMember]
        public Vector4 ValueVector4;

        [ProtoMember]
        public Generation2DProperty Value2D;
    }

    #endregion

    #region Interfaces

    public interface IMyConstProperty
    {
        string Name { get; set; }
        string ValueType { get; }
        string BaseValueType { get; }
        bool Animated { get; }
        bool Is2D { get; }
        void Serialize(XmlWriter writer);
        void Deserialize(XmlReader reader);
        void DeserializeFromObjectBuilder(GenerationProperty property);
        void SerializeValue(XmlWriter writer, object value);
        void DeserializeValue(XmlReader reader, out object value);
        void SetValue(object val);
        object GetValue();
        IMyConstProperty Duplicate();       
        Type GetValueType();        
    }

    #endregion

    #region MyConstProperty generic

    public class MyConstProperty<T> : IMyConstProperty
    {
        string m_name;
        T m_value;

        public MyConstProperty()
        {
            Init();
        }

        public MyConstProperty(string name)
            : this()
        {
            m_name = name;
        }

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        public virtual string ValueType
        {
            get { return typeof(T).Name; }
        }

        public virtual string BaseValueType
        {
            get { return ValueType; }
        }

        public virtual bool Animated
        {
            get { return false; }
        }

        public virtual bool Is2D
        {
            get { return false; }
        }

        protected virtual void Init()
        {
        }

        object IMyConstProperty.GetValue()
        {
            return m_value;
        }             

        public U GetValue<U>() where U : T
        {
            return (U)m_value;
        }

        public virtual void SetValue(object val)
        {
            SetValue((T)val);
        }

        public void SetValue(T val)
        {
            m_value = val;
        }

        public virtual IMyConstProperty Duplicate()
        {
            System.Diagnostics.Debug.Assert(false);
            return null;
        }

        protected virtual void Duplicate(IMyConstProperty targetProp)
        {
            targetProp.SetValue(GetValue<T>());
        }

        Type IMyConstProperty.GetValueType()
        {
            return GetValueTypeInternal();
        }

        protected virtual Type GetValueTypeInternal()
        {
            return typeof(T);
        }

        #region Serialization

        public virtual void Serialize(XmlWriter writer)
        {
            writer.WriteStartElement("Value" + ValueType);

            SerializeValue(writer, m_value);

            writer.WriteEndElement(); //Typename
        }

        public virtual void Deserialize(XmlReader reader)
        {
            m_name = reader.GetAttribute("name");
            reader.ReadStartElement(); // Type

            object v;
            DeserializeValue(reader, out v);
            m_value = (T)v;

            reader.ReadEndElement(); // Type
        }

        public virtual void DeserializeFromObjectBuilder(GenerationProperty property)
        {
            m_name = property.Name;

            object v;
            switch (property.Type)
            {
                case "Float":
                    v = property.ValueFloat;
                    break;

                case "Vector3":
                    v = property.ValueVector3;
                    break;

                case "Vector4":
                    v = property.ValueVector4;
                    break;

                default:
                case "Int":
                    v = property.ValueInt;
                    break;

                case "Bool":
                    v = property.ValueBool;
                    break;

                case "String":
                    v = property.ValueString;
                    break;

                case "MyTransparentMaterial":
                    v = MyTransparentMaterials.GetMaterial(property.ValueString);
                    break;
            }
            m_value = (T)v;
        }

        public virtual void SerializeValue(XmlWriter writer, object value)
        {
        }

        public virtual void DeserializeValue(XmlReader reader, out object value)
        {
            value = reader.Value;
            reader.Read();
        }

        #endregion
    }

    #endregion

    #region Derived const properties

    public class MyConstPropertyFloat : MyConstProperty<float>
    {
        public MyConstPropertyFloat()
        { }

        public MyConstPropertyFloat(string name)
            : base(name)
        { }

        public override string ValueType
        {
            get { return "Float"; }
        }

        public override void SerializeValue(XmlWriter writer, object value)
        {
            writer.WriteValue(((float)value).ToString(CultureInfo.InvariantCulture));
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            base.DeserializeValue(reader, out value);
            value = Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        public override IMyConstProperty Duplicate()
        {
            MyConstPropertyFloat prop = new MyConstPropertyFloat(Name);
            Duplicate(prop);
            return prop;
        }

        #region Implicit and explicit conversions

        static public implicit operator float(MyConstPropertyFloat f)
        {
            return f.GetValue<float>();
        }

        #endregion
    }

    public class MyConstPropertyVector3 : MyConstProperty<Vector3>
    {
        public MyConstPropertyVector3()
        { }

        public MyConstPropertyVector3(string name)
            : base(name)
        { }

        public override string ValueType
        {
            get { return "Vector3"; }
        }

        public override void SerializeValue(XmlWriter writer, object value)
        {
            writer.WriteElementString("X", ((Vector3)value).X.ToString());
            writer.WriteElementString("Y", ((Vector3)value).Y.ToString());
            writer.WriteElementString("Z", ((Vector3)value).Z.ToString());
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            Vector3 v;
            MyUtils.DeserializeValue(reader, out v);
            value = v;
        }

        public override IMyConstProperty Duplicate()
        {
            MyConstPropertyVector3 prop = new MyConstPropertyVector3(Name);
            Duplicate(prop);
            return prop;
        }

        #region Implicit and explicit conversions

        static public implicit operator Vector3(MyConstPropertyVector3 f)
        {
            return f.GetValue<Vector3>();
        }

        #endregion
    }

    public class MyConstPropertyVector4 : MyConstProperty<Vector4>
    {
        public MyConstPropertyVector4() { }

        public MyConstPropertyVector4(string name)
            : base(name)
        { }

        public override string ValueType
        {
            get { return "Vector4"; }
        }

        public override void SerializeValue(XmlWriter writer, object value)
        {
            writer.WriteElementString("W", ((Vector4)value).W.ToString());
            writer.WriteElementString("X", ((Vector4)value).X.ToString());
            writer.WriteElementString("Y", ((Vector4)value).Y.ToString());
            writer.WriteElementString("Z", ((Vector4)value).Z.ToString());
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            Vector4 v;
            MyUtils.DeserializeValue(reader, out v);
            value = v;
        }

        public override IMyConstProperty Duplicate()
        {
            MyConstPropertyVector4 prop = new MyConstPropertyVector4(Name);
            Duplicate(prop);
            return prop;
        }

        #region Implicit and explicit conversions

        static public implicit operator Vector4(MyConstPropertyVector4 f)
        {
            return f.GetValue<Vector4>();
        }

        #endregion
    }

    public class MyConstPropertyInt : MyConstProperty<int>
    {
        public MyConstPropertyInt() { }

        public MyConstPropertyInt(string name)
            : base(name)
        { }

        public override string ValueType
        {
            get { return "Int"; }
        }

        public override void SerializeValue(XmlWriter writer, object value)
        {
            writer.WriteValue(((int)value).ToString(CultureInfo.InvariantCulture));
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            base.DeserializeValue(reader, out value);
            value = Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        public override IMyConstProperty Duplicate()
        {
            MyConstPropertyInt prop = new MyConstPropertyInt(Name);
            Duplicate(prop);
            return prop;
        }

        #region Implicit and explicit conversions

        static public implicit operator int(MyConstPropertyInt f)
        {
            return f.GetValue<int>();
        }

        #endregion
    }

    public class MyConstPropertyEnum : MyConstPropertyInt
#if !UNSHARPER
        , IMyConstProperty
#endif
    {
        Type m_enumType;
        List<string> m_enumStrings;

        public MyConstPropertyEnum() { }

        public MyConstPropertyEnum(string name)
            : this(name, null, null)
        {
        }

        public MyConstPropertyEnum(string name, Type enumType, List<string> enumStrings)
            : base(name)
        {
            m_enumType = enumType;
            m_enumStrings = enumStrings;
        }

        public override string BaseValueType
        {
            get { return "Enum"; }
        }

        public override void SerializeValue(XmlWriter writer, object value)
        {
            writer.WriteValue(((int)value).ToString(CultureInfo.InvariantCulture));
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            base.DeserializeValue(reader, out value);
            value = Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        public Type GetEnumType()
        {
            return m_enumType;
        }

        public List<string> GetEnumStrings()
        {
            return m_enumStrings;
        }   
  
        public override IMyConstProperty Duplicate()
        {
            MyConstPropertyEnum prop = new MyConstPropertyEnum(Name);
            Duplicate(prop);
            prop.m_enumType = m_enumType;
            prop.m_enumStrings = m_enumStrings;
            return prop;
        }

#if UNSHARPER
        protected override Type GetValueTypeInternal()
        {
            return m_enumType;
        }
#else
        Type IMyConstProperty.GetValueType()
        {
            return m_enumType;
        }
#endif
        public override void SetValue(object val)
        {            
            int ival = Convert.ToInt32(val); // because just simple cast (int) thrown exception on ParticleTypeEnum type
            base.SetValue(ival);
        }
    }

    public class MyConstPropertyGenerationIndex : MyConstPropertyInt
    {
        public MyConstPropertyGenerationIndex() { }

        public MyConstPropertyGenerationIndex(string name)
            : base(name)
        {
        }

        public override string BaseValueType
        {
            get { return "GenerationIndex"; }
        }

        public override void SerializeValue(XmlWriter writer, object value)
        {
            writer.WriteValue(((int)value).ToString(CultureInfo.InvariantCulture));
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            base.DeserializeValue(reader, out value);
            value = Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        public override IMyConstProperty Duplicate()
        {
            MyConstPropertyGenerationIndex prop = new MyConstPropertyGenerationIndex(Name);
            Duplicate(prop);
            return prop;
        }
    }

    public class MyConstPropertyBool : MyConstProperty<bool>
    {
        public MyConstPropertyBool() { }

        public MyConstPropertyBool(string name)
            : base(name)
        { }

        public override string ValueType
        {
            get { return "Bool"; }
        }

        public override void SerializeValue(XmlWriter writer, object value)
        {
            writer.WriteValue(value.ToString().ToLower());
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            base.DeserializeValue(reader, out value);
            value = Convert.ToBoolean(value);
        }

        public override IMyConstProperty Duplicate()
        {
            MyConstPropertyBool prop = new MyConstPropertyBool(Name);
            Duplicate(prop);
            return prop;
        }

        #region Implicit and explicit conversions

        static public implicit operator bool(MyConstPropertyBool f)
        {
            return f != null && f.GetValue<bool>();
        }

        #endregion
    }

    public class MyConstPropertyString : MyConstProperty<string>
    {
        public MyConstPropertyString() { }

        public MyConstPropertyString(string name)
            : base(name)
        { }

        public override string ValueType
        {
            get { return "String"; }
        }

        public override void SerializeValue(XmlWriter writer, object value)
        {
            writer.WriteValue((string)value);
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            base.DeserializeValue(reader, out value);
            value = value.ToString();
        }

        public override IMyConstProperty Duplicate()
        {
            MyConstPropertyString prop = new MyConstPropertyString(Name);
            Duplicate(prop);
            return prop;
        }

        #region Implicit and explicit conversions

        static public implicit operator string(MyConstPropertyString f)
        {
            return f.GetValue<string>();
        }

        #endregion
    }

    #endregion
}

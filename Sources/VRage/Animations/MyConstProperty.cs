#region Using

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using VRage.Utils;
using VRageMath;

#endregion

namespace VRage.Animations
{
    #region Interfaces

    public interface IMyConstProperty
    {
        string Name { get; }
        void Serialize(XmlWriter writer);
        void Deserialize(XmlReader reader);
        void SerializeValue(XmlWriter writer, object value);
        void DeserializeValue(XmlReader reader, out object value);
        void SetValue(object val);
        IMyConstProperty Duplicate();       
        Type GetValueType();
        /// <summary>
        /// Warning, this does allocation, use only in editor!
        /// </summary>
        object EditorGetValue();
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
        }

        protected virtual void Init()
        {
        }

        object IMyConstProperty.EditorGetValue()
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
            return typeof(T);
        }

        #region Serialization

        public virtual void Serialize(XmlWriter writer)
        {
            writer.WriteStartElement(this.GetType().Name);
            writer.WriteAttributeString("name", Name);

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

        public override void SerializeValue(XmlWriter writer, object value)
        {
            MyUtils.SerializeValue(writer, (Vector3)value);
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
        {  }

        public override void SerializeValue(XmlWriter writer, object value)
        {
            MyUtils.SerializeValue(writer, (Vector4)value);
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

    public class MyConstPropertyEnum : MyConstPropertyInt, IMyConstProperty
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

        Type IMyConstProperty.GetValueType()
        {
            return m_enumType;
        }

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

        public override void SerializeValue(XmlWriter writer, object value)
        {
            writer.WriteValue((bool)value);
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
            return f.GetValue<bool>();
        }

        #endregion
    }

    #endregion
}

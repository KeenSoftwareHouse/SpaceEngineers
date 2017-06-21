#region Using

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using VRageRender.Animations;
using VRage.Utils;
using VRageMath;

#endregion

namespace VRageRender.Animations
{
    #region Interfaces

    public interface IMyAnimatedProperty : IMyConstProperty
    {
        void GetInterpolatedValue(float time, out object value);
        int AddKey(float time, object val);
        void RemoveKey(float time);
        void RemoveKey(int index);
        void RemoveKeyByID(int id);
        void ClearKeys();
        int GetKeysCount();
        void SetKey(int index, float time);
        void SetKey(int index, float time, object value);
        void GetKey(int index, out float time, out object value);
        void GetKey(int index, out int id, out float time, out object value);
        void SetKeyByID(int id, float time);
        void SetKeyByID(int id, float time, object value);
        void GetKeyByID(int id, out float time, out object value);
    }

    [System.Reflection.Obfuscation(Feature = System.Reflection.Obfuscator.NoRename, Exclude = true, ApplyToMembers = true)]
    public interface IMyAnimatedProperty<T> : IMyAnimatedProperty
    {
        [System.Reflection.Obfuscation(Feature = System.Reflection.Obfuscator.NoRename, Exclude = true)]
        void GetInterpolatedValue<U>(float time, out U value) where U : T;

        [System.Reflection.Obfuscation(Feature = System.Reflection.Obfuscator.NoRename, Exclude = true)]
        int AddKey<U>(float time, U val) where U : T;
    }

    #endregion

    #region Interpolators

    static class MyIntInterpolator
    {
        public static void Lerp(ref int val1, ref int val2, float time, out int value)
        {
            value = val1 + (int)((val2 - val1) * time);// (int)MathHelper.Lerp(val1, val2, time);
        }
        public static void Switch(ref int val1, ref int val2, float time, out int value)
        {
            value = time < 0.5f ? val1 : val2;
        }
    }

    static class MyFloatInterpolator
    {
        public static void Lerp(ref float val1, ref float val2, float time, out float value)
        {
            // value = 0;// MathHelper.Lerp(val1, val2, time);
            value = val1 + (val2 - val1) * time;
        }
    }

    static class MyVector3Interpolator
    {
        public static void Lerp(ref Vector3 val1, ref Vector3 val2, float time, out Vector3 value)
        {
            //value = Vector3.Zero;// Vector3.Lerp(ref val1, ref val2, time, out value);
            value.X = val1.X + (val2.X - val1.X) * time;
            value.Y = val1.Y + (val2.Y - val1.Y) * time;
            value.Z = val1.Z + (val2.Z - val1.Z) * time;
        }
    }

    static class MyVector4Interpolator
    {
        public static void Lerp(ref Vector4 val1, ref Vector4 val2, float time, out Vector4 value)
        {
            //value = Vector4.Zero;// Vector4.Lerp(ref val1, ref val2, time, out value);
            value.X = val1.X + (val2.X - val1.X) * time;
            value.Y = val1.Y + (val2.Y - val1.Y) * time;
            value.Z = val1.Z + (val2.Z - val1.Z) * time;
            value.W = val1.W + (val2.W - val1.W) * time;
        }
    }



    #endregion

    #region MyAnimatedProperty generic

    public class MyAnimatedProperty<T> : IMyAnimatedProperty<T>
    {
        public struct ValueHolder
        {
            public ValueHolder(int id, float time, T value, float diff)
            {
                ID = id;
                Time = time;
                Value = value;
                PrecomputedDiff = diff;
            }

            public T Value;
            public float PrecomputedDiff;
            public float Time;
            public int ID;

            public ValueHolder Duplicate()
            {
                ValueHolder duplicate = new ValueHolder();
                duplicate.Time = Time;
                duplicate.PrecomputedDiff = PrecomputedDiff;
                duplicate.ID = ID;

                if (Value is IMyConstProperty)
                    duplicate.Value = (T)((IMyConstProperty)Value).Duplicate();
                else
                    duplicate.Value = Value;

                return duplicate;
            }
        }

        #region Comparer

        class MyKeysComparer : IComparer<MyAnimatedProperty<T>.ValueHolder>
        {
            public int Compare(MyAnimatedProperty<T>.ValueHolder x, MyAnimatedProperty<T>.ValueHolder y)
            {
                return x.Time.CompareTo(y.Time);
            }
        }

        #endregion

        protected List<ValueHolder> m_keys = new List<ValueHolder>(); //cannot preinit space, because it would grow the animatedparticle pool much
        public delegate void InterpolatorDelegate(ref T previousValue, ref T nextValue, float time, out T value);
        public InterpolatorDelegate Interpolator;
        protected string m_name;
        bool m_interpolateAfterEnd;

        static MyKeysComparer m_keysComparer = new MyKeysComparer();
        static int m_globalKeyCounter = 0;

        public MyAnimatedProperty()
        {
            Init();
        }

        public MyAnimatedProperty(string name, bool interpolateAfterEnd, InterpolatorDelegate interpolator)
            : this()
        {
            m_name = name;
            m_interpolateAfterEnd = interpolateAfterEnd;
            if (interpolator != null)
                Interpolator = interpolator;
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
            get { return true; }
        }

        public virtual bool Is2D
        {
            get { return false; }
        }

        protected virtual void Init()
        {
        }

        public void SetValue(object val)
        {
        }

        public void SetValue(T val)
        {
        }

        object IMyConstProperty.GetValue()
        {
            return null;
        }

        public U GetValue<U>()
        {
            return default(U);
        }

        void IMyAnimatedProperty.GetInterpolatedValue(float time, out object value)
        {
            T valueT;
            GetInterpolatedValue<T>(time, out valueT);
            value = valueT;
        }

        public void GetInterpolatedValue<U>(float time, out U value) where U : T
        {
            if (m_keys.Count == 0)
            {
                value = default(U);
                return;
            }

            if (m_keys.Count == 1)
            {
                value = (U)m_keys[0].Value;
                return;
            }

            if (time > m_keys[m_keys.Count - 1].Time)
            {
                if (m_interpolateAfterEnd)
                {
                    T previousValue, nextValue;
                    float previousTime, nextTime, difference;

                    GetPreviousValue(m_keys[m_keys.Count - 1].Time, out previousValue, out previousTime);
                    GetNextValue(time, out nextValue, out nextTime, out difference);

                    T val;
                    if (Interpolator != null)
                    {
                        Interpolator(ref previousValue, ref nextValue, (time - previousTime) * difference, out val);
                        value = (U)val;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Fail("Interpolator is not set for this property!");
                        value = default(U);
                    }
                }
                else
                    value = (U)m_keys[m_keys.Count - 1].Value;

                return;
            }

            {
                T previousValue, nextValue;
                float previousTime, nextTime, difference;

                //   VRageRender.MyRenderProxy.GetRenderProfiler().StartParticleProfilingBlock("GetPreviousValue");
                GetPreviousValue(time, out previousValue, out previousTime);
                //   VRageRender.MyRenderProxy.GetRenderProfiler().EndParticleProfilingBlock();

                //   VRageRender.MyRenderProxy.GetRenderProfiler().StartParticleProfilingBlock("GetNextValue");
                GetNextValue(time, out nextValue, out nextTime, out difference);
                //  VRageRender.MyRenderProxy.GetRenderProfiler().EndParticleProfilingBlock();

                if (nextTime == previousTime)
                    value = (U)previousValue;
                else
                {
                    T val;
                    if (Interpolator != null)
                    {
                        Interpolator(ref previousValue, ref nextValue, (time - previousTime) * difference, out val);
                        value = (U)val;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Fail("Interpolator is not set for this property!");
                        value = default(U);
                    }
                }
            }
        }


        public void GetPreviousValue(float time, out T previousValue, out float previousTime)
        {
            previousValue = default(T);
            previousTime = 0;

            if (m_keys.Count > 0)
            {
                previousTime = m_keys[0].Time;
                previousValue = m_keys[0].Value;
            }

            for (int i = 1; i < m_keys.Count; i++)
            {
                if (m_keys[i].Time >= time)
                    break;

                previousTime = m_keys[i].Time;
                previousValue = m_keys[i].Value;
            }
        }

        void IMyAnimatedProperty.GetKey(int index, out float time, out object value)
        {
            T val;
            GetKey(index, out time, out val);
            value = val;
        }

        void IMyAnimatedProperty.GetKey(int index, out int id, out float time, out object value)
        {
            T val;
            GetKey(index, out id, out time, out val);
            value = val;
        }


        void IMyAnimatedProperty.GetKeyByID(int id, out float time, out object value)
        {
            T val;
            GetKeyByID(id, out time, out val);
            value = val;
        }


        void IMyAnimatedProperty.SetKey(int index, float time, object value)
        {
            var key = m_keys[index];
            key.Time = time;
            key.Value = (T)value;
            m_keys[index] = key;

            UpdateDiff(index - 1);
            UpdateDiff(index);
            UpdateDiff(index + 1);

            m_keys.Sort(m_keysComparer);
        }

        void IMyAnimatedProperty.SetKey(int index, float time)
        {
            var key = m_keys[index];
            key.Time = time;
            m_keys[index] = key;

            UpdateDiff(index - 1);
            UpdateDiff(index);
            UpdateDiff(index + 1);

            m_keys.Sort(m_keysComparer);
        }

        void IMyAnimatedProperty.SetKeyByID(int id, float time, object value)
        {
            int index = -1;
            var key = new ValueHolder();

            for (int i = 0; i < m_keys.Count; i++)
            {
                if (m_keys[i].ID == id)
                {
                    key = m_keys[i];
                    index = i;
                    break;
                }
            }

            key.Time = time;
            key.Value = (T)value;

            if (index == -1)
            {
                key.ID = id;
                index = m_keys.Count;
                m_keys.Add(key);
            }
            else
                m_keys[index] = key;

            UpdateDiff(index - 1);
            UpdateDiff(index);
            UpdateDiff(index + 1);

            m_keys.Sort(m_keysComparer);
        }

        void IMyAnimatedProperty.SetKeyByID(int id, float time)
        {
            var key = m_keys.Find(x => x.ID == id);
            int index = m_keys.IndexOf(key);
            key.Time = time;
            m_keys[index] = key;

            UpdateDiff(index - 1);
            UpdateDiff(index);
            UpdateDiff(index + 1);

            m_keys.Sort(m_keysComparer);
        }

        public void GetNextValue(float time, out T nextValue, out float nextTime, out float difference)
        {
            nextValue = default(T);
            nextTime = -1;
            difference = 0;

            for (int i = 0; i < m_keys.Count; i++)
            {
                nextTime = m_keys[i].Time;
                nextValue = m_keys[i].Value;
                difference = m_keys[i].PrecomputedDiff;

                if (nextTime >= time)
                    break;
            }
        }

        public void AddKey(ValueHolder val)
        {
            m_keys.Add(val);
        }

        public int AddKey<U>(float time, U val) where U : T
        {
            var value = new ValueHolder(m_globalKeyCounter++, time, (T)val, 0);
            m_keys.Add(value);
            m_keys.Sort(m_keysComparer);

            int index = 0;
            for (index = 0; index < m_keys.Count; index++)
            {
                if (m_keys[index].Time == time)
                {
                    break;
                }
            }

            if (index > 0)
            {
                //Calculate relative difference with previous value to faster calculation of interpolated values
                //(time - prevtime) / (nexttime - prevtime)
                UpdateDiff(index);
            }

            return value.ID;
        }

        private void UpdateDiff(int index)
        {
            //Calculate relative difference with previous value to faster calculation of interpolated values
            //(time - prevtime) / (nexttime - prevtime)
            if (index < 1 || index >= m_keys.Count)
                return;

            float time = m_keys[index].Time;
            float prevTime = m_keys[index - 1].Time;
            m_keys[index] = new ValueHolder(m_keys[index].ID, time, (T)m_keys[index].Value, 1.0f / (time - prevTime));
        }

        int IMyAnimatedProperty.AddKey(float time, object val)
        {
            return AddKey(time, (T)val);
        }

        public void RemoveKey(float time)
        {
            for (int i = 0; i < m_keys.Count; i++)
            {
                if (m_keys[i].Time == time)
                {
                    RemoveKey(i);
                    break;
                }
            }
        }

        void IMyAnimatedProperty.RemoveKey(int index)
        {
            RemoveKey(index);
        }

        void IMyAnimatedProperty.RemoveKeyByID(int id)
        {
            var key = m_keys.Find(x => x.ID == id);
            int index = m_keys.IndexOf(key);
            RemoveKey(index);
        }

        void RemoveKey(int index)
        {
            m_keys.RemoveAt(index);
            UpdateDiff(index);
        }

        public void ClearKeys()
        {
            m_keys.Clear();
        }

        public void GetKey(int index, out float time, out T value)
        {
            time = m_keys[index].Time;
            value = m_keys[index].Value;
        }

        public void GetKey(int index, out int id, out float time, out T value)
        {
            id = m_keys[index].ID;
            time = m_keys[index].Time;
            value = m_keys[index].Value;
        }

        public void GetKeyByID(int id, out float time, out T value)
        {
            var key = m_keys.Find(x => x.ID == id);
            time = key.Time;
            value = key.Value;
        }

        public int GetKeysCount()
        {
            return m_keys.Count;
        }

        public virtual IMyConstProperty Duplicate()
        {
            System.Diagnostics.Debug.Assert(false);
            return null;
        }

        protected virtual void Duplicate(IMyConstProperty targetProp)
        {
            MyAnimatedProperty<T> animatedTargetProp = targetProp as MyAnimatedProperty<T>;
            System.Diagnostics.Debug.Assert(animatedTargetProp != null);

            animatedTargetProp.Interpolator = Interpolator;
            animatedTargetProp.ClearKeys();

            foreach (ValueHolder pair in m_keys)
            {
                animatedTargetProp.AddKey(pair.Duplicate());
            }
        }

        Type IMyConstProperty.GetValueType()
        {
            return typeof(T);
        }

        #region Serialization

        public virtual void Serialize(XmlWriter writer)
        {
            writer.WriteStartElement("Keys");
            foreach (ValueHolder key in m_keys)
            {
                writer.WriteStartElement("Key");

                writer.WriteElementString("Time", key.Time.ToString(CultureInfo.InvariantCulture));

                if (Is2D)
                    writer.WriteStartElement("Value2D");
                else
                    writer.WriteStartElement("Value" + ValueType);
                SerializeValue(writer, key.Value);
                writer.WriteEndElement(); //Value

                writer.WriteEndElement(); //Key
            }
            writer.WriteEndElement(); //Keys
        }


        public virtual void Deserialize(XmlReader reader)
        {
            m_name = reader.GetAttribute("name");
            reader.ReadStartElement(); // Type

            m_keys.Clear();

            bool isEmpty = reader.IsEmptyElement;

            reader.ReadStartElement(); // Keys

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement(); // Key

                float time = reader.ReadElementContentAsFloat();

                reader.ReadStartElement(); // Value
                object value;
                DeserializeValue(reader, out value);
                reader.ReadEndElement(); //Value

                AddKey<T>(time, (T)value);

                reader.ReadEndElement(); //Key
            }

            //RemoveRedundantKeys();

            if (!isEmpty)
                reader.ReadEndElement(); //Keys

            reader.ReadEndElement(); // Type
        }

        public void DeserializeFromObjectBuilder_Animation(Generation2DProperty property, string type)
        {
            DeserializeKeys(property.Keys, type);
        }

        public virtual void DeserializeFromObjectBuilder(GenerationProperty property)
        {
            m_name = property.Name;

            DeserializeKeys(property.Keys, property.Type);
        }

        public void DeserializeKeys(List<AnimationKey> keys, string type)
        {
            m_keys.Clear();

            foreach (var key in keys)
            {
                object v;
                switch (type)
                {
                    case "Float":
                        v = key.ValueFloat;
                        break;

                    case "Vector3":
                        v = key.ValueVector3;
                        break;

                    case "Vector4":
                        v = key.ValueVector4;
                        break;

                    default:
                    case "Enum":
                    case "GenerationIndex":
                    case "Int":
                        v = key.ValueInt;
                        break;

                    case "Bool":
                        v = key.ValueBool;
                        break;

                    case "MyTransparentMaterial":
                        v = MyTransparentMaterials.GetMaterial(key.ValueString);
                        break;

                    case "String":
                        v = key.ValueString;
                        break;
                }

                AddKey<T>(key.Time, (T)v);
            }
        }

        void RemoveRedundantKeys()
        {
            //remove redundant keys
            int i = 0;
            bool previousDifferent = true;
            while (i < m_keys.Count - 1)
            {
                object value1 = m_keys[i].Value;
                object value2 = m_keys[i + 1].Value;

                bool same = EqualsValues(value1, value2);
                if (same && (previousDifferent == false))
                {
                    RemoveKey(i);
                    //MyParticlesLibrary.RedundancyDetected++;
                    continue;
                }

                previousDifferent = !same;

                i++;
            }

            if (m_keys.Count == 2)
            {
                object value1 = m_keys[0].Value;
                object value2 = m_keys[1].Value;

                bool same = EqualsValues(value1, value2);
                if (same)
                {
                    RemoveKey(i);
                    //MyParticlesLibrary.RedundancyDetected++;
                }
            }
        }

        public virtual void SerializeValue(XmlWriter writer, object value)
        {
        }

        public virtual void DeserializeValue(XmlReader reader, out object value)
        {
            value = reader.Value;
            reader.Read();
        }

        protected virtual bool EqualsValues(object value1, object value2)
        {
            return false;
        }

        #endregion
    }

    #endregion

    #region Derived animation properties

    public class MyAnimatedPropertyFloat : MyAnimatedProperty<float>
    {
        public MyAnimatedPropertyFloat()
        { }

        public MyAnimatedPropertyFloat(string name)
            : this(name, false, null)
        { }

        public MyAnimatedPropertyFloat(string name, bool interpolateAfterEnd, InterpolatorDelegate interpolator)
            : base(name, interpolateAfterEnd, interpolator)
        {
        }

        public override string ValueType
        {
            get { return "Float"; }
        }

        protected override void Init()
        {
            Interpolator = MyFloatInterpolator.Lerp;
            base.Init();
        }

        public override IMyConstProperty Duplicate()
        {
            MyAnimatedPropertyFloat prop = new MyAnimatedPropertyFloat(Name);
            Duplicate(prop);
            return prop;
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

        protected override bool EqualsValues(object value1, object value2)
        {
            return MyUtils.IsZero((float)value1 - (float)value2);
        }
    }

    public class MyAnimatedPropertyVector3 : MyAnimatedProperty<Vector3>
    {
        public MyAnimatedPropertyVector3()
        { }

        public MyAnimatedPropertyVector3(string name)
            : this(name, false, null)
        { }

        public MyAnimatedPropertyVector3(string name, bool interpolateAfterEnd, InterpolatorDelegate interpolator)
            : base(name, interpolateAfterEnd, interpolator)
        {
        }

        public override string ValueType
        {
            get { return "Vector3"; }
        }

        protected override void Init()
        {
            Interpolator = MyVector3Interpolator.Lerp;
            base.Init();
        }

        public override IMyConstProperty Duplicate()
        {
            MyAnimatedPropertyVector3 prop = new MyAnimatedPropertyVector3(Name);
            Duplicate(prop);
            return prop;
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

        protected override bool EqualsValues(object value1, object value2)
        {
            return MyUtils.IsZero((Vector3)value1 - (Vector3)value2);
        }
    }

    public class MyAnimatedPropertyVector4 : MyAnimatedProperty<Vector4>
    {
        public MyAnimatedPropertyVector4() { }

        public MyAnimatedPropertyVector4(string name)
            : this(name, null)
        {
        }

        public MyAnimatedPropertyVector4(string name, InterpolatorDelegate interpolator)
            : base(name, false, interpolator)
        {
        }

        public override string ValueType
        {
            get { return "Vector4"; }
        }

        protected override void Init()
        {
            Interpolator = MyVector4Interpolator.Lerp;
            base.Init();
        }

        public override IMyConstProperty Duplicate()
        {
            MyAnimatedPropertyVector4 prop = new MyAnimatedPropertyVector4(Name);
            Duplicate(prop);
            return prop;
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

        protected override bool EqualsValues(object value1, object value2)
        {
            return MyUtils.IsZero((Vector4)value1 - (Vector4)value2);
        }
    }

    public class MyAnimatedPropertyInt : MyAnimatedProperty<int>
    {
        public MyAnimatedPropertyInt() { }

        public MyAnimatedPropertyInt(string name)
            : this(name, null)
        { }

        public MyAnimatedPropertyInt(string name, InterpolatorDelegate interpolator)
            : base(name, false, interpolator)
        {
        }

        public override string ValueType
        {
            get { return "Int"; }
        }

        protected override void Init()
        {
            Interpolator = MyIntInterpolator.Lerp;
            base.Init();
        }

        public override IMyConstProperty Duplicate()
        {
            MyAnimatedPropertyInt prop = new MyAnimatedPropertyInt(Name);
            Duplicate(prop);
            return prop;
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

        protected override bool EqualsValues(object value1, object value2)
        {
            return (int)value1 == (int)value2;
        }
    }

    public class MyAnimatedPropertyEnum : MyAnimatedPropertyInt
    {
        Type m_enumType;
        List<string> m_enumStrings;

        public MyAnimatedPropertyEnum() { }

        public MyAnimatedPropertyEnum(string name)
            : this(name, null, null)
        { }

        public MyAnimatedPropertyEnum(string name, Type enumType, List<string> enumStrings)
            : this(name, null, enumType, enumStrings)
        { }

        public override string BaseValueType
        {
            get { return "Enum"; }
        }

        public MyAnimatedPropertyEnum(string name, InterpolatorDelegate interpolator, Type enumType, List<string> enumStrings)
            : base(name, interpolator)
        {
            m_enumType = enumType;
            m_enumStrings = enumStrings;
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
            MyAnimatedPropertyEnum prop = new MyAnimatedPropertyEnum(Name);
            Duplicate(prop);
            prop.m_enumType = m_enumType;
            prop.m_enumStrings = m_enumStrings;
            return prop;
        }
    }




    #endregion
}

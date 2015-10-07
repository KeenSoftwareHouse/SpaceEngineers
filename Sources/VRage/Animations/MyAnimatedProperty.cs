#region Using

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using VRage.Utils;
using VRageMath;



#endregion

namespace VRage.Animations
{
    #region Interfaces

    public interface IMyAnimatedProperty : IMyConstProperty
    {
        void GetInterpolatedValue(float time, out object value);
        void AddKey(float time, object val);
        void RemoveKey(float time);
        void RemoveKey(int index);
        //IEnumerable GetKeys();
        void ClearKeys();
        int GetKeysCount();        
        void SetKey(int index, float time);

        /// <summary>
        /// Warning this will do allocations, use only in editor!
        /// </summary>
        void EditorSetKey(int index, float time, object value);
        /// <summary>
        /// Warning this will do allocations, use only in editor!
        /// </summary>
        void EditorGetKey(int index, out float time, out object value);        
    }

    [System.Reflection.Obfuscation(Feature = System.Reflection.Obfuscator.NoRename, Exclude = true, ApplyToMembers = true)]
    public interface IMyAnimatedProperty<T> : IMyAnimatedProperty
    {
        [System.Reflection.Obfuscation(Feature = System.Reflection.Obfuscator.NoRename, Exclude = true)]
        void GetInterpolatedValue<U>(float time, out U value) where U : T;

        [System.Reflection.Obfuscation(Feature = System.Reflection.Obfuscator.NoRename, Exclude = true)]
        void AddKey<U>(float time, U val) where U : T;
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
            public ValueHolder(float time, T value, float diff)
            {
                Time = time;
                Value = value;
                PrecomputedDiff = diff;
            }

            public T Value;
            public float PrecomputedDiff;
            public float Time;
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

        //protected SortedList<float, ValueHolder> m_keys = new SortedList<float, ValueHolder>(16);
        protected List<ValueHolder> m_keys = new List<ValueHolder>(); //cannot preinit space, because it would grow the animatedparticle pool mych
        public delegate void InterpolatorDelegate(ref T previousValue, ref T nextValue, float time, out T value);
        public InterpolatorDelegate Interpolator;
        string m_name;
        bool m_interpolateAfterEnd;
        static MyKeysComparer m_keysComparer = new MyKeysComparer();

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

        object IMyConstProperty.EditorGetValue()
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

        void IMyAnimatedProperty.EditorGetKey(int index, out float time, out object value)
        {
            T val;
            GetKey(index, out time, out val);
            value = val;
        }

        void IMyAnimatedProperty.EditorSetKey(int index, float time, object value)
        {
            var key = m_keys[index];
            key.Time = time;
            key.Value = (T)value;
            m_keys[index] = key;
            m_keys.Sort(m_keysComparer);
        }

        void IMyAnimatedProperty.SetKey(int index, float time)
        {
            var key = m_keys[index];
            key.Time = time;
            m_keys[index] = key;
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

        public void AddKey<U>(float time, U val) where U : T
        {
            //if (m_keys.ContainsKey(time))
            //  m_keys.Remove(time);
            RemoveKey(time);

            m_keys.Add(new ValueHolder(time, (T)val, 0));
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
        }

        private void UpdateDiff(int index)
        {
            //Calculate relative difference with previous value to faster calculation of interpolated values
            //(time - prevtime) / (nexttime - prevtime)
            if (index == 0 || index >= m_keys.Count)
                return;

            float time = m_keys[index].Time;
            float prevTime = m_keys[index - 1].Time;
            m_keys[index] = new ValueHolder(time, (T)m_keys[index].Value, 1.0f / (time - prevTime));
        }

        void IMyAnimatedProperty.AddKey(float time, object val)
        {
            AddKey(time, (T)val);
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
                animatedTargetProp.AddKey(pair);
            }
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

            writer.WriteStartElement("Keys");
            foreach (ValueHolder key in m_keys)
            {
                writer.WriteStartElement("Key");

                writer.WriteElementString("Time", key.Time.ToString(CultureInfo.InvariantCulture));

                writer.WriteStartElement("Value");
                SerializeValue(writer, key.Value);
                writer.WriteEndElement(); //Value

                writer.WriteEndElement(); //Key
            }
            writer.WriteEndElement(); //Keys

            writer.WriteEndElement(); //Typename
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
            Vector3 v = (Vector3)value;
            writer.WriteValue(v.X.ToString(CultureInfo.InvariantCulture) + " " + v.Y.ToString(CultureInfo.InvariantCulture) + " " + v.Z.ToString(CultureInfo.InvariantCulture));
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
            Vector4 v = (Vector4)value;
            writer.WriteValue(v.X.ToString(CultureInfo.InvariantCulture) + " " + v.Y.ToString(CultureInfo.InvariantCulture) + " " + v.Z.ToString(CultureInfo.InvariantCulture) + " " + v.W.ToString(CultureInfo.InvariantCulture));
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

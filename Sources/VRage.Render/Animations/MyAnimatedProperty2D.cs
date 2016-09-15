#region Using

using System;
using System.Collections.Generic;
using System.Xml;
using VRageRender.Animations;
using VRage.Utils;
using VRageMath;

#endregion

namespace VRageRender.Animations
{
    #region Interfaces

    public interface IMyAnimatedProperty2D : IMyAnimatedProperty
    {
        IMyAnimatedProperty CreateEmptyKeys();
        void GetInterpolatedKeys(float overallTime, float multiplier, IMyAnimatedProperty interpolatedKeys);
    }

    public interface IMyAnimatedProperty2D<T, V, W> : IMyAnimatedProperty2D
    {
        X GetInterpolatedValue<X>(float overallTime, float time) where X : V;
        void GetInterpolatedKeys(float overallTime, W variance, float multiplier, IMyAnimatedProperty interpolatedKeys);
    }


    #endregion

    #region MyAnimatedProperty2D generic

    public class MyAnimatedProperty2D<T, V, W> : MyAnimatedProperty<T>, IMyAnimatedProperty2D<T, V, W> where T : MyAnimatedProperty<V>, new()
    {   //List<0 - effect time> of list<0-1> of keys

        protected MyAnimatedProperty<V>.InterpolatorDelegate m_interpolator2;

        public MyAnimatedProperty2D()
        { }

        public MyAnimatedProperty2D(string name, MyAnimatedProperty<V>.InterpolatorDelegate interpolator)
            : base(name, false, null)
        {
            m_interpolator2 = interpolator;
        }

        public X GetInterpolatedValue<X>(float overallTime, float time) where X : V
        {
            T previousKeys, nextKeys;
            float previousTime, nextTime, difference;
            GetPreviousValue(overallTime, out previousKeys, out previousTime);
            GetNextValue(overallTime, out nextKeys, out nextTime, out difference);

            V prevValue, nextValue;
            previousKeys.GetInterpolatedValue<V>(time, out prevValue);
            nextKeys.GetInterpolatedValue<V>(time, out nextValue);

            V interpolatedValue;
            previousKeys.Interpolator(ref prevValue, ref nextValue, (overallTime - previousTime) * difference, out interpolatedValue);

            return (X)interpolatedValue;
        }

        public void GetInterpolatedKeys(float overallTime, float multiplier, IMyAnimatedProperty interpolatedKeys)
        {
            GetInterpolatedKeys(overallTime, default(W), multiplier, interpolatedKeys);
        }

        public override bool Is2D
        {
            get { return true; }
        }

        public void GetInterpolatedKeys(float overallTime, W variance, float multiplier, IMyAnimatedProperty interpolatedKeysOb)
        {
            T previousKeys, nextKeys;
            float previousTime, nextTime, difference;
            GetPreviousValue(overallTime, out previousKeys, out previousTime);
            GetNextValue(overallTime, out nextKeys, out nextTime, out difference);

            T interpolatedKeys = interpolatedKeysOb as T;
            interpolatedKeys.ClearKeys();

            if (previousKeys == null)
                return;

            if (m_interpolator2 != null)
                interpolatedKeys.Interpolator = m_interpolator2;

            for (int i = 0; i < previousKeys.GetKeysCount(); i++)
            {
                float key; V value;
                previousKeys.GetKey(i, out key, out value);

                V prevValue, nextValue;
                previousKeys.GetInterpolatedValue<V>(key, out prevValue);
                nextKeys.GetInterpolatedValue<V>(key, out nextValue);

                V interpolatedValue = prevValue;

                if (nextTime != previousTime)
                    interpolatedKeys.Interpolator(ref prevValue, ref nextValue, (overallTime - previousTime) * difference, out interpolatedValue);

                ApplyVariance(ref interpolatedValue, ref variance, multiplier, out interpolatedValue);

                interpolatedKeys.AddKey(key, interpolatedValue);
            }
        }

        public virtual void ApplyVariance(ref V interpolatedValue, ref W variance, float multiplier, out V value)
        {
            System.Diagnostics.Debug.Assert(false);
            value = default(V);
        }

        public IMyAnimatedProperty CreateEmptyKeys()
        {
            return new T();
        }

        public override void SerializeValue(XmlWriter writer, object value)
        {
            IMyAnimatedProperty prop = value as IMyAnimatedProperty;
            prop.Serialize(writer);
        }

        public override IMyConstProperty Duplicate()
        {
            System.Diagnostics.Debug.Assert(false);
            return null;
        }

        protected override void Duplicate(IMyConstProperty targetProp)
        {
            MyAnimatedProperty2D<T, V, W> animatedTargetProp = targetProp as MyAnimatedProperty2D<T, V, W>;
            System.Diagnostics.Debug.Assert(animatedTargetProp != null);

            animatedTargetProp.Interpolator = Interpolator;
            animatedTargetProp.m_interpolator2 = m_interpolator2;

            animatedTargetProp.ClearKeys();

            foreach (ValueHolder key in m_keys)
            {
                animatedTargetProp.AddKey(key.Duplicate());
            }
        }

        public override void DeserializeFromObjectBuilder(GenerationProperty property)
        {
            m_name = property.Name;

            m_keys.Clear();

            foreach (var key in property.Keys)
            {
                T value = new T();
                value.DeserializeFromObjectBuilder_Animation(key.Value2D, property.Type);

                AddKey<T>(key.Time, value);
            }
        }
    }

    #endregion

    #region Derived 2D animation properties

    public class MyAnimatedProperty2DFloat : MyAnimatedProperty2D<MyAnimatedPropertyFloat, float, float>
    {
        public MyAnimatedProperty2DFloat(string name)
            : this(name, null)
        { }

        public MyAnimatedProperty2DFloat(string name, MyAnimatedProperty<float>.InterpolatorDelegate interpolator)
            : base(name, interpolator)
        {
        }

        public override string ValueType
        {
            get { return "Float"; }
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            MyAnimatedPropertyFloat prop = new MyAnimatedPropertyFloat(this.Name, false, m_interpolator2);
            prop.Deserialize(reader);
            value = prop;
        }

        public override IMyConstProperty Duplicate()
        {
            MyAnimatedProperty2DFloat prop = new MyAnimatedProperty2DFloat(Name);
            Duplicate(prop);
            return prop;
        }

        public override void ApplyVariance(ref float interpolatedValue, ref float variance, float multiplier, out float value)
        {
            if ((variance != 0) || (multiplier != 1))
            {
                interpolatedValue = MyUtils.GetRandomFloat(interpolatedValue - variance, interpolatedValue + variance) * multiplier;
            }

            value = interpolatedValue;
        }
    }

    public class MyAnimatedProperty2DInt : MyAnimatedProperty2D<MyAnimatedPropertyInt, int, int>
    {
        public MyAnimatedProperty2DInt(string name)
            : this(name, null)
        { }

        public MyAnimatedProperty2DInt(string name, MyAnimatedProperty<int>.InterpolatorDelegate interpolator)
            : base(name, interpolator)
        {
        }

        public override string ValueType
        {
            get { return "Int"; }
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            MyAnimatedPropertyInt prop = new MyAnimatedPropertyInt(this.Name, m_interpolator2);
            prop.Deserialize(reader);
            value = prop;
        }

        public override IMyConstProperty Duplicate()
        {
            MyAnimatedProperty2DInt prop = new MyAnimatedProperty2DInt(Name);
            Duplicate(prop);
            return prop;
        }

        public override void ApplyVariance(ref int interpolatedValue, ref int variance, float multiplier, out int value)
        {
            if ((variance != 0) || (multiplier != 1))
            {
                interpolatedValue = (int)(MyUtils.GetRandomInt(interpolatedValue - variance, interpolatedValue + variance) * multiplier);
            }

            value = interpolatedValue;
        }
    }

    public class MyAnimatedProperty2DEnum : MyAnimatedProperty2DInt
    {
        Type m_enumType;
        List<string> m_enumStrings;

        public MyAnimatedProperty2DEnum(string name)
            : this(name, null, null)
        { }

        public MyAnimatedProperty2DEnum(string name, Type enumType, List<string> enumStrings)
            : this(name, null, enumType, enumStrings)
        { }

        public override string BaseValueType
        {
            get { return "Enum"; }
        }

        public MyAnimatedProperty2DEnum(string name, MyAnimatedProperty<int>.InterpolatorDelegate interpolator, Type enumType, List<string> enumStrings)
            : base(name, interpolator)
        {
            m_enumType = enumType;
            m_enumStrings = enumStrings;
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            MyAnimatedPropertyInt prop = new MyAnimatedPropertyInt(this.Name, m_interpolator2);
            prop.Deserialize(reader);
            value = prop;
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
            MyAnimatedProperty2DEnum prop = new MyAnimatedProperty2DEnum(Name);
            Duplicate(prop);
            prop.m_enumType = m_enumType;
            prop.m_enumStrings = m_enumStrings;
            return prop;
        }
    }


    public class MyAnimatedProperty2DVector3 : MyAnimatedProperty2D<MyAnimatedPropertyVector3, Vector3, Vector3>
    {
        public MyAnimatedProperty2DVector3(string name)
            : this(name, null)
        { }

        public MyAnimatedProperty2DVector3(string name, MyAnimatedProperty<Vector3>.InterpolatorDelegate interpolator)
            : base(name, interpolator)
        {
        }

        public override string ValueType
        {
            get { return "Vector3"; }
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            MyAnimatedPropertyVector3 prop = new MyAnimatedPropertyVector3(this.Name, false, m_interpolator2);
            prop.Deserialize(reader);
            value = prop;
        }

        public override IMyConstProperty Duplicate()
        {
            MyAnimatedProperty2DVector3 prop = new MyAnimatedProperty2DVector3(Name);
            Duplicate(prop);
            return prop;
        }

        public override void ApplyVariance(ref Vector3 interpolatedValue, ref Vector3 variance, float multiplier, out Vector3 value)
        {
            if ((variance != Vector3.Zero) || (multiplier != 1))
            {
                value.X = MyUtils.GetRandomFloat(interpolatedValue.X - variance.X, interpolatedValue.X + variance.X) * multiplier;
                value.Y = MyUtils.GetRandomFloat(interpolatedValue.Y - variance.Y, interpolatedValue.Y + variance.Y) * multiplier;
                value.Z = MyUtils.GetRandomFloat(interpolatedValue.Z - variance.Z, interpolatedValue.Z + variance.Z) * multiplier;
            }

            value = interpolatedValue;
        }
    }

    public class MyAnimatedProperty2DVector4 : MyAnimatedProperty2D<MyAnimatedPropertyVector4, Vector4, float>
    {
        public MyAnimatedProperty2DVector4(string name)
            : this(name, null)
        { }

        public MyAnimatedProperty2DVector4(string name, MyAnimatedProperty<Vector4>.InterpolatorDelegate interpolator)
            : base(name, null)
        {
        }

        public override string ValueType
        {
            get { return "Vector4"; }
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            MyAnimatedPropertyVector4 prop = new MyAnimatedPropertyVector4(this.Name, m_interpolator2);
            prop.Deserialize(reader);
            value = prop;
        }

        public override IMyConstProperty Duplicate()
        {
            MyAnimatedProperty2DVector4 prop = new MyAnimatedProperty2DVector4(Name);
            Duplicate(prop);
            return prop;
        }

        public override void ApplyVariance(ref Vector4 interpolatedValue, ref float variance, float multiplier, out Vector4 value)
        {
            float rnd = MyUtils.GetRandomFloat(1 - variance, 1 + variance);

            value.X = interpolatedValue.X * rnd;
            value.Y = interpolatedValue.Y * rnd;
            value.Z = interpolatedValue.Z * rnd;
            value.W = interpolatedValue.W;
            //value.W = interpolatedValue.W * rnd;
            value.X = MathHelper.Clamp(value.X, 0, 1);
            value.Y = MathHelper.Clamp(value.Y, 0, 1);
            value.Z = MathHelper.Clamp(value.Z, 0, 1);
            //MathHelper.Clamp(value.W, 0, 1);
        }
    }

    #endregion
}

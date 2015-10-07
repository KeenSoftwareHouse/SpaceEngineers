using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Library.Collections;
using VRage.Serialization;

namespace VRage
{
    public delegate bool SyncValidate<T>(T newValue);

    public abstract class SyncBase : IBitSerializable
    {
        public readonly int Id;
        public readonly Type ValueType;
        public readonly MySerializeInfo SerializeInfo;

        /// <summary>
        /// ValueChanged event is raised when value is set locally (settings Value property) or remotely (through deserialization).
        /// When validation fails, value is not changed and ValueChanged is not raised.
        /// </summary>
        public event Action<SyncBase> ValueChanged;

        public SyncBase(Type valueType, int id, MySerializeInfo serializeInfo)
        {
            ValueType = valueType;
            Id = id;
            SerializeInfo = serializeInfo;
        }

        protected void RaiseValueChanged()
        {
            var handler = ValueChanged;
            if (handler != null) handler(this);
        }

        public abstract bool Serialize(BitStream stream, bool validate);

        public static implicit operator BitReaderWriter(SyncBase sync)
        {
            return new BitReaderWriter(sync);
        }
    }

    public class Sync<T> : SyncBase
    {
        public static readonly MySerializer<T> TypeSerializer = MyFactory.GetSerializer<T>();

        private T m_value;

        public T Value
        {
            get { return m_value; }
            set { SetValue(ref value, false); }
        }

        /// <summary>
        /// Validate handler is raised on server after deserialization.
        /// </summary>
        public SyncValidate<T> Validate;

        public Sync(int id, MySerializeInfo serializeInfo)
            : base(typeof(T), id, serializeInfo)
        {
        }

        bool IsValid(ref T value)
        {
            var handler = Validate;
            return handler == null || handler(value);
        }

        bool SetValue(ref T newValue, bool validate)
        {
            if (TypeSerializer.Equals(ref m_value, ref newValue))
            {
                return true;
            }
            else if (!validate || IsValid(ref newValue))
            {
                m_value = newValue;
                RaiseValueChanged();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Validates the value and sets it (when valid).
        /// </summary>
        public void ValidateAndSet(T newValue)
        {
            SetValue(ref newValue, true);
        }

        public override bool Serialize(BitStream stream, bool validate)
        {
            if (stream.Reading)
            {
                T newValue;
                MySerializer.CreateAndRead(stream, out newValue, SerializeInfo);
                return SetValue(ref newValue, validate);
            }
            else
            {
                MySerializer.Write(stream, ref m_value, SerializeInfo);
                return true;
            }
        }

        public static implicit operator T(Sync<T> sync)
        {
            return sync.Value;
        }
    }
}

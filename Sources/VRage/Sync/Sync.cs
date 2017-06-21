using System;
using VRage.Library.Collections;
using VRage.Serialization;

namespace VRage.Sync
{
    public delegate bool SyncValidate<T>(T newValue);

    public abstract class SyncBase : IBitSerializable
    {
        public readonly int Id;
        public readonly Type ValueType;
#if !XB1 // !XB1_SYNC_NOREFLECTION
        public readonly MySerializeInfo SerializeInfo;
#endif // XB1

        /// <summary>
        /// ValueChanged event is raised when value is set locally (settings Value property) or remotely (through deserialization).
        /// When validation fails, value is not changed and ValueChanged is not raised.
        /// </summary>
        public event Action<SyncBase> ValueChanged;

#if !XB1 // !XB1_SYNC_NOREFLECTION
        public SyncBase(Type valueType, int id, MySerializeInfo serializeInfo)
        {
            ValueType = valueType;
            Id = id;
            SerializeInfo = serializeInfo;
        }
#else // XB1
        public SyncBase(Type valueType, int id)
        {
            ValueType = valueType;
            Id = id;
        }
#endif // XB1

        protected void RaiseValueChanged()
        {
            var handler = ValueChanged;
            if (handler != null) handler(this);
        }

        public abstract SyncBase Clone(int newId);
        public abstract bool Serialize(BitStream stream, bool validate);

        protected static void CopyValueChanged(SyncBase from, SyncBase to)
        {
            to.ValueChanged = from.ValueChanged;
        }

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

#if !XB1 // !XB1_SYNC_NOREFLECTION
        public Sync(int id, MySerializeInfo serializeInfo)
            : base(typeof(T), id, serializeInfo)
        {
        }
#else // XB1
        public Sync(int id)
            : base(typeof(T), id)
        {
        }
#endif // XB1

        public override string ToString()
        {
            return Value.ToString();
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

        public override SyncBase Clone(int newId)
        {
#if !XB1 // !XB1_SYNC_NOREFLECTION
            var sync = new Sync<T>(newId, SerializeInfo);
#else // XB1
            var sync = new Sync<T>(newId);
#endif // XB1
            CopyValueChanged(this, sync);
            sync.Validate = Validate;
            sync.m_value = m_value;
            return sync;
        }

        public override bool Serialize(BitStream stream, bool validate)
        {
#if !XB1 // !XB1_SYNC_NOREFLECTION
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
#else // XB1
            System.Diagnostics.Debug.Assert(false);
            return false;
#endif // XB1
        }

        public static implicit operator T(Sync<T> sync)
        {
            return sync.Value;
        }
    }
}

using System;
using System.Globalization;
using System.Reflection;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.ModAPI.Ingame
{
    public struct TerminalActionParameter
    {
        /// <summary>
        /// Gets an empty parameter.
        /// </summary>
        public static readonly TerminalActionParameter Empty = new TerminalActionParameter();

        static Type ToType(TypeCode code)
        {
            switch (code)
            {
                case TypeCode.Boolean:
                    return typeof(bool);

                case TypeCode.Byte:
                    return typeof(byte);

                case TypeCode.Char:
                    return typeof(char);

                case TypeCode.DateTime:
                    return typeof(DateTime);

                case TypeCode.Decimal:
                    return typeof(decimal);

                case TypeCode.Double:
                    return typeof(double);

                case TypeCode.Int16:
                    return typeof(short);

                case TypeCode.Int32:
                    return typeof(int);

                case TypeCode.Int64:
                    return typeof(long);

                case TypeCode.SByte:
                    return typeof(sbyte);

                case TypeCode.Single:
                    return typeof(float);

                case TypeCode.String:
                    return typeof(string);

                case TypeCode.UInt16:
                    return typeof(ushort);

                case TypeCode.UInt32:
                    return typeof(uint);

                case TypeCode.UInt64:
                    return typeof(ulong);
            }

            return null;
        }
        
        /// <summary>
        /// Creates a <see cref="TerminalActionParameter"/> from a serialized value in a string and a type code.
        /// </summary>
        /// <param name="serializedValue"></param>
        /// <param name="typeCode"></param>
        /// <returns></returns>
        public static TerminalActionParameter Deserialize(string serializedValue, TypeCode typeCode)
        {
            AssertTypeCodeValidity(typeCode);
            var targetType = ToType(typeCode);
            if (targetType == null)
                return Empty;
            var value = Convert.ChangeType(serializedValue, typeCode, CultureInfo.InvariantCulture);
            return new TerminalActionParameter(typeCode, value);
        }
        
        /// <summary>
        /// Creates a <see cref="TerminalActionParameter"/> from the given value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TerminalActionParameter Get(object value)
        {
            if (value == null)
                return Empty;
            var typeCode = Type.GetTypeCode(value.GetType());
            AssertTypeCodeValidity(typeCode);
            return new TerminalActionParameter(typeCode, value);
        }

        private static void AssertTypeCodeValidity(TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Empty:
                case TypeCode.Object:
                case TypeCode.DBNull:
                    throw new ArgumentException("Only primitive types are allowed for action parameters", "value");
            }
        }

        public readonly TypeCode TypeCode;
        public readonly object Value;

        private TerminalActionParameter(TypeCode typeCode, object value)
        {
            TypeCode = typeCode;
            Value = value;
        }

        public bool IsEmpty { get { return this.TypeCode == TypeCode.Empty; } }
    
        public MyObjectBuilder_ToolbarItemActionParameter GetObjectBuilder()
        {
            var itemObjectBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemActionParameter>();
            itemObjectBuilder.TypeCode = TypeCode;
            itemObjectBuilder.Value = (this.Value == null) ? null : Convert.ToString(this.Value, CultureInfo.InvariantCulture);
            return itemObjectBuilder;
        }
    }
}
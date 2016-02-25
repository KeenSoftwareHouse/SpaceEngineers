using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;

namespace VRage.Generics.StateMachine
{
    /// <summary>
    /// Implementation of generic condition. Immutable class, once set, its parameters cant be changed.
    /// </summary>
    public class MyCondition<T> : IMyCondition where T: struct
    {
        public enum MyOperation
        {
            AlwaysFalse,
            AlwaysTrue,
            NotEqual,
            Less,
            LessOrEqual,
            Equal,
            GreaterOrEqual,
            Greater
        }

        // variable storage
        private readonly IMyVariableStorage<T> m_storage;
        // comparison operation
        private readonly MyOperation m_operation;
        // name of the variable on the left side
        private readonly MyStringId m_leftSideStorage;
        // name of the variable on the right side
        private readonly MyStringId m_rightSideStorage;
        // value on the left side, taken in account if leftSideStorage is null
        private readonly T m_leftSideValue;
        // value on the right side, taken in account if leftSideStorage is null
        private readonly T m_rightSideValue;

        public MyCondition(IMyVariableStorage<T> storage, MyOperation operation, string leftSideStorage, string rightSideStorage)
        {
            Debug.Assert(storage != null, "Variable storage must not be null.");
            m_storage = storage;
            m_operation = operation;
            m_leftSideStorage = MyStringId.GetOrCompute(leftSideStorage);
            m_rightSideStorage = MyStringId.GetOrCompute(rightSideStorage);
        }

        public MyCondition(IMyVariableStorage<T> storage, MyOperation operation, string leftSideStorage, T rightSideValue)
        {
            Debug.Assert(storage != null, "Variable storage must not be null.");
            m_storage = storage;
            m_operation = operation;
            m_leftSideStorage = MyStringId.GetOrCompute(leftSideStorage);
            m_rightSideStorage = MyStringId.NullOrEmpty;
            m_rightSideValue = rightSideValue;
        }

        public MyCondition(IMyVariableStorage<T> storage, MyOperation operation, T leftSideValue, string rightSideStorage)
        {
            Debug.Assert(storage != null, "Variable storage must not be null.");
            m_storage = storage;
            m_operation = operation;
            m_leftSideStorage = MyStringId.NullOrEmpty;
            m_rightSideStorage = MyStringId.GetOrCompute(rightSideStorage);
            m_leftSideValue = leftSideValue;
        }

        public MyCondition(IMyVariableStorage<T> storage, MyOperation operation, T leftSideValue, T rightSideValue)
        {
            // Debug.Assert(storage != null, "Variable storage must not be null."); // the storage can be null here
            m_storage = storage;
            m_operation = operation;
            m_leftSideStorage = MyStringId.NullOrEmpty;
            m_rightSideStorage = MyStringId.NullOrEmpty;
            m_leftSideValue = leftSideValue;
            m_rightSideValue = rightSideValue;
        }

        // Compare values and return result.
        public bool Evaluate()
        {
            T lhs;
            T rhs;
            // fetch values
            if (m_leftSideStorage != MyStringId.NullOrEmpty)
            {
                if (!m_storage.GetValue(m_leftSideStorage, out lhs))
                    return false;
            }
            else
            {
                lhs = m_leftSideValue;
            }

            if (m_rightSideStorage != MyStringId.NullOrEmpty)
            {
                if (!m_storage.GetValue(m_rightSideStorage, out rhs))
                    return false;
            }
            else
            {
                rhs = m_rightSideValue;
            }

            // compare them
            int comparisonResult = Comparer<T>.Default.Compare(lhs, rhs);
            switch (m_operation)
            {
                case MyOperation.Less:
                    return comparisonResult < 0;
                case MyOperation.LessOrEqual:
                    return comparisonResult <= 0;
                case MyOperation.Equal:
                    return comparisonResult == 0;
                case MyOperation.GreaterOrEqual:
                    return comparisonResult >= 0;
                case MyOperation.Greater:
                    return comparisonResult > 0;
                case MyOperation.NotEqual:
                    return comparisonResult != 0;
                case MyOperation.AlwaysTrue:
                    return true;
                case MyOperation.AlwaysFalse:
                    return false;
                default:
                    return false;
            }
        }

        // Implementation of ToString - for better debugging in VS. :)
        public override string ToString()
        {
            if (m_operation == MyOperation.AlwaysTrue)
                return "true";
            if (m_operation == MyOperation.AlwaysFalse)
                return "false";

            StringBuilder strBuilder = new StringBuilder(128);
            // fetch values
            if (m_leftSideStorage != MyStringId.NullOrEmpty)
            {
                strBuilder.Append(m_leftSideStorage.ToString());
            }
            else
            {
                strBuilder.Append(m_leftSideValue);
            }

            strBuilder.Append(" ");
            switch (m_operation)
            {
                case MyOperation.Less:
                    strBuilder.Append("<");
                    break;
                case MyOperation.LessOrEqual:
                    strBuilder.Append("<=");
                    break;
                case MyOperation.Equal:
                    strBuilder.Append("==");
                    break;
                case MyOperation.GreaterOrEqual:
                    strBuilder.Append(">=");
                    break;
                case MyOperation.Greater:
                    strBuilder.Append(">");
                    break;
                case MyOperation.NotEqual:
                    strBuilder.Append("!=");
                    break;
                default:
                    strBuilder.Append("???");
                    break;
            }
            strBuilder.Append(" ");

            if (m_rightSideStorage != MyStringId.NullOrEmpty)
            {
                strBuilder.Append(m_rightSideStorage.ToString());
            }
            else
            {
                strBuilder.Append(m_rightSideValue);
            }
            return strBuilder.ToString();
        }
    }
}

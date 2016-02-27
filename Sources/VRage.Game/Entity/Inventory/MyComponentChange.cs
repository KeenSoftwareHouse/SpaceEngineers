using System.Diagnostics;

namespace VRage.Game.Entity
{
    public struct MyComponentChange
    {
        private const int OPERATION_REMOVAL = 0;
        private const int OPERATION_ADDITION = 1;
        private const int OPERATION_CHANGE = 2;

        private byte m_operation;
        public bool IsRemoval()
        {
            return m_operation == OPERATION_REMOVAL;
        }

        public bool IsAddition()
        {
            return m_operation == OPERATION_ADDITION;
        }

        public bool IsChange()
        {
            return m_operation == OPERATION_CHANGE;
        }

        private MyDefinitionId m_toRemove;
        private MyDefinitionId m_toAdd;

        public MyDefinitionId ToRemove
        {
            get
            {
                Debug.Assert(m_operation == OPERATION_REMOVAL || m_operation == OPERATION_CHANGE);
                return m_toRemove;
            }
            set
            {
                m_toRemove = value;
            }
        }

        public MyDefinitionId ToAdd
        {
            get
            {
                Debug.Assert(m_operation == OPERATION_ADDITION || m_operation == OPERATION_CHANGE);
                return m_toAdd;
            }
            set
            {
                m_toAdd = value;
            }
        }

        public int Amount;              // How many times to apply this change (i.e. how many instances of the component to change)

        public static MyComponentChange CreateRemoval(MyDefinitionId toRemove, int amount)
        {
            return new MyComponentChange() { ToRemove = toRemove, Amount = amount, m_operation = OPERATION_REMOVAL };
        }

        public static MyComponentChange CreateAddition(MyDefinitionId toAdd, int amount)
        {
            return new MyComponentChange() { ToAdd = toAdd, Amount = amount, m_operation = OPERATION_ADDITION };
        }

        public static MyComponentChange CreateChange(MyDefinitionId toRemove, MyDefinitionId toAdd, int amount)
        {
            return new MyComponentChange() { ToRemove = toRemove, ToAdd = toAdd, Amount = amount, m_operation = OPERATION_CHANGE };
        }
    }
}

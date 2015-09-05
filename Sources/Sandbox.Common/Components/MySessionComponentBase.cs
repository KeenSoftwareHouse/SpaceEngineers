using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.ObjectBuilders;

namespace Sandbox.Common
{
    [Flags]
    public enum MyUpdateOrder
    {
        BeforeSimulation = 0x01,
        Simulation = 0x02,
        AfterSimulation = 0x04,
        NoUpdate = 0x08,
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MySessionComponentDescriptor : System.Attribute
    {
        public MyUpdateOrder UpdateOrder;

        /// <summary>
        /// Lower Priority is loaded before higher Priority
        /// </summary>
        public int Priority;

        public MyObjectBuilderType ObjectBuilderType;

        public MySessionComponentDescriptor(MyUpdateOrder updateOrder)
            : this(updateOrder, 1000)
        {
        }

        public MySessionComponentDescriptor(MyUpdateOrder updateOrder, int priority)
            : this(updateOrder, priority, null)
        {
        }

        public MySessionComponentDescriptor(MyUpdateOrder updateOrder, int priority, Type type)
        {
            UpdateOrder = updateOrder;
            Priority = priority;
            ObjectBuilderType = type;
        }
    }

    public abstract class MySessionComponentBase : IMyUserInputComponent
    {
        public readonly string DebugName;
        public readonly int Priority;
        public readonly MyUpdateOrder UpdateOrder;
        public readonly MyObjectBuilderType ObjectBuilderType;

        virtual public bool UpdatedBeforeInit()
        {
            return false;
        }
        public bool Loaded;

        public MySessionComponentBase()
        {
            var type = GetType();
            var attr = (MySessionComponentDescriptor)Attribute.GetCustomAttribute(type, typeof(MySessionComponentDescriptor), false);

            DebugName = type.Name;
            Priority = attr.Priority;
            UpdateOrder = attr.UpdateOrder;
            ObjectBuilderType = attr.ObjectBuilderType;

            if (ObjectBuilderType != MyObjectBuilderType.Invalid)
                MySessionComponentMapping.Map(GetType(), ObjectBuilderType);
        }

        public virtual Type[] Dependencies
        {
            get { return Type.EmptyTypes; }
        }

        /// <summary>
        /// Indicates whether a session component should be used in current configuration.
        /// Example: MyDestructionData component returns true only when game uses Havok Destruction
        /// </summary>
        public virtual bool IsRequiredByGame
        {
            get { return true; }
        }

        public virtual void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
        }

        public virtual MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            if (ObjectBuilderType != MyObjectBuilderType.Invalid)
                return Activator.CreateInstance(ObjectBuilderType) as MyObjectBuilder_SessionComponent;
            else
                return null;
        }

        public virtual void LoadData()
        {           
        }

        protected virtual void UnloadData()
        {
        }

        public void AfterLoadData()
        {
            Loaded = true;
        }

        public void UnloadDataConditional()
        {
            if (Loaded)
            {
                UnloadData();
                Loaded = false;
            }
        }

        public virtual void SaveData()
        {
        }

        public virtual void BeforeStart()
        {
        }

        public virtual void UpdateBeforeSimulation()
        {
        }

        public virtual void Simulate()
        {
        }

        public virtual void UpdateAfterSimulation()
        {
        }

        public virtual void UpdatingStopped()
        {
        }

        public virtual void Draw()
        {
        }

        public virtual void HandleInput()
        {
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}

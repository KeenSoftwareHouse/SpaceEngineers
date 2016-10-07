﻿using System;
using System.Diagnostics;
using VRage.ObjectBuilders;

namespace VRage.Game.Components
{
    [Flags]
    public enum MyUpdateOrder
    {
        BeforeSimulation = 0x01,
        Simulation = 0x02,
        AfterSimulation = 0x04,
        NoUpdate = 0x0,
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

        // So the component may override or extend the functionality of another component.
        public Type ComponentType;

        public MySessionComponentDescriptor(MyUpdateOrder updateOrder)
            : this(updateOrder, 1000)
        {
        }

        public MySessionComponentDescriptor(MyUpdateOrder updateOrder, int priority)
            : this(updateOrder, priority, null)
        {
        }

        public MySessionComponentDescriptor(MyUpdateOrder updateOrder, int priority, Type obType)
        {
            UpdateOrder = updateOrder;
            Priority = priority;
            ObjectBuilderType = obType;

            if (obType != null)
            {
                Debug.Assert(typeof(MyObjectBuilder_SessionComponent).IsAssignableFrom(obType), obType.FullName);

                if (!typeof(MyObjectBuilder_SessionComponent).IsAssignableFrom(obType))
                {
                    ObjectBuilderType = MyObjectBuilderType.Invalid;
                }
            }
        }
    }

    public abstract class MySessionComponentBase : Interfaces.IMyUserInputComponent
    {
        public readonly string DebugName;
        public readonly int Priority;
        public MyUpdateOrder UpdateOrder { get; set; }
        public readonly MyObjectBuilderType ObjectBuilderType;
        public Type ComponentType;

        virtual public bool UpdatedBeforeInit()
        {
            return false;
        }
        public bool Loaded;
        private bool m_initialized;

        public bool Initialized
        {
            get { return m_initialized; }
        }

        public MySessionComponentBase()
        {
            var type = GetType();
            var attr = (MySessionComponentDescriptor)Attribute.GetCustomAttribute(type, typeof(MySessionComponentDescriptor), false);

            DebugName = type.Name;
            Priority = attr.Priority;
            UpdateOrder = attr.UpdateOrder;
            ObjectBuilderType = attr.ObjectBuilderType;
            ComponentType = attr.ComponentType;

            if (ObjectBuilderType != MyObjectBuilderType.Invalid)
                MySessionComponentMapping.Map(GetType(), ObjectBuilderType);

            if (ComponentType == null)
                ComponentType = GetType();
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
            m_initialized = true;

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
            return ComponentType.ToString();
        }
    }
}

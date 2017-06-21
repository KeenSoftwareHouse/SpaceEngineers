using System;
using System.Diagnostics;
using VRage.Game.Components.Session;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace VRage.Game.Components
{
    [Flags]
    public enum MyUpdateOrder
    {
        BeforeSimulation = 1 << 0,
        Simulation = 1 << 1,
        AfterSimulation = 1 << 2,
        NoUpdate = 0,
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

        public MySessionComponentDescriptor(MyUpdateOrder updateOrder, int priority, Type obType, Type registrationType = null)
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

            ComponentType = registrationType;
        }
    }

    public abstract class MySessionComponentBase : Interfaces.IMyUserInputComponent
    {
        public readonly string DebugName;
        public readonly int Priority;
        public MyUpdateOrder UpdateOrder { get; private set; }
        public MyObjectBuilderType ObjectBuilderType { get; private set; }
        public readonly Type ComponentType;

        public IMySession Session;

        public virtual bool UpdatedBeforeInit()
        {
            return false;
        }
        public bool Loaded { get; private set; }
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
            else if (ComponentType == GetType() || ComponentType.IsSubclassOf(GetType()))
            {
                MyLog.Default.Error("Component {0} tries to register itself as a component it does not inherit from ({1}). Ignoring...", GetType(), ComponentType);
                ComponentType = GetType();
            }
        }

        public MyDefinitionId? Definition { get; set; }

        public void SetUpdateOrder(MyUpdateOrder order)
        {
            Session.SetComponentUpdateOrder(this, order);
            UpdateOrder = order;
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
            get { return false; }
        }

        public virtual void InitFromDefinition(MySessionComponentDefinition definition)
        {
        }

        public virtual void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            m_initialized = true;
            if (sessionComponent != null && sessionComponent.Definition.HasValue)
            {
                Definition = sessionComponent.Definition;
            }

            if (Definition.HasValue)
            {
                var def = MyDefinitionManagerBase.Static.GetDefinition<MySessionComponentDefinition>(Definition.Value);

                if (def == null)
                    MyLog.Default.Warning("Missing definition {0} : for session component {1}", Definition, GetType().Name);
                else
                    InitFromDefinition(def);
            }
        }

        public virtual MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            if (ObjectBuilderType != MyObjectBuilderType.Invalid)
            {
                var ob = Activator.CreateInstance(ObjectBuilderType) as MyObjectBuilder_SessionComponent;

                ob.Definition = Definition;

                return ob;
            }
            return null;
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

        public virtual void LoadData()
        {
        }

        protected virtual void UnloadData()
        {
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
            return DebugName;
        }
    }
}

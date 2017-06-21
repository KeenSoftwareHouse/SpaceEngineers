using Sandbox.Game.Components;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.ComponentSystem;

namespace Sandbox.Game.Entities.Inventory
{
    [MyComponentBuilder(typeof(MyObjectBuilder_TriggerAggregate))]
    //[StaticEventOwner]
    public class MyTriggerAggregate : MyEntityComponentBase, IMyComponentAggregate
    {
        private int m_triggerCount;
        public event Action<MyTriggerAggregate, int> OnTriggerCountChanged;
        public override string ComponentTypeDebugString { get { return "TriggerAggregate"; } }

        /// <summary>
        /// Returns number of inventories of MyInventory type contained in this aggregate
        /// </summary>
        public int TriggerCount
        {
            get
            {
                return m_triggerCount;
            }
            private set
            {
                if (m_triggerCount != value)
                {
                    int change = value - m_triggerCount;
                    m_triggerCount = value;
                    var handler = OnTriggerCountChanged;
                    if (handler != null)
                    {
                        OnTriggerCountChanged(this, change);
                    }
                }
            }
        }
        public override bool IsSerialized()
        {
            return true;
        }

        private MyAggregateComponentList m_children = new MyAggregateComponentList();
        public MyAggregateComponentList ChildList
        {
            get { return m_children; }
        }

        public void AfterComponentAdd(MyComponentBase component)
        {
            var trigger = component as MyTriggerComponent;
            if (component is MyTriggerComponent)
            {
                TriggerCount++;
            }
            else if (component is MyTriggerAggregate)
            {
                (component as MyTriggerAggregate).OnTriggerCountChanged += OnChildAggregateCountChanged;
                TriggerCount += (component as MyTriggerAggregate).TriggerCount;
            }
            /*if (OnAfterComponentAdd != null)
            {
                OnAfterComponentAdd(this, trigger);
            }*/
        }

        private void OnChildAggregateCountChanged(MyTriggerAggregate obj, int change)
        {
            TriggerCount += change;
        }

        public void BeforeComponentRemove(MyComponentBase component)
        {
            var trigger = component as MyTriggerComponent;
            /*if (OnBeforeComponentRemove != null)
            {
                OnBeforeComponentRemove(this, trigger);
            }*/
            if (component is MyTriggerComponent)
            {
                TriggerCount--;
            }
            else if (component is MyTriggerAggregate)
            {
                (component as MyTriggerAggregate).OnTriggerCountChanged -= OnChildAggregateCountChanged;
                TriggerCount -= (component as MyTriggerAggregate).TriggerCount;
            }
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var ob = base.Serialize() as MyObjectBuilder_TriggerAggregate;

            var reader = m_children.Reader;
            if (reader.Count > 0)
            {
                ob.AreaTriggers = new List<MyObjectBuilder_TriggerBase>(reader.Count);
                foreach (var trigger in reader)
                {
                    MyObjectBuilder_TriggerBase triggerOb = trigger.Serialize() as MyObjectBuilder_TriggerBase;
                    if (triggerOb != null)
                        ob.AreaTriggers.Add(triggerOb);
                }
            }

            return ob;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);

            var ob = builder as MyObjectBuilder_TriggerAggregate;

            if (ob != null && ob.AreaTriggers != null)
            {
                foreach (var obTrigger in ob.AreaTriggers)
                {
                    var comp = MyComponentFactory.CreateInstanceByTypeId(obTrigger.TypeId);
                    comp.Deserialize(obTrigger);
                    this.AddComponent(comp);
                }
            }
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            foreach (var componentBase in ChildList.Reader)
            {
                componentBase.OnAddedToScene();
            }
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            foreach (var componentBase in ChildList.Reader)
            {
                componentBase.OnRemovedFromScene();
            }
        }
    }
}

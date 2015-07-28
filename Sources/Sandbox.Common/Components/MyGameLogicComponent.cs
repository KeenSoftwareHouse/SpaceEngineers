using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

//this must be in sandbox.common namespace  when assembly is sanbox.common  becaose of script checking.
namespace Sandbox.Common.Components
{
    public abstract class MyGameLogicComponent : MyEntityComponentBase
    {
        public MyEntityUpdateEnum NeedsUpdate
        {
            get
            {
                MyEntityUpdateEnum needsUpdate = MyEntityUpdateEnum.NONE;

                if ((Container.Entity.Flags & EntityFlags.NeedsUpdate) != 0)
                    needsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

                if ((Container.Entity.Flags & EntityFlags.NeedsUpdate10) != 0)
                    needsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

                if ((Container.Entity.Flags & EntityFlags.NeedsUpdate100) != 0)
                    needsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

                if ((Container.Entity.Flags & EntityFlags.NeedsUpdateBeforeNextFrame) != 0)
                    needsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

                return needsUpdate;
            }
            set
            {
                bool hasChanged = value != NeedsUpdate;

                if (hasChanged)
                {
                    if (Container.Entity.InScene)
                        Sandbox.ModAPI.MyAPIGateway.Entities.UnregisterForUpdate(Container.Entity);

                    Container.Entity.Flags &= ~EntityFlags.NeedsUpdateBeforeNextFrame;
                    Container.Entity.Flags &= ~EntityFlags.NeedsUpdate;
                    Container.Entity.Flags &= ~EntityFlags.NeedsUpdate10;
                    Container.Entity.Flags &= ~EntityFlags.NeedsUpdate100;

                    if ((value & MyEntityUpdateEnum.BEFORE_NEXT_FRAME) != 0)
                        Container.Entity.Flags |= EntityFlags.NeedsUpdateBeforeNextFrame;
                    if ((value & MyEntityUpdateEnum.EACH_FRAME) != 0)
                        Container.Entity.Flags |= EntityFlags.NeedsUpdate;
                    if ((value & MyEntityUpdateEnum.EACH_10TH_FRAME) != 0)
                        Container.Entity.Flags |= EntityFlags.NeedsUpdate10;
                    if ((value & MyEntityUpdateEnum.EACH_100TH_FRAME) != 0)
                        Container.Entity.Flags |= EntityFlags.NeedsUpdate100;

                    if (Container.Entity.InScene)
                        Sandbox.ModAPI.MyAPIGateway.Entities.RegisterForUpdate(Container.Entity);
                }
            }
        }


        public bool Closed { get; protected set; }
        public bool MarkedForClose { get; protected set; }
        //Called after internal implementation
        public virtual void UpdateOnceBeforeFrame()
        {}
        public virtual void UpdateBeforeSimulation()
        {}
        public virtual void UpdateBeforeSimulation10()
        {}
        public virtual void UpdateBeforeSimulation100()
        {}
        public virtual void UpdateAfterSimulation()
        {}
        public virtual void UpdateAfterSimulation10()
        {}
        public virtual void UpdateAfterSimulation100()
        {}
        public virtual void UpdatingStopped()
        {}

        //Entities are usualy initialized from builder immediately after creation by factory
        public virtual void Init(MyObjectBuilder_EntityBase objectBuilder)
        {}
        public abstract MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false);

        //Only use for setting flags, no heavy logic or cleanup
        public virtual void MarkForClose()
        {}

        //Called before internal implementation
        //Cleanup here
        public virtual void Close()
        {}

        public override string ComponentTypeDebugString
        {
            get { return "Game Logic"; }
        }
    }
}

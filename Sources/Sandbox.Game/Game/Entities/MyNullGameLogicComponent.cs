using Sandbox.Common.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Components;
using VRage.ObjectBuilders;
using Sandbox.Common;

namespace Sandbox.Game.Entities
{
    public class MyNullGameLogicComponent : MyGameLogicComponent
    {
        public override void UpdateOnceBeforeFrame()
        {
        }

        public override void UpdateBeforeSimulation()
        {
        }

        public override void UpdateBeforeSimulation10()
        {
        }

        public override void UpdateBeforeSimulation100()
        {
        }

        public override void UpdateAfterSimulation()
        {
        }

        public override void UpdateAfterSimulation10()
        {
        }

        public override void UpdateAfterSimulation100()
        {
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
        }

        public override void MarkForClose()
        {
        }

        public override void Close()
        {
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return null;
        }
    }
}

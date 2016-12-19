using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Components;
using VRage.Profiler;
using VRageMath;

namespace Sandbox.Game.Components
{
    class MyFracturePiecePositionComponent : MyPositionComponent
    {
        protected override void UpdateChildren(object source)
        {
            return;
        }

        protected override void OnWorldPositionChanged(object source, bool updateChildren)
        {
            Debug.Assert(source != this && (Container.Entity == null || source != Container.Entity), "Recursion detected!");
            ProfilerShort.Begin("FP.Volume+InvalidateRender");
            m_worldVolumeDirty = true;
            m_worldAABBDirty = true;
            m_normalizedInvMatrixDirty = true;
            m_invScaledMatrixDirty = true;

            if (Entity.Physics != null && Entity.Physics.Enabled && Entity.Physics != source)
            {
                Entity.Physics.OnWorldPositionChanged(source);
            }

            if(Container.Entity.Render != null)
                Container.Entity.Render.InvalidateRenderObjects();
            //ProfilerShort.BeginNextBlock("FP.Prunning.Move");
            //MyGamePruningStructure.Move(Entity as MyEntity);
            ProfilerShort.End();
        }
    }
}

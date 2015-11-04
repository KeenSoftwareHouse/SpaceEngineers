using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Components;
using VRageMath;

namespace Sandbox.Game.Components
{
    class MyFracturePiecePositionComponent : MyPositionComponent
    {
        protected override void UpdateWorldVolume()
        {
            m_worldAABB.Min = m_worldMatrix.Translation;// -Vector3.One * m_localVolume.Radius;
            m_worldAABB.Max = m_worldMatrix.Translation;// +Vector3.One * m_localVolume.Radius;
            m_worldVolume.Center = m_worldMatrix.Translation;
            m_worldVolume.Radius = m_localVolume.Radius;
            var component = Container.Get<MyRenderComponentBase>();
            Debug.Assert(component != null, "Missing render component!!");
			if(component != null)
				component.InvalidateRenderObjects();
        }

        protected override void UpdateChildren(object source)
        {
            return;
        }

        protected override void OnWorldPositionChanged(object source)
        {
            Debug.Assert(source != this && (Container.Entity == null || source != Container.Entity), "Recursion detected!");
            ProfilerShort.Begin("FP.Volume+InvalidateRender");
            UpdateWorldVolume();
            //ProfilerShort.BeginNextBlock("FP.Prunning.Move");
            //MyGamePruningStructure.Move(Entity as MyEntity);
            ProfilerShort.End();
        }
    }
}

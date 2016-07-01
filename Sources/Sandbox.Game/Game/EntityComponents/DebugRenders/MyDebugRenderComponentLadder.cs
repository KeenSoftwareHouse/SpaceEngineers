using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using Sandbox.ModAPI;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Diagnostics;
using System.Threading;


using Sandbox.Graphics;
using VRage.ModAPI;

namespace Sandbox.Game.Components
{
    public class MyDebugRenderComponentLadder : MyDebugRenderComponent
    {
        IMyEntity m_ladder = null;

        public MyDebugRenderComponentLadder(IMyEntity ladder):base(ladder)
        {
            m_ladder = ladder;
        }

        public override void DebugDraw()
        {
            //return true;

            VRageRender.MyRenderProxy.DebugDrawAxis(m_ladder.PositionComp.WorldMatrix, 1, false);

            //VRageRender.MyRenderProxy.DebugDrawSphere(new Vector3(-72.47623f, 33.94679f, 44.4399f), 0.2f, Vector3.One, 1, false);

            //BoundingBox bb = new BoundingBox(new Vector3(-0.4f, -1.25f, -0.75f), new Vector3(0.4f, 1.25f, 0.75f));
            //Matrix mm = Matrix.CreateScale(bb.HalfExtents * 2);
            //mm.Translation = bb.Center;

            //mm = mm * WorldMatrix;

            //VRageRender.MyRenderProxy.DebugDrawOBB(mm, Color.Violet.ToVector3(), 0.7f, false, true);

            //b = new BoundingBox(new Vector3(-0.7f, 1.1f, -0.75f), new Vector3(0.7f, 1.25f, -1.25f));
            //m = Matrix.CreateScale(b.HalfExtents * 2);
            //m.Translation = b.Center;

            //m = m * WorldMatrix;

            base.DebugDraw();
        }
    }
}

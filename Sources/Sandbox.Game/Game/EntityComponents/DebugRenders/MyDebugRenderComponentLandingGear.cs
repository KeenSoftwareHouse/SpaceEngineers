﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities.Cube;
using VRageMath;
using VRageRender;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentLandingGear: MyDebugRenderComponent
    {
        MyLandingGear m_langingGear = null;

        public MyDebugRenderComponentLandingGear(MyLandingGear landingGear)
            : base(landingGear)
        {
            m_langingGear = landingGear;
        }

        public override void DebugDraw()
        {
            var result = true;

            if (MyDebugDrawSettings.DEBUG_DRAW_MODEL_DUMMIES)
            {
                Quaternion orientation;
                Vector3D pos;
                Vector3 halfExtents;
                foreach (var srcMatrix in m_langingGear.LockPositions)
                {
                    m_langingGear.GetBoxFromMatrix(srcMatrix, out halfExtents, out pos, out orientation);

                    var m = Matrix.CreateFromQuaternion(orientation);
                    m.Translation = pos;
                    m = Matrix.CreateScale(halfExtents * 2 * new Vector3(2.0f, 1.0f, 2.0f)) * m;
                    MyRenderProxy.DebugDrawOBB(m, Color.Yellow.ToVector3(), 1.0f, false, false);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using Sandbox.ModAPI;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Diagnostics;
using System.Threading;

using Sandbox.Game.Entities;
using Sandbox.Common.Components;
using VRage.Voxels;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentPlanet: MyDebugRenderComponent
    {
        MyPlanet m_planet = null;

        public MyDebugRenderComponentPlanet(MyPlanet voxelMap)
            : base(voxelMap)
        {
            m_planet = voxelMap;
        }

        public override bool DebugDraw()
        {
            var minCorner = m_planet.PositionLeftBottomCorner;
            if (MyDebugDrawSettings.DEBUG_DRAW_VOXEL_MAP_AABB)
            {
                m_planet.DebugDrawEnviromentSectors();
            }
            m_planet.DebugDrawPhysics();
            return true;
        }
    }
}

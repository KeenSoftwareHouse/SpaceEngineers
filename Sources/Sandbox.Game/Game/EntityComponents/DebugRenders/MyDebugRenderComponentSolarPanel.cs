using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.GameSystems.Electricity;

using Sandbox.Game.Entities.Blocks;
using VRageMath;
using VRageRender;
using Sandbox.Game.World;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Components;

namespace Sandbox.Game.Components
{
    public class MyDebugRenderComponentSolarPanel : MyDebugRenderComponent
    {
        MyTerminalBlock m_solarBlock = null;
        MySolarGameLogicComponent m_solarComponent = null;

        public MyDebugRenderComponentSolarPanel(MyTerminalBlock solarBlock):base(solarBlock)
        {
            m_solarBlock = solarBlock;

            MyGameLogicComponent logicComponent;
            if (m_solarBlock.Components.TryGet(out logicComponent))
            {
                m_solarComponent = logicComponent as MySolarGameLogicComponent;
            }

            if (m_solarComponent == null)
            {
                System.Diagnostics.Debug.Fail("No solar component was found!");
            }
        }
        public override void DebugDraw()
        {
            Matrix WorldMatrix = m_solarBlock.PositionComp.WorldMatrix;
            Matrix rot = Matrix.CreateFromDir(WorldMatrix.Forward, WorldMatrix.Up);
            float scale = WorldMatrix.Forward.Dot(Vector3.Normalize(Vector3.Transform(m_solarComponent.PanelOrientation, WorldMatrix.GetOrientation())));
            float unit = m_solarBlock.BlockDefinition.CubeSize == MyCubeSize.Large ? 2.5f : 0.5f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 pivot = WorldMatrix.Translation;
                var tmp = Vector3.Transform(WorldMatrix.Left, rot);
                pivot += ((i % 4 - 1.5f) * unit * scale * (m_solarBlock.BlockDefinition.Size.X / 4f)) * WorldMatrix.Left;
                pivot += ((i / 4 - 0.5f) * unit * scale * (m_solarBlock.BlockDefinition.Size.Y / 2f)) * WorldMatrix.Up;
                pivot += unit * scale * (m_solarBlock.BlockDefinition.Size.Z / 2f) * Vector3.Transform(m_solarComponent.PanelOrientation, rot) * m_solarComponent.PanelOffset;
                if (m_solarComponent.DebugIsPivotInSun[i])
                    MyRenderProxy.DebugDrawLine3D(pivot, pivot + MySector.DirectionToSunNormalized * 5, Color.Red, Color.Red, false);
                else
                    MyRenderProxy.DebugDrawLine3D(pivot, pivot + MySector.DirectionToSunNormalized * 5, Color.Green, Color.Green, false);
                if (i == m_solarComponent.DebugCurrentPivot)
                    MyRenderProxy.DebugDrawLine3D(pivot, pivot + MySector.DirectionToSunNormalized * 7, Color.Yellow, Color.Yellow, false);
            }
        }
    }
}

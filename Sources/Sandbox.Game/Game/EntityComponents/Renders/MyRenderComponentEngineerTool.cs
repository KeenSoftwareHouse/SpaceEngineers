using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender;
using VRageMath;
using Sandbox.Graphics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Weapons;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.World;
using VRage.Game.Components;
using VRage.Game;

namespace Sandbox.Game.Components
{
    class MyRenderComponentEngineerTool:MyRenderComponent
    {
        MyEngineerToolBase m_tool;

        #region overrides
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_tool = Container.Entity as MyEngineerToolBase;
        }
        public override void Draw()
        {
            base.Draw();

            // Custom draw crosshair - center of screen
            //MyHud.Crosshair.Position = MyHudCrosshair.ScreenCenter;

            if (m_tool.CanBeDrawn())
            {
                DrawHighlight();
            }
        }
        
        #endregion
        public void DrawHighlight()
        {
            if (m_tool.GetTargetGrid() == null || m_tool.HasHitBlock == false)
            {
                return;
            }

            var block = m_tool.GetTargetGrid().GetCubeBlock(m_tool.TargetCube);
            //Debug.Assert(block != null, "Call programmer");
            if (block == null) // This is just a workaround, so that the above assert does not crash the game on release
                return;

            Matrix blockWorldMatrixF;
            block.Orientation.GetMatrix(out blockWorldMatrixF);
            MatrixD blockWorldMatrix = blockWorldMatrixF;
            MatrixD gridWorld = m_tool.GetTargetGrid().Physics.GetWorldMatrix();
            blockWorldMatrix = blockWorldMatrix * Matrix.CreateTranslation(block.Position) * Matrix.CreateScale(m_tool.GetTargetGrid().GridSize) * gridWorld;

            /*Vector3 pos = Vector3.Transform(new Vector3(0.0f, 0.0f, 0.0f), blockRotation);
            MyRenderProxy.DebugDrawAxis(blockRotation, m_targetGrid.GridSize, false);
            MyRenderProxy.DebugDrawSphere(pos, 0.5f, new Vector3(1.0f, 0.0f, 0.0f), 1.0f, false);*/
            float highlightThickness = m_tool.GetTargetGrid().GridSizeEnum == MyCubeSize.Large ? 0.06f : 0.03f;

            Vector3 centerOffset = new Vector3(0.5f, 0.5f, 0.5f);
            TimeSpan time = MySession.Static.ElapsedPlayTime;
            Vector3 inflate = new Vector3(0.05f);
            BoundingBoxD bb = new BoundingBoxD(-block.BlockDefinition.Center - centerOffset - inflate, block.BlockDefinition.Size - block.BlockDefinition.Center - centerOffset + inflate);
            Color color = m_tool.HighlightColor;
			var lineMaterial = m_tool.HighlightMaterial;
            MySimpleObjectDraw.DrawTransparentBox(ref blockWorldMatrix, ref bb, ref color, MySimpleObjectRasterizer.Wireframe, 1, highlightThickness, null, lineMaterial, false);
        }
    }
}

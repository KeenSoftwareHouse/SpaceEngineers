using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities.Cube;
using System.Diagnostics;
using ProtoBuf;
using System.Reflection;
using VRage.Plugins;
using VRage.Import;
using VRageRender;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Engine.Physics;
using Sandbox.Common.Components;
using Sandbox.Game.Multiplayer;
using VRage;
using VRage.Utils;
using VRage.Library.Utils;

namespace Sandbox.Game.Components
{
    public class MyDebugRenderComponentCubeGrid : MyDebugRenderComponent
    {
        MyCubeGrid m_cubeGrid = null;

        Dictionary<Vector3I, MyTimeSpan> m_dirtyBlocks = new Dictionary<Vector3I, MyTimeSpan>();
        List<Vector3I> m_tmpRemoveList = new List<Vector3I>();

        public MyDebugRenderComponentCubeGrid(MyCubeGrid cubeGrid)
            : base(cubeGrid)
        {
            m_cubeGrid = cubeGrid;
        }

        public override void PrepareForDraw()
        {
            base.PrepareForDraw();

            if (MyDebugDrawSettings.DEBUG_DRAW_GRID_DIRTY_BLOCKS)
            {
                MyTimeSpan delay = MyTimeSpan.FromMiliseconds(1500);
                using (m_tmpRemoveList.GetClearToken())
                {
                    foreach (var b in m_dirtyBlocks)
                    {
                        if ((MySandboxGame.Static.UpdateTime - b.Value) > delay)
                        {
                            m_tmpRemoveList.Add(b.Key);
                        }
                    }
                    foreach (var b in m_tmpRemoveList)
                    {
                        m_dirtyBlocks.Remove(b);
                    }
                }

                foreach (var block in m_cubeGrid.DirtyBlocks)
                {
                    m_dirtyBlocks[block] = MySandboxGame.Static.UpdateTime;
                }
            }
        }

        public override bool DebugDraw()
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_GRID_NAMES || MyDebugDrawSettings.DEBUG_DRAW_GRID_CONTROL)
            {
                string text = "";
                var color = Color.White;

                if (MyDebugDrawSettings.DEBUG_DRAW_GRID_NAMES) text += m_cubeGrid.ToString() + " ";
                if (MyDebugDrawSettings.DEBUG_DRAW_GRID_CONTROL)
                {
                    var controllingPlayer = Sync.Players.GetControllingPlayer(m_cubeGrid);
                    if (controllingPlayer != null)
                    {
                        text += "Controlled by: " + controllingPlayer.DisplayName;
                        color = Color.LightGreen;
                    }
                }

                MyRenderProxy.DebugDrawText3D(m_cubeGrid.PositionComp.WorldAABB.Center, text, color, 0.7f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            }

            MyRenderComponentCubeGrid gridRender = m_cubeGrid.Render;
            if (MyDebugDrawSettings.DEBUG_DRAW_BLOCK_GROUPS)
            {
                var tpos = m_cubeGrid.PositionComp.WorldMatrix.Translation;
                foreach (var group in m_cubeGrid.BlockGroups)
                {
                    MyRenderProxy.DebugDrawText3D(tpos, group.Name.ToString(), Color.Red, 1, false);
                    tpos += m_cubeGrid.PositionComp.WorldMatrix.Right * group.Name.Length * 0.1f;
                }
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_GRID_DIRTY_BLOCKS)
            {
                foreach (var b in m_dirtyBlocks)
                {
                    var color = m_cubeGrid.GetCubeBlock(b.Key) != null ? Color.Red.ToVector3() : Color.Yellow.ToVector3();

                    var m = Matrix.CreateScale(m_cubeGrid.GridSize) * Matrix.CreateTranslation(b.Key * m_cubeGrid.GridSize) * m_cubeGrid.WorldMatrix;
                    MyRenderProxy.DebugDrawOBB(m, color, 0.15f, false, true);
                }
            }

            // Bone debug draw
            if (MyDebugDrawSettings.DEBUG_DRAW_DISPLACED_BONES)
            {
                Vector3 cameraPos = (Vector3)MySector.MainCamera.Position;

                foreach (var bone in m_cubeGrid.Skeleton.Bones)
                {
                    var bonePos = (bone.Key / (float)m_cubeGrid.Skeleton.BoneDensity) * gridRender.GridSize + bone.Value;
                    bonePos -= new Vector3(gridRender.GridSize / m_cubeGrid.Skeleton.BoneDensity);
                    Vector3 pos = Vector3.Transform(bonePos, (Matrix)m_cubeGrid.PositionComp.WorldMatrix);
                    MyRenderProxy.DebugDrawSphere(pos, 0.05f, Color.Red.ToVector3(), 0.5f, false, true);
                    if ((cameraPos - pos).LengthSquared() < 200.0f)
                        MyRenderProxy.DebugDrawText3D(pos, bone.Key.ToString(), Color.Red, 0.4f, false);
                }
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_STRUCTURAL_INTEGRITY &&
                m_cubeGrid.StructuralIntegrity != null)
            {
                m_cubeGrid.StructuralIntegrity.DebugDraw();
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_CUBES)
            {
                foreach (var cubeBlock in m_cubeGrid.CubeBlocks)
                {
                    var cb = cubeBlock.FatBlock;
                    if (cb == null) continue;

                    cb.DebugDraw();
                }
            }

            m_cubeGrid.GridSystems.DebugDraw();

            if (MyDebugDrawSettings.DEBUG_DRAW_GRID_TERMINAL_SYSTEMS)
            {
                /* if (grid.OnBlockAdded != null)
                 {
                     MyRenderProxy.DebugDrawText3D(Entity.PositionComp.WorldMatrix.Translation + new Vector3(0.0f, 0.0f, 0.5f), grid.OnBlockAdded.GetInvocationList().Count().ToString(), Color.NavajoWhite, 1.0f, false);
                 }*/
            }

            if (MyFakes.ENABLE_TRASH_REMOVAL && MyDebugDrawSettings.DEBUG_DRAW_TRASH_REMOVAL)
            {
                Color color = m_cubeGrid.IsTrash() ? Color.Red : Color.Green;
                float sphereRadius = m_cubeGrid.PositionComp.LocalAABB.HalfExtents.AbsMax();
                MyRenderProxy.DebugDrawSphere(m_cubeGrid.Physics != null ? m_cubeGrid.Physics.CenterOfMassWorld : m_cubeGrid.PositionComp.WorldMatrix.Translation, sphereRadius, color, 1.0f, false);
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_GRID_ORIGINS)
            {
                MyRenderProxy.DebugDrawAxis(m_cubeGrid.PositionComp.WorldMatrix, 1.0f, false);
            }

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS_ALL)
            {
                foreach (MySlimBlock block in m_cubeGrid.GetBlocks())
                {
                    if((m_cubeGrid.GridIntegerToWorld(block.Position) - MySector.MainCamera.Position).LengthSquared() < 200)
                        DebugDrawMountPoints(block);
                }
            }

            if(MyDebugDrawSettings.DEBUG_DRAW_BLOCK_INTEGRITY)
            {
                if (MySector.MainCamera != null && (MySector.MainCamera.Position - m_cubeGrid.PositionComp.WorldVolume.Center).Length() < 16 +m_cubeGrid.PositionComp.WorldVolume.Radius)
                    foreach (var cubeBlock in m_cubeGrid.CubeBlocks)
                    {
                        var pos = m_cubeGrid.GridIntegerToWorld(cubeBlock.Position);
                        if (m_cubeGrid.GridSizeEnum == MyCubeSize.Large || (MySector.MainCamera != null && (MySector.MainCamera.Position - pos).LengthSquared() < 9))
                            MyRenderProxy.DebugDrawText3D(m_cubeGrid.GridIntegerToWorld(cubeBlock.Position), ((int)cubeBlock.Integrity).ToString(), Color.White, m_cubeGrid.GridSizeEnum == MyCubeSize.Large ? 0.65f : 0.5f, false);
                    }
            }

            return base.DebugDraw();
        }

        private void DebugDrawMountPoints(MySlimBlock block)
        {
            
            if (block.FatBlock is MyCompoundCubeBlock)
            {
                MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                foreach (MySlimBlock componentBlock in compoundBlock.GetBlocks())
                {
                    DebugDrawMountPoints(componentBlock);
                }
            }
            else
            {
                MatrixD blockWorldMatrix = m_cubeGrid.WorldMatrix;
                Matrix blockMatrix;                
                block.Orientation.GetMatrix(out blockMatrix);
                blockWorldMatrix *= blockMatrix;
                blockWorldMatrix.Translation = m_cubeGrid.GridIntegerToWorld(block.Position);
                MyCubeBuilder.DrawMountPoints(m_cubeGrid.GridSize, block.BlockDefinition, ref blockWorldMatrix);
            }             
        }


        public override void DebugDrawInvalidTriangles()
        {
            base.DebugDrawInvalidTriangles();

            MyRenderComponentCubeGrid gridRender = m_cubeGrid.Render;
            MyCubeGridRenderData data = gridRender.RenderData;
            foreach (var cell in data.Cells)
            {
                HashSet<MyCubePart> parts = cell.Value.CubeParts;
                foreach (var part in parts)
                {
                    MyModel model = part.Model;
                    if (model != null)
                    {
                        int triCount = model.GetTrianglesCount();
                        for (int i = 0; i < triCount; ++i)
                        {
                            var triangle = model.GetTriangle(i);
                            if (MyUtils.IsWrongTriangle(model.GetVertex(triangle.I0), model.GetVertex(triangle.I1), model.GetVertex(triangle.I2)))
                            {
                                Vector3 v0 = Vector3.Transform(model.GetVertex(triangle.I0), (Matrix)m_cubeGrid.PositionComp.WorldMatrix);
                                Vector3 v1 = Vector3.Transform(model.GetVertex(triangle.I1), (Matrix)m_cubeGrid.PositionComp.WorldMatrix);
                                Vector3 v2 = Vector3.Transform(model.GetVertex(triangle.I2), (Matrix)m_cubeGrid.PositionComp.WorldMatrix);
                                VRageRender.MyRenderProxy.DebugDrawLine3D(v0, v1, Color.Purple, Color.Purple, false);
                                VRageRender.MyRenderProxy.DebugDrawLine3D(v1, v2, Color.Purple, Color.Purple, false);
                                VRageRender.MyRenderProxy.DebugDrawLine3D(v2, v0, Color.Purple, Color.Purple, false);
                                Vector3 center = (v0 + v1 + v2) / 3f;
                                VRageRender.MyRenderProxy.DebugDrawLine3D(center, center + Vector3.UnitX, Color.Yellow, Color.Yellow, false);
                                VRageRender.MyRenderProxy.DebugDrawLine3D(center, center + Vector3.UnitY, Color.Yellow, Color.Yellow, false);
                                VRageRender.MyRenderProxy.DebugDrawLine3D(center, center + Vector3.UnitZ, Color.Yellow, Color.Yellow, false);
                            }
                        }
                    }
                }
            }
        }
    }
}

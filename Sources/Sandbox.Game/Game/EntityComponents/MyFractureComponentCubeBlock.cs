using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Havok;

using VRage.Game.ObjectBuilders.ComponentSystem;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using VRage;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Profiler;
using VRageMath;

namespace Sandbox.Game.EntityComponents
{
    [MyComponentBuilder(typeof(MyObjectBuilder_FractureComponentCubeBlock))]
    public class MyFractureComponentCubeBlock : MyFractureComponentBase
    {
        private readonly List<MyObjectBuilder_FractureComponentBase.FracturedShape> m_tmpShapeListInit = new List<MyObjectBuilder_FractureComponentBase.FracturedShape>();

        public MySlimBlock Block { get; private set; }

        /// <summary>
        /// Mountpoints rotated by block orientation.
        /// </summary>
        public List<MyCubeBlockDefinition.MountPoint> MountPoints { get; private set; }

        // Remembered builder for later initialization.
        private MyObjectBuilder_ComponentBase m_obFracture;

        public override MyPhysicalModelDefinition PhysicalModelDefinition
        {
            get { return Block.BlockDefinition;  }
        }


        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            Block = (Entity as MyCubeBlock).SlimBlock;
            Block.FatBlock.CheckConnectionAllowed = true;

            var blockOnPosition = Block.CubeGrid.GetCubeBlock(Block.Position);
            if (blockOnPosition != null)
                blockOnPosition.FatBlock.CheckConnectionAllowed = true;

            if (m_obFracture != null)
            {
                Init(m_obFracture);

                m_obFracture = null;
            }
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();

            Block.FatBlock.CheckConnectionAllowed = false;
            var blockOnPosition = Block.CubeGrid.GetCubeBlock(Block.Position);
            if (blockOnPosition != null && blockOnPosition.FatBlock is MyCompoundCubeBlock)
            {
                bool checkConnectionAllowed = false;
                foreach (var block in (blockOnPosition.FatBlock as MyCompoundCubeBlock).GetBlocks())
                    checkConnectionAllowed |= block.FatBlock.CheckConnectionAllowed;

                if (!checkConnectionAllowed)
                    blockOnPosition.FatBlock.CheckConnectionAllowed = false;
            }
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var ob = base.Serialize() as MyObjectBuilder_FractureComponentCubeBlock;

            SerializeInternal(ob);

            return ob;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);

            if (Block != null)
            {
                Init(builder);
            }
            else
            {
                m_obFracture = builder;
            }
        }

        public override void SetShape(HkdBreakableShape shape, bool compound)
        {
            base.SetShape(shape, compound);

            CreateMountPoints();

            // Update neighbours for parent block
            var blockOnPosition = Block.CubeGrid.GetCubeBlock(Block.Position);
            if (blockOnPosition != null)
                blockOnPosition.CubeGrid.UpdateBlockNeighbours(blockOnPosition);

            if (Block.CubeGrid.Physics != null)
                Block.CubeGrid.Physics.AddDirtyBlock(Block);
        }

        public override bool RemoveChildShapes(string[] shapeNames)
        {
            base.RemoveChildShapes(shapeNames);

            if (!Shape.IsValid() || Shape.GetChildrenCount() == 0)
            {
                MountPoints.Clear();
                // Remove block when no children
                if (Sync.IsServer)
                {
                    return true;
                }
                else
                {
                    // Remove this component - if not done then clients add empty shapes to grid shape (MyGridShape.CreateBlockShape)
                    Block.FatBlock.Components.Remove<MyFractureComponentBase>();
                }
            }

            return false;
        }

        private void Init(MyObjectBuilder_ComponentBase builder)
        {
            ProfilerShort.Begin("FractureComponent.Deserialize");

            var ob = builder as MyObjectBuilder_FractureComponentCubeBlock;
            if (ob.Shapes.Count == 0)
            {
                ProfilerShort.End();
                Debug.Fail("No relevant shape was found for fractured block. It was probably reexported and names changed. Block definition: " + Block.BlockDefinition.Id.ToString());
                throw new Exception("No relevant shape was found for fractured block. It was probably reexported and names changed. Block definition: " + Block.BlockDefinition.Id.ToString());
            }

            RecreateShape(ob.Shapes);

            ProfilerShort.End();
        }

        public void OnCubeGridChanged()
        {
            Debug.Assert(m_tmpShapeList.Count == 0);
            m_tmpShapeList.Clear();

            GetCurrentFracturedShapeList(m_tmpShapeList);
            RecreateShape(m_tmpShapeList);

            m_tmpShapeList.Clear();
        }

        protected override void RecreateShape(List<MyObjectBuilder_FractureComponentBase.FracturedShape> shapeList)
        {
            Debug.Assert(m_tmpChildren.Count == 0);
            Debug.Assert(m_tmpShapeInfos.Count == 0);

            ProfilerShort.Begin("FractureComponent.RecreateShape");

            if (Shape.IsValid())
            {
                Shape.RemoveReference();
                Shape = new HkdBreakableShape();
            }

            var render = Block.FatBlock.Render as MyRenderComponentFracturedPiece;
            if (render != null)
            {
                render.ClearModels();
                render.UpdateRenderObject(false);
            }
            else
            {
                Debug.Fail("Invalid render type");
            }

            if (shapeList.Count == 0)
            {
                ProfilerShort.End();
                return;
            }

            var removeRefsList = new List<HkdShapeInstanceInfo>();

            {
                var blockDef = Block.BlockDefinition;
                var model = blockDef.Model;
                if (VRage.Game.Models.MyModels.GetModelOnlyData(model).HavokBreakableShapes == null)
                    MyDestructionData.Static.LoadModelDestruction(model, blockDef, Vector3.One);

                var shape = VRage.Game.Models.MyModels.GetModelOnlyData(model).HavokBreakableShapes[0];
                var si = new HkdShapeInstanceInfo(shape, null, null);
                removeRefsList.Add(si);
                m_tmpChildren.Add(si);
                shape.GetChildren(m_tmpChildren);

                if (blockDef.BuildProgressModels != null)
                {
                    foreach (var progress in blockDef.BuildProgressModels)
                    {
                        model = progress.File;

                        if (VRage.Game.Models.MyModels.GetModelOnlyData(model).HavokBreakableShapes == null)
                            MyDestructionData.Static.LoadModelDestruction(model, blockDef, Vector3.One);

                        shape = VRage.Game.Models.MyModels.GetModelOnlyData(model).HavokBreakableShapes[0];
                        si = new HkdShapeInstanceInfo(shape, null, null);
                        removeRefsList.Add(si);
                        m_tmpChildren.Add(si);
                        shape.GetChildren(m_tmpChildren);
                    }
                }
            }

            Debug.Assert(m_tmpShapeListInit.Count == 0);
            m_tmpShapeListInit.Clear();
            m_tmpShapeListInit.AddList(shapeList);

            for (int i = 0; i < m_tmpChildren.Count; i++)
            {
                var child = m_tmpChildren[i];
                var result = m_tmpShapeListInit.Where(s => s.Name == child.ShapeName);
                if (result.Count() > 0)
                {
                    var found = result.First();
                    var si = new HkdShapeInstanceInfo(child.Shape.Clone(), Matrix.Identity);
                    if (found.Fixed)
                        si.Shape.SetFlagRecursively(HkdBreakableShape.Flags.IS_FIXED);
                    removeRefsList.Add(si);
                    m_tmpShapeInfos.Add(si);
                    m_tmpShapeListInit.Remove(found);
                }
                else
                {
                    child.GetChildren(m_tmpChildren);
                }
            }

            m_tmpShapeListInit.Clear();

            if (shapeList.Count > 0 && m_tmpShapeInfos.Count == 0)
            {
                ProfilerShort.End();
                m_tmpChildren.Clear();
                Debug.Fail("No relevant shape was found for fractured block. It was probably reexported and names changed. Block definition: " + Block.BlockDefinition.Id.ToString());
                throw new Exception("No relevant shape was found for fractured block. It was probably reexported and names changed. Block definition: " + Block.BlockDefinition.Id.ToString());
            }

            if (render != null)
            {
                foreach (var shape in m_tmpShapeInfos)
                {
                    if (!string.IsNullOrEmpty(shape.Shape.Name))
                        render.AddPiece(shape.Shape.Name, Matrix.Identity);
                }

                render.UpdateRenderObject(true);
            }

            m_tmpChildren.Clear();

            if (Block.CubeGrid.CreatePhysics)
            {
                HkdBreakableShape compound = new HkdCompoundBreakableShape(null, m_tmpShapeInfos);
                ((HkdCompoundBreakableShape)compound).RecalcMassPropsFromChildren();
                var mp = new HkMassProperties();
                compound.BuildMassProperties(ref mp);
                Shape = new HkdBreakableShape(compound.GetShape(), ref mp);
                compound.RemoveReference();
                foreach (var si in m_tmpShapeInfos)
                {
                    var siRef = si;
                    Shape.AddShape(ref siRef);
                }
                Shape.SetStrenght(MyDestructionConstants.STRENGTH);
                CreateMountPoints();

                // Update neighbours for parent block
                var blockOnPosition = Block.CubeGrid.GetCubeBlock(Block.Position);
                if (blockOnPosition != null)
                    blockOnPosition.CubeGrid.UpdateBlockNeighbours(blockOnPosition);

                if (Block.CubeGrid.Physics != null)
                    Block.CubeGrid.Physics.AddDirtyBlock(Block);
            }

            foreach (var si in m_tmpShapeInfos)
                si.Shape.RemoveReference();
            m_tmpShapeInfos.Clear();

            foreach (var si in removeRefsList)
                si.RemoveReference();

            ProfilerShort.End();
        }

        private void CreateMountPoints()
        {
            ProfilerShort.Begin("FractureComponent.CreateMountPoints");

            Debug.Assert(m_tmpChildren.Count == 0);

            if (MyFakes.FRACTURED_BLOCK_AABB_MOUNT_POINTS)
            {
                if (MountPoints == null)
                    MountPoints = new List<MyCubeBlockDefinition.MountPoint>();
                else
                    MountPoints.Clear();

                var blockDef = Block.BlockDefinition;
                var size = new Vector3(blockDef.Size);
                var bb = new BoundingBox(-size / 2, size / 2);

                var he = bb.HalfExtents;
                bb.Min += he;
                bb.Max += he;

                Shape.GetChildren(m_tmpChildren);
                if (m_tmpChildren.Count > 0)
                {
                    foreach (var child in m_tmpChildren)
                    {
                        var shape = child.Shape;
                        shape = AddMountForShape(shape, Matrix.Identity, ref bb, Block.CubeGrid.GridSize, MountPoints);
                    }
                }
                else
                {
                    AddMountForShape(Shape, Matrix.Identity, ref bb, Block.CubeGrid.GridSize, MountPoints);
                }

                m_tmpChildren.Clear();
            }
            else
            {
                MountPoints = MyCubeBuilder.AutogenerateMountpoints(new HkShape[] { Shape.GetShape() }, Block.CubeGrid.GridSize);
            }
            ProfilerShort.End();
        }

        public static HkdBreakableShape AddMountForShape(HkdBreakableShape shape, Matrix transform, ref BoundingBox blockBB, float gridSize,
            List<MyCubeBlockDefinition.MountPoint> outMountPoints)
        {
            Vector4 min;
            Vector4 max;
            shape.GetShape().GetLocalAABB(0.01f, out min, out max);//.Transform(CubeGrid.PositionComp.WorldMatrix);
            var bb = new BoundingBox(new Vector3(min), new Vector3(max));
            bb = bb.Transform(transform);
            bb.Min /= gridSize; //normalize for mount point
            bb.Max /= gridSize;

            bb.Inflate(0.04f);//add tolerance (fracture shapes are smaller than block)
            bb.Min += blockBB.HalfExtents;
            bb.Max += blockBB.HalfExtents;

            if (blockBB.Contains(bb) == ContainmentType.Intersects)
            {
                bb.Inflate(-0.04f);
                foreach (var directionEnum in Base6Directions.EnumDirections)
                {
                    int dirEnum = (int)directionEnum;
                    Vector3 direction = Base6Directions.Directions[dirEnum];
                    Vector3 absDir = Vector3.Abs(direction);
                    var mp = new MyCubeBlockDefinition.MountPoint();
                    mp.Start = bb.Min;
                    mp.End = bb.Max;
                    mp.Enabled = true;
                    var start = mp.Start * absDir / (blockBB.HalfExtents * 2) - absDir * 0.04f;
                    var end = mp.End * absDir / (blockBB.HalfExtents * 2) + absDir * 0.04f;
                    bool add = false;
                    bool one = false;
                    if (start.Max() < 1 && end.Max() > 1 && direction.Max() > 0)
                    {
                        add = true;
                        one = true;
                    }
                    else if (start.Min() < 0 && end.Max() > 0 && direction.Min() < 0)
                    {
                        add = true;
                    }
                    if (!add)
                    {
                        continue;
                    }
                    mp.Start -= mp.Start * absDir - absDir * 0.04f;
                    mp.End -= mp.End * absDir + absDir * 0.04f;
                    if (one)
                    {
                        mp.Start += absDir * blockBB.HalfExtents * 2;
                        mp.End += absDir * blockBB.HalfExtents * 2;
                    }
                    mp.Start -= blockBB.HalfExtents - Vector3.One / 2;
                    mp.End -= blockBB.HalfExtents - Vector3.One / 2;
                    mp.Normal = new Vector3I(direction);
                    outMountPoints.Add(mp);
                }
            }
            return shape;
        }

        public float GetIntegrityRatioFromFracturedPieceCounts()
        {
            if (Shape.IsValid() && Block != null)
            {
                int totalCount = Block.GetTotalBreakableShapeChildrenCount();
                Debug.Assert(totalCount > 0);
                if (totalCount > 0)
                {
                    int count = Shape.GetTotalChildrenCount();
                    Debug.Assert(count <= totalCount);
                    if (count <= totalCount)
                        return (float)count / totalCount;
                }
            }

            return 0f;
        }

        class MyFractureComponentBlockDebugRender : MyDebugRenderComponentBase
        {
            private MyCubeBlock m_block;
            public MyFractureComponentBlockDebugRender(MyCubeBlock b)
            {
                m_block = b;
            }

            public override void DebugDraw()
            {
                if (!MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS || !m_block.Components.Has<MyFractureComponentBase>())
                    return;

                MyFractureComponentCubeBlock component = m_block.GetFractureComponent();
                if (component != null)
                {
                    MatrixD m = m_block.CubeGrid.PositionComp.WorldMatrix;
                    m.Translation = m_block.CubeGrid.GridIntegerToWorld(m_block.Position);
                    MyCubeBuilder.DrawMountPoints(m_block.CubeGrid.GridSize, m_block.BlockDefinition, m, component.MountPoints.ToArray());
                }
            }

            public override void DebugDrawInvalidTriangles()
            {
            }
        }


    }

}

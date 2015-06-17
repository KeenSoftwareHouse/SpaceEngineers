using Havok;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Components;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_FracturedBlock))]
    public class MyFracturedBlock : MyCubeBlock
    {
        new MyRenderComponentFracturedPiece Render { get { return (MyRenderComponentFracturedPiece)base.Render; } set { base.Render = value; } }

        private static List<HkdShapeInstanceInfo> m_children = new List<HkdShapeInstanceInfo>();
        private static List<HkdShapeInstanceInfo> m_shapeInfos = new List<HkdShapeInstanceInfo>();
        public struct Info
        {
            public HkdBreakableShape Shape;
            public Vector3I Position;
            public bool Compound;
            public List<MyDefinitionId> OriginalBlocks;// = new List<MyDefinitionId>();
            public List<MyBlockOrientation> Orientations;// = new List<MyDefinitionId>();
        }

        public HkdBreakableShape Shape;
        public List<MyDefinitionId> OriginalBlocks;// = new List<MyDefinitionId>();
        public List<MyBlockOrientation> Orientations;
        public MyFracturedBlock()
            : base()
        {
            
            EntityId = MyEntityIdentifier.AllocateId();

            Render = new MyRenderComponentFracturedPiece();
            Render.NeedsDraw = true;
            Render.PersistentFlags = MyPersistentEntityFlags2.Enabled;
            //NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            CheckConnectionAllowed = true;
            AddDebugRenderComponent(new MyFBDebugRender(this));
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_FracturedBlock;
            if (string.IsNullOrEmpty(Shape.Name) || Shape.IsCompound())
            {
                Shape.GetChildren(m_children);
                foreach (var child in m_children)
                {
                    var shape = new MyObjectBuilder_FracturedBlock.ShapeB()
                    {
                        Name = child.ShapeName,
                        Orientation = Quaternion.CreateFromRotationMatrix(child.GetTransform().GetOrientation()),
                        Fixed = MyDestructionHelper.IsFixed(child.Shape)
                    };
                    ob.Shapes.Add(shape);
                }
                m_children.Clear();
            }
            else
            {
                ob.Shapes.Add(new MyObjectBuilder_FracturedBlock.ShapeB() { Name = Shape.Name });
            }
            foreach (var def in OriginalBlocks)
                ob.BlockDefinitions.Add(def);
            foreach (var or in Orientations)
                ob.BlockOrientations.Add(or);
            return ob;
        }

        List<MyObjectBuilder_FracturedBlock.ShapeB> m_shapes = new List<MyObjectBuilder_FracturedBlock.ShapeB>();
        public List<MyCubeBlockDefinition.MountPoint> MountPoints { get; private set; }
        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            base.Init(builder, cubeGrid);
            ProfilerShort.Begin("FP.Init");
            CheckConnectionAllowed = true;
            var ob = builder as MyObjectBuilder_FracturedBlock;
            if (ob.Shapes.Count == 0)
            {
                ProfilerShort.End();
                return;
            }
            
            OriginalBlocks = new List<MyDefinitionId>();
            Orientations = new List<MyBlockOrientation>();
            var lst = new List<HkdShapeInstanceInfo>();
            foreach (var def in ob.BlockDefinitions)
            {
                var blockDef = MyDefinitionManager.Static.GetCubeBlockDefinition(def);

                var model = blockDef.Model;
                if (MyModels.GetModelOnlyData(model).HavokBreakableShapes == null)
                    MyDestructionData.Static.LoadModelDestruction(model, blockDef, false, Vector3.One);
                var shape = MyModels.GetModelOnlyData(model).HavokBreakableShapes[0];
                var si = new HkdShapeInstanceInfo(shape, null, null);
                lst.Add(si);
                m_children.Add(si);
                shape.GetChildren(m_children);
                if(blockDef.BuildProgressModels != null)
                {
                    foreach(var progress in blockDef.BuildProgressModels)
                    {
                        model = progress.File;
                        if (MyModels.GetModelOnlyData(model).HavokBreakableShapes == null)
                            MyDestructionData.Static.LoadModelDestruction(model, blockDef, false, Vector3.One);
                        shape = MyModels.GetModelOnlyData(model).HavokBreakableShapes[0];
                        si = new HkdShapeInstanceInfo(shape, null, null);
                        lst.Add(si);
                        m_children.Add(si);
                        shape.GetChildren(m_children);
                    }
                }


                OriginalBlocks.Add(def);
            }
            foreach (var or in ob.BlockOrientations)
            {
                Orientations.Add(or);
            }
            m_shapes.AddRange(ob.Shapes);
            for (int i = 0; i < m_children.Count; i++)
            {
                var child = m_children[i];
                Func<MyObjectBuilder_FracturedBlock.ShapeB, bool> x = s => s.Name == child.ShapeName;
                var result = m_shapes.Where(x);
                if (result.Count() > 0)
                {
                    var found = result.First();
                    var m = Matrix.CreateFromQuaternion(found.Orientation);
                    m.Translation = child.GetTransform().Translation;
                    var si = new HkdShapeInstanceInfo(child.Shape.Clone(), m);
                    if(found.Fixed)
                        si.Shape.SetFlagRecursively(HkdBreakableShape.Flags.IS_FIXED);
                    lst.Add(si);
                    m_shapeInfos.Add(si);
                    m_shapes.Remove(found);
                }
                else
                {
                    child.GetChildren(m_children);
                }
            }

            if (m_shapeInfos.Count == 0)
            {
                m_children.Clear();
                ProfilerShort.End();
                Debug.Fail("No relevant shape was found for fractured block. It was probably reexported and names changed.");
                throw new Exception("No relevant shape was found for fractured block. It was probably reexported and names changed.");
            }

            foreach (var shape in m_shapeInfos)
            {
                if(! string.IsNullOrEmpty(shape.Shape.Name))
                    Render.AddPiece(shape.Shape.Name, Matrix.CreateFromQuaternion(Quaternion.CreateFromRotationMatrix(shape.GetTransform().GetOrientation())));
            }

            if (CubeGrid.CreatePhysics)
            {
                HkdBreakableShape compound = new HkdCompoundBreakableShape(null, m_shapeInfos);
                ((HkdCompoundBreakableShape)compound).RecalcMassPropsFromChildren();
                Shape = compound;
                var mp = new HkMassProperties();
                compound.BuildMassProperties(ref mp);
                Shape = new HkdBreakableShape(compound.GetShape(), ref mp);
                compound.RemoveReference();
                foreach (var si in m_shapeInfos)
                {
                    var siRef = si;
                    Shape.AddShape(ref siRef);
                }
                Shape.SetStrenght(MyDestructionConstants.STRENGTH);
                CreateMountPoints();
            }
            m_children.Clear();
            foreach (var si in m_shapeInfos)
                si.Shape.RemoveReference();
            foreach (var si in lst)
                si.RemoveReference();
            m_shapeInfos.Clear();

            ProfilerShort.End();
        }

        public void SetDataFromCompound(HkdBreakableShape compound)
        {
            var render = this.Render as MyRenderComponentFracturedPiece;

            if (render != null)
            {
                compound.GetChildren(m_shapeInfos);

                foreach (var shapeInstanceInfo in m_shapeInfos)
                {
                    System.Diagnostics.Debug.Assert(shapeInstanceInfo.IsValid(), "Invalid shapeInstanceInfo!");
                    if (shapeInstanceInfo.IsValid())
                    {
                        render.AddPiece(shapeInstanceInfo.ShapeName, shapeInstanceInfo.GetTransform());
                    }
                }

                m_shapeInfos.Clear();
            }
        }

        private void AddMeshBuilderRecursively(List<HkdShapeInstanceInfo> children)
        {
            var render = this.Render as MyRenderComponentFracturedPiece;

            foreach (var child in children)
            {
                render.AddPiece(child.ShapeName, Matrix.Identity);
            }
        }

        internal void SetDataFromHavok(HkdBreakableShape shape, bool compound)
        {
            Shape = shape;
            if (compound)
            {
                SetDataFromCompound(shape);
            }
            else
            {
                var render = this.Render as MyRenderComponentFracturedPiece;
                render.AddPiece(shape.Name, Matrix.Identity);
            }
            CreateMountPoints();
        }

        private void CreateMountPoints()
        {
            ProfilerShort.Begin("FB.CreateMountPoints");
            if (MyFakes.FRACTURED_BLOCK_AABB_MOUNT_POINTS)
            {
                MountPoints = new List<MyCubeBlockDefinition.MountPoint>();
                var blockBB = BoundingBox.CreateInvalid();
                for (int i = 0; i < OriginalBlocks.Count; i++)
                {
                    var b = OriginalBlocks[i];
                    Matrix m;
                    Orientations[i].GetMatrix(out m);
                    var size = new Vector3(MyDefinitionManager.Static.GetCubeBlockDefinition(b).Size);
                    var bb = new BoundingBox(-size / 2, size / 2);
                    blockBB = blockBB.Include(bb.Transform(m));
                }
                var he = blockBB.HalfExtents;
                blockBB.Min += he;
                blockBB.Max += he;

                Shape.GetChildren(m_children);
                foreach (var child in m_children)
                {
                    var shape = child.Shape;
                    shape = AddMountForShape(shape, child.GetTransform(), ref blockBB);
                }
                if (m_children.Count == 0)
                    AddMountForShape(Shape, Matrix.Identity, ref blockBB);
                m_children.Clear();
            }
            else
            {
                MountPoints = MyCubeBuilder.AutogenerateMountpoints(new HkShape[] { Shape.GetShape() }, CubeGrid.GridSize);
            }
            ProfilerShort.End();
        }

        private HkdBreakableShape AddMountForShape(HkdBreakableShape shape, Matrix transform, ref BoundingBox blockBB)
        {
            Vector4 min;
            Vector4 max;
            shape.GetShape().GetLocalAABB(0.01f, out min, out max);//.Transform(CubeGrid.PositionComp.WorldMatrix);
            var bb = new BoundingBox(new Vector3(min), new Vector3(max));
            bb = bb.Transform(transform);
            bb.Min /= CubeGrid.GridSize; //normalize for mount point
            bb.Max /= CubeGrid.GridSize;
            
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
                    MountPoints.Add(mp);
                }
            }
            return shape;
        }

        protected override void Closing()
        {
            if(Shape.IsValid())
                Shape.RemoveReference(); 
            base.Closing();
        }

        List<MyCubeBlockDefinition.MountPoint> m_mpCache = new List<MyCubeBlockDefinition.MountPoint>();
        public override bool ConnectionAllowed(ref Vector3I otherBlockPos, ref Vector3I faceNormal, MyCubeBlockDefinition def)
        {
            if (MountPoints == null)
                return true;
            var otherPos = Position + faceNormal;
            var other = CubeGrid.GetCubeBlock(otherPos);
            MyBlockOrientation or;
            if (other != null)
                or = other.Orientation;
            else
                or = MyBlockOrientation.Identity;
            var position = Position;
            m_mpCache.Clear();
            if (other != null && other.FatBlock is MyFracturedBlock)
                m_mpCache.AddRange((other.FatBlock as MyFracturedBlock).MountPoints);
            else
            {
                if(other != null && other.FatBlock is MyCompoundCubeBlock)
                {
                    var lst = new List<MyCubeBlockDefinition.MountPoint>();
                    foreach(var b in (other.FatBlock as MyCompoundCubeBlock).GetBlocks())
                    {
                        MyCubeGrid.TransformMountPoints(lst, b.BlockDefinition, ref b.Orientation);
                        m_mpCache.AddRange(lst);
                    }
                }
                else
                    MyCubeGrid.TransformMountPoints(m_mpCache, def, ref or);
            }
            return MyCubeGrid.CheckMountPointsForSide(MountPoints, ref SlimBlock.Orientation, ref position, BlockDefinition.Id, ref faceNormal, m_mpCache,
                ref or, ref otherPos, def.Id);
        }

        class MyFBDebugRender : MyDebugRenderComponentBase
        {
            private MyFracturedBlock m_block;
            public MyFBDebugRender(MyFracturedBlock b)
            {
                m_block = b;
            }

            public override bool DebugDraw()
            {
                if (!MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS || m_block.MountPoints == null)
                    return true;
                //var cube = BoundingBox.CreateInvalid();
                //for (int i = 0; i < m_block.OriginalBlocks.Count; i++)
                //{
                //    var b = m_block.OriginalBlocks[i];
                //    Matrix m2;
                //    m_block.Orientations[i].GetMatrix(out m2);
                //    var size = new Vector3(MyDefinitionManager.Static.GetCubeBlockDefinition(b).Size);
                //    var bb2 = new BoundingBox(-size / 2, size / 2);
                //    cube = cube.Include(bb2.Transform(m2));
                //}

                //var obb = new MyOrientedBoundingBoxD(m_block.WorldMatrix.Translation, cube.HalfExtents * m_block.CubeGrid.GridSize, Quaternion.CreateFromRotationMatrix(m_block.WorldMatrix));
                //VRageRender.MyRenderProxy.DebugDrawOBB(obb, Color.Red, 0.05f, false, false);

                MatrixD m = m_block.CubeGrid.PositionComp.WorldMatrix;
                m.Translation = m_block.CubeGrid.GridIntegerToWorld(m_block.Position);
                MyCubeBuilder.DrawMountPoints(m_block.CubeGrid.GridSize, m_block.BlockDefinition, m, m_block.MountPoints.ToArray());
                return true;
            }

            public override void DebugDrawInvalidTriangles()
            {
            }
        }

    }
}

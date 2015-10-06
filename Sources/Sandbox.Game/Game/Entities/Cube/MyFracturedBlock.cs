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
using Sandbox.Game.EntityComponents;
using Sandbox.Common.ObjectBuilders.ComponentSystem;
using VRage.Game.ObjectBuilders.ComponentSystem;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_FracturedBlock))]
    public class MyFracturedBlock : MyCubeBlock
    {
        new MyRenderComponentFracturedPiece Render { get { return (MyRenderComponentFracturedPiece)base.Render; } set { base.Render = value; } }

        private static List<HkdShapeInstanceInfo> m_children = new List<HkdShapeInstanceInfo>();
        private static List<HkdShapeInstanceInfo> m_shapeInfos = new List<HkdShapeInstanceInfo>();

        public class MultiBlockPartInfo
        {
            public MyDefinitionId MultiBlockDefinition;
            public int MultiBlockId;
        }

        public struct Info
        {
            public HkdBreakableShape Shape;
            public Vector3I Position;
            public bool Compound;
            public List<MyDefinitionId> OriginalBlocks;// = new List<MyDefinitionId>();
            public List<MyBlockOrientation> Orientations;// = new List<MyDefinitionId>();
            // Info about blocks in multiblock (list can be null or value can be null).
            public List<MultiBlockPartInfo> MultiBlocks;
        }

        public HkdBreakableShape Shape;
        public List<MyDefinitionId> OriginalBlocks;// = new List<MyDefinitionId>();
        public List<MyBlockOrientation> Orientations;
        public List<MultiBlockPartInfo> MultiBlocks;


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
            Debug.Assert(!MyFakes.ENABLE_FRACTURE_COMPONENT, "Fractured block saved with fracture components enabled");

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

            if (MultiBlocks != null) 
            {
                foreach (var mbpart in MultiBlocks)
                {
                    if (mbpart != null)
                        ob.MultiBlocks.Add(new MyObjectBuilder_FracturedBlock.MyMultiBlockPart() { MultiBlockDefinition = mbpart.MultiBlockDefinition, MultiBlockId = mbpart.MultiBlockId });
                    else
                        ob.MultiBlocks.Add(null);
                }
            }

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
                if(ob.CreatingFracturedBlock)
                    return;
                Debug.Fail("No relevant shape was found for fractured block. It was probably reexported and names changed.");
                throw new Exception("No relevant shape was found for fractured block. It was probably reexported and names changed.");
            }
            
            OriginalBlocks = new List<MyDefinitionId>();
            Orientations = new List<MyBlockOrientation>();
            var lst = new List<HkdShapeInstanceInfo>();
            foreach (var def in ob.BlockDefinitions)
            {
                var blockDef = MyDefinitionManager.Static.GetCubeBlockDefinition(def);

                var model = blockDef.Model;
                if (MyModels.GetModelOnlyData(model).HavokBreakableShapes == null)
                    MyDestructionData.Static.LoadModelDestruction(model, blockDef, Vector3.One);
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
                            MyDestructionData.Static.LoadModelDestruction(model, blockDef, Vector3.One);
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

            if (ob.MultiBlocks.Count > 0)
            {
                MultiBlocks = new List<MultiBlockPartInfo>();
                foreach (var mbpart in ob.MultiBlocks)
                {
                    if (mbpart != null)
                        MultiBlocks.Add(new MultiBlockPartInfo() { MultiBlockDefinition = mbpart.MultiBlockDefinition, MultiBlockId = mbpart.MultiBlockId });
                    else
                        MultiBlocks.Add(null);
                }
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
                    shape = MyFractureComponentCubeBlock.AddMountForShape(shape, child.GetTransform(), ref blockBB, CubeGrid.GridSize, MountPoints);
                }
                if (m_children.Count == 0)
                    MyFractureComponentCubeBlock.AddMountForShape(Shape, Matrix.Identity, ref blockBB, CubeGrid.GridSize, MountPoints);
                m_children.Clear();
            }
            else
            {
                MountPoints = MyCubeBuilder.AutogenerateMountpoints(new HkShape[] { Shape.GetShape() }, CubeGrid.GridSize);
            }
            ProfilerShort.End();
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
				if (other != null && other.FatBlock is MyCompoundCubeBlock)
				{
					var lst = new List<MyCubeBlockDefinition.MountPoint>();
					foreach (var b in (other.FatBlock as MyCompoundCubeBlock).GetBlocks())
					{
						var mountPoints = b.BlockDefinition.GetBuildProgressModelMountPoints(b.BuildLevelRatio);
						MyCubeGrid.TransformMountPoints(lst, b.BlockDefinition, mountPoints, ref b.Orientation);
						m_mpCache.AddRange(lst);
					}
				}
				else if(other != null)
				{
					var mountPoints = def.GetBuildProgressModelMountPoints(other.BuildLevelRatio);
					MyCubeGrid.TransformMountPoints(m_mpCache, def, mountPoints, ref or);
				}
            }
            return MyCubeGrid.CheckMountPointsForSide(MountPoints, ref SlimBlock.Orientation, ref position, BlockDefinition.Id, ref faceNormal, m_mpCache,
                ref or, ref otherPos, def.Id);
        }

        /// <summary>
        /// Returns true if the fractured block is part of the given multiblock, otherwise false.
        /// </summary>
        public bool IsMultiBlockPart(MyDefinitionId multiBlockDefinition, int multiblockId)
        {
            if (MultiBlocks == null)
                return false;

            foreach (var mbpart in MultiBlocks)
            {
                if (mbpart != null && mbpart.MultiBlockDefinition == multiBlockDefinition && mbpart.MultiBlockId == multiblockId)
                    return true;
            }

            return false;
        }

        public MyObjectBuilder_CubeBlock ConvertToOriginalBlocksWithFractureComponent()
        {
            List<MyObjectBuilder_CubeBlock> blockBuilders = new List<MyObjectBuilder_CubeBlock>();
            Quaternion q;
            for (int i = 0; i < OriginalBlocks.Count; ++i)
            {
                var defId = OriginalBlocks[i];
                MyCubeBlockDefinition def;
                MyDefinitionManager.Static.TryGetCubeBlockDefinition(defId, out def);
                if (def == null)
                {
                    Debug.Fail("Cube block definition not found");
                    continue;
                }
                var orientation = Orientations[i];
                MultiBlockPartInfo multiBlockInfo = MultiBlocks != null && MultiBlocks.Count > i ? MultiBlocks[i] : null;

                MyObjectBuilder_CubeBlock blockBuilder = MyObjectBuilderSerializer.CreateNewObject(defId) as MyObjectBuilder_CubeBlock;
                orientation.GetQuaternion(out q);
                blockBuilder.Orientation = q;
                blockBuilder.Min = Position;
                blockBuilder.MultiBlockId = multiBlockInfo != null ? multiBlockInfo.MultiBlockId : 0;
                blockBuilder.MultiBlockDefinition = null;
                if (multiBlockInfo != null)
                    blockBuilder.MultiBlockDefinition = multiBlockInfo.MultiBlockDefinition;
                blockBuilder.ComponentContainer = new MyObjectBuilder_ComponentContainer();

                var fractureBuilder = new MyObjectBuilder_FractureComponentCubeBlock();
                HashSet<string> shapeNames = new HashSet<string>();
                GetAllBlockBreakableShapeNames(def, shapeNames);
                ConvertAllShapesToFractureComponentShapeBuilder(Shape, ref Matrix.Identity, orientation, shapeNames, fractureBuilder);
                // Count of shapes can be 0!
                if (fractureBuilder.Shapes.Count == 0)
                    continue;

                var componentData = new MyObjectBuilder_ComponentContainer.ComponentData();
                componentData.TypeId = typeof(MyFractureComponentBase).Name;
                componentData.Component = fractureBuilder;
                blockBuilder.ComponentContainer.Components.Add(componentData);

                if (i == 0 && CubeGrid.GridSizeEnum == MyCubeSize.Small)
                    return blockBuilder;

                blockBuilders.Add(blockBuilder);
            }

            if (blockBuilders.Count > 0)
            {
                MyObjectBuilder_CompoundCubeBlock compoundBuilder = MyCompoundCubeBlock.CreateBuilder(blockBuilders);
                return compoundBuilder;
            }

            return null;
        }

        private static void GetAllBlockBreakableShapeNames(MyCubeBlockDefinition blockDef, HashSet<string> outNames)
        {
            var model = blockDef.Model;
            if (MyModels.GetModelOnlyData(model).HavokBreakableShapes == null)
                MyDestructionData.Static.LoadModelDestruction(model, blockDef, Vector3.One);

            var shape = MyModels.GetModelOnlyData(model).HavokBreakableShapes[0];
            GetAllBlockBreakableShapeNames(shape, outNames);
        }

        private static void GetAllBlockBreakableShapeNames(HkdBreakableShape shape, HashSet<string> outNames)
        {
            var name = shape.Name;
            if (!string.IsNullOrEmpty(name))
                outNames.Add(name);

            if (shape.GetChildrenCount() > 0)
            {
                List<HkdShapeInstanceInfo> children = new List<HkdShapeInstanceInfo>();
                shape.GetChildren(children);
                foreach (var child in children)
                    GetAllBlockBreakableShapeNames(child.Shape, outNames);
            }
        }

        private static void ConvertAllShapesToFractureComponentShapeBuilder(HkdBreakableShape shape, ref Matrix shapeRotation, MyBlockOrientation blockOrientation, HashSet<string> names, MyObjectBuilder_FractureComponentCubeBlock fractureComponentBuilder)
        {
            var name = shape.Name;
            if (names.Contains(name))
            {
                MyBlockOrientation shapeOrientation = new MyBlockOrientation(ref shapeRotation);
                if (shapeOrientation == blockOrientation)
                {
                    MyObjectBuilder_FractureComponentCubeBlock.FracturedShape builderShape = new MyObjectBuilder_FractureComponentBase.FracturedShape();
                    builderShape.Name = name;
                    builderShape.Fixed = MyDestructionHelper.IsFixed(shape);

                    fractureComponentBuilder.Shapes.Add(builderShape);
                }
            }

            if (shape.GetChildrenCount() > 0)
            {
                List<HkdShapeInstanceInfo> children = new List<HkdShapeInstanceInfo>();
                shape.GetChildren(children);
                foreach (var child in children) 
                {
                    var childShapeRotation = child.GetTransform();
                    ConvertAllShapesToFractureComponentShapeBuilder(child.Shape, ref childShapeRotation, blockOrientation, names, fractureComponentBuilder);
                }
            }

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

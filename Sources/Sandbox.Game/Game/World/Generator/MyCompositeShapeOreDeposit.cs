using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using VRage.Library.Utils;
using VRage.Noise;
using VRageMath;
using VRage;
using VRage.Voxels;
using System.IO.MemoryMappedFiles;
using System.IO;
using Sandbox.Engine.Utils;
using SharpDX.Toolkit.Graphics;
using SharpDXImage = SharpDX.Toolkit.Graphics.Image;
using Sandbox.Game.GameSystems;
using System.Diagnostics;
using Sandbox.Common.ObjectBuilders.Definitions;


namespace Sandbox.Game.World.Generator
{
    class MyCompositeShapeOreDeposit
    {
        public readonly MyCsgShapeBase Shape;
        protected readonly MyVoxelMaterialDefinition m_material;

        virtual public void DebugDraw(ref Vector3D translation, ref Color materialColor)
        {
            Shape.DebugDraw(ref translation, materialColor);
            VRageRender.MyRenderProxy.DebugDrawText3D(Shape.Center() + translation, m_material.Id.SubtypeName, Color.White, 1f, false);
        }

        public MyCompositeShapeOreDeposit(MyCsgShapeBase shape, MyVoxelMaterialDefinition material)
        {
            System.Diagnostics.Debug.Assert(material != null, "Shape must have material");
            Shape = shape;
            m_material = material;
        }

        public virtual MyVoxelMaterialDefinition GetMaterialForPosition(ref Vector3 pos, float lodSize)
        {
            return m_material;
        }

        public virtual void ReadMaterialRange(MyStorageDataCache target, ref Vector3I writeOffset, int lodIndex, ref Vector3I minInLod, ref Vector3I maxInLod, float lodVoxelSizeHalf)
        {
            target.BlockFillMaterial(writeOffset, writeOffset + (maxInLod - minInLod), m_material.Index);
        }

        public virtual bool SpawnsFlora()
        {
            return m_material.SpawnsFlora;
        }


        public virtual bool ProvidesOcclusionHint { get { return false; } }
    }

}
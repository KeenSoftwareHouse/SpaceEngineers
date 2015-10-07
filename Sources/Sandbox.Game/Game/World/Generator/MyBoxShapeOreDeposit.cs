using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRage.Noise;
using VRageMath;
using VRage;
using VRage.Voxels;

namespace Sandbox.Game.World.Generator
{
   
    class MyBoxOreDeposit : MyCompositeShapeOreDeposit
    {
        MyCsgBox m_boxShape;

        public MyBoxOreDeposit(MyCsgShapeBase baseShape, MyVoxelMaterialDefinition material) :
            base(baseShape, material)
        {
            m_boxShape = (MyCsgBox)baseShape;
        }

        public override void ReadMaterialRange(MyStorageDataCache target, ref Vector3I writeOffset, int lodIndex, ref Vector3I minInLod, ref Vector3I maxInLod, float lodVoxelSizeHalf)
        {
            float lodVoxelSize = 2f * lodVoxelSizeHalf;

            byte defaultMaterial = m_material.Index;

            Vector3I v = new Vector3I();
            for (v.Z = minInLod.Z; v.Z <= maxInLod.Z; ++v.Z)
            {
                for (v.Y = minInLod.Y; v.Y <= maxInLod.Y; ++v.Y)
                {
                    for (v.X = minInLod.X; v.X <= maxInLod.X; ++v.X)
                    {
                        var write = v - minInLod + writeOffset;

                        byte slope = target.Material(ref write);
                        if (slope == 0) continue;

                        Vector3 localPos = v * lodVoxelSize;

                        var mat = GetMaterialForPosition(ref localPos, lodVoxelSize);
                        target.Material(ref write, mat.Index);
                    }
                }
            }
        }

        public override MyVoxelMaterialDefinition GetMaterialForPosition(ref Vector3 pos, float lodSize)
        {
            var voxelMaterals = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().ToList();

            float boxSize = 2 * m_boxShape.HalfExtents;

            var localPos = pos - m_boxShape.Center() + m_boxShape.HalfExtents;

            int materialIndex = (int)(MathHelper.Clamp(localPos.X / boxSize, 0, 1) * (voxelMaterals.Count - 1));

            return voxelMaterals[materialIndex];
        }

        public override bool SpawnsFlora()
        {
            return false;
        }


        public override void DebugDraw(ref Vector3D translation, ref Color materialColor)
        {
        }
    }
}

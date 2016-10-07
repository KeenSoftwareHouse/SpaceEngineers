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

        public override MyVoxelMaterialDefinition GetMaterialForPosition(ref Vector3 pos, float lodSize)
        {
            var voxelMaterals = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().ToList();

            float boxSize = 2 * m_boxShape.HalfExtents;

            var localPos = pos - m_boxShape.Center() + m_boxShape.HalfExtents;

            int materialIndex = (int)(MathHelper.Clamp(localPos.X / boxSize, 0, 1) * (voxelMaterals.Count - 1));

            return voxelMaterals[materialIndex];
        }

        public override void DebugDraw(ref Vector3D translation, ref Color materialColor)
        {
        }
    }
}

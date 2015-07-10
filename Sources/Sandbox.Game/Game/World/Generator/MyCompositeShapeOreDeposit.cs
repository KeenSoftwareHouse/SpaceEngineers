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
    class MyCompositeShapeOreDeposit
    {
        public readonly MyCsgShapeBase Shape;
        readonly MyVoxelMaterialDefinition m_material;

        virtual public void DebugDraw(ref Vector3D translation, ref Color materialColor)
        {
            Shape.DebugDraw(ref translation, materialColor);
            VRageRender.MyRenderProxy.DebugDrawText3D(Shape.Center() + translation, m_material.Id.SubtypeName, Color.White, 1f, false);
        }

        public MyCompositeShapeOreDeposit(MyCsgShapeBase shape, MyVoxelMaterialDefinition material)
        {
            Shape = shape;
            m_material = material;
        }

        public virtual MyVoxelMaterialDefinition GetMaterialForPosition(ref Vector3 pos,float lodSize)
        {
            return m_material;
        }

        public virtual bool SpawnsFlora()
        {
            return m_material.SpawnsFlora;
        }
    }

    class MyCompositeLayeredOreDeposit : MyCompositeShapeOreDeposit
    {
        IMyModule m_noise = null;
        MyMaterialLayer[] m_materialLayers = null;
        MyCompositeOrePlanetDeposit m_oreDeposits = null;

        public override void DebugDraw(ref Vector3D translation, ref Color materialColor)
        {
            Shape.DebugDraw(ref translation, materialColor);
            VRageRender.MyRenderProxy.DebugDrawText3D(Shape.Center() + translation, "layered", Color.White, 1f, false);

            m_oreDeposits.DebugDraw(ref translation, ref materialColor);
        }

        public MyCompositeLayeredOreDeposit(MyCsgShapeBase shape, MyMaterialLayer[] materialLayers,IMyModule noise, MyCompositeOrePlanetDeposit oresDeposits) :
            base(shape, null)
        {
            m_materialLayers = materialLayers;
            m_noise = noise;
            m_oreDeposits = oresDeposits;
        }

        public override MyVoxelMaterialDefinition GetMaterialForPosition(ref Vector3 pos, float lodSize)
        {
            Vector3 localPosition = pos - Shape.Center();
            float lenghtToCenter = localPosition.Length();
           
           
            if (lenghtToCenter <= m_oreDeposits.MinDepth)
            {
                 MyVoxelMaterialDefinition definiton = m_oreDeposits.GetMaterialForPosition(ref pos, lodSize);
                 if (definiton != null)
                 {
                     return definiton;
                 }
            }

            float angleToPole = Vector3.Dot(localPosition / lenghtToCenter,Vector3.Up);

            int nearestMaterial = -1;
            float minDistance = float.MaxValue;
            float noiseValue = (float)(0.5*m_noise.GetValue(pos.X, pos.Y, pos.Z)+0.5);
  

            for (int i = 0; i < m_materialLayers.Length; ++i)
            {
                float heightStartDistance = m_materialLayers[i].HeightStartDeviation * noiseValue;
                float angleStartDistance = m_materialLayers[i].AngleStartDeviation *noiseValue;

                float heightEndDistance = m_materialLayers[i].HeightEndDeviation * noiseValue;
                float angleEndDistance = m_materialLayers[i].AngleEndDeviation * noiseValue;

                if (lenghtToCenter >= (m_materialLayers[i].StartHeight - lodSize - heightStartDistance) && (m_materialLayers[i].EndHeight +heightEndDistance) >= lenghtToCenter &&
                    angleToPole > m_materialLayers[i].StartAngle - angleStartDistance && m_materialLayers[i].EndAngle + angleEndDistance > angleToPole)
                {
                    float distanceTolayer = Math.Abs(lenghtToCenter - m_materialLayers[i].StartHeight + heightStartDistance);
                    if (minDistance > distanceTolayer)
                    {
                        minDistance = distanceTolayer;
                        nearestMaterial = i;
                    }
                    
                }
            }
           
            return nearestMaterial == -1 ? null : m_materialLayers[nearestMaterial].MaterialDefinition;
        }

        public override bool SpawnsFlora()
        {
            for (int i = 0; i < m_materialLayers.Length; ++i)
            {
                if (m_materialLayers[i].MaterialDefinition != null && m_materialLayers[i].MaterialDefinition.SpawnsFlora)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

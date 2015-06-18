using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Game.World.Generator
{
    class MyCompositeShapeOreDeposit
    {
        public readonly MyCsgShapeBase Shape;
        readonly MyVoxelMaterialDefinition m_material;

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

        public MyCompositeLayeredOreDeposit(MyCsgShapeBase shape, MyMaterialLayer[] materialLayers,IMyModule noise) :
            base(shape, null)
        {
            m_materialLayers = materialLayers;
            m_noise = noise;
        }

        public override MyVoxelMaterialDefinition GetMaterialForPosition(ref Vector3 pos, float lodSize)
        {
           Vector3 localPosition = pos - Shape.Center();
            float lenghtToCenter = localPosition.Length();
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

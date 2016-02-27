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
    public class MyOreProbability
    {
        public float CummulativeProbability;
        public string OreName;
    }

    class MyCompositeOrePlanetDeposit : MyCompositeShapeOreDeposit
    {
        float m_minDepth;
        public float MinDepth
        {
            get
            {
                return m_minDepth;
            }
        }

        
        const float DEPOSIT_MAX_SIZE = 1000;
        int m_numDeposits = 0;

        Dictionary<Vector3I, MyCompositeShapeOreDeposit> m_deposits = new Dictionary<Vector3I, MyCompositeShapeOreDeposit>();
        Dictionary<string, List<MyVoxelMaterialDefinition>> m_materialsByOreType = new Dictionary<string, List<MyVoxelMaterialDefinition>>();

        public MyCompositeOrePlanetDeposit(MyCsgShapeBase baseShape, int seed, float minDepth, float maxDepth, MyOreProbability[] oreProbabilties, MyVoxelMaterialDefinition material) :
            base(baseShape, material)
        {

            m_minDepth = minDepth;
            double outherSphereVolume = (4.0 * MathHelper.Pi * Math.Pow(minDepth, 3.0f)) / 3.0;
            double innerSphereVolume = (4.0 * MathHelper.Pi * Math.Pow(maxDepth, 3.0f)) / 3.0;

            double depositVolume = (4.0 * MathHelper.Pi * Math.Pow(DEPOSIT_MAX_SIZE, 3.0f)) / 3.0;
            double volume = outherSphereVolume - innerSphereVolume;

            m_numDeposits = oreProbabilties.Length > 0 ? (int)Math.Floor((volume * 0.4f) / depositVolume) : 0;

            int numSectors = (int)(minDepth / DEPOSIT_MAX_SIZE);

            MyRandom random = MyRandom.Instance;
            FillMaterialCollections();
            Vector3D offset = -new Vector3D(DEPOSIT_MAX_SIZE/2.0);
            using (var stateToken = random.PushSeed(seed))
            {
                for (int i = 0; i < m_numDeposits; ++i)
                {
                    Vector3D direction = MyProceduralWorldGenerator.GetRandomDirection(random);
                    float distanceFromCenter = random.NextFloat(maxDepth,minDepth);
                    Vector3D position = direction * distanceFromCenter;

                    Vector3I cellPos = Vector3I.Ceiling((Shape.Center() + position)/ DEPOSIT_MAX_SIZE);

                    MyCompositeShapeOreDeposit deposit;
                    if (m_deposits.TryGetValue(cellPos, out deposit) == false)
                    {
                        var oreDefinition = GetOre(random.NextFloat(0, 1), oreProbabilties);
                        var materialDefinition = m_materialsByOreType[oreDefinition.OreName][random.Next() % m_materialsByOreType[oreDefinition.OreName].Count];
                        deposit = new MyCompositeShapeOreDeposit(new MyCsgSimpleSphere(cellPos * DEPOSIT_MAX_SIZE + offset, random.NextFloat(64, DEPOSIT_MAX_SIZE / 2.0f)), materialDefinition);
                        m_deposits[cellPos] = deposit;
                    }
                }
            }

            m_materialsByOreType.Clear();
        }

        public override MyVoxelMaterialDefinition GetMaterialForPosition(ref Vector3 pos, float lodSize)
        {
            Vector3I cellPos = Vector3I.Ceiling(pos / DEPOSIT_MAX_SIZE);
            MyCompositeShapeOreDeposit deposit;
            if (m_deposits.TryGetValue(cellPos, out deposit) == true)
            {
                if (deposit.Shape.SignedDistance(ref pos,lodSize,null,null) == -1)
                {
                    return deposit.GetMaterialForPosition(ref pos,lodSize);
                }
            }

            return null;
        }

        private MyOreProbability GetOre(float probability, MyOreProbability[] probalities)
        {
            foreach (var oreProbability in probalities)
            {
                if (oreProbability.CummulativeProbability >= probability)
                {
                    return oreProbability;
                }
            }

            return null;
        }

        private void FillMaterialCollections()
        {
            foreach (var material in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
            {
                if (material.MinedOre != "Organic")
                {
                    List<MyVoxelMaterialDefinition> materialDefinitions;
                    if (false == m_materialsByOreType.TryGetValue(material.MinedOre, out materialDefinitions))
                    {
                        materialDefinitions = new List<MyVoxelMaterialDefinition>();
                    }
                    materialDefinitions.Add(material);
                    m_materialsByOreType[material.MinedOre] = materialDefinitions;
                }
            }
        }

        public override void DebugDraw(ref Vector3D translation, ref Color materialColor)
        {
            foreach (var material in m_deposits)
            {
                material.Value.DebugDraw(ref translation, ref materialColor);
            }
        }
    }
}

using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Noise;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.World.Generator
{
    class MyMaterialLayer
    {
        public float StartAngle = -1.0f;
        public float EndAngle  = 1;
        public float StartHeight;
        public float EndHeight;
        public float HeightStartDeviation = 0.0f;
        public float AngleStartDeviation = 0.0f;
        public float HeightEndDeviation = 0.0f;
        public float AngleEndDeviation = 0.0f;

        public MyVoxelMaterialDefinition MaterialDefinition;

        public MyMaterialLayer()
        {
            StartHeight = 0;
            EndHeight = 0;
            MaterialDefinition = null;
        }

        public MyMaterialLayer(float start, float end, string name)
        {
            StartHeight = start;
            EndHeight = end;
            MaterialDefinition = null;
        }
    }

    internal delegate void MyCompositeShapeGeneratorDelegate(int seed, float size, out MyCompositeShapeGeneratedData data);
    internal delegate void MyCompositeShapeGeneratorPlanetDelegate(ref MyCsgShapePlanetShapeAttributes shapeAttributes, ref MyCsgShapePlanetHillAttributes hillAttributes, ref MyCsgShapePlanetHillAttributes canyonAttributes, MyMaterialLayer[] materialLevels, out MyCompositeShapeGeneratedData data);

    internal static class MyCompositeShapes
    {
        private static readonly List<MyVoxelMaterialDefinition> m_surfaceMaterials = new List<MyVoxelMaterialDefinition>();
        private static readonly List<MyVoxelMaterialDefinition> m_depositMaterials = new List<MyVoxelMaterialDefinition>();
        private static readonly List<MyVoxelMaterialDefinition> m_coreMaterials = new List<MyVoxelMaterialDefinition>();

        /// <summary>
        /// Table of methods that take care of creation of asteroids and possibly other composite shapes.
        /// Backwards compatibility! Do not change indices of these entries!
        /// </summary>
        public static readonly MyCompositeShapeGeneratorDelegate[] AsteroidGenerators = new MyCompositeShapeGeneratorDelegate[]
        {
            Generator0,
            Generator1,
            Generator2,
        };

        public static readonly MyCompositeShapeGeneratorPlanetDelegate[] PlanetGenerators = new MyCompositeShapeGeneratorPlanetDelegate[]
        {
            PlanetGenerator0,
        };

        private static void Generator0(int seed, float size, out MyCompositeShapeGeneratedData data)
        {
            Generator(0, seed, size, out data);
        }
       
        //Added ice material
        private static void Generator1(int seed, float size, out MyCompositeShapeGeneratedData data)
        {
            Generator(1, seed, size, out data);
        }

        private static void Generator2(int seed, float size, out MyCompositeShapeGeneratedData data)
        {
            Generator(2, seed, size, out data);
        }
        private static void PlanetGenerator0(ref MyCsgShapePlanetShapeAttributes shapeAttributes, ref MyCsgShapePlanetHillAttributes hillAttributes, ref MyCsgShapePlanetHillAttributes canyonAttributes, MyMaterialLayer[] materialLevels, out MyCompositeShapeGeneratedData data)
        {
            PlanetGenerator(ref shapeAttributes, ref hillAttributes, ref canyonAttributes, materialLevels, out data);
        }


        private static void PlanetGenerator(ref MyCsgShapePlanetShapeAttributes shapeAttributes, ref MyCsgShapePlanetHillAttributes hillAttributes, ref MyCsgShapePlanetHillAttributes canyonAttributes, MyMaterialLayer[] materialLevels, out MyCompositeShapeGeneratedData data)
        {
            var random = MyRandom.Instance;
            using (var stateToken = random.PushSeed(shapeAttributes.Seed))
            {
                data = new MyCompositeShapeGeneratedData();
                data.FilledShapes = new MyCsgShapeBase[1];
                data.RemovedShapes = new MyCsgShapeBase[0];


                data.MacroModule = new MyBillowFast(quality: MyNoiseQuality.Low,seed: shapeAttributes.Seed, frequency: shapeAttributes.NoiseFrequency / shapeAttributes.Diameter, layerCount: 3);

                data.DetailModule = null;

                float halfSize = shapeAttributes.Radius;
                float storageSize = VRageMath.MathHelper.GetNearestBiggerPowerOfTwo(shapeAttributes.Diameter);
                float halfStorageSize = storageSize * 0.5f;
                float storageOffset = halfStorageSize - halfSize;

                data.FilledShapes[0] = new MyCsgShapePlanet(
                                        new Vector3(halfStorageSize),
                                        ref shapeAttributes,
                                        ref hillAttributes,
                                        ref canyonAttributes,
                                        detailFrequency: 0.09f,
                                        deviationFrequency: 10.0f);


                FillMaterials(2);

                data.DefaultMaterial = m_surfaceMaterials[(int)random.Next() % m_surfaceMaterials.Count];

                int depositCount = 1;
                data.Deposits = new MyCompositeShapeOreDeposit[depositCount];

                for (int i = 0; i < depositCount; ++i)
                {
                    data.Deposits[i] = new MyCompositeLayeredOreDeposit(new MyCsgSimpleSphere(
                                                                        new Vector3(halfStorageSize), halfSize), materialLevels, 
                                                                        new MyBillowFast(layerCount:3,
                                                                        seed: shapeAttributes.LayerDeviationSeed, frequency: shapeAttributes.LayerDeviationNoiseFrequency / shapeAttributes.Diameter));
                }

                m_depositMaterials.Clear();
                m_surfaceMaterials.Clear();
                m_coreMaterials.Clear();
            }
        }

        private static MyVoxelMaterialDefinition GetMaterialByName(String name)
        {
            foreach (var material in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
            {
                if (material.Id.SubtypeName == name)
                {
                    return material;
                }
            }
            return null;
        }

        private static void Generator(int version, int seed, float size, out MyCompositeShapeGeneratedData data)
        {
            var random = MyRandom.Instance;
            using (var stateToken = random.PushSeed(seed))
            {
                data = new MyCompositeShapeGeneratedData();
                data.FilledShapes = new MyCsgShapeBase[2];
                data.RemovedShapes = new MyCsgShapeBase[2];
                data.MacroModule = new MySimplexFast(seed: seed, frequency: 7f / size);
                switch (random.Next() & 0x1)
                {
                    case 0:
                        data.DetailModule = new MyRidgedMultifractalFast(
                            seed: seed,
                            quality: MyNoiseQuality.Low,
                            frequency: random.NextFloat() * 0.09f + 0.11f,
                            layerCount: 1);
                        break;

                    case 1:
                    default:
                        data.DetailModule = new MyBillowFast(
                            seed: seed,
                            quality: MyNoiseQuality.Low,
                            frequency: random.NextFloat() * 0.07f + 0.13f,
                            layerCount: 1);
                        break;
                }

                float halfSize = size * 0.5f;
                float storageSize = VRageMath.MathHelper.GetNearestBiggerPowerOfTwo(size);
                float halfStorageSize = storageSize * 0.5f;
                float storageOffset = halfStorageSize - halfSize;

                MyCsgShapeBase primaryShape;
                { // determine primary shape
                    var primaryType = random.Next() % 3;
                    switch (primaryType)
                    {
                        case 0: //ShapeType.Torus
                            {
                                var secondaryRadius = (random.NextFloat() * 0.05f + 0.1f) * size;
                                var torus = new MyCsgTorus(
                                    translation: new Vector3(halfStorageSize),
                                    invRotation: CreateRandomRotation(random),
                                    primaryRadius: (random.NextFloat() * 0.1f + 0.2f) * size,
                                    secondaryRadius: secondaryRadius,
                                    secondaryHalfDeviation: (random.NextFloat() * 0.4f + 0.4f) * secondaryRadius,
                                    deviationFrequency: random.NextFloat() * 0.8f + 0.2f,
                                    detailFrequency: random.NextFloat() * 0.6f + 0.4f);
                                primaryShape = torus;
                            }
                            break;

                        case 1: //ShapeType.Sphere
                        default:
                            {
                                var sphere = new MyCsgSphere(
                                    translation: new Vector3(halfStorageSize),
                                    radius: (random.NextFloat() * 0.1f + 0.35f) * size,
                                    halfDeviation: (random.NextFloat() * 0.05f + 0.05f) * size + 1f,
                                    deviationFrequency: random.NextFloat() * 0.8f + 0.2f,
                                    detailFrequency: random.NextFloat() * 0.6f + 0.4f);
                                primaryShape = sphere;
                            }
                            break;
                    }
                }

                { // add some additional shapes
                    int filledShapeCount = 0;
                    data.FilledShapes[filledShapeCount++] = primaryShape;
                    while (filledShapeCount < data.FilledShapes.Length)
                    {
                        var fromBorders = size * (random.NextFloat() * 0.2f + 0.1f) + 2f;
                        var fromBorders2 = 2f * fromBorders;
                        var sizeMinusFromBorders2 = size - fromBorders2;
                        var shapeType = random.Next() % 3;
                        switch (shapeType)
                        {
                            case 0: //ShapeType.Sphere
                                {
                                    Vector3 center = CreateRandomPointOnBox(random, sizeMinusFromBorders2) + fromBorders;
                                    float radius = fromBorders * (random.NextFloat() * 0.4f + 0.35f);

                                    MyCsgSphere sphere = new MyCsgSphere(
                                        translation: center + storageOffset,
                                        radius: radius,
                                        halfDeviation: radius * (random.NextFloat() * 0.1f + 0.1f),
                                        deviationFrequency: random.NextFloat() * 0.8f + 0.2f,
                                        detailFrequency: random.NextFloat() * 0.6f + 0.4f);

                                    data.FilledShapes[filledShapeCount++] = sphere;
                                }
                                break;

                            case 1: //ShapeType.Capsule
                                {
                                    var start = CreateRandomPointOnBox(random, sizeMinusFromBorders2) + fromBorders;
                                    var end = new Vector3(size) - start;
                                    if ((random.Next() % 2) == 0) MyUtils.Swap(ref start.X, ref end.X);
                                    if ((random.Next() % 2) == 0) MyUtils.Swap(ref start.Y, ref end.Y);
                                    if ((random.Next() % 2) == 0) MyUtils.Swap(ref start.Z, ref end.Z);
                                    float radius = (random.NextFloat() * 0.25f + 0.5f) * fromBorders;

                                    MyCsgCapsule capsule = new MyCsgCapsule(
                                        pointA: start + storageOffset,
                                        pointB: end + storageOffset,
                                        radius: radius,
                                        halfDeviation: (random.NextFloat() * 0.25f + 0.5f) * radius,
                                        deviationFrequency: (random.NextFloat() * 0.4f + 0.4f),
                                        detailFrequency: (random.NextFloat() * 0.6f + 0.4f));

                                    data.FilledShapes[filledShapeCount++] = capsule;
                                }
                                break;

                            case 2: //ShapeType.Torus
                                {
                                    Vector3 center = CreateRandomPointInBox(random, sizeMinusFromBorders2) + fromBorders;
                                    var rotation = CreateRandomRotation(random);
                                    var borderDistance = ComputeBoxSideDistance(center, size);
                                    var secondaryRadius = (random.NextFloat() * 0.15f + 0.1f) * borderDistance;

                                    var torus = new MyCsgTorus(
                                        translation: center + storageOffset,
                                        invRotation: rotation,
                                        primaryRadius: (random.NextFloat() * 0.2f + 0.5f) * borderDistance,
                                        secondaryRadius: secondaryRadius,
                                        secondaryHalfDeviation: (random.NextFloat() * 0.25f + 0.2f) * secondaryRadius,
                                        deviationFrequency: random.NextFloat() * 0.8f + 0.2f,
                                        detailFrequency: random.NextFloat() * 0.6f + 0.4f);

                                    data.FilledShapes[filledShapeCount++] = torus;
                                }
                                break;
                        }
                    }
                }

                { // make some holes
                    int removedShapesCount = 0;

                    while (removedShapesCount < data.RemovedShapes.Length)
                    {
                        var fromBorders = size * (random.NextFloat() * 0.2f + 0.1f) + 2f;
                        var fromBorders2 = 2f * fromBorders;
                        var sizeMinusFromBorders2 = size - fromBorders2;
                        var shapeType = random.Next() % 7;
                        switch (shapeType)
                        {
                            // Sphere
                            case 0:
                                {
                                    Vector3 center = CreateRandomPointInBox(random, sizeMinusFromBorders2) + fromBorders;

                                    float borderDistance = ComputeBoxSideDistance(center, size);
                                    float radius = (random.NextFloat() * 0.4f + 0.3f) * borderDistance;
                                    MyCsgSphere sphere = new MyCsgSphere(
                                        translation: center + storageOffset,
                                        radius: radius,
                                        halfDeviation: (random.NextFloat() * 0.3f + 0.35f) * radius,
                                        deviationFrequency: (random.NextFloat() * 0.8f + 0.2f),
                                        detailFrequency: (random.NextFloat() * 0.6f + 0.4f));

                                    data.RemovedShapes[removedShapesCount++] = sphere;
                                    break;
                                }

                            // Torus
                            case 1:
                            case 2:
                            case 3:
                                {
                                    Vector3 center = CreateRandomPointInBox(random, sizeMinusFromBorders2) + fromBorders;
                                    var rotation = CreateRandomRotation(random);
                                    var borderDistance = ComputeBoxSideDistance(center, size);
                                    var secondaryRadius = (random.NextFloat() * 0.15f + 0.1f) * borderDistance;

                                    var torus = new MyCsgTorus(
                                        translation: center + storageOffset,
                                        invRotation: rotation,
                                        primaryRadius: (random.NextFloat() * 0.2f + 0.5f) * borderDistance,
                                        secondaryRadius: secondaryRadius,
                                        secondaryHalfDeviation: (random.NextFloat() * 0.25f + 0.2f) * secondaryRadius,
                                        deviationFrequency: random.NextFloat() * 0.8f + 0.2f,
                                        detailFrequency: random.NextFloat() * 0.6f + 0.4f);

                                    data.RemovedShapes[removedShapesCount++] = torus;
                                }
                                break;

                            // Capsule
                            default:
                                {
                                    var start = CreateRandomPointOnBox(random, sizeMinusFromBorders2) + fromBorders;
                                    var end = new Vector3(size) - start;
                                    if ((random.Next() % 2) == 0) MyUtils.Swap(ref start.X, ref end.X);
                                    if ((random.Next() % 2) == 0) MyUtils.Swap(ref start.Y, ref end.Y);
                                    if ((random.Next() % 2) == 0) MyUtils.Swap(ref start.Z, ref end.Z);
                                    float radius = (random.NextFloat() * 0.25f + 0.5f) * fromBorders;

                                    MyCsgCapsule capsule = new MyCsgCapsule(
                                        pointA: start + storageOffset,
                                        pointB: end + storageOffset,
                                        radius: radius,
                                        halfDeviation: (random.NextFloat() * 0.25f + 0.5f) * radius,
                                        deviationFrequency: random.NextFloat() * 0.4f + 0.4f,
                                        detailFrequency: random.NextFloat() * 0.6f + 0.4f);
                                    
                                    data.RemovedShapes[removedShapesCount++] = capsule;
                                }
                                break;
                        }
                    }
                }

                { // generating materials
                    // What to do when we (or mods) change the number of materials? Same seed will then produce different results.
                    FillMaterials(version);

                    Action<List<MyVoxelMaterialDefinition>> shuffleMaterials = (list) =>
                    {
                        int n = list.Count;
                        while (n > 1)
                        {
                            int k = (int)random.Next() % n;
                            n--;
                            var value = list[k];
                            list[k] = list[n];
                            list[n] = value;
                        }
                    };
                    shuffleMaterials(m_depositMaterials);

                    data.DefaultMaterial = m_surfaceMaterials[(int)random.Next() % m_surfaceMaterials.Count];

                    if (false)
                    {
                        data.Deposits = new MyCompositeShapeOreDeposit[data.FilledShapes.Length];
                        int currentMaterial = 0;
                        int depositCount = 0;

                        data.Deposits[depositCount] = new MyCompositeShapeOreDeposit(data.FilledShapes[depositCount].DeepCopy(), m_coreMaterials[(int)random.Next() % m_coreMaterials.Count]);
                        data.Deposits[depositCount].Shape.ShrinkTo(random.NextFloat() * 0.15f + 0.6f);
                        ++depositCount;
                        while (depositCount < data.FilledShapes.Length)
                        {
                            data.Deposits[depositCount] = new MyCompositeShapeOreDeposit(data.FilledShapes[depositCount].DeepCopy(), m_depositMaterials[currentMaterial++]);
                            data.Deposits[depositCount].Shape.ShrinkTo(random.NextFloat() * 0.15f + 0.6f);
                            ++depositCount;
                            if (currentMaterial == m_depositMaterials.Count)
                            {
                                currentMaterial = 0;
                                shuffleMaterials(m_depositMaterials);
                            }
                        }
                    }
                    else
                    {
                        int depositCount = Math.Max((int)Math.Log(size), data.FilledShapes.Length);
                        data.Deposits = new MyCompositeShapeOreDeposit[depositCount];

                        var depositSize = size / 10f;

                        int currentMaterial = 0;
                        for (int i = 0; i < data.FilledShapes.Length; ++i)
                        {
                            MyVoxelMaterialDefinition material;
                            if (i == 0)
                            {
                                material = m_coreMaterials[(int)random.Next() % m_coreMaterials.Count];
                            }
                            else
                            {
                                material = m_depositMaterials[currentMaterial++];
                            }
                            data.Deposits[i] = new MyCompositeShapeOreDeposit(data.FilledShapes[i].DeepCopy(), material);
                            data.Deposits[i].Shape.ShrinkTo(random.NextFloat() * 0.15f + 0.6f);
                            if (currentMaterial == m_depositMaterials.Count)
                            {
                                currentMaterial = 0;
                                shuffleMaterials(m_depositMaterials);
                            }
                        }
                        for (int i = data.FilledShapes.Length; i < depositCount; ++i)
                        {
                            var center = CreateRandomPointInBox(random, size * 0.7f) + storageOffset + size * 0.15f;
                            var radius = random.NextFloat() * depositSize + 8f;
                            random.NextFloat();random.NextFloat();//backwards compatibility
                            MyCsgShapeBase shape = new MyCsgSphere(center, radius);
                            data.Deposits[i] = new MyCompositeShapeOreDeposit(shape, m_depositMaterials[currentMaterial++]);
                            if (currentMaterial == m_depositMaterials.Count)
                            {
                                currentMaterial = 0;
                                shuffleMaterials(m_depositMaterials);
                            }
                        }
                    }

                    m_surfaceMaterials.Clear();
                    m_coreMaterials.Clear();
                    m_depositMaterials.Clear();
                }
            }
        }

        private static void FillMaterials(int version)
        {
            m_depositMaterials.Clear();
            m_surfaceMaterials.Clear();
            m_coreMaterials.Clear();

            foreach (var material in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
            {
                if (!material.SpawnsInAsteroids || material.MinVersion > version) // filter out non-natural and version-incompatible materials
                    continue;

                if (material.MinedOre == "Stone") // Surface
                    m_surfaceMaterials.Add(material);
                else if (material.MinedOre == "Iron") // Core
                    m_coreMaterials.Add(material);
                else if (material.MinedOre == "Uranium") // Uranium
                {
                    // We want more uranium, by design
                    m_depositMaterials.Add(material);
                    m_depositMaterials.Add(material);
                }
                else if (material.MinedOre == "Ice")
                {
                    // We also want more ice, by design
                    m_depositMaterials.Add(material);
                    m_depositMaterials.Add(material);
                }
                else
                    m_depositMaterials.Add(material);
            }

            if (m_surfaceMaterials.Count == 0) // this can happen if all materials are disabled or set to not spawn in asteroids
                throw new Exception("There are no voxel materials allowed to spawn in asteroids!");
        }

        private static Vector3 CreateRandomPointInBox(MyRandom self, float boxSize)
        {
            return new Vector3(
                self.NextFloat() * boxSize,
                self.NextFloat() * boxSize,
                self.NextFloat() * boxSize);
        }

        private static Vector3 CreateRandomPointOnBox(MyRandom self, float boxSize)
        {
            Vector3 result = Vector3.Zero;
            switch (self.Next() & 6)
            {// each side of a box
                case 0: return new Vector3(0f, self.NextFloat(), self.NextFloat());
                case 1: return new Vector3(1f, self.NextFloat(), self.NextFloat());
                case 2: return new Vector3(self.NextFloat(), 0f, self.NextFloat());
                case 3: return new Vector3(self.NextFloat(), 1f, self.NextFloat());
                case 4: return new Vector3(self.NextFloat(), self.NextFloat(), 0f);
                case 5: return new Vector3(self.NextFloat(), self.NextFloat(), 1f);
            }
            result *= boxSize;
            return result;
        }

        private static Quaternion CreateRandomRotation(MyRandom self)
        {
            Quaternion q = new Quaternion(
                self.NextFloat() * 2f - 1f,
                self.NextFloat() * 2f - 1f,
                self.NextFloat() * 2f - 1f,
                self.NextFloat() * 2f - 1f);
            q.Normalize();
            return q;
        }

        private static float ComputeBoxSideDistance(Vector3 point, float boxSize)
        {
            return Vector3.Min(point, new Vector3(boxSize) - point).Min();
        }
    }
}

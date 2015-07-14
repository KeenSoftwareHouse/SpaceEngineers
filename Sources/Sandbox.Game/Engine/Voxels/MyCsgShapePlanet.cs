using ParallelTasks;
using Sandbox.Definitions;
using Sandbox.Game.World.Generator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Noise;
using VRageMath;
using VRageMath.PackedVector;

namespace Sandbox.Engine.Voxels
{
    public struct MyCsgShapePlanetMaterialAttributes
    {
        public MyMaterialLayer[] Layers;
        public MyOreProbability[] OreProbabilities;

        public float OreStartDepth;
        public float OreEndDepth;

        public void WriteTo(Stream stream)
        {
            if (Layers != null)
            {
                stream.WriteNoAlloc(Layers.Length);
                for (int i = 0; i < Layers.Length; ++i)
                {
                    stream.WriteNoAlloc(Layers[i].StartHeight);
                    stream.WriteNoAlloc(Layers[i].EndHeight);
                    stream.WriteNoAlloc(Layers[i].StartAngle);
                    stream.WriteNoAlloc(Layers[i].EndAngle);
                    stream.WriteNoAlloc(Layers[i].HeightStartDeviation);
                    stream.WriteNoAlloc(Layers[i].AngleStartDeviation);
                    stream.WriteNoAlloc(Layers[i].HeightEndDeviation);
                    stream.WriteNoAlloc(Layers[i].AngleEndDeviation);
                    stream.WriteNoAlloc(Layers[i].MaterialDefinition.Id.SubtypeName);
                }
            }
            else
            {
                stream.WriteNoAlloc((int)0);
            }

            if (OreProbabilities != null)
            {
                stream.WriteNoAlloc(OreProbabilities.Length);
                for (int i = 0; i < OreProbabilities.Length; ++i)
                {
                    stream.WriteNoAlloc(OreProbabilities[i].CummulativeProbability);
                    stream.WriteNoAlloc(OreProbabilities[i].OreName);
                }
            }
            else
            {
                stream.WriteNoAlloc((int)0);
            }

            stream.WriteNoAlloc(OreStartDepth);
            stream.WriteNoAlloc(OreEndDepth);
        }
        public void ReadFrom(Stream stream)
        {
            int numMaterials = stream.ReadInt32();
            Layers = new MyMaterialLayer[numMaterials];
            for (int i = 0; i < numMaterials; ++i)
            {
                Layers[i] = new MyMaterialLayer();
                Layers[i].StartHeight = stream.ReadFloat();
                Layers[i].EndHeight = stream.ReadFloat();
                Layers[i].StartAngle = stream.ReadFloat();
                Layers[i].EndAngle = stream.ReadFloat();
                Layers[i].HeightStartDeviation = stream.ReadFloat();
                Layers[i].AngleStartDeviation = stream.ReadFloat();
                Layers[i].HeightEndDeviation = stream.ReadFloat();
                Layers[i].AngleEndDeviation = stream.ReadFloat();
                Layers[i].MaterialDefinition = MyDefinitionManager.Static.GetVoxelMaterialDefinition(stream.ReadString());
            }

            int numOreProbabilities = stream.ReadInt32();
            OreProbabilities = new MyOreProbability[numOreProbabilities];
            for (int i = 0; i < numOreProbabilities; ++i)
            {
                OreProbabilities[i] = new MyOreProbability();
                OreProbabilities[i].CummulativeProbability = stream.ReadFloat();
                OreProbabilities[i].OreName = stream.ReadString();
            }

            OreStartDepth = stream.ReadFloat();
            OreEndDepth = stream.ReadFloat();
        }
    }

    public struct MyCsgShapePlanetShapeAttributes
    {
        public float NoiseFrequency;
        public int Seed;
        public float Diameter;
        public float Radius;
        public float DeviationScale;
        public float LayerDeviationNoiseFrequency;
        public int LayerDeviationSeed;

        public void WriteTo(Stream stream)
        {
            stream.WriteNoAlloc(Seed);
            stream.WriteNoAlloc(Radius);
            stream.WriteNoAlloc(NoiseFrequency);
            stream.WriteNoAlloc(DeviationScale);
            stream.WriteNoAlloc(LayerDeviationNoiseFrequency);
            stream.WriteNoAlloc(LayerDeviationSeed);
        }
        public void ReadFrom(Stream stream)
        {
            Seed = stream.ReadInt32();
            Radius = stream.ReadFloat();
            NoiseFrequency = stream.ReadFloat();
            DeviationScale = stream.ReadFloat();
            LayerDeviationNoiseFrequency = stream.ReadFloat();
            LayerDeviationSeed = stream.ReadInt32();
            Diameter = Radius * 2.0f;
        }
    }

    public struct MyCsgShapePlanetHillAttributes
    {
        public float Treshold;
        public float BlendTreshold;
        public float SizeRatio;
        public float Frequency;
        public int NumNoises;


        public void WriteTo(Stream stream)
        {
            stream.WriteNoAlloc(BlendTreshold);
            stream.WriteNoAlloc(Treshold);
            stream.WriteNoAlloc(SizeRatio);
            stream.WriteNoAlloc(NumNoises);
            stream.WriteNoAlloc(Frequency);
        }
        public void ReadFrom(Stream stream)
        {
            BlendTreshold = stream.ReadFloat();
            Treshold = stream.ReadFloat();
            SizeRatio = stream.ReadFloat();
            NumNoises = stream.ReadInt32();
            Frequency = stream.ReadFloat();
        }
    }

    class MyCsgShapePlanet : MyCsgShapeBase
    {
        const bool ENABLE_NOISE_CACHING = true;
        const bool USE_STEREOGRAPHIC = true; 

        MyCsgShapePlanetShapeAttributes m_shapeAttributes;
        MyCsgShapePlanetHillAttributes m_hillAttributes;
        MyCsgShapePlanetHillAttributes m_canyonAttributes;

        private MyCompositeNoise m_hillModule;
        private float m_hillHalfDeviation;
        private float m_hillBlendTreshold;

        private float m_canyonHalfDeviation;
        private float m_canyonBlendTreshold;

        private float m_deviationFrequency;
        private float m_detailFrequency;
        private float m_halfDeviation;


        private float m_outerRadius;
        private float m_innerRadius;

        private Vector3 m_translation;

        const int NOISE_RESOLUTION = 1024;
        int m_tapSize;

        HalfVector2[] m_cachedNoise;

        void Encode(ref Vector3 n, ref Vector2 ret)
        {
            if (USE_STEREOGRAPHIC)
            {
                ret.X = (float)(Math.Atan2(n.Y, n.X) / Math.PI + 1.0) * 0.5f;
                ret.Y = (n.Z + 1.0f) * 0.5f;
            }
            else
            {
                float f = (float)Math.Sqrt(8 * n.Z + 8);
                ret.X = n.X / f + 0.5f;
                ret.Y = n.Y / f + 0.5f;
            }
        }

        void Decode(ref Vector2 enc, ref Vector3 ret)
        {
            if (USE_STEREOGRAPHIC)
            {
                Vector2 ang = enc * 2 - 1;
                Vector2 scth;
                scth.X = (float)Math.Sin(ang.X * Math.PI);
                scth.Y = (float)Math.Cos(ang.X * Math.PI);

                Vector2 scphi = new Vector2((float)Math.Sqrt(1.0 - ang.Y * ang.Y), ang.Y);
                ret.X = scth.Y * scphi.X;
                ret.Y = scth.X * scphi.X;
                ret.Z = scphi.Y;
            }
            else
            {
                Vector2 fenc = enc * 4 - 2;
                float f = Vector2.Dot(fenc, fenc);
                float g = (float)Math.Sqrt(1 - f / 4);

                ret.X = fenc.X * g;
                ret.Y = fenc.Y * g;
                ret.Z = 1 - f / 2;
            }
        }

        public MyCsgShapePlanet(Vector3 translation, ref MyCsgShapePlanetShapeAttributes shapeAttributes, ref MyCsgShapePlanetHillAttributes hillAttributes, ref MyCsgShapePlanetHillAttributes canyonAttributes, float deviationFrequency = 0, float detailFrequency = 0)
        {
            m_detailSize = 1.0f;
            m_translation = translation;
            m_shapeAttributes = shapeAttributes;
            m_hillAttributes = hillAttributes;
            m_canyonAttributes = canyonAttributes;

            m_canyonBlendTreshold = m_canyonAttributes.Treshold + m_canyonAttributes.BlendTreshold;
            m_hillBlendTreshold = m_hillAttributes.Treshold - m_hillAttributes.BlendTreshold;

            m_shapeAttributes.Radius = (shapeAttributes.Diameter / 2.0f) * (1 - shapeAttributes.DeviationScale * m_hillAttributes.SizeRatio);
            m_shapeAttributes.Diameter = m_shapeAttributes.Radius * 2.0f;
            m_halfDeviation = (shapeAttributes.Diameter / 2.0f) * shapeAttributes.DeviationScale;

            m_deviationFrequency = deviationFrequency;
            m_detailFrequency = detailFrequency;

            m_hillHalfDeviation = m_halfDeviation * m_hillAttributes.SizeRatio;
            m_canyonHalfDeviation = m_halfDeviation * m_canyonAttributes.SizeRatio;

            m_enableModulation = true;

            m_hillModule = new MyCompositeNoise(hillAttributes.NumNoises, hillAttributes.Frequency / m_shapeAttributes.Radius);

            ComputeDerivedProperties();
        }

        internal override void GenerateNoiseHelpTexture(int storageSize, IMyModule macroModulator)
        {
            if (m_cachedNoise != null || !ENABLE_NOISE_CACHING)
            {
                return;
            }

            m_tapSize = storageSize / NOISE_RESOLUTION;

            Vector2 encodedPosition = Vector2.Zero;
            float halfDistance = 1 / (2.0f * NOISE_RESOLUTION);
            Vector3 encoded = Vector3.Zero;
            Vector3 localPos = Vector3.Zero;
            m_cachedNoise = new HalfVector2[(NOISE_RESOLUTION + 1) * (NOISE_RESOLUTION + 1)];
            for (int i = 0; i <= NOISE_RESOLUTION; ++i)
            {
                encodedPosition.X = (i / (float)NOISE_RESOLUTION);
                encodedPosition.X += halfDistance;
                for (int j = 0; j <= NOISE_RESOLUTION; ++j)
                {
                    encodedPosition.Y = (j / (float)NOISE_RESOLUTION);
                    encodedPosition.Y += halfDistance;

                    Decode(ref encodedPosition, ref  encoded);

                    if (encoded.X.IsValid() == false || encoded.Y.IsValid()==false || encoded.Z.IsValid()==false)
                    {
                        m_cachedNoise[i * (NOISE_RESOLUTION + 1) + j] = new HalfVector2(-m_hillHalfDeviation, m_canyonHalfDeviation);
                        continue;
                    }

                    localPos = encoded * m_shapeAttributes.Radius;
                    m_cachedNoise[i * (NOISE_RESOLUTION + 1) + j] = CalculateNoiseValuesAtPoint(macroModulator, ref localPos);
                }
            }

        }

        HalfVector2 CalculateNoiseValuesAtPoint(IMyModule macroModulator, ref Vector3 localPos)
        {       
            var tmp = localPos * m_deviationFrequency;
            float noiseValue = (float)macroModulator.GetValue(tmp.X, tmp.Y, tmp.Z);
            noiseValue = MathHelper.Clamp(noiseValue, -1, 1);
            Vector2 noiseValues = Vector2.Zero;

            noiseValues.X = GetValueNoiseValue(noiseValue + m_shapeAttributes.NoiseFrequency/30.0f, false, ref tmp);
            noiseValues.Y = GetValueNoiseValue(noiseValue - m_shapeAttributes.NoiseFrequency/30.0f, true,ref tmp);

            return new HalfVector2(noiseValues);
        }

        float GetValueNoiseValue(float noiseValue,bool min,ref Vector3 pos)
        {
            float retVal = 0;

            if (noiseValue > m_hillBlendTreshold)
            {
                float hillValue = (float)m_hillModule.GetValue(pos.X, pos.Y, pos.Z);
                float maxHillValue = MathHelper.Saturate(hillValue + m_hillAttributes.Frequency /5.0f);
                float minHillValue = MathHelper.Saturate(hillValue - m_hillAttributes.Frequency /5.0f);

                float blendValue = MathHelper.Saturate((noiseValue - m_hillBlendTreshold) / (m_hillAttributes.Treshold - m_hillBlendTreshold));
                if (min)
                {
                    retVal = -MathHelper.Lerp(noiseValue * m_halfDeviation, minHillValue * m_hillHalfDeviation, blendValue);
                }
                else
                {
                    retVal = -MathHelper.Lerp(noiseValue * m_halfDeviation, maxHillValue * m_hillHalfDeviation, blendValue);
                }
            }
            else if (noiseValue < m_canyonBlendTreshold)
            {

                float blendValue = MathHelper.Saturate((noiseValue - m_canyonBlendTreshold) / (m_canyonAttributes.Treshold - m_canyonBlendTreshold));
                retVal -= MathHelper.Lerp(noiseValue * m_halfDeviation, -m_canyonHalfDeviation, blendValue);
            }
            else
            {
                retVal -= noiseValue * m_halfDeviation;
            }

            retVal -= m_detailSize;

            return retVal;
        }

        internal override void ReleaseNoiseTexture() 
        {
            m_cachedNoise = null;
        }

        internal override ContainmentType Contains(ref BoundingBox queryAabb, ref BoundingSphere querySphere, float lodVoxelSize)
        {
            ContainmentType outerContainment, innerContainment;

            BoundingSphere sphere = new BoundingSphere(
                m_translation,
                m_outerRadius + lodVoxelSize);

            sphere.Contains(ref queryAabb, out outerContainment);
            if (outerContainment == ContainmentType.Disjoint)
                return ContainmentType.Disjoint;

            sphere.Radius = m_innerRadius - lodVoxelSize;
            sphere.Contains(ref queryAabb, out innerContainment);
            if (innerContainment == ContainmentType.Contains)
                return ContainmentType.Contains;

            if (m_cachedNoise != null)
            {
                float minDistance = m_canyonHalfDeviation;
                float maxDistance = -m_hillHalfDeviation;

                unsafe
                {
                    const int cornersLength = 8;
                    Vector3* corners = stackalloc Vector3[cornersLength];
                    queryAabb.GetCornersUnsafe(corners);
                    bool first = true;

                    for (int i = 0; i < cornersLength; ++i)
                    {
                        Vector3 localPosition = corners[i] - m_translation;
                        float distanceMin = localPosition.Length();

                        localPosition.Normalize();

                        Vector2 encodedPosition = Vector2.Zero;

                        Encode(ref localPosition, ref encodedPosition);

                        Vector2 samplePosition = encodedPosition * NOISE_RESOLUTION;
                        Vector2I position = Vector2I.Floor(samplePosition);

                        Vector2 unpackedValue = m_cachedNoise[position.X*(NOISE_RESOLUTION + 1)+position.Y].ToVector2();
                        if (first)
                        {
                            minDistance = unpackedValue.Y;
                            maxDistance = unpackedValue.X;
                            first = false;
                        }
                        else
                        {
                            minDistance = MathHelper.Max(minDistance, unpackedValue.Y);
                            maxDistance = MathHelper.Min(maxDistance, unpackedValue.X);
                        }
                    }
                }
       
                sphere.Radius = m_shapeAttributes.Radius - maxDistance + lodVoxelSize;

                sphere.Contains(ref queryAabb, out outerContainment);
                if (outerContainment == ContainmentType.Disjoint)
                    return ContainmentType.Disjoint;

                sphere.Radius = m_shapeAttributes.Radius - minDistance - lodVoxelSize;
                sphere.Contains(ref queryAabb, out innerContainment);
                if (innerContainment == ContainmentType.Contains)
                    return ContainmentType.Contains;
            }

            return ContainmentType.Intersects;
        }

        Vector3 GetSectorPositionOnSphere(ref Vector2I currentSector)
        {
            float halfDistance = 1 / (2.0f * NOISE_RESOLUTION);

            Vector2 centerPositionTexture = Vector2.Zero;
            centerPositionTexture.X = 2.0f * currentSector.X / (float)NOISE_RESOLUTION - 1.0f;
            centerPositionTexture.X += halfDistance;
            centerPositionTexture.Y = currentSector.Y / (float)NOISE_RESOLUTION + halfDistance;
            Vector3 centerPositionSphere = Vector3.Zero;
            Decode(ref centerPositionTexture, ref  centerPositionSphere);

            return centerPositionSphere;
        }

        internal override float SignedDistance(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator)
        {
            Vector3 localPosition = position - m_translation;
            float distance = localPosition.Length();
            if ((m_innerRadius - lodVoxelSize) > distance)
                return -1f;
            if ((m_outerRadius + lodVoxelSize) < distance)
                return 1f;

            float? reconstructeddNoise = null;
            if (m_cachedNoise != null && distance > 0.0f)
            {
                Vector3 localPosNormalized = localPosition;
                localPosNormalized.Normalize();

                Vector2 encodedPosition = Vector2.Zero;
                Encode(ref localPosNormalized, ref encodedPosition);
                Vector2 samplePosition = encodedPosition * NOISE_RESOLUTION;
                Vector2I currentSector = Vector2I.Floor(samplePosition);
                Debug.Assert(currentSector.X >= 0,currentSector.X.ToString());
                Debug.Assert(currentSector.Y >= 0, currentSector.Y.ToString());

                if (currentSector.X >= 0 && currentSector.Y >= 0)
                {
                    Vector2 noiseValues = m_cachedNoise[currentSector.X * (NOISE_RESOLUTION + 1) + currentSector.Y].ToVector2();

                    if ((m_shapeAttributes.Radius - noiseValues.X + lodVoxelSize) < distance)
                    {
                        return 1f;
                    }

                    if ((m_shapeAttributes.Radius - noiseValues.Y - lodVoxelSize) > distance)
                    {
                        return -1f;
                    }
                }
            }

            return SignedDistanceInternal(lodVoxelSize, macroModulator, detailModulator, ref localPosition, distance, reconstructeddNoise);
        }

        private float SignedDistanceInternal(float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator, ref Vector3 localPosition, float distance,float? noiseValue=null)
        {
            float signedDistance = distance - m_shapeAttributes.Radius;
            float normalizer = m_deviationFrequency * m_shapeAttributes.Radius / distance;
            var tmp = localPosition * normalizer;

            float terrainValue = (float)macroModulator.GetValue(tmp.X, tmp.Y, tmp.Z);

            if (terrainValue > m_hillBlendTreshold)
            {
                float hillValue = (float)m_hillModule.GetValue(tmp.X, tmp.Y, tmp.Z);

                float blendValue = MathHelper.Saturate((terrainValue - m_hillBlendTreshold) / (m_hillAttributes.Treshold - m_hillBlendTreshold));
                signedDistance -= MathHelper.Lerp(terrainValue * m_halfDeviation, hillValue * m_hillHalfDeviation, blendValue);
            }
            else if (terrainValue < m_canyonBlendTreshold)
            {
                float blendValue = MathHelper.Saturate((terrainValue - m_canyonBlendTreshold) / (m_canyonAttributes.Treshold - m_canyonBlendTreshold));
                signedDistance -= MathHelper.Lerp(terrainValue * m_halfDeviation, -m_canyonHalfDeviation, blendValue);
            }
            else 
            {
                signedDistance -= terrainValue * m_halfDeviation;
                normalizer = m_detailFrequency * m_shapeAttributes.Radius / distance;
                tmp = localPosition * normalizer;
                signedDistance -= m_detailSize * (float)detailModulator.GetValue(tmp.X, tmp.Y, tmp.Z);
            }

            return signedDistance / lodVoxelSize;
        }

        internal override float SignedDistanceUnchecked(ref Vector3 position, float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator)
        {
            Vector3 localPosition = position - m_translation;
            float distance = localPosition.Length();

            return SignedDistanceInternal(lodVoxelSize, macroModulator, detailModulator, ref localPosition, distance);
        }

        internal override void DebugDraw(ref Vector3D worldTranslation, Color color)
        {
            VRageRender.MyRenderProxy.DebugDrawSphere(worldTranslation + m_translation, m_shapeAttributes.Diameter, color.ToVector3(), alpha: 0.5f, depthRead: true, smooth: false);
            foreach (var noiseSector in m_cachedNoise)
            {

            }
        }

        internal override MyCsgShapeBase DeepCopy()
        {
            return new MyCsgShapePlanet(
                m_translation,
                ref m_shapeAttributes,
                ref m_hillAttributes,
                ref m_canyonAttributes,
                m_deviationFrequency,
                m_detailFrequency);
        }

        internal override void ShrinkTo(float percentage)
        {
            m_shapeAttributes.Radius *= percentage;
            m_shapeAttributes.Diameter *= percentage;
            m_halfDeviation *= percentage;
            m_hillAttributes.SizeRatio *= percentage;
            m_canyonAttributes.SizeRatio *= percentage;

            m_canyonHalfDeviation = m_halfDeviation * m_canyonAttributes.SizeRatio;
            m_hillHalfDeviation = m_halfDeviation * m_hillAttributes.SizeRatio;

            ComputeDerivedProperties();
        }

        private void ComputeDerivedProperties()
        {
            m_outerRadius = m_shapeAttributes.Radius + m_hillHalfDeviation + m_detailSize;
            m_innerRadius = m_shapeAttributes.Radius - m_canyonHalfDeviation - m_detailSize;
        }

        internal override Vector3 Center()
        {
            return m_translation;
        }

    }
}

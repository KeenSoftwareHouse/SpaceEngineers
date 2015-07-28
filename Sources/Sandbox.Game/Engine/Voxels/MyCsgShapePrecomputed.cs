using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    class MyCsgPrecomputedHelpres
    {
        class MyFaceConversion
        {
            public int NewPos;
            public Vector3I YOffset;
        }

        public static float FROZEN_OCEAN_LEVEL = 80.0f;
        public const int NUM_MAPS = 6;

        static MyFaceConversion[] m_faceConversionX;

        static MyCsgPrecomputedHelpres()
        {
            m_faceConversionX = new MyFaceConversion[NUM_MAPS];

            m_faceConversionX[(int)Faces.XPositive] = new MyFaceConversion();
            m_faceConversionX[(int)Faces.XPositive].NewPos = (int)Faces.ZNegative;
            m_faceConversionX[(int)Faces.XPositive].YOffset = new Vector3I(0, 1, 0);

            m_faceConversionX[(int)Faces.XNegative] = new MyFaceConversion();
            m_faceConversionX[(int)Faces.XNegative].NewPos = (int)Faces.ZPositive;
            m_faceConversionX[(int)Faces.XNegative].YOffset = new Vector3I(0, 1, 0);

            m_faceConversionX[(int)Faces.ZNegative] = new MyFaceConversion();
            m_faceConversionX[(int)Faces.ZNegative].NewPos = (int)Faces.XNegative;
            m_faceConversionX[(int)Faces.ZNegative].YOffset = new Vector3I(0, 1, 0);

            m_faceConversionX[(int)Faces.ZPositive] = new MyFaceConversion();
            m_faceConversionX[(int)Faces.ZPositive].NewPos = (int)Faces.XPositive;
            m_faceConversionX[(int)Faces.ZPositive].YOffset = new Vector3I(0, 1, 0);

            m_faceConversionX[(int)Faces.YPositive] = new MyFaceConversion();
            m_faceConversionX[(int)Faces.YPositive].NewPos = (int)Faces.XNegative;
            m_faceConversionX[(int)Faces.YPositive].YOffset = new Vector3I(-1, 0, 1);

            m_faceConversionX[(int)Faces.YNegative] = new MyFaceConversion();
            m_faceConversionX[(int)Faces.YNegative].NewPos = (int)Faces.XPositive;
            m_faceConversionX[(int)Faces.YNegative].YOffset = new Vector3I(1, 0, 0);
        }

        enum Faces : byte
        {
            XPositive,
            XNegative,
            YPositive,
            YNegative,
            ZPositive,
            ZNegative,
        }

        static public void GetNameForFace(int i, ref string name)
        {
            switch (i)
            {
                case (int)Faces.XPositive:
                    name = "left";
                    break;
                case (int)Faces.XNegative:
                    name = "right";
                    break;
                case (int)Faces.YPositive:
                    name = "up";
                    break;
                case (int)Faces.YNegative:
                    name = "down";
                    break;
                case (int)Faces.ZPositive:
                    name = "back";
                    break;
                case (int)Faces.ZNegative:
                    name = "front";
                    break;
            }
        }

        static public void CalculateSamplePosition(ref Vector3 localPos, out Vector3I samplePosition, ref Vector2 texCoord, int resolution)
        {
            Vector3 abs = Vector3.Abs(localPos);
            float maxAbsValue = Math.Max(abs.X, Math.Max(abs.Y, abs.Z));
            Vector3 max = localPos.MaxAbsComponent();
            localPos /= maxAbsValue;

            int originalFacePosition = (int)Faces.XNegative;
            if (abs.X == maxAbsValue)
            {
                texCoord.X = localPos.Z;
                texCoord.Y = -localPos.Y;

                if (localPos.X > 0.0)
                {
                    texCoord.X = -localPos.Z;
                    originalFacePosition = (int)Faces.XPositive;
                }
            }
            else if (abs.Y == maxAbsValue)
            {
                originalFacePosition = (int)Faces.YNegative;
                texCoord.X = localPos.X;
                texCoord.Y = -localPos.Z;
                if (localPos.Y > 0.0)
                {
                    texCoord.X = -localPos.X;
                    originalFacePosition = (int)Faces.YPositive;
                }
            }
            else if (abs.Z == maxAbsValue)
            {
                originalFacePosition = (int)Faces.ZNegative;
                texCoord.X = -localPos.X;
                texCoord.Y = -localPos.Y;
                if (localPos.Z > 0.0)
                {
                    originalFacePosition = (int)Faces.ZPositive;
                    texCoord.X = localPos.X;
                }
            }

            texCoord = (texCoord + 1) / 2.0f;
            texCoord *= (resolution - 1);

            samplePosition.X = originalFacePosition;
            samplePosition.Y = (int)(texCoord.X);
            samplePosition.Z = (int)(texCoord.Y);
        }

        static public int GetFaceForTexcoord(ref Vector2I newSamplePos, int pos,int resolution)
        {
            if (newSamplePos.X >= resolution)
            {
                MyFaceConversion conversion = m_faceConversionX[pos];
                newSamplePos.Y = resolution * conversion.YOffset.Z + newSamplePos.X * conversion.YOffset.X + newSamplePos.Y * conversion.YOffset.Y;
                newSamplePos.X = 0;
                pos = conversion.NewPos;                   
            }

            return pos;
        }

    }

    class MyPrecomputedHeader
    {
        public ushort MaxValue = ushort.MaxValue;
        public ushort BatchSize = 32;
        public int TypeSize = sizeof(ushort);
        public int Resolution = 0; 
    }

    class MyCsgShapePrecomputed : MyCsgShapeBase
    {   
        MyPrecomputedHeader m_header = new MyPrecomputedHeader();
        const int NUM_SAMPLES = 1;
        const bool USE_MEMORY = false;

        private int m_headerLenght = 0;
        private float m_averageRadius;
        private float m_outerRadius;
        private float m_innerRadius;
        private float m_minValue;
        private float m_maxValue;
        private int m_sectorCount;
        private int m_batchLength;

        private string m_dataFileName;

        MemoryMappedFile[] m_file;
        MemoryMappedViewAccessor[][] m_reader;
        Vector3 m_translation;
        float m_maxHillHeight;

        public MyCsgShapePrecomputed(Vector3 translation, float averageRadius, string folderPath, float maxHillHeight)
        {
            m_maxHillHeight = maxHillHeight;

            m_file = new MemoryMappedFile[MyCsgPrecomputedHelpres.NUM_MAPS];
            m_reader = new MemoryMappedViewAccessor[MyCsgPrecomputedHelpres.NUM_MAPS][];

            for (int i = 0; i < MyCsgPrecomputedHelpres.NUM_MAPS; ++i)
            {
                string name = null;
                MyCsgPrecomputedHelpres.GetNameForFace(i, ref name);
                    
                name = Path.Combine(folderPath, name + ".bin");
                FileInfo fi = new FileInfo(name);
                long length = fi.Length;

                m_file[i] = MemoryMappedFile.CreateFromFile(name, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
                
                // read header separately
                m_headerLenght = 2 * sizeof(int) + 2 * sizeof(ushort);
                var headerReader = m_file[i].CreateViewAccessor(0, m_headerLenght, MemoryMappedFileAccess.Read);
                headerReader.Read(0, out m_header.MaxValue);
                headerReader.Read(sizeof(ushort), out m_header.BatchSize);
                headerReader.Read(2 * sizeof(ushort), out m_header.TypeSize);
                headerReader.Read(2 * sizeof(ushort) + sizeof(int), out m_header.Resolution);
                headerReader.Dispose();

                var batchSectorsCount = m_header.Resolution / m_header.BatchSize;
                long batchLength = m_header.TypeSize * m_header.BatchSize * m_header.BatchSize;

                m_reader[i] = new MemoryMappedViewAccessor[batchSectorsCount * batchSectorsCount];

                for (int j = 0; j < batchSectorsCount; ++j)
                {
                    for (int k = 0; k < batchSectorsCount; ++k)
                    {
                        int batchIndex = j * batchSectorsCount + k;
                        long offset = m_headerLenght + j * batchSectorsCount * batchLength + k * batchLength;
                        long size = batchLength;
                        m_reader[i][batchIndex] = m_file[i].CreateViewAccessor(offset, size, MemoryMappedFileAccess.Read);
                    }
                }
            }

            m_batchLength = m_header.BatchSize * m_header.BatchSize * m_header.TypeSize;
            m_sectorCount = m_header.Resolution / m_header.BatchSize;
            m_averageRadius = averageRadius;
            m_translation = translation;
            m_innerRadius = averageRadius;
            m_outerRadius = averageRadius + m_maxHillHeight;
        }

        internal override ContainmentType Contains(ref BoundingBox queryAabb, ref BoundingSphere querySphere, float lodVoxelSize)
        {
           ContainmentType outerContainment, innerContainment;

            BoundingSphere sphere = new BoundingSphere(
                m_translation,
                m_outerRadius+ lodVoxelSize);

            sphere.Contains(ref queryAabb, out outerContainment);
            if (outerContainment == ContainmentType.Disjoint)
                return ContainmentType.Disjoint;

            sphere.Radius = m_innerRadius - lodVoxelSize;
            sphere.Contains(ref queryAabb, out innerContainment);
            if (innerContainment == ContainmentType.Contains)
                return ContainmentType.Contains;

            float minDistance = float.MaxValue;
            float maxDistance = -float.MaxValue;

            Vector3 localPosition = queryAabb.Min - m_translation;
            float distance = localPosition.LengthSquared();
            if(distance < 0.01f)
            {
                return ContainmentType.Intersects;
            }

            Vector3I samplePos;
            Vector2 pos = Vector2.Zero;
            MyCsgPrecomputedHelpres.CalculateSamplePosition(ref localPosition, out samplePos, ref pos, m_header.Resolution);

            float value = GetValueForPosition(ref samplePos, ref pos, true);

            minDistance = MathHelper.Min(minDistance, value);
            maxDistance = MathHelper.Max(maxDistance, value);

            localPosition = queryAabb.Max - m_translation;
            distance = localPosition.LengthSquared();
            if (distance < 0.01f)
            {
                return ContainmentType.Intersects;
            }

            MyCsgPrecomputedHelpres.CalculateSamplePosition(ref localPosition, out samplePos, ref pos, m_header.Resolution);

            value = GetValueForPosition(ref samplePos, ref pos, true);

            minDistance = MathHelper.Min(minDistance, value);
            maxDistance = MathHelper.Max(maxDistance, value);

            sphere.Radius = m_innerRadius + maxDistance + lodVoxelSize;
      
            sphere.Contains(ref queryAabb, out outerContainment);
            if (outerContainment == ContainmentType.Disjoint)
                return ContainmentType.Disjoint;

            sphere.Radius = m_innerRadius + minDistance - lodVoxelSize;
            sphere.Contains(ref queryAabb, out innerContainment);
            if (innerContainment == ContainmentType.Contains)
                return ContainmentType.Contains;
            
            return ContainmentType.Intersects;
        }

        internal override float SignedDistance(ref Vector3 position, float lodVoxelSize, VRage.Noise.IMyModule macroModulator, VRage.Noise.IMyModule detailModulator)
        {
            Vector3 localPosition = position - m_translation;
            float distance = localPosition.Length();
            if ((m_innerRadius - lodVoxelSize) > distance)
                return -1f;
            if ((m_outerRadius + lodVoxelSize) < distance)
                return 1f;
        
            return SignedDistanceInternal(lodVoxelSize, macroModulator, detailModulator, ref localPosition, distance);
        }

        internal override float SignedDistanceUnchecked(ref Vector3 position, float lodVoxelSize, VRage.Noise.IMyModule macroModulator, VRage.Noise.IMyModule detailModulator)
        {
            Vector3 localPosition = position - m_translation;
            float distance = localPosition.Length();

            return SignedDistanceInternal(lodVoxelSize, macroModulator, detailModulator, ref localPosition, distance);
        }

        internal float SampleField(ref Vector3 position)
        {
            Vector3 localPosition = position - m_translation;
            Vector3I samplePos;
            Vector2 pos = Vector2.Zero;
            MyCsgPrecomputedHelpres.CalculateSamplePosition(ref localPosition, out samplePos, ref pos, m_header.Resolution);
            return GetValueForPosition(ref samplePos, ref pos, true);    
        }

        private float SignedDistanceInternal(float lodVoxelSize, IMyModule macroModulator, IMyModule detailModulator, ref Vector3 localPosition, float distance)
        {
            if (distance > 0.0f)
            {            
                float signedDistance = distance - m_averageRadius;
                Vector3I samplePos;
                Vector2 pos = Vector2.Zero;
                MyCsgPrecomputedHelpres.CalculateSamplePosition(ref localPosition, out samplePos, ref pos, m_header.Resolution);
                float value = GetValueForPosition(ref samplePos,ref pos,true);
                return (signedDistance - value) / lodVoxelSize;
            }
            return 0.0f;
        }

        private float GetValueForPosition(ref Vector3I samplePos,ref Vector2 pos, bool interpolate)
        {
            float value = 0.0f;
            if (interpolate)
            {
                float fx = pos.X - samplePos.Y;
                float fy = pos.Y - samplePos.Z;
                float fx1 = 1.0f - fx;
                float fy1 = 1.0f - fy;

                Vector2I newSamplePos = new Vector2I(samplePos.Y, samplePos.Z);

                ushort height;

                int newFacePosition = 0;

                GetHeightFromBatch(samplePos.X, newSamplePos.X, newSamplePos.Y, out height);
                value += height * (fx1 * fy1);

                newSamplePos.X = samplePos.Y + 1;
                newSamplePos.Y = samplePos.Z;

                newFacePosition = MyCsgPrecomputedHelpres.GetFaceForTexcoord(ref newSamplePos, samplePos.X, m_header.Resolution);
                GetHeightFromBatch(newFacePosition, newSamplePos.X, newSamplePos.Y, out height);
                value += height * (fx * fy1);

                newSamplePos.X = samplePos.Y;
                newSamplePos.Y = samplePos.Z + 1;

                if (newSamplePos.Y < m_header.Resolution)
                {
                    GetHeightFromBatch(samplePos.X, newSamplePos.X, newSamplePos.Y, out height);
                    value += height * (fx1 * fy);

                    newSamplePos.X = samplePos.Y + 1;

                    newFacePosition = MyCsgPrecomputedHelpres.GetFaceForTexcoord(ref newSamplePos, samplePos.X, m_header.Resolution);
                    GetHeightFromBatch(newFacePosition, newSamplePos.X, newSamplePos.Y, out height);
                    value += height * (fx * fy);
                }

            }
            else
            {
                ushort height;
                GetHeightFromBatch(samplePos.X, samplePos.Y, samplePos.Z, out height);
                value = height;
            }

            if (MyFakes.ENABLE_PLANET_FROZEN_SEA)
            {
                return Math.Max((value * m_maxHillHeight) / (float)m_header.MaxValue, MyCsgPrecomputedHelpres.FROZEN_OCEAN_LEVEL);
            }

            return (value * m_maxHillHeight) / (float)m_header.MaxValue;
            
        }

        private void GetHeightFromBatch(int face, int texCoordX, int texCoordY, out ushort height)
        {
            int batchX = (int)(texCoordX / (m_header.BatchSize));
            int batchY = (int)(texCoordY / (m_header.BatchSize));

            int realTexCoordX = texCoordX - batchX * m_header.BatchSize;
            int realTexCoordY = texCoordY - batchY * m_header.BatchSize;

            var reader = m_reader[face][batchY * m_sectorCount + batchX];
            height = reader.ReadUInt16((realTexCoordY * m_header.BatchSize + realTexCoordX) * m_header.TypeSize);
        }

        internal override Vector3 Center()
        {
            return m_translation;
        }

        internal override MyCsgShapeBase DeepCopy()
        {
            return new MyCsgShapePrecomputed(
               m_translation,
               m_averageRadius,
               m_dataFileName,
               m_maxHillHeight);
        }

        internal override void ShrinkTo(float percentage)
        {
            throw new NotImplementedException();
        }

        internal override void ReleaseMaps() 
        {
            for (int i = 0; i < MyCsgPrecomputedHelpres.NUM_MAPS; ++i)
            {
                for (int j = 0; j < m_reader[i].Length; j++)
                    m_reader[i][j].Dispose();
                m_file[i].Dispose();
            }
        }
    }
}

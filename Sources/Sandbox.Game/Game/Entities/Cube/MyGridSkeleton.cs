using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using VRageMath;
using VRage;
using Sandbox.Game.Entities.Cube;
using VRage.Game;
using VRage.Profiler;

namespace Sandbox.Game.Entities
{
    class MyGridSkeleton
    {
        public readonly Dictionary<Vector3I, Vector3> Bones = new Dictionary<Vector3I, Vector3>();
        private List<Vector3I> m_tmpRemovedCubes = new List<Vector3I>();
        private HashSet<Vector3I> m_usedBones = new HashSet<Vector3I>();
        private HashSet<Vector3I> m_testedCubes = new HashSet<Vector3I>();

        /// <summary>
        /// Multiply this with your grid size before use!
        /// </summary>
        private static readonly float MAX_BONE_ERROR;

        /// <summary>
        /// This needs to be ThreadStatic because mods can call MyCubeGrid.GetObjectBuilder from other threads
        /// </summary>
        [ThreadStatic]
        private static List<Vector3I> m_tempAffectedCubes = new List<Vector3I>();
        /// <summary>
        /// Density multiplier
        // Many algorithm relies on this
        /// </summary>
        public const int BoneDensity = 2;

        public readonly Vector3I[] BoneOffsets;
        static MyGridSkeleton()
        {
            MAX_BONE_ERROR = Vector3UByte.Denormalize(new Vector3UByte(128, 128, 128), 1f).X * 0.75f;
        }

        public static float GetMaxBoneError(float gridSize)
        {
            return MAX_BONE_ERROR * gridSize;
        }

        public MyGridSkeleton()
        {
            BoneOffsets = new Vector3I[(int)Math.Pow(BoneDensity+1,3)];
            int idx = 0;
            Vector3I offset = Vector3I.Zero;
            for(;offset.X <= BoneDensity; offset.X++)
                for(offset.Y = 0; offset.Y <= BoneDensity; offset.Y++)
                    for(offset.Z = 0; offset.Z <= BoneDensity; offset.Z++)
                    {
                        BoneOffsets[idx] = offset;
                        idx++;
                    }
        }

        public void Reset()
        {
            Bones.Clear();
        }

        /// <summary>
        /// Copies part of skeleton to other skeleton, both positions are inclusive
        /// </summary>
        public void CopyTo(MyGridSkeleton target, Vector3I fromGridPosition, Vector3I toGridPosition)
        {
            Vector3I baseBonePos = fromGridPosition * BoneDensity;
            Vector3I max = (toGridPosition - fromGridPosition + Vector3I.One) * BoneDensity;
            Vector3I boneOffset;
            for (boneOffset.X = 0; boneOffset.X <= max.X; boneOffset.X++)
            {
                for (boneOffset.Y = 0; boneOffset.Y <= max.Y; boneOffset.Y++)
                {
                    for (boneOffset.Z = 0; boneOffset.Z <= max.Z; boneOffset.Z++)
                    {
                        Vector3I bonePos = baseBonePos + boneOffset;

                        Vector3 bone;
                        if (Bones.TryGetValue(bonePos, out bone))
                        {
                            target.Bones[bonePos] = bone;
                        }
                        else
                        {
                            target.Bones.Remove(bonePos);
                        }
                    }
                }
            }
        }

        public void CopyTo(MyGridSkeleton target, MatrixI transformationMatrix, MyCubeGrid targetGrid)
        {
            Vector3I oldPosition, newPosition;
            Vector3 oldBone, newBone;

            // transformationMatrix is in cube coordinates, so change it to bone coords
            MatrixI BoneOriginToGridOrigin = new MatrixI(new Vector3I(1, 1, 1), Base6Directions.Direction.Forward, Base6Directions.Direction.Up);
            MatrixI GridOriginToBoneOrigin = new MatrixI(new Vector3I(-1, -1, -1), Base6Directions.Direction.Forward, Base6Directions.Direction.Up);

            transformationMatrix.Translation = transformationMatrix.Translation * BoneDensity;

            MatrixI tmp;
            MatrixI.Multiply(ref GridOriginToBoneOrigin, ref transformationMatrix, out tmp);
            MatrixI.Multiply(ref tmp, ref BoneOriginToGridOrigin, out transformationMatrix);

            Matrix orientation;
            transformationMatrix.GetBlockOrientation().GetMatrix(out orientation);

            foreach (var bone in Bones)
            {
                oldPosition = bone.Key;
                Vector3I.Transform(ref oldPosition, ref transformationMatrix, out newPosition);
                Vector3 transformedBone = Vector3.Transform(bone.Value, orientation);

                if (target.Bones.TryGetValue(newPosition, out oldBone))
                {
                    newBone = (oldBone + transformedBone) * 0.5f;
                    target.Bones[newPosition] = newBone;
                }
                else
                {
                    target.Bones[newPosition] = transformedBone;
                }

                Vector3I cubePosition = newPosition / BoneDensity;
  
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                        for (int k = -1; k <= 1; k++)
                        {
                            targetGrid.SetCubeDirty(cubePosition + new Vector3I(i, j, k));
                        }

            }
        }

        /// <summary>
        /// Fixes bone in way that it won't be further than neighbour bones.
        /// This helps fix invalid triangles during rendering.
        /// </summary>
        public void FixBone(Vector3I gridPosition, Vector3I boneOffset, float gridSize, float minBoneDist = 0.05f)
        {
            FixBone(gridPosition * BoneDensity + boneOffset, minBoneDist);
        }

        private void FixBone(Vector3I bonePosition, float gridSize, float minBoneDist = 0.05f)
        {
            Vector3 lowerDef = -Vector3.One * gridSize;
            Vector3 upperDef = Vector3.One * gridSize;

            Vector3 lower;
            lower.X = TryGetBone(bonePosition - Vector3I.UnitX, ref lowerDef).X;
            lower.Y = TryGetBone(bonePosition - Vector3I.UnitY, ref lowerDef).Y;
            lower.Z = TryGetBone(bonePosition - Vector3I.UnitZ, ref lowerDef).Z;
            lower -= new Vector3(gridSize / BoneDensity);
            lower += new Vector3(minBoneDist);

            Vector3 upper;
            upper.X = TryGetBone(bonePosition + Vector3I.UnitX, ref upperDef).X;
            upper.Y = TryGetBone(bonePosition + Vector3I.UnitY, ref upperDef).Y;
            upper.Z = TryGetBone(bonePosition + Vector3I.UnitZ, ref upperDef).Z;
            upper += new Vector3(gridSize / BoneDensity);
            upper -= new Vector3(minBoneDist);

            Bones[bonePosition] = Vector3.Clamp(Bones[bonePosition], lower, upper);
        }

        private Vector3 TryGetBone(Vector3I bonePosition, ref Vector3 defaultBone)
        {
            Vector3 result;
            if (Bones.TryGetValue(bonePosition, out result))
                return result;
            else
                return defaultBone;
        }

        public void Serialize(List<BoneInfo> result, float boneRange, MyCubeGrid grid)
        {
            var info = new BoneInfo();
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Serialize");
            
            float boneErrorSquared = GetMaxBoneError(grid.GridSize);
            boneErrorSquared *= boneErrorSquared;

            foreach (var bone in Bones)
            {
                Vector3I? cube = GetCubeFromBone(bone.Key, grid);
                if (cube != null)
                {
                    var boneOffset = GetDefinitionOffsetWithNeighbours(cube.Value, bone.Key, grid);
                    float distance = Math.Abs(boneOffset.LengthSquared() - bone.Value.LengthSquared());
                    if (distance > boneErrorSquared)
                    {
                        info.BonePosition = bone.Key;
                        info.BoneOffset = Vector3UByte.Normalize(bone.Value, boneRange);
                        if (!Vector3UByte.IsMiddle(info.BoneOffset)) // Middle number means zero in floats
                        {
                            result.Add(info);
                        }
                    }
                }
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        private Vector3I? GetCubeFromBone(Vector3I bone, MyCubeGrid grid)
        {
            Vector3I result = Vector3I.Zero;
            result = bone / 2;

            if (grid.CubeExists(result))
            {
                return result;
            }

            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <=1; j++)
                    for (int k = -1; k <= 1; k++)
                    {
                        Vector3I current = result + new Vector3I(i, j, k);
                        Vector3I test = bone - current * 2;

                        if (test.X > 2 || test.Y > 2 || test.Z > 2)
                            continue;

                        if (grid.CubeExists(current))
                        {
                            return current;
                        }
                    }

            return null;
        }

        public void Deserialize(List<BoneInfo> data, float boneRange, float gridSize, bool clear = false)
        {
            ProfilerShort.Begin("MyGridSkeleton.Deserialize(...)");
            if (clear)
            {
                Bones.Clear();
            }

            foreach (var bone in data)
            {
                Bones[bone.BonePosition] = Vector3UByte.Denormalize(bone.BoneOffset, boneRange);
                //FixBone(bone.BonePosition, gridSize);
            }
            ProfilerShort.End();
        }

        public void SerializePart(Vector3I minBone, Vector3I maxBone, float boneRange, List<byte> result)
        {
            Vector3I pos;
            for (pos.X = minBone.X; pos.X <= maxBone.X; pos.X++)
            {
                for (pos.Y = minBone.Y; pos.Y <= maxBone.Y; pos.Y++)
                {
                    for (pos.Z = minBone.Z; pos.Z <= maxBone.Z; pos.Z++)
                    {
                        var boneValue = Vector3UByte.Normalize(this[pos], boneRange);
                        result.Add(boneValue.X);
                        result.Add(boneValue.Y);
                        result.Add(boneValue.Z);
                    }
                }
            }
        }

        public void DeserializePart(Vector3I minBone, Vector3I maxBone, float boneRange, List<byte> data)
        {
            var size = maxBone - minBone;
            size += Vector3I.One; // bounds inclusive

            if (size.Size * 3 < data.Count)
            {
                Debug.Fail("Data has wrong length");
                return;
            }

            int index = 0;

            Vector3I pos;
            for (pos.X = minBone.X; pos.X <= maxBone.X; pos.X++)
            {
                for (pos.Y = minBone.Y; pos.Y <= maxBone.Y; pos.Y++)
                {
                    for (pos.Z = minBone.Z; pos.Z <= maxBone.Z; pos.Z++)
                    {
                        this[pos] = Vector3UByte.Denormalize(new Vector3UByte(data[index], data[index + 1], data[index + 2]), boneRange);
                        index += 3;
                    }
                }
            }
        }

        public Vector3 GetBone(Vector3I cubePos, Vector3I bonePos)
        {
            Vector3 result;
            if (!Bones.TryGetValue(cubePos * BoneDensity + bonePos, out result))
            {
                return Vector3.Zero;
            }
            return result;
        }

        public void GetBone(ref Vector3I pos, out Vector3 bone)
        {
            if (!Bones.TryGetValue(pos, out bone))
            {
                bone = Vector3.Zero;
            }
        }

        public bool TryGetBone(ref Vector3I pos, out Vector3 bone)
        {
            return Bones.TryGetValue(pos, out bone);
        }

        public void SetBone(ref Vector3I pos, ref Vector3 bone)
        {
            Bones[pos] = bone;
        }

        public void SetOrClearBone(ref Vector3I pos, ref Vector3 bone)
        {
            if (bone == Vector3.Zero)
                Bones.Remove(pos);
            else
                Bones[pos] = bone;
        }

        /// <summary>
        /// Returns true when bone was really removed
        /// </summary>
        public bool ClearBone(ref Vector3I pos)
        {
            return Bones.Remove(pos);
        }

        /// <summary>
        /// Returns true when bone was changed.
        /// When new bone offset length is smaller than epsilon, it will remove bone.
        /// Factor is used as t paramter in a lerp. This is because the default position
        /// for a bone may not be 0
        /// </summary>
        public bool MultiplyBone(ref Vector3I pos, float factor, ref Vector3I cubePos, MyCubeGrid cubeGrid, float epsilon = 0.005f)
        {
            Vector3 value;
            if (Bones.TryGetValue(pos, out value))
            {
                var offset = GetDefinitionOffsetWithNeighbours(cubePos, pos, cubeGrid);

                factor = 1f - factor;

                if (factor < 0.1f)
                {
                    factor = 0.1f;
                }
                
                var newBone = Vector3.Lerp(value, offset, factor);
                if (newBone.LengthSquared() < epsilon * epsilon)
                {
                    Bones.Remove(pos);
                }
                else
                {
                    Bones[pos] = newBone;
                }
                return true;
            }
            return false;
        }

        public Vector3 this[Vector3I pos]
        {
            get
            {
                Vector3 result;
                if (Bones.TryGetValue(pos, out result))
                {
                    return result;
                }
                else
                {
                    return Vector3.Zero;
                }
            }
            set
            {
                Bones[pos] = value;
            }
        }

        [Conditional("DEBUG")]
        void AssertBone(Vector3 value, float range)
        {
            Debug.Assert(value.X <= range && value.X >= -range && value.Y <= range && value.Y >= -range && value.Z <= range && value.Z >= -range, "Bone offset out of range");
        }

        public bool IsDeformed(Vector3I cube, float ignoredDeformation, MyCubeGrid cubeGrid, bool checkBlockDefinition)
        {
            float ignoredDeformationSq = ignoredDeformation * ignoredDeformation;
            float boneErrorSquared = GetMaxBoneError(cubeGrid.GridSize);
            boneErrorSquared *= boneErrorSquared;

            foreach (var boneOffset in BoneOffsets)
            {
                Vector3 offset;
                if (Bones.TryGetValue(cube*BoneDensity + boneOffset, out offset))
                {
                    if (checkBlockDefinition)
                    {
                        float definitionLength =
                            GetDefinitionOffsetWithNeighbours(cube, cube*BoneDensity + boneOffset, cubeGrid)
                                .LengthSquared();
                        float offsetLength = offset.LengthSquared();

                        if (Math.Abs(definitionLength - offsetLength) > boneErrorSquared)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (offset.LengthSquared() > ignoredDeformationSq)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public float MaxDeformation(Vector3I cube, MyCubeGrid cubeGrid)
        {
            float maxDeformationSq = 0.0f;
            float maxBoneErrorSq = GetMaxBoneError(cubeGrid.GridSize);
            maxBoneErrorSq *= maxBoneErrorSq;

            foreach (var boneOffset in BoneOffsets)
            {
                Vector3 offset;
                Vector3I bonePos = cube*BoneDensity + boneOffset;
                bool boneExits = Bones.TryGetValue(bonePos, out offset);

                float definitionLength =
                    GetDefinitionOffsetWithNeighbours(cube, cube*BoneDensity + boneOffset, cubeGrid).LengthSquared();
                float offsetLength = offset.LengthSquared();

                float deformationSq = Math.Abs(definitionLength - offsetLength);

                if (deformationSq > maxDeformationSq)
                {
                    maxDeformationSq = deformationSq;
                }

                if (!boneExits && deformationSq > maxBoneErrorSq)
                {
                    Bones.Add(bonePos, offset);
                    cubeGrid.AddDirtyBone(cube, boneOffset);
                }
            }
            return (float)Math.Sqrt(maxDeformationSq);
        }

        /// <summary>
        /// Bone offset is normally between 0 and BoneDensity (including)
        /// This function takes any other values and transforms it into cube and proper bone offset
        /// </summary>
        public void Wrap(ref Vector3I cube, ref Vector3I boneOffset)
        {
            Vector3I bonePos = cube * BoneDensity + boneOffset;
            cube = VRageMath.Vector3I.Floor((Vector3D)(bonePos / BoneDensity));
            boneOffset = bonePos - cube * BoneDensity;
        }

        /// <summary>
        /// Gets all cubes which are affected by bone.
        /// </summary>
        /// <param name="onlyExisting">Returns only cubes which were added to skeleton.</param>
        public void GetAffectedCubes(Vector3I cube, Vector3I boneOffset, List<Vector3I> resultList, MyCubeGrid grid)
        {
            Debug.Assert(BoneDensity == 2, "This algorithm requires BoneDensity to be 2");

            Vector3I dist = boneOffset - Vector3I.One;
            Vector3I sign = Vector3I.Sign(dist);
            dist *= sign;

            Vector3I offset;
            for (offset.X = 0; offset.X <= dist.X; offset.X++)
            {
                for (offset.Y = 0; offset.Y <= dist.Y; offset.Y++)
                {
                    for (offset.Z = 0; offset.Z <= dist.Z; offset.Z++)
                    {
                        var targetCube = cube + offset * sign;
                        if (grid.CubeExists(targetCube)) resultList.Add(targetCube);
                    }
                }
            }
        }

        public void GetCubeBox(Vector3I cubePos, out Vector3 lowerBone, out Vector3 upperBone, float? clampTo)
        {
            var basePos = cubePos * BoneDensity;
            lowerBone.X = TryGetBone(basePos + new Vector3I(0, 1, 1), ref Vector3.Zero).X;
            lowerBone.Y = TryGetBone(basePos + new Vector3I(1, 0, 1), ref Vector3.Zero).Y;
            lowerBone.Z = TryGetBone(basePos + new Vector3I(1, 1, 0), ref Vector3.Zero).Z;
            upperBone.X = TryGetBone(basePos + new Vector3I(2, 1, 1), ref Vector3.Zero).X;
            upperBone.Y = TryGetBone(basePos + new Vector3I(1, 2, 1), ref Vector3.Zero).Y;
            upperBone.Z = TryGetBone(basePos + new Vector3I(1, 1, 2), ref Vector3.Zero).Z;

            if (clampTo.HasValue)
            {
                Vector3 maxSize = new Vector3(clampTo.Value / 2);
                lowerBone = Vector3.Clamp(lowerBone, -maxSize, maxSize);
                upperBone = Vector3.Clamp(upperBone, -maxSize, maxSize);
            }
        }

        public void MarkCubeRemoved(ref Vector3I pos)
        {
            m_tmpRemovedCubes.Add(pos);
        }

        public void RemoveUnusedBones(MyCubeGrid grid)
        {
            ProfilerShort.Begin("RemoveUnusedBones");
            if (m_tmpRemovedCubes.Count != 0)
            {
                Debug.Assert(m_testedCubes.Count == 0);
                Debug.Assert(m_usedBones.Count == 0);

                foreach (var cube in m_tmpRemovedCubes)
                {
                    if (grid.CubeExists(cube))
                    {
                        if (!m_testedCubes.Contains(cube))
                        {
                            m_testedCubes.Add(cube);
                            AddUsedBones(cube);
                        }
                        continue;
                    }

                    Vector3I centerBonePos = cube * BoneDensity + Vector3I.One;
                    Vector3I dir, neighbor;

                    // Iterate over all the neighbors of the cube and check whether they are present in the grid
                    for (int x = -1; x <= 1; ++x) 
                        for (int y = -1; y <= 1; ++y)
                            for (int z = -1; z <= 1; ++z)
                            {
                                dir.X = x;
                                dir.Y = y;
                                dir.Z = z;
                                neighbor = cube + dir;

                                if (grid.CubeExists(neighbor) && !m_testedCubes.Contains(neighbor))
                                {
                                    m_testedCubes.Add(neighbor);
                                    AddUsedBones(neighbor);
                                }
                            }
                }

                foreach (var cube in m_tmpRemovedCubes)
                {
                    Vector3I pos = cube * BoneDensity;
                    for (int x = 0; x <= BoneDensity; ++x)
                    {
                        for (int y = 0; y <= BoneDensity; ++y)
                        {
                            for (int z = 0; z <= BoneDensity; ++z)
                            {
                                if (!m_usedBones.Contains(pos)) ClearBone(ref pos);

                                pos.Z++;
                            }
                            pos.Y++;
                            pos.Z -= BoneDensity + 1;
                        }
                        pos.X++;
                        pos.Y -= BoneDensity + 1;
                    }
                }

                m_testedCubes.Clear();
                m_usedBones.Clear();
                m_tmpRemovedCubes.Clear();
            }
            ProfilerShort.End();
        }

        /// <summary>
        /// MArks the bones of the cube at position "pos" as used
        /// </summary>
        private void AddUsedBones(Vector3I pos)
        {
            pos = pos * BoneDensity;

            for (int x = 0; x <= BoneDensity; ++x)
            {
                for (int y = 0; y <= BoneDensity; ++y)
                {
                    for (int z = 0; z <= BoneDensity; ++z)
                    {
                        m_usedBones.Add(pos);

                        pos.Z++;
                    }
                    pos.Y++;
                    pos.Z -= BoneDensity + 1;
                }
                pos.X++;
                pos.Y -= BoneDensity + 1;
            }
        }

        public Vector3 GetDefinitionOffsetWithNeighbours(Vector3I cubePos, Vector3I bonePos, MyCubeGrid grid)
        {
            Vector3I boneOffset = GetCubeBoneOffset(cubePos, bonePos);

            if (m_tempAffectedCubes == null)
            {
                m_tempAffectedCubes = new List<Vector3I>();
            }

            m_tempAffectedCubes.Clear();
            GetAffectedCubes(cubePos, boneOffset, m_tempAffectedCubes, grid);

            Vector3 offset = Vector3.Zero;
            int affectedCount = 0;
            foreach (var cube in m_tempAffectedCubes)
            {
                var cubeBlock = grid.GetCubeBlock(cube);
                if (cubeBlock != null && cubeBlock.BlockDefinition.Skeleton != null)
                {
                    Vector3I currentBoneOffset = GetCubeBoneOffset(cube, bonePos);

                    var defOffset = GetDefinitionOffset(cubeBlock, currentBoneOffset);
                    if (defOffset != null)
                    {
                        offset += defOffset.Value;
                        affectedCount++;
                    }
                }
            }

            if (affectedCount == 0)
            {
                return offset;
            }
            return offset / affectedCount;
        }

        private Vector3I GetCubeBoneOffset(Vector3I cubePos, Vector3I boneOffset)
        {
            Debug.Assert(BoneDensity == 2, "This algorithm requires BoneDensity to be 2");

            Vector3I result = Vector3I.Zero;

            //X
            if (boneOffset.X % 2 != 0)
            {
                result.X = 1;
            }
            else
            {
                if (boneOffset.X / 2 != cubePos.X)
                {
                    result.X = 2;
                }
            }

            //Y
            if (boneOffset.Y % 2 != 0)
            {
                result.Y = 1;
            }
            else
            {
                if (boneOffset.Y / 2 != cubePos.Y)
                {
                    result.Y = 2;
                }
            }

            //Z
            if (boneOffset.Z % 2 != 0)
            {
                result.Z = 1;
            }
            else
            {
                if (boneOffset.Z / 2 != cubePos.Z)
                {
                    result.Z = 2;
                }
            }


            return result;
        }

        /// <summary>
        /// Assumes cubeBlock is not null
        /// </summary>
        private Vector3? GetDefinitionOffset(MySlimBlock cubeBlock, Vector3I bonePos)
        {
            Vector3I rotatedCubeBonePos = bonePos;
            rotatedCubeBonePos -= Vector3I.One;

            Matrix rotationMatrix;
            cubeBlock.Orientation.GetMatrix(out rotationMatrix);
            Matrix invertedRotationMatrix = Matrix.Transpose(rotationMatrix);

            Vector3I cubeBonePos;
            Vector3I.Transform(ref rotatedCubeBonePos, ref invertedRotationMatrix, out cubeBonePos);

            cubeBonePos += Vector3I.One;

            Vector3 offset;
            if (cubeBlock.BlockDefinition.Bones.TryGetValue(cubeBonePos, out offset))
            {
                Vector3 rotatedOffset = Vector3.Transform(offset, rotationMatrix);
                return rotatedOffset;
            }

            return null;
        }
    }
}

using System;
using VRageMath;
using VRageRender;
using VRage.Utils;
using VRage.ModAPI;
using System.Collections.Generic;

namespace VRage.Game.Models
{
    //  Result of intersection between a ray and a triangle. This structure can be used only if intersection was found!
    //  If returned intersection is with voxel, all coordinates are in absolute/world space
    //  If returned intersection is with model instance, all coordinates are in model's local space (so for drawing we need to trasform them using world matrix)
    public struct MyIntersectionResultLineTriangle
    {
        //  IMPORTANT: Use these members only for readonly acces. Change them only inside the constructor.
        //  We can't mark them 'readonly' because sometimes they are sent to different methods through "ref"

        //  Distance to the intersection point (calculated as distance from 'line.From' to 'intersection point')
        public double Distance;
        
        //  World coordinates of intersected triangle. It is also used as input parameter for col/det functions.
        public MyTriangle_Vertices InputTriangle;
        public MyTriangle_BoneIndicesWeigths? BoneWeights;

        //  Normals of vertexes of intersected triangle
        public Vector3 InputTriangleNormal;

        // Index to model triangles
        public int TriangleIndex;

        public MyIntersectionResultLineTriangle(int triangleIndex, ref MyTriangle_Vertices triangle, ref Vector3 triangleNormal, double distance)
        {
            InputTriangle = triangle;
            InputTriangleNormal = triangleNormal;
            Distance = distance;
            BoneWeights = null;
            TriangleIndex = triangleIndex;
        }

        public MyIntersectionResultLineTriangle(int triangleIndex, ref MyTriangle_Vertices triangle, ref MyTriangle_BoneIndicesWeigths? boneWeigths, ref Vector3 triangleNormal, double distance)
        {
            InputTriangle = triangle;
            InputTriangleNormal = triangleNormal;
            Distance = distance;
            BoneWeights = boneWeigths;
            TriangleIndex = triangleIndex;
        }
        
        //  Find and return closer intersection of these two. If intersection is null then it's not really an intersection.
        public static MyIntersectionResultLineTriangle? GetCloserIntersection(ref MyIntersectionResultLineTriangle? a, ref MyIntersectionResultLineTriangle? b)
        {
            if (((a == null) && (b != null)) ||
                ((a != null) && (b != null) && (b.Value.Distance < a.Value.Distance)))
            {
                //  If only "b" contains valid intersection, or when it's closer than "a"
                return b;
            }
            else
            {
                //  This will be returned also when ((a == null) && (b == null))
                return a;
            }
        }
    }

    //  More detailed version of MyIntersectionResultLineTriangle, contains some calculated data, etc. This is usually 
    //  used as a result of triangle intersection searches
    public struct MyIntersectionResultLineTriangleEx
    {
        //  IMPORTANT: Use these members only for readonly acces. Change them only inside the constructor.
        //  We can't mark them 'readonly' because sometimes they are sent to different methods through "ref"

        public MyIntersectionResultLineTriangle Triangle;

        //  Point of intersection, always in object space. Use only if intersection with object.
        public Vector3 IntersectionPointInObjectSpace;

        //  Point of intersection - always in world space
        public Vector3D IntersectionPointInWorldSpace;

        //  If intersection occured with phys object, here will be it
        public IMyEntity Entity;

        //  Normal vector of intersection triangle - always in world space. Can be calculaed from input positions.
        public Vector3 NormalInWorldSpace;

        //  Normal vector of intersection triangle, always in object space. Use only if intersection with object.
        public Vector3 NormalInObjectSpace;

        //  Line used to get intersection, transformed to object space. For voxels it is also in world space, but for objects, use GetLineInWorldSpace()
        public LineD InputLineInObjectSpace;

        public MyIntersectionResultLineTriangleEx(MyIntersectionResultLineTriangle triangle, IMyEntity entity, ref LineD line)
        {
            Triangle = triangle;
            Entity = entity;
            InputLineInObjectSpace = line;

            NormalInObjectSpace = MyUtils.GetNormalVectorFromTriangle(ref Triangle.InputTriangle);
            // fixme: calculated normal for degenerated triangles was NaN; here should be some better calculation of normal from degenerated triangle
            if (!NormalInObjectSpace.IsValid())
                NormalInObjectSpace = new Vector3(0, 0, 1);
            IntersectionPointInObjectSpace = line.From + line.Direction * Triangle.Distance;

            if (Entity is IMyVoxelBase)
            {
                IntersectionPointInWorldSpace = (Vector3D)IntersectionPointInObjectSpace;
                NormalInWorldSpace = NormalInObjectSpace;

                //  This will move intersection point from world space into voxel map's object space
                IntersectionPointInObjectSpace = IntersectionPointInObjectSpace - ((IMyVoxelBase)Entity).PositionLeftBottomCorner;
            }
            else
            {
                var worldMatrix = Entity.WorldMatrix;
                NormalInWorldSpace = (Vector3)MyUtils.GetTransformNormalNormalized((Vector3D)NormalInObjectSpace, ref worldMatrix);
                IntersectionPointInWorldSpace = Vector3D.Transform((Vector3D)IntersectionPointInObjectSpace, ref worldMatrix);
            }
        }

        public MyIntersectionResultLineTriangleEx(MyIntersectionResultLineTriangle triangle, IMyEntity entity, ref LineD line, Vector3D intersectionPointInWorldSpace, Vector3 normalInWorldSpace)
        {
            Triangle = triangle;
            Entity = entity;
            InputLineInObjectSpace = line;

            NormalInObjectSpace = NormalInWorldSpace = normalInWorldSpace;
            IntersectionPointInWorldSpace = intersectionPointInWorldSpace;
            IntersectionPointInObjectSpace = (Vector3)IntersectionPointInWorldSpace;
        }

        public VertexBoneIndicesWeights? GetAffectingBoneIndicesWeights(ref List<VertexArealBoneIndexWeight> tmpStorage)
        {
            if (!Triangle.BoneWeights.HasValue)
                return null;

            if (tmpStorage == null)
                tmpStorage = new List<VertexArealBoneIndexWeight>(4);

            tmpStorage.Clear();

            MyTriangle_BoneIndicesWeigths boneWeights = Triangle.BoneWeights.Value;

            float u, v, w;
            Vector3.Barycentric(IntersectionPointInObjectSpace, Triangle.InputTriangle.Vertex0,
                Triangle.InputTriangle.Vertex1, Triangle.InputTriangle.Vertex2, out u, out v, out w);

            FillIndicesWeightsStorage(tmpStorage, ref boneWeights.Vertex0, u);
            FillIndicesWeightsStorage(tmpStorage, ref boneWeights.Vertex1, v);
            FillIndicesWeightsStorage(tmpStorage, ref boneWeights.Vertex2, w);

            tmpStorage.Sort(Comparison);

            VertexBoneIndicesWeights indicesWeights = new VertexBoneIndicesWeights();
            FillIndicesWeights(ref indicesWeights, 0, tmpStorage);
            FillIndicesWeights(ref indicesWeights, 1, tmpStorage);
            FillIndicesWeights(ref indicesWeights, 2, tmpStorage);
            FillIndicesWeights(ref indicesWeights, 3, tmpStorage);

            NormalizeBoneWeights(ref indicesWeights);
            return indicesWeights;
        }

        // Compare in reverse order
        private int Comparison(VertexArealBoneIndexWeight x, VertexArealBoneIndexWeight y)
        {
            if (x.Weight >  y.Weight)
                return -1;
            else if (x.Weight == y.Weight)
                return 0;
            else
                return 1;
        }

        private void FillIndicesWeights(ref VertexBoneIndicesWeights indicesWeights, int index, List<VertexArealBoneIndexWeight> tmpStorage)
        {
            if (index >= tmpStorage.Count)
                return;
            
            indicesWeights.Indices[index] = tmpStorage[index].Index;
            indicesWeights.Weights[index] = tmpStorage[index].Weight;
        }

        private void FillIndicesWeightsStorage(List<VertexArealBoneIndexWeight> tmpStorage, ref MyVertex_BoneIndicesWeights indicesWeights, float arealCoord)
        {
            HandleAddBoneIndexWeight(tmpStorage, ref indicesWeights, 0, arealCoord);
            HandleAddBoneIndexWeight(tmpStorage, ref indicesWeights, 1, arealCoord);
            HandleAddBoneIndexWeight(tmpStorage, ref indicesWeights, 2, arealCoord);
            HandleAddBoneIndexWeight(tmpStorage, ref indicesWeights, 3, arealCoord);
        }

        private void HandleAddBoneIndexWeight(List<VertexArealBoneIndexWeight> tmpStorage, ref MyVertex_BoneIndicesWeights indicesWeights, int index, float arealCoord)
        {
            float boneWeight = indicesWeights.Weights[index];
            if (boneWeight == 0)
                return;

            byte boneIndex = indicesWeights.Indices[index];
            int existingBoneIndex = FindExsistingBoneIndexWeight(tmpStorage, boneIndex);
            if (existingBoneIndex == -1)
            {
                tmpStorage.Add(new VertexArealBoneIndexWeight() { Index = boneIndex, Weight = boneWeight * arealCoord });
            }
            else
            {
                VertexArealBoneIndexWeight boneIndexWeight = tmpStorage[existingBoneIndex];
                boneIndexWeight.Weight += boneWeight * arealCoord;
                tmpStorage[existingBoneIndex] = boneIndexWeight;
            }
        }

        private int FindExsistingBoneIndexWeight(List<VertexArealBoneIndexWeight> tmpStorage, int boneIndex)
        {
            int foundIndex = -1;
            for (int it = 0; it < tmpStorage.Count; it++)
            {
                if (tmpStorage[it].Index == boneIndex)
                {
                    foundIndex = it;
                    break;
                }
            }

            return foundIndex;
        }

        private void NormalizeBoneWeights(ref VertexBoneIndicesWeights indicesWeights)
        {
            float sum = 0;
            for (int it = 0; it < 4; it++)
                sum += indicesWeights.Weights[it];

            for (int it = 0; it < 4; it++)
                indicesWeights.Weights[it] /= sum;
        }

        //  Find and return closer intersection of these two. If intersection is null then it's not really an intersection.
        public static MyIntersectionResultLineTriangleEx? GetCloserIntersection(ref MyIntersectionResultLineTriangleEx? a, ref MyIntersectionResultLineTriangleEx? b)
        {
            if (((a == null) && (b != null)) ||
                ((a != null) && (b != null) && (b.Value.Triangle.Distance < a.Value.Triangle.Distance)))
            {
                //  If only "b" contains valid intersection, or when it's closer than "a"
                return b;
            }
            else
            {
                //  This will be returned also when ((a == null) && (b == null))
                return a;
            }
        }

        //  Find if distance between two intersections is less than "tolerance distance".
        public static bool IsDistanceLessThanTolerance(ref MyIntersectionResultLineTriangleEx? a, ref MyIntersectionResultLineTriangleEx? b,
            float distanceTolerance)
        {
            if (((a == null) && (b != null)) ||
                ((a != null) && (b != null) && (Math.Abs(b.Value.Triangle.Distance - a.Value.Triangle.Distance) <= distanceTolerance)))
            {
                return true;
            }
            else
            {
                //  This will be returned also when ((a == null) && (b == null))
                return false;
            }
        }
    }

    public struct VertexBoneIndicesWeights
    {
        public Vector4UByte Indices;
        public Vector4 Weights;
    }

    public struct VertexArealBoneIndexWeight
    {
        public byte Index;
        public float Weight;
    }
}

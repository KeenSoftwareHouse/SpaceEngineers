using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;

namespace VRageRender
{

    class MyLineHelpers
    {
        static void AddTriangle(List<ushort> I, int i0, int i1, int i2)
        {
            I.Add((ushort)i0);
            I.Add((ushort)i1);
            I.Add((ushort)i2);
        }

        internal static ushort[] GenerateIndices(int verticesNum)
        {
            List<ushort> indices = new List<ushort>();

            ushort vertexCounter = 0;

            AddTriangle(indices, vertexCounter + 0, vertexCounter + 1, vertexCounter + 2);
            AddTriangle(indices, vertexCounter + 0, vertexCounter + 2, vertexCounter + 3);

            for (; vertexCounter < (verticesNum - 4); vertexCounter += 4)
            {
                AddTriangle(indices, vertexCounter + 0, vertexCounter + 4, vertexCounter + 5);
                AddTriangle(indices, vertexCounter + 0, vertexCounter + 5, vertexCounter + 1);
                AddTriangle(indices, vertexCounter + 1, vertexCounter + 5, vertexCounter + 6);
                AddTriangle(indices, vertexCounter + 1, vertexCounter + 6, vertexCounter + 2);
                AddTriangle(indices, vertexCounter + 2, vertexCounter + 6, vertexCounter + 7);
                AddTriangle(indices, vertexCounter + 2, vertexCounter + 7, vertexCounter + 3);
                AddTriangle(indices, vertexCounter + 3, vertexCounter + 7, vertexCounter + 4);
                AddTriangle(indices, vertexCounter + 3, vertexCounter + 4, vertexCounter + 0); 
            }

            AddTriangle(indices, vertexCounter + 2, vertexCounter + 1, vertexCounter + 0);
            AddTriangle(indices, vertexCounter + 3, vertexCounter + 2, vertexCounter + 0);

            return indices.ToArray();
        }

        internal static VRageMath.BoundingBoxD GetBoundingBox(ref Vector3D worldPointA, ref Vector3D worldPointB)
        {
            var worldPosition = (worldPointA + worldPointB) * 0.5f;
            var pointA = worldPointA - worldPosition;
            var pointB = worldPointB - worldPosition;

            var aabb = BoundingBoxD.CreateInvalid();
            aabb.Include(ref pointA);
            aabb.Include(ref pointB);
            aabb.Inflate(0.25);
            //aabb.Translate(worldPosition);
            return aabb;
        }

        internal static void GenerateVertexData(ref Vector3D worldPointA, ref Vector3D worldPointB,
            out MyVertexFormatPositionH4[] stream0, out MyVertexFormatTexcoordNormalTangentTexindices[] stream1)
        {
            var worldPosition = (worldPointA + worldPointB) * 0.5f;
            var pointA = (Vector3)(worldPointA - worldPosition);
            var pointB = (Vector3)(worldPointB - worldPosition);

            var length = (pointA - pointB).Length() * 10.0f;

            Vector3 lineTangent, normal, binormal;
            lineTangent = pointB - pointA;
            VRageMath.Vector3.Normalize(ref lineTangent, out lineTangent);
            lineTangent.CalculatePerpendicularVector(out normal);
            Vector3.Cross(ref lineTangent, ref normal, out binormal);

            var offsetX = normal * 0.025f;
            var offsetY = binormal * 0.025f;

            List<MyVertexFormatPositionH4> vertexPositionList = new List<MyVertexFormatPositionH4>();
            List<MyVertexFormatTexcoordNormalTangentTexindices> vertexList = new List<MyVertexFormatTexcoordNormalTangentTexindices>();

            Byte4 defaultArrayTexIndex = new Byte4(0,0,0,0);
            unsafe
            {
                Vector3* points = stackalloc Vector3[2];
                points[0] = pointA;
                points[1] = pointB;
                int vertexCounter = 0;
                for (int i = 0; i < 2; ++i)
                {
                    int baseVertex = i * 4;
                    float texCoordX = (i - 0.5f) * length;

                    vertexPositionList.Add(new MyVertexFormatPositionH4(points[i] + offsetX));
                    vertexList.Add(new MyVertexFormatTexcoordNormalTangentTexindices(new Vector2(texCoordX, 0.0f), normal, Vector3.Cross(lineTangent, normal), defaultArrayTexIndex));

                    vertexPositionList.Add(new MyVertexFormatPositionH4(points[i] + offsetY));
                    vertexList.Add(new MyVertexFormatTexcoordNormalTangentTexindices(new Vector2(texCoordX, 0.33333f), binormal, Vector3.Cross(lineTangent, binormal), defaultArrayTexIndex));

                    vertexPositionList.Add(new MyVertexFormatPositionH4(points[i] - offsetX));
                    vertexList.Add(new MyVertexFormatTexcoordNormalTangentTexindices(new Vector2(texCoordX, 0.66667f), -normal, Vector3.Cross(lineTangent, -normal), defaultArrayTexIndex));

                    vertexPositionList.Add(new MyVertexFormatPositionH4(points[i] - offsetY));
                    vertexList.Add(new MyVertexFormatTexcoordNormalTangentTexindices(new Vector2(texCoordX, 1.0f), -binormal, Vector3.Cross(lineTangent, -binormal), defaultArrayTexIndex));
                }
            }

            stream0 = vertexPositionList.ToArray();
            stream1 = vertexList.ToArray();
        }
    }
}

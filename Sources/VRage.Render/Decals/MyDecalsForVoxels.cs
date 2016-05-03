using System.Collections.Generic;
using VRage.Utils;
using System;

using SharpDX;
using SharpDX.Direct3D9;

namespace VRageRender
{
    using Byte4 = VRageMath.PackedVector.Byte4;
    using HalfVector2 = VRageMath.PackedVector.HalfVector2;
    using HalfVector4 = VRageMath.PackedVector.HalfVector4;
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Rectangle = VRageMath.Rectangle;
    using Matrix = VRageMath.Matrix;
    using Color = VRageMath.Color;
    using BoundingBox = VRageMath.BoundingBox;
    using BoundingSphere = VRageMath.BoundingSphere;
    using BoundingFrustum = VRageMath.BoundingFrustum;
    using VRageRender.Textures;
    using VRageRender.Graphics;

    class MyDecalsForVoxels
    {
        enum MyDecalForVoxelsState : byte
        {
            READY,
            FADING_OUT
        }

        struct MyDecalsForVoxelsDictionaryKey : IEqualityComparer<MyDecalsForVoxelsDictionaryKey>, IEquatable<MyDecalsForVoxelsDictionaryKey>
        {
            public readonly uint VoxelMapId;
            public readonly MyDecalTexturesEnum DecalTexture;

            public MyDecalsForVoxelsDictionaryKey(uint voxelMapId, MyDecalTexturesEnum decalTexture)
            {
                VoxelMapId = voxelMapId;
                DecalTexture = decalTexture;                
            }

            #region Implementation of IEquatable<MyDecalsForVoxelsDictionaryKey>

            /// <summary>
            /// Equalses the specified other.
            /// </summary>
            /// <param name="other">The other.</param>
            /// <returns></returns>
            public bool Equals(MyDecalsForVoxelsDictionaryKey other)
            {
                return other.VoxelMapId == VoxelMapId &&
                       other.DecalTexture == DecalTexture;
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    uint result = VoxelMapId;
                    result = (result*397) ^ (uint)((int)DecalTexture).GetHashCode();
                    return (int)result;
                }
            }

            #endregion

            #region Implementation of IEqualityComparer<in MyDecalsForVoxelsDictionaryKey>

            /// <summary>
            /// Determines whether the specified objects are equal.
            /// </summary>
            /// <returns>
            /// true if the specified objects are equal; otherwise, false.
            /// </returns>
            /// <param name="x">The first object of type <paramref name="T"/> to compare.</param><param name="y">The second object of type <paramref name="T"/> to compare.</param>
            public bool Equals(MyDecalsForVoxelsDictionaryKey x, MyDecalsForVoxelsDictionaryKey y)
            {
                return x.Equals(y);
            }

            /// <summary>
            /// Returns a hash code for the specified object.
            /// </summary>
            /// <returns>
            /// A hash code for the specified object.
            /// </returns>
            /// <param name="obj">The <see cref="T:System.Object"/> for which a hash code is to be returned.</param><exception cref="T:System.ArgumentNullException">The type of <paramref name="obj"/> is a reference type and <paramref name="obj"/> is null.</exception>
            public int GetHashCode(MyDecalsForVoxelsDictionaryKey obj)
            {
                return obj.GetHashCode();
            }

            #endregion
        }
        
        int m_capacity;
        int m_fadingOutStartLimit;
        int m_fadingOutBuffersCount;
        MyDecalsForVoxelsTriangleBuffer[] m_triangleBuffers;
        Dictionary<MyDecalsForVoxelsDictionaryKey, MyDecalsForVoxelsTriangleBuffer> m_triangleBuffersByKey;
        Stack<MyDecalsForVoxelsTriangleBuffer> m_freeTriangleBuffers;
        Queue<MyDecalsForVoxelsTriangleBuffer> m_usedTriangleBuffers;
        List<MyDecalsForVoxelsTriangleBuffer> m_sortTriangleBuffersByTexture;        


        public MyDecalsForVoxels(int capacity)
        {
            m_capacity = capacity;
            m_fadingOutStartLimit = (int)(m_capacity * MyDecalsConstants.TEXTURE_LARGE_FADING_OUT_START_LIMIT_PERCENT);
            m_fadingOutBuffersCount = (int)(m_capacity * MyDecalsConstants.TEXTURE_LARGE_FADING_OUT_MINIMAL_TRIANGLE_COUNT_PERCENT);

            m_sortTriangleBuffersByTexture = new List<MyDecalsForVoxelsTriangleBuffer>(m_capacity);
            m_triangleBuffersByKey = new Dictionary<MyDecalsForVoxelsDictionaryKey, MyDecalsForVoxelsTriangleBuffer>(m_capacity);
            m_freeTriangleBuffers = new Stack<MyDecalsForVoxelsTriangleBuffer>(m_capacity);
            m_usedTriangleBuffers = new Queue<MyDecalsForVoxelsTriangleBuffer>(m_capacity);

            m_triangleBuffers = new MyDecalsForVoxelsTriangleBuffer[m_capacity];
            for (int i = 0; i < m_capacity; i++)
            {
                m_triangleBuffers[i] = new MyDecalsForVoxelsTriangleBuffer(MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER);
                m_freeTriangleBuffers.Push(m_triangleBuffers[i]);
            }
        }

        public MyDecalsForVoxelsTriangleBuffer GetTrianglesBuffer(MyRenderVoxelCell voxelMap, MyDecalTexturesEnum decalTexture)
        {
            MyDecalsForVoxelsDictionaryKey key = new MyDecalsForVoxelsDictionaryKey(voxelMap.ID, decalTexture);

            MyDecalsForVoxelsTriangleBuffer outValue;
            if (m_triangleBuffersByKey.TryGetValue(key, out outValue) == true)
            {
                //  Combination of cell/texture was found in dictionary, so we can return in right now
                return outValue;
            }
            else
            {
                if (m_triangleBuffersByKey.Count >= m_capacity)
                {
                    //  We are full, can't place decal on a new cell/texture. Need to wait for next CheckBufferFull.
                    return null;
                }
                else
                {
                    //  This is first time we want to place decal to this cell/texture, so here we allocate and initialize buffer
                    MyDecalsForVoxelsTriangleBuffer newBuffer = m_freeTriangleBuffers.Pop();
                    m_triangleBuffersByKey.Add(key, newBuffer);
                    m_usedTriangleBuffers.Enqueue(newBuffer);
                    newBuffer.Start(voxelMap, decalTexture);
                    return newBuffer;
                }
            }
        }

        //  Blends-out triangles affected by explosion (radius + some safe delta). Triangles there have zero alpha are flaged to not-draw at all.
        public void HideTrianglesAfterExplosion(uint voxelCellId, ref BoundingSphere explosionSphere)
        {
            //  Search for all buffers storing this voxelmap and render cell
            foreach (MyDecalsForVoxelsTriangleBuffer buffer in m_usedTriangleBuffers)
            {
                if (buffer.VoxelCell.ID == voxelCellId)
                {
                    buffer.HideTrianglesAfterExplosion(ref explosionSphere);
                }
            }
        } 

        public void Clear()
        {
            for (int i = 0; i < m_capacity; i++)
            {
                m_triangleBuffers[i].Clear(true);
            }
        }
    }
}
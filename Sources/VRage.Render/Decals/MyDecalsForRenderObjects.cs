using System.Collections.Generic;
using System;

//using VRageMath.Graphics;
using SharpDX;
using SharpDX.Direct3D9;




//  This class mainstains collection of model/texture decal triangleVertexes buffers.

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
    using VRageRender.Graphics;
    using VRageRender.Effects;
    using VRageRender.Textures;


    class MyDecalsForRenderObjects
    {
        enum MyDecalForModelsState : byte
        {
            READY,
            FADING_OUT
        }

        struct MyDecalsForModelsDictionaryKey
        {
            public MyRenderObject RenderObject;
            public MyDecalTexturesEnum DecalTexture;

            public MyDecalsForModelsDictionaryKey(MyRenderObject renderObject, MyDecalTexturesEnum decalTexture)
            {
                RenderObject = renderObject;
                DecalTexture = decalTexture;
            }
        }

        MyDecalForModelsState m_status;
        int m_fadingOutStartTime;
        int m_capacity;
        int m_fadingOutStartLimit;
        int m_fadingOutBuffersCount;
        MyDecalsForRenderObjectsTriangleBuffer[] m_triangleBuffers;
        Dictionary<MyDecalsForModelsDictionaryKey, MyDecalsForRenderObjectsTriangleBuffer> m_triangleBuffersByKey;
        Stack<MyDecalsForRenderObjectsTriangleBuffer> m_freeTriangleBuffers;
        List<MyDecalsForRenderObjectsTriangleBuffer> m_usedTriangleBuffers;
        List<MyDecalsForRenderObjectsTriangleBuffer> m_sortTriangleBuffersByTexture;


        public MyDecalsForRenderObjects(int capacity)
        {
            m_status = MyDecalForModelsState.READY;
            m_capacity = capacity;
            m_fadingOutStartLimit = (int)(m_capacity * MyDecalsConstants.TEXTURE_LARGE_FADING_OUT_START_LIMIT_PERCENT);
            m_fadingOutBuffersCount = (int)(m_capacity * MyDecalsConstants.TEXTURE_LARGE_FADING_OUT_MINIMAL_TRIANGLE_COUNT_PERCENT);

            m_sortTriangleBuffersByTexture = new List<MyDecalsForRenderObjectsTriangleBuffer>(m_capacity);
            m_triangleBuffersByKey = new Dictionary<MyDecalsForModelsDictionaryKey, MyDecalsForRenderObjectsTriangleBuffer>(m_capacity);
            m_freeTriangleBuffers = new Stack<MyDecalsForRenderObjectsTriangleBuffer>(m_capacity);
            m_usedTriangleBuffers = new List<MyDecalsForRenderObjectsTriangleBuffer>(m_capacity);

            m_triangleBuffers = new MyDecalsForRenderObjectsTriangleBuffer[m_capacity];
            for (int i = 0; i < m_capacity; i++)
            {
                m_triangleBuffers[i] = new MyDecalsForRenderObjectsTriangleBuffer(MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER);
                m_freeTriangleBuffers.Push(m_triangleBuffers[i]);
            }
        }

        public MyDecalsForRenderObjectsTriangleBuffer GetTrianglesBuffer(MyRenderObject renderObject, MyDecalTexturesEnum decalTexture)
        {
            MyDecalsForModelsDictionaryKey key = new MyDecalsForModelsDictionaryKey(renderObject, decalTexture);

            MyDecalsForRenderObjectsTriangleBuffer outValue;
            if (m_triangleBuffersByKey.TryGetValue(key, out outValue))
            {
                //  Combination of model/texture was found in dictionary, so we can return in right now
                return outValue;
            }
            else
            {
                if (m_triangleBuffersByKey.Count >= m_capacity)
                {
                    //  We are full, can't place decal on a new model/texture. Need to wait for next CheckBufferFull.
                    return null;
                }
                else
                {
                    //  This is first time we want to place decal on this model/texture, so here we allocate and initialize buffer
                    MyDecalsForRenderObjectsTriangleBuffer newBuffer = m_freeTriangleBuffers.Pop();
                    m_triangleBuffersByKey.Add(key, newBuffer);
                    m_usedTriangleBuffers.Add(newBuffer);
                    newBuffer.Start(renderObject, decalTexture);
                    return newBuffer;
                }
            }
        }

        public void ReturnTrianglesBuffer(MyRenderObject renderObject)
        {
            foreach (byte value in Enum.GetValues(typeof(MyDecalTexturesEnum)))
            {
                var key = new MyDecalsForModelsDictionaryKey(renderObject, (MyDecalTexturesEnum)value);

                MyDecalsForRenderObjectsTriangleBuffer outValue;
                if (m_triangleBuffersByKey.TryGetValue(key, out outValue))
                {
                    MyDecalsForRenderObjectsTriangleBuffer usedBuffer = outValue;

                    m_triangleBuffersByKey.Remove(key);
                    usedBuffer.Clear();
                    m_usedTriangleBuffers.Remove(usedBuffer);
                    m_freeTriangleBuffers.Push(usedBuffer);
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

        public void CheckIfBufferIsFull()
        {
            if (m_status == MyDecalForModelsState.FADING_OUT)
            {
                if ((MyRender.RenderTimeInMS - m_fadingOutStartTime) > MyDecalsConstants.DECALS_FADE_OUT_INTERVAL_MILISECONDS)
                {
                    //  If fading-out phase finished, we change state and remove faded-out buffers
                    for (int i = 0; i < m_fadingOutBuffersCount; i++)
                    {
                        if (m_usedTriangleBuffers.Count > 0)
                        {
                            MyDecalsForRenderObjectsTriangleBuffer releasedBuffer = m_usedTriangleBuffers[0];
                            m_usedTriangleBuffers.RemoveAt(0);
                            releasedBuffer.Clear();
                            m_freeTriangleBuffers.Push(releasedBuffer);
                            m_triangleBuffersByKey.Remove(new MyDecalsForModelsDictionaryKey(releasedBuffer.RenderObject, releasedBuffer.DecalTexture));
                        }
                    }

                    m_status = MyDecalForModelsState.READY;
                }
            }
            else
            {
                if (m_triangleBuffersByKey.Count >= m_fadingOutStartLimit)
                {
                    int i = 0;
                    foreach (MyDecalsForRenderObjectsTriangleBuffer buffer in m_usedTriangleBuffers)
                    {
                        if (i < m_fadingOutBuffersCount)
                        {
                            buffer.FadeOutAll();
                        }
                        i++;
                    }

                    m_status = MyDecalForModelsState.FADING_OUT;
                    m_fadingOutStartTime = (int)MyRender.RenderTimeInMS;
                }
            }
        }

        public void Draw(MyVertexFormatDecal[] vertices, MyEffectDecals effect, MyTexture2D[] texturesDiffuse, MyTexture2D[] texturesNormalMap)
        {
            CheckIfBufferIsFull();

            //  SortForSAP buffers by texture
            m_sortTriangleBuffersByTexture.Clear();
            foreach (MyDecalsForRenderObjectsTriangleBuffer buffer in m_usedTriangleBuffers)
            {
                if (buffer.RenderObject.Visible == true)
                {
                    /*  todo drawdecals flag
                    if ((buffer.Entity == MyGuiScreenGamePlay.Static.ControlledEntity
                        || buffer.Entity.Parent == MyGuiScreenGamePlay.Static.ControlledEntity) && 
                        MyGuiScreenGamePlay.Static.IsFirstPersonView)
                    {
                        //  Don't draw decals if they are on an entity in which the camera is
                        continue;
                    } */

                    // Decal with "ExplosionSmut" texture is much larger, so it must be drawed to larger distance.
                    float fadeoutDistance = MyDecals.GetMaxDistanceForDrawingDecals();
                    //if (buffer.DecalTexture == MyDecalTexturesEnum.ExplosionSmut)
                      //  fadeoutDistance *= MyDecalsConstants.DISTANCE_MULTIPLIER_FOR_LARGE_DECALS;

                    //if (Vector3.Distance(MyCamera.m_initialSunWindPosition, buffer.PhysObject.GetPosition()) >= (MyDecals.GetMaxDistanceForDrawingDecals()))
                    //if (buffer.PhysObject.GetDistanceBetweenCameraAndBoundingSphere() >= MyDecals.GetMaxDistanceForDrawingDecals())
                    
                    /*if (buffer.RenderObject.GetDistanceBetweenCameraAndBoundingSphere() >= fadeoutDistance)
                    {
                        continue;
                    } */

                    m_sortTriangleBuffersByTexture.Add(buffer);
                }
            }            
            m_sortTriangleBuffersByTexture.Sort();
            
            //  Draw decals - sorted by texture
            MyDecalTexturesEnum? lastDecalTexture = null;
            for (int i = 0; i < m_sortTriangleBuffersByTexture.Count; i++)
            {
                MyDecalsForRenderObjectsTriangleBuffer buffer = m_sortTriangleBuffersByTexture[i];

                int trianglesCount = buffer.CopyDecalsToVertices(vertices);

                if (trianglesCount <= 0) continue;

                //  Switch texture only if different than previous one
                if ((lastDecalTexture == null) || (lastDecalTexture != buffer.DecalTexture))
                {
                    int textureIndex = (int)buffer.DecalTexture;
                    effect.SetDecalDiffuseTexture(texturesDiffuse[textureIndex]);
                    effect.SetDecalNormalMapTexture(texturesNormalMap[textureIndex]);
                    lastDecalTexture = buffer.DecalTexture;
                }

                //effect.SetWorldMatrix(buffer.Entity.WorldMatrix * Matrix.CreateTranslation(-MyCamera.Position));
                if (buffer.RenderObject is MyRenderTransformObject)
                    effect.SetWorldMatrix((Matrix)((MyRenderTransformObject)buffer.RenderObject).GetWorldMatrixForDraw());
                else
                    effect.SetWorldMatrix((Matrix)((MyManualCullableRenderObject)buffer.RenderObject).GetWorldMatrixForDraw());

                effect.SetViewProjectionMatrix(MyRenderCamera.ViewProjectionMatrixAtZero);

                // set FadeoutDistance
                float fadeoutDistance = MyDecals.GetMaxDistanceForDrawingDecals();
                //if (buffer.DecalTexture == MyDecalTexturesEnum.ExplosionSmut)
                  //  fadeoutDistance *= MyDecalsConstants.DISTANCE_MULTIPLIER_FOR_LARGE_DECALS;

                effect.SetFadeoutDistance(fadeoutDistance);

                effect.SetTechnique(MyEffectDecals.Technique.Model);

                MyRender.GraphicsDevice.VertexDeclaration = MyVertexFormatDecal.VertexDeclaration;

                effect.Begin();
                MyRender.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, 0, trianglesCount, vertices);
                effect.End();

                MyPerformanceCounter.PerCameraDrawWrite.DecalsForEntitiesInFrustum += trianglesCount;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRageMath;
using VRage.Utils;
using VRage.Import;
using VRageRender.Graphics;


//  This class is a buffer of model decal triangles. Capacity is constant. 
//  Triangles in this buffer must belong to one model and texture.
//
//  Add method checks if buffer is free and only if is, we add the triangles.
//  If buffer is full, release phase is initiated, and during this phase, small amount of
//  triangles is faded-out. After that, places of this triangles can be used for storing new decals.
//
//  Each triangleVertexes belongs to one decal, so one decal can have more triangles. This connections are
//  monitored and when removing triangles, we always remove all triangles of a decal.

namespace VRageRender
{
    class MyDecalsForRenderObjectsTriangleBuffer : IComparable, IMyDecalsBuffer
    {
        enum MyDecalsBufferState : byte
        {
            READY,
            FADING_OUT_ONLY_BEGINNING,
            FADING_OUT_ALL
        }

        public MyRenderObject RenderObject;
        public MyDecalTexturesEnum DecalTexture;

        public int MaxNeighbourTriangles;

        Stack<MyDecalTriangle> m_freeTriangles;
        Queue<MyDecalTriangle> m_trianglesQueue;
        MyDecalsBufferState m_status;
        int m_capacity;
        //int m_capacityAfterStart;                   // This must be less than m_capacity, because buffer is initialized only once. This capacity is only work bounding number of decals for this type of texture
        int m_fadingOutStartLimit;                  //  Start fading out if this percent of buffer 'fillness' is achieved
        int m_fadingOutMinimalTriangleCount;        //  Minimal number of triangles we fade-out from the beggining of the queue
        int m_fadingOutStartTime;                   //  When last fading-out started
        int m_fadingOutRealTriangleCount;           //  When in fade-out phase, this is the real number of triangles we will fade-out. Always equal or more than 'm_fadingOutMinimalTriangleCount'


        public MyDecalsForRenderObjectsTriangleBuffer(int capacity)
        {
            m_capacity = capacity;
            m_trianglesQueue = new Queue<MyDecalTriangle>(m_capacity);

            //  Preallocate triangles
            m_freeTriangles = new Stack<MyDecalTriangle>(m_capacity);
            for (int i = 0; i < m_capacity; i++)
            {
                m_freeTriangles.Push(new MyDecalTriangle());
            }
        }

        //  Because this class is reused in buffers, it isn't really initialized by constructor. We make real initialization here.
        public void Start(MyRenderObject renderObject, MyDecalTexturesEnum decalTexture)
        {
            RenderObject = renderObject;
            DecalTexture = decalTexture;
            m_status = MyDecalsBufferState.READY;
            m_fadingOutStartTime = 0;
                    /*
            if (MyDecals.IsLargeTexture(decalTexture) == true)
            {
                //m_capacityAfterStart = MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER_LARGE;
                m_fadingOutStartLimit = (int)(m_capacity * MyDecalsConstants.TEXTURE_LARGE_FADING_OUT_START_LIMIT_PERCENT);
                m_fadingOutMinimalTriangleCount = (int)(m_capacity * MyDecalsConstants.TEXTURE_LARGE_FADING_OUT_MINIMAL_TRIANGLE_COUNT_PERCENT);
                MaxNeighbourTriangles = MyDecalsConstants.TEXTURE_LARGE_MAX_NEIGHBOUR_TRIANGLES;
            }
            else  */
            {
                //m_capacityAfterStart = MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER_SMALL;
                m_fadingOutStartLimit = (int)(m_capacity * MyDecalsConstants.TEXTURE_SMALL_FADING_OUT_START_LIMIT_PERCENT);
                m_fadingOutMinimalTriangleCount = (int)(m_capacity * MyDecalsConstants.TEXTURE_SMALL_FADING_OUT_MINIMAL_TRIANGLE_COUNT_PERCENT);
                MaxNeighbourTriangles = MyDecalsConstants.TEXTURE_SMALL_MAX_NEIGHBOUR_TRIANGLES;
            }
        }

        //  We can just erase decal triangles, because here we don't have 'free triangles stack' as voxel decals do.
        public void Clear(bool destroy = false)
        {
            while (m_trianglesQueue.Count > 0)
            {
                MyDecalTriangle triangle = m_trianglesQueue.Dequeue();
                triangle.Close();
                m_freeTriangles.Push(triangle);
            }

            if (destroy)
                RenderObject = null;
        }
        
        public void FadeOutAll()
        {
            if (m_status != MyDecalsBufferState.FADING_OUT_ONLY_BEGINNING)
            {
                m_fadingOutStartTime = (int)MyRender.RenderTimeInMS;
            }

            m_status = MyDecalsBufferState.FADING_OUT_ALL;
        }

        //  Checks if buffer has enough free triangles for adding new decal. If not, we can't add triangles
        //  of the decal (because we add all decal triangles or none)
        public bool CanAddTriangles(int newTrianglesCount)
        {
            //  If whole buffer is fading out or if buffer is full, we can't add new triangles
            return (m_status != MyDecalsBufferState.FADING_OUT_ALL) && ((m_trianglesQueue.Count + newTrianglesCount) < m_capacity);
        }

        public void Add(MyDecalTriangle_Data triangle, int remainingTrianglesOfThisDecal, Vector3D position, float lightSize, float emissivity)
        {
            //  We can't add triangles while fading out
            //if (m_status == MyDecalsBufferState.FADING_OUT) return;            

            MyDecalTriangle decalTriangle = m_freeTriangles.Pop();
            decalTriangle.Start(lightSize);
            decalTriangle.Emissivity = emissivity;
            //decalTriangle.RandomOffset = MyMwcUtils.GetRandomFloat(0.0f, MathHelper.Pi);
            decalTriangle.RandomOffset = (float)MyRender.RenderTimeInMS;
            decalTriangle.Position = position;

            // We must repack vertex positions before copying to new triange, to avoid Z-fight.
            decalTriangle.Position0 = triangle.Vertexes.Vertex0;
            decalTriangle.Position1 = triangle.Vertexes.Vertex1;
            decalTriangle.Position2 = triangle.Vertexes.Vertex2;

            //  Texture coords
            decalTriangle.TexCoord0 = triangle.TexCoords.TexCoord0;
            decalTriangle.TexCoord1 = triangle.TexCoords.TexCoord1;
            decalTriangle.TexCoord2 = triangle.TexCoords.TexCoord2;


            //  Alpha
            decalTriangle.Color0 = triangle.Colors.Color0;
            decalTriangle.Color1 = triangle.Colors.Color1;
            decalTriangle.Color2 = triangle.Colors.Color2;

            //AdjustAlphaByDistance(normal, decalSize, position, decalTriangle);

            //  Bump mapping
            decalTriangle.Normal0 = triangle.Normals.Normal0;
            decalTriangle.Normal1 = triangle.Normals.Normal1;
            decalTriangle.Normal2 = triangle.Normals.Normal2;
            /*
            decalTriangle.Tangent0 = triangle.Tangents.Normal0;
            decalTriangle.Tangent1 = triangle.Tangents.Normal1;
            decalTriangle.Tangent2 = triangle.Tangents.Normal2;
            */
            decalTriangle.RemainingTrianglesOfThisDecal = remainingTrianglesOfThisDecal;

            m_trianglesQueue.Enqueue(decalTriangle);
        }

        /// <summary>
        /// Reduces the alpha of the decalTriangle if it's in front of the decal position.
        /// The further it is, the lower alpha it gets.
        /// </summary>
        private static void AdjustAlphaByDistance(Vector3 normal, float decalSize, Vector3 position,
                                                  MyDecalTriangle decalTriangle)
        {
            Vector3 displacement0 = decalTriangle.Position0 - position;
            var dot0 = Vector3.Dot(displacement0, normal) / decalSize;
            if (dot0 > 0)
            {
                float alignment0 = 1 - dot0;
                decalTriangle.Color0.W *= alignment0;
            }

            Vector3 displacement1 = decalTriangle.Position1 - position;
            var dot1 = Vector3.Dot(displacement1, normal) / decalSize;
            if (dot1 > 0)
            {
                float alignment1 = 1 - dot1;
                decalTriangle.Color1.W *= alignment1;
            }

            Vector3 displacement2 = decalTriangle.Position2 - position;
            var dot2 = Vector3.Dot(displacement2, normal) / decalSize;
            if (dot2 > 0)
            {
                float alignment2 = 1 - dot2;
                decalTriangle.Color2.W *= alignment2;
            }
        }

        float GetAlphaByAngleDiff(ref Vector3 referenceNormal, ref Vector3 vertexNormal)
        {
            float dot = Vector3.Dot(referenceNormal, vertexNormal);
            if (dot < MyMathConstants.EPSILON)
                return 0;
            float result = (float) Math.Pow(dot, 3f);
            return MathHelper.Clamp(result, 0, 1);
        }

        //  Checks if buffer isn't full at 80% (or something like that). If is, we start fadeing-out first 20% triangles (or something like that). But always all triangles of a decal.
        void CheckIfBufferIsFull()
        {
            if (m_status == MyDecalsBufferState.FADING_OUT_ALL)
            {
                return;
            } 
            else if (m_status == MyDecalsBufferState.FADING_OUT_ONLY_BEGINNING)
            {
                if ((MyRender.RenderTimeInMS - m_fadingOutStartTime) > MyDecalsConstants.DECALS_FADE_OUT_INTERVAL_MILISECONDS)
                {
                    //  If fading-out phase finished, we change state and remove faded-out triangles
                    for (int i = 0; i < m_fadingOutRealTriangleCount; i++)
                    {
                        MyDecalTriangle triangle = m_trianglesQueue.Dequeue();
                        triangle.Close();
                        m_freeTriangles.Push(triangle);
                    }

                    m_status = MyDecalsBufferState.READY;
                }
            }
            else
            {
                if (m_trianglesQueue.Count >= m_fadingOutStartLimit) 
                {
                    //  If we get here, buffer is close to be full, so we start fade-out phase
                    m_fadingOutRealTriangleCount = GetFadingOutRealTriangleCount();
                    m_status = MyDecalsBufferState.FADING_OUT_ONLY_BEGINNING;
                    m_fadingOutStartTime = (int)MyRender.RenderTimeInMS;
                }
            }
        }

        int GetFadingOutRealTriangleCount()
        {
            int result = 1;
            foreach (MyDecalTriangle decalTriangle in m_trianglesQueue)
            {
                if ((result >= m_fadingOutMinimalTriangleCount) && (decalTriangle.RemainingTrianglesOfThisDecal == 0))
                {
                    break;
                }

                result++;
            }

            return result;
        }

        //  For sorting buffers by texture
        public int CompareTo(object compareToObject)
        {
            MyDecalsForRenderObjectsTriangleBuffer compareToBuffer = (MyDecalsForRenderObjectsTriangleBuffer)compareToObject;
            return ((int)compareToBuffer.DecalTexture).CompareTo((int)this.DecalTexture);
        }

        //  Copy triangles to array of vertexes and return count of triangles to draw
        public int CopyDecalsToVertices(MyVertexFormatDecal[] vertices)
        {
            CheckIfBufferIsFull();            

            float fadingOutAlpha = 1;
            if ((m_status == MyDecalsBufferState.FADING_OUT_ONLY_BEGINNING) || (m_status == MyDecalsBufferState.FADING_OUT_ALL))
            {
                fadingOutAlpha = 1 - MathHelper.Clamp((float)(MyRender.RenderTimeInMS - m_fadingOutStartTime) / (float)MyDecalsConstants.DECALS_FADE_OUT_INTERVAL_MILISECONDS, 0, 1);
            }

            int i = 0;
            foreach (MyDecalTriangle decalTriangle in m_trianglesQueue)
            {
                float alpha = 1;

                //  If fading-out, we blend first 'm_fadingOutRealTriangleCount' triangles
                if (m_status == MyDecalsBufferState.FADING_OUT_ALL)
                {
                    alpha = fadingOutAlpha;
                }
                else if ((m_status == MyDecalsBufferState.FADING_OUT_ONLY_BEGINNING) && (i < m_fadingOutRealTriangleCount))
                {
                    alpha = fadingOutAlpha;
                }

                int vertexIndexStart = i * MyDecalsConstants.VERTEXES_PER_DECAL;

                vertices[vertexIndexStart + 0].Position = decalTriangle.Position0;
                vertices[vertexIndexStart + 1].Position = decalTriangle.Position1;
                vertices[vertexIndexStart + 2].Position = decalTriangle.Position2;

                vertices[vertexIndexStart + 0].TexCoord = decalTriangle.TexCoord0;
                vertices[vertexIndexStart + 1].TexCoord = decalTriangle.TexCoord1;
                vertices[vertexIndexStart + 2].TexCoord = decalTriangle.TexCoord2;

                Vector4 color0 = decalTriangle.Color0;
                Vector4 color1 = decalTriangle.Color1;
                Vector4 color2 = decalTriangle.Color2;

                color0.W *= alpha;
                color1.W *= alpha;
                color2.W *= alpha;

                vertices[vertexIndexStart + 0].Color = color0;
                vertices[vertexIndexStart + 1].Color = color1;
                vertices[vertexIndexStart + 2].Color = color2;

                vertices[vertexIndexStart + 0].Normal = decalTriangle.Normal0;
                vertices[vertexIndexStart + 1].Normal = decalTriangle.Normal1;
                vertices[vertexIndexStart + 2].Normal = decalTriangle.Normal2;
                /*
                vertices[vertexIndexStart + 0].Tangent = decalTriangle.Tangent0;
                vertices[vertexIndexStart + 1].Tangent = decalTriangle.Tangent1;
                vertices[vertexIndexStart + 2].Tangent = decalTriangle.Tangent2;
                */
                float emisivity = MyDecals.UpdateDecalEmissivity(decalTriangle, alpha, RenderObject);

                vertices[vertexIndexStart + 0].EmissiveRatio = emisivity;
                vertices[vertexIndexStart + 1].EmissiveRatio = emisivity;
                vertices[vertexIndexStart + 2].EmissiveRatio = emisivity;

                i++;
            }

            return i;
        }
    }
}
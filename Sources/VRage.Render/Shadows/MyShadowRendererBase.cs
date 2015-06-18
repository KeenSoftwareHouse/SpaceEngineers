using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using System.Diagnostics;
using VRageRender.Effects;
using SharpDX.Direct3D9;

namespace VRageRender.Shadows
{
    class MyShadowRendererBase : MyRenderComponentBase
    {
        static protected List<MyElement> m_castingRenderObjects = new List<MyElement>();
        static protected HashSet<MyRenderObject> m_castingRenderObjectsUnique = new HashSet<MyRenderObject>();
        static protected List<MyElement> m_castingCullObjects = new List<MyElement>();
        static protected List<MyElement> m_castingManualCullObjects = new List<MyElement>();
        static protected List<MyRender.MyRenderElement> m_renderElementsForShadows = new List<MyRender.MyRenderElement>();
        static protected List<MyRender.MyRenderElement> m_transparentRenderElementsForShadows = new List<MyRender.MyRenderElement>();

        static protected Matrix[] m_bonesBuffer = new Matrix[MyRenderConstants.MAX_SHADER_BONES];

        //  Used to sort render elements by their properties to spare switching render states
        public class MyShadowRenderElementsComparer : IComparer<MyRender.MyRenderElement>
        {
            public int Compare(MyRender.MyRenderElement x, MyRender.MyRenderElement y)
            {
                return x.VertexBuffer.GetHashCode().CompareTo(y.VertexBuffer.GetHashCode());
            }
        }

        public override int GetID()
        {
            return (int)MyRenderComponentID.ShadowRendererBase;
        }

        public override void LoadContent()
        {
        }

        public override void UnloadContent()
        {
            Clear();
        }

        protected static void Clear()
        {
            m_castingRenderObjects.Clear();
            m_castingRenderObjectsUnique.Clear();
            m_castingCullObjects.Clear();
            m_castingManualCullObjects.Clear();
            m_renderElementsForShadows.Clear();
            m_transparentRenderElementsForShadows.Clear();
        }

        protected static MyShadowRenderElementsComparer m_shadowElementsComparer = new MyShadowRenderElementsComparer();

        protected static void DrawElements(List<MyRender.MyRenderElement> elements, MyEffectShadowMap effect, bool relativeCamera, Vector3D cameraPos, int perfCounterIndex, bool ditheredShadows)
        {            
            // Draw shadows.
            effect.SetTechnique(MyEffectShadowMap.ShadowTechnique.GenerateShadow);
            DrawShadowsForElements(effect, relativeCamera, cameraPos, elements, false, false, false, false, perfCounterIndex, ditheredShadows);

            effect.SetTechnique(MyEffectShadowMap.ShadowTechnique.GenerateShadowForVoxels);
            DrawShadowsForElements(effect, relativeCamera, cameraPos, elements, true, false, false, false, perfCounterIndex, ditheredShadows);

            effect.SetTechnique(MyEffectShadowMap.ShadowTechnique.GenerateShadowForSkinned);
            DrawShadowsForElements(effect, relativeCamera, cameraPos, elements, false, true, false, false, perfCounterIndex, ditheredShadows);

            effect.SetTechnique(MyEffectShadowMap.ShadowTechnique.GenerateShadowInstanced);
            DrawShadowsForElements(effect, relativeCamera, cameraPos, elements, false, false, true, false, perfCounterIndex, ditheredShadows);

            effect.SetTechnique(MyEffectShadowMap.ShadowTechnique.GenerateShadowInstancedGeneric);
            DrawShadowsForElements(effect, relativeCamera, cameraPos, elements, false, false, false, true, perfCounterIndex, ditheredShadows);

            // We don't want to keep objects alive
            Clear();
        }

        private static void DrawShadowsForElements(MyEffectShadowMap effect, bool relativeCamera, Vector3D cameraPos, List<MyRender.MyRenderElement> elements, bool voxelShadows, bool skinnedShadows, bool instancedShadows, bool genericInstancedShadows, int perfCounterIndex, bool ditheredShadows)
        {
            effect.Begin();

            long lastVertexBuffer = 0;

            for (int i = 0; i < elements.Count; i++)
            {
                MyRender.MyRenderElement renderElement = elements[i];

                if ((renderElement.RenderObject is MyRenderVoxelCell) != voxelShadows ||
                    renderElement.VertexBuffer.IsDisposed ||
                    renderElement.IndexBuffer.IsDisposed)
                    continue;

                if ((renderElement.RenderObject is MyRenderCharacter) != skinnedShadows)
                    continue;

                //if ((renderElement.InstanceBuffer != null) != instancedShadows)
                //    continue;

                if ((renderElement.InstanceBuffer != null && renderElement.InstanceStride == 64) != instancedShadows)
                    continue;

                if ((renderElement.InstanceBuffer != null && renderElement.InstanceStride == 32) != genericInstancedShadows)
                    continue;

                long currentVertexBuffer = renderElement.VertexBuffer.GetHashCode();
                if ((lastVertexBuffer != currentVertexBuffer) || instancedShadows || genericInstancedShadows)
                {
                    lastVertexBuffer = currentVertexBuffer;

                    MyRender.GraphicsDevice.Indices = renderElement.IndexBuffer;
                    MyRender.GraphicsDevice.VertexDeclaration = renderElement.VertexDeclaration;

                    if (renderElement.InstanceBuffer == null)
                    {
                        MyRender.GraphicsDevice.ResetStreamSourceFrequency(0);
                        MyRender.GraphicsDevice.ResetStreamSourceFrequency(1);
                        MyRender.GraphicsDevice.SetStreamSource(1, null, 0, 0);
                    }
                    else
                    {
                        MyRender.GraphicsDevice.SetStreamSourceFrequency(0, renderElement.InstanceCount, StreamSource.IndexedData);
                        MyRender.GraphicsDevice.SetStreamSourceFrequency(1, 1, StreamSource.InstanceData);
                        MyRender.GraphicsDevice.SetStreamSource(1, renderElement.InstanceBuffer, renderElement.InstanceStart * renderElement.InstanceStride, renderElement.InstanceStride);
                    }
                    MyRender.GraphicsDevice.SetStreamSource(0, renderElement.VertexBuffer, 0, renderElement.VertexStride);
                    
                    System.Diagnostics.Debug.Assert(renderElement.IndexBuffer != null);
                }

                if (relativeCamera)
                {
                    MatrixD m = renderElement.WorldMatrix;
                    m.Translation -= cameraPos;
                    effect.SetWorldMatrix((Matrix)m);
                }
                else
                {
                    effect.SetWorldMatrix((Matrix)renderElement.WorldMatrix);
                }

                if (voxelShadows)
                {
                    var voxelCell = renderElement.RenderObject as MyRenderVoxelCell;
                    if (voxelCell != null)
                    {
                        MyRenderVoxelCell.EffectArgs args;
                        voxelCell.GetEffectArgs(out args);
                        effect.VoxelVertex.SetArgs(ref args);
                    }
                }

                var skinMatrices = skinnedShadows ? ((MyRenderCharacter)renderElement.RenderObject).SkinMatrices : null;
                if (skinMatrices != null)
                {
                    var bonesUsed = renderElement.BonesUsed;
                    if (bonesUsed == null)
                        for (int b = 0; b < Math.Min(skinMatrices.Length, MyRenderConstants.MAX_SHADER_BONES); b++)
                            m_bonesBuffer[b] = skinMatrices[b];
                    else
                        for (int b = 0; b < bonesUsed.Length; b++)
                            m_bonesBuffer[b] = skinMatrices[bonesUsed[b]];
                    effect.SetBones(m_bonesBuffer);
                }

                effect.SetDithering(ditheredShadows ? renderElement.Dithering : 0);

                MyPerformanceCounter.PerCameraDrawWrite.ShadowDrawCalls[perfCounterIndex]++;

                effect.D3DEffect.CommitChanges();

                MyRender.GraphicsDevice.DrawIndexedPrimitive(SharpDX.Direct3D9.PrimitiveType.TriangleList, 0, 0, renderElement.VertexCount, renderElement.IndexStart, renderElement.TriCount);
                MyPerformanceCounter.PerCameraDrawWrite.TotalDrawCalls++;
            }

            MyRender.GraphicsDevice.ResetStreamSourceFrequency(0);
            MyRender.GraphicsDevice.ResetStreamSourceFrequency(1);

            effect.End();
        }
    }
}

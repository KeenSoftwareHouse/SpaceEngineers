using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRageMath;
using System.Linq;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageRender.Messages;

namespace VRageRender
{
    internal static class MyGPUEmitters
    {
        internal const int MAX_LIVE_EMITTERS = 1024;
        internal const int MAX_PARTICLES = 400 * 1024;

        private const int ATLAS_INDEX_BITS = 12;
        private const int ATLAS_DIMENSION_BITS = 6;
        private const int ATLAS_TEXTURE_BITS = 8;
        private const int MAX_ATLAS_DIMENSION = 1 << ATLAS_DIMENSION_BITS;
        private const int MAX_ATLAS_INDEX = 1 << ATLAS_INDEX_BITS;

        private class MyLiveData:IComparable
        {
            internal float ParticlesEmittedFraction;
            internal int BufferIndex;
            internal MyGPUEmitter GPUEmitter;
            internal float DieAt;
            internal bool JustAdded;
            public Vector3D LastWorldPosition;

            public int CompareTo(object obj)
            {
                var c = obj as MyLiveData;
                var cDist = Vector3D.DistanceSquared(c.GPUEmitter.WorldPosition, MyRender11.Environment.Matrices.CameraPosition);
                var dist = Vector3D.DistanceSquared(GPUEmitter.WorldPosition, MyRender11.Environment.Matrices.CameraPosition);
                return dist < cDist ? -1 : 1;
            }
        }

        private static Stack<int> m_freeBufferIndices;

        private class MyTextureArrayIndex
        {
            internal uint Index;
            internal uint Counter;
        }
        private static Dictionary<string, MyTextureArrayIndex> m_textureArrayIndices = new Dictionary<string, MyTextureArrayIndex>();
        private static IFileArrayTexture m_textureArray;
        private static bool m_textureArrayDirty;

        private static Dictionary<uint, MyLiveData> m_emitters = new Dictionary<uint, MyLiveData>();

        internal static void Init()
        {
            m_emitters.Clear();
            m_textureArrayIndices.Clear();
            m_freeBufferIndices = new Stack<int>();
            for (int i = 0; i < MAX_LIVE_EMITTERS; i++)
                m_freeBufferIndices.Push(i);
        }
        internal static void OnDeviceReset()
        {
            DoneDevice();
            //InitDevice();
        }
        private static void DoneDevice()
        {
            var arrayManager = MyManagers.FileArrayTextures;
            arrayManager.DisposeTex(ref m_textureArray);
            m_textureArray = null;
            m_textureArrayDirty = false;
        }
        internal static void OnSessionEnd()
        {
            Debug.Assert(m_emitters.Count == 0);
            DoneDevice();

            Init();
        }

        internal static void Create(uint GID)
        {
            m_emitters.Add(GID, new MyLiveData 
            {
                BufferIndex = -1,
                DieAt = float.MaxValue,
                GPUEmitter = new MyGPUEmitter { GID = GID },
                JustAdded = true
            });
        }

        internal static void Remove(uint GID, bool instant = true, bool check = true)
        {
            MyLiveData emitter;
            if (m_emitters.TryGetValue(GID, out emitter))
            {
                if (emitter.BufferIndex == -1)
                    Remove(emitter);
                else if (instant)
                    emitter.GPUEmitter.Data.Flags |= GPUEmitterFlags.Dead;
                else
                {
                    emitter.GPUEmitter.ParticlesPerSecond = 0;
                    emitter.GPUEmitter.ParticlesPerFrame = 0;
                    emitter.DieAt = MyCommon.TimerMs + emitter.GPUEmitter.Data.ParticleLifeSpan * 1000;
                }
            }
            else MyRenderProxy.Assert(check, "Invalid emitter id: " + GID);
        }

        const double MAX_DISTANCE = 100;
        internal static void UpdateData(MyGPUEmitter[] def)
        {
            for (int i = 0; i < def.Length; i++)
            {
                MyLiveData emitter;
                if (m_emitters.TryGetValue(def[i].GID, out emitter))
                {
                    MarkTextureUnused(emitter.GPUEmitter.AtlasTexture);
                    AddTexture(def[i].AtlasTexture);

                    emitter.GPUEmitter = def[i];
                    emitter.GPUEmitter.Data.TextureIndex1 = GenerateTextureIndex(emitter);
                    emitter.GPUEmitter.Data.TextureIndex2 = (uint)def[i].AtlasFrameModulo;
                    emitter.GPUEmitter.Data.RotationMatrix = Matrix.Transpose(def[i].Data.RotationMatrix);
                    if (emitter.JustAdded)
                        emitter.LastWorldPosition = emitter.GPUEmitter.WorldPosition;
                    emitter.JustAdded = false;
                }
                else MyRenderProxy.Assert(false, "invalid emitter id: " + def[i].GID);
            }
        }
        internal static void UpdateTransforms(MyGPUEmitterTransformUpdate[] emitters)
        {
            for (int i = 0; i < emitters.Length; i++)
            {
                MyLiveData emitter;
                if (m_emitters.TryGetValue(emitters[i].GID, out emitter))
                {
                    emitter.GPUEmitter.WorldPosition = emitters[i].Transform.Translation;
                    emitter.GPUEmitter.Data.RotationMatrix = MatrixD.Transpose(emitters[i].Transform);
                    emitter.GPUEmitter.Data.Gravity = emitters[i].Gravity;
                    emitter.GPUEmitter.Data.Scale = emitters[i].Scale;
                    emitter.GPUEmitter.ParticlesPerSecond = emitters[i].ParticlesPerSecond;
                }
                else MyRenderProxy.Assert(false, "invalid emitter id: " + emitters[i].GID);
            }
        }
        internal static void UpdateLight(MyGPUEmitterLight[] emitters)
        {
            for (int i = 0; i < emitters.Length; i++)
            {
                MyLiveData emitter;
                if (m_emitters.TryGetValue(emitters[i].GID, out emitter))
                {
                    emitter.GPUEmitter.ParticlesPerSecond = emitters[i].ParticlesPerSecond;
                }
                else MyRenderProxy.Assert(false, "invalid emitter id: " + emitters[i].GID);
            }
        }
        internal static void ReloadTextures()
        {
            m_textureArrayDirty = true;
        }
        private static void Remove(MyLiveData emitter)
        {
            if (emitter.BufferIndex != -1)
                m_freeBufferIndices.Push(emitter.BufferIndex);

            m_emitters.Remove(emitter.GPUEmitter.GID);

            MarkTextureUnused(emitter.GPUEmitter.AtlasTexture);
        }

        private static void AddTexture(string tex)
        {
            if (tex == null)
                return;

            if (m_textureArrayIndices.ContainsKey(tex))
                m_textureArrayIndices[tex].Counter++;
            else
            {
                m_textureArrayIndices.Add(tex,
                    new MyTextureArrayIndex() { Index = (uint)m_textureArrayIndices.Count, Counter = 1 });
                m_textureArrayDirty = true;
            }
        }

        private static void CleanUpTextures()
        {
            // cleanup unused textures with incompatible format
            // leave the ones, that are not used
            var toRemove = new List<string>();
            foreach (var item in m_textureArrayIndices)
            {
                if (item.Value.Counter == 0)
                    toRemove.Add(item.Key);
            }
            foreach (var item in toRemove)
                RemoveTexture(item);
        }

        private static void RemoveTexture(string tex)
        {
            var arrayIndex = m_textureArrayIndices[tex];
            m_textureArrayIndices.Remove(tex);

            foreach (var item in m_textureArrayIndices)
                if (item.Value.Index > arrayIndex.Index)
                    m_textureArrayIndices[item.Key].Index--;
        }
        private static void MarkTextureUnused(string tex)
        {
            if (string.IsNullOrEmpty(tex))
                return;

            var arrayIndex = m_textureArrayIndices[tex];
            arrayIndex.Counter--;
        }
        private static string[] GetTextureArrayFileList()
        {
            var texts = new string[m_textureArrayIndices.Count];
            foreach (var item in m_textureArrayIndices)
                texts[item.Value.Index] = item.Key;
            return texts;
        }
        private static void UpdateTextureArray()
        {
            if (m_textureArrayDirty)
            {
                var arrayManager = MyManagers.FileArrayTextures;
                arrayManager.DisposeTex(ref m_textureArray);

                m_textureArray = null;

                var texts = GetTextureArrayFileList();
                if (texts.Length > 0)
                {
                    if (!arrayManager.CheckConsistency(texts))
                    {
                        CleanUpTextures();
                        texts = GetTextureArrayFileList();
                    }
                    if (texts.Length > 0)
                        m_textureArray = arrayManager.CreateFromFiles("gpuParticles", texts, MyFileTextureEnum.GPUPARTICLES, "", false);
                }

                m_textureArrayDirty = false;
            }
        }
        
        private static uint GenerateTextureIndex(MyLiveData emitter)
        {
            if (emitter.GPUEmitter.AtlasDimension.X >= MAX_ATLAS_DIMENSION)
                MyRenderProxy.Error("emitter.AtlasDimension.X < " + MAX_ATLAS_DIMENSION);
            if (emitter.GPUEmitter.AtlasDimension.Y >= MAX_ATLAS_DIMENSION)
                MyRenderProxy.Error("emitter.AtlasDimension.Y < " + MAX_ATLAS_DIMENSION);
            uint atlasOffset = (uint)emitter.GPUEmitter.AtlasFrameOffset;
            if (atlasOffset >= MAX_ATLAS_INDEX)
                MyRenderProxy.Error("atlasOffset < " + MAX_ATLAS_INDEX);
            if (emitter.GPUEmitter.AtlasFrameModulo >= ((1 << ATLAS_INDEX_BITS) - 1))
                MyRenderProxy.Error("emitter.AtlasFrameModulo < " + ((1 << ATLAS_INDEX_BITS) - 1));
            if ((emitter.GPUEmitter.AtlasFrameOffset + emitter.GPUEmitter.AtlasFrameModulo - 1) >= (emitter.GPUEmitter.AtlasDimension.X * emitter.GPUEmitter.AtlasDimension.Y))
                MyRenderProxy.Error("Emitter animation is out of bounds. (emitter.AtlasFrameOffset + emitter.AtlasFrameModulo - 1) < (emitter.AtlasDimension.X * emitter.AtlasDimension.Y)");
            return atlasOffset | ((uint)emitter.GPUEmitter.AtlasDimension.X << (ATLAS_INDEX_BITS + ATLAS_DIMENSION_BITS)) |
                ((uint)emitter.GPUEmitter.AtlasDimension.Y << (ATLAS_INDEX_BITS));
        }

        internal static int Gather(MyGPUEmitterData[] data, out ISrvBindable textureArraySrv)
        {
            for (int i = 0; i < MAX_LIVE_EMITTERS; i++)
            {
                data[i].NumParticlesToEmitThisFrame = 0;
            }

            // sort emitters!
            List<MyLiveData> emitters = m_emitters.Values.ToList();
            if (emitters.Count > MAX_LIVE_EMITTERS)
                emitters.Sort();

            int maxEmitterIndex = -1;
            int unassociatedCount = 0;
            int skipCount = 0;
            int unsortedCount = 0;
            int firstUnassociatedIndex = -1;
            int lastAssociatedIndex = -1;
            
            for (int i = 0; i < emitters.Count; i++)
            {
                var emitter = emitters[i];
                // assiociate buffer index to new emitters & track overload to free space for unassociated near emitters later
                if (emitter.BufferIndex == -1)
                {
                    if (m_freeBufferIndices.Count > 0)
                    {
                        emitter.BufferIndex = m_freeBufferIndices.Pop();
                    }
                    else
                    {
                        unassociatedCount++;
                        if (firstUnassociatedIndex == -1)
                            firstUnassociatedIndex = i;
                    }
                }
                else
                {
                    skipCount = unassociatedCount;
                    lastAssociatedIndex = i;
                    if (unassociatedCount > 0)
                        unsortedCount++;
                }

                if (MyCommon.TimerMs > emitter.DieAt)
                    emitter.GPUEmitter.Data.Flags |= GPUEmitterFlags.Dead;

                if (emitter.BufferIndex != -1)
                {
                    if ((emitter.GPUEmitter.Data.Flags & GPUEmitterFlags.FreezeEmit) == 0)
                    {
                        float toEmit = MyCommon.LastFrameDelta() * emitter.GPUEmitter.ParticlesPerSecond +
                            emitter.ParticlesEmittedFraction + emitter.GPUEmitter.ParticlesPerFrame;
                        emitter.GPUEmitter.Data.NumParticlesToEmitThisFrame = (int)toEmit;
                        emitter.ParticlesEmittedFraction = toEmit - emitter.GPUEmitter.Data.NumParticlesToEmitThisFrame;
                        emitter.GPUEmitter.ParticlesPerFrame = 0;
                    }

                    var textureIndex = m_textureArrayIndices.ContainsKey(emitter.GPUEmitter.AtlasTexture) ? 
                        m_textureArrayIndices[emitter.GPUEmitter.AtlasTexture].Index : 0;

                    int bufferIndex = emitter.BufferIndex;
                    data[bufferIndex] = emitter.GPUEmitter.Data;
                    Vector3 pos = emitter.GPUEmitter.WorldPosition - MyRender11.Environment.Matrices.CameraPosition;
                    data[bufferIndex].RotationMatrix.M14 = pos.X;
                    data[bufferIndex].RotationMatrix.M24 = pos.Y;
                    data[bufferIndex].RotationMatrix.M34 = pos.Z;
                    data[bufferIndex].PositionDelta = emitter.GPUEmitter.WorldPosition - emitter.LastWorldPosition;
                    data[bufferIndex].TextureIndex1 |= textureIndex << (ATLAS_INDEX_BITS + ATLAS_DIMENSION_BITS * 2);

                    if (bufferIndex > maxEmitterIndex)
                        maxEmitterIndex = bufferIndex;
                }
                emitter.LastWorldPosition = emitter.GPUEmitter.WorldPosition;
            }
            /*MyRenderStats.Generic.WriteFormat("GPU particles allocated: {0}", totalParticles, VRage.Stats.MyStatTypeEnum.CurrentValue, 300, 0);
            MyRenderStats.Generic.WriteFormat("GPU particles overload: {0}", overloadedParticles ? 1.0f : 0, VRage.Stats.MyStatTypeEnum.CurrentValue, 300, 0);
            MyRenderStats.Generic.WriteFormat("GPU emitters overload: {0}", overloadedEmitters ? 1.0f : 0, VRage.Stats.MyStatTypeEnum.CurrentValue, 300, 0);*/

            UpdateTextureArray();
            textureArraySrv = m_textureArray;

            // stop emitters far away to make room for emitters nearby
            if (skipCount > 0 && unsortedCount > 0)
            {
                // iterate until buffer-unassociated index is larger then unassocited one
                for (int i = firstUnassociatedIndex, j = lastAssociatedIndex; i < j; )
                {
                    var emitter = emitters[j];
                    // free last buffer-associated emitter
                    data[emitter.BufferIndex].Flags |= GPUEmitterFlags.Dead;
                    data[emitter.BufferIndex].NumParticlesToEmitThisFrame = 0;
                    m_freeBufferIndices.Push(emitter.BufferIndex);
                    emitter.BufferIndex = -1;

                    // find new last buffer-associated emitter
                    do
                    {
                        j--;
                    }
                    while (j > 0 && emitters[j].BufferIndex == -1);

                    // find next buffer-unassociated emitter
                    do
                    {
                        i++;
                    } while (i < emitters.Count && emitters[i].BufferIndex != -1);
                }
            }

            foreach (var emitter in emitters)
                if ((emitter.GPUEmitter.Data.Flags & GPUEmitterFlags.Dead) > 0)
                {
                    Remove(emitter);
                }

            return maxEmitterIndex + 1;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRageMath;
using System.Linq;

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
            public float ParticlesEmittedFraction;
            public int BufferIndex;
            public Resources.TexId TextureId;
            public MyGPUEmitter GPUEmitter;
            public float DieAt;

            public int CompareTo(object obj)
            {
                var c = obj as MyLiveData;
                var cDist = Vector3D.DistanceSquared(c.GPUEmitter.WorldPosition, MyRender11.Environment.CameraPosition);
                var dist = Vector3D.DistanceSquared(GPUEmitter.WorldPosition, MyRender11.Environment.CameraPosition);
                return dist < cDist ? -1 : 1;
            }
        }

        private static Stack<int> m_freeBufferIndices;

        private class MyTextureArrayIndex
        {
            public uint Index;
            public uint Counter;
        }
        private static Dictionary<Resources.TexId, MyTextureArrayIndex> m_textureArrayIndices = new Dictionary<Resources.TexId, MyTextureArrayIndex>();
        private static Resources.MyTextureArray m_textureArray;
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
            if (m_textureArray != null)
                m_textureArray.Dispose();
            m_textureArray = null;
            m_textureArrayDirty = false;
        }
        internal static void OnSessionEnd()
        {
            Init();
        }

        internal static void Create(uint GID)
        {
            m_emitters.Add(GID, new MyLiveData 
            {
                BufferIndex = -1,
                TextureId = Resources.TexId.NULL,
                DieAt = float.MaxValue,
                GPUEmitter = new MyGPUEmitter { GID = GID }
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
                    emitter.DieAt = MyCommon.TimerMs + emitter.GPUEmitter.Data.ParticleLifeSpan * 1000;
                }
            }
            else MyRenderProxy.Assert(check, "Invalid emitter id: " + GID);
        }
        internal static void UpdateData(MyGPUEmitter[] def)
        {
            for (int i = 0; i < def.Length; i++)
            {
                MyLiveData emitter;
                if (m_emitters.TryGetValue(def[i].GID, out emitter))
                {
                    var texId = Resources.MyTextures.GetTexture(def[i].AtlasTexture, Resources.MyTextureEnum.GUI, true);
                    if (emitter.TextureId != texId)
                    {
                        RemoveTexture(emitter.TextureId);
                        AddTexture(texId);
                    }
                    emitter.TextureId = texId;
                    MyRenderProxy.Assert(emitter.TextureId != Resources.TexId.NULL);

                    emitter.GPUEmitter = def[i];
                    emitter.GPUEmitter.Data.TextureIndex1 = GenerateTextureIndex(emitter);
                    emitter.GPUEmitter.Data.TextureIndex2 = (uint)def[i].AtlasFrameModulo;
                    emitter.GPUEmitter.Data.RotationMatrix = Matrix.Transpose(def[i].Data.RotationMatrix);
                }
                else MyRenderProxy.Assert(false, "invalid emitter id: " + def[i].GID);
            }
        }
        internal static void UpdateTransforms(uint[] GIDs, MatrixD[] transforms)
        {
            MyRenderProxy.Assert(GIDs.Length == transforms.Length);
            for (int i = 0; i < GIDs.Length; i++)
            {
                MyLiveData emitter;
                if (m_emitters.TryGetValue(GIDs[i], out emitter))
                {
                    emitter.GPUEmitter.WorldPosition = transforms[i].Translation;
                    emitter.GPUEmitter.Data.RotationMatrix = MatrixD.Transpose(transforms[i]);
                }
                else MyRenderProxy.Assert(false, "invalid emitter id: " + GIDs[i]);
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

            RemoveTexture(emitter.TextureId);
        }

        private static void AddTexture(Resources.TexId texId)
        {
            if (texId == Resources.TexId.NULL)
                return;

            if (m_textureArrayIndices.ContainsKey(texId))
                m_textureArrayIndices[texId].Counter++;
            else
            {
                m_textureArrayIndices.Add(texId,
                    new MyTextureArrayIndex() { Index = (uint)m_textureArrayIndices.Count, Counter = 1 });
                m_textureArrayDirty = true;
            }
        }
        private static void RemoveTexture(Resources.TexId texId)
        {
            if (texId == Resources.TexId.NULL)
                return;

            var arrayIndex = m_textureArrayIndices[texId];
            arrayIndex.Counter--;
            if (arrayIndex.Counter == 0)
            {
                m_textureArrayIndices.Remove(texId);

                foreach (var item in m_textureArrayIndices)
                    if (item.Value.Index > arrayIndex.Index)
                        m_textureArrayIndices[item.Key].Index--;

                m_textureArrayDirty = true;
            }
        }
        private static void UpdateTextureArray()
        {
            if (m_textureArrayDirty)
            {
                if (m_textureArray != null)
                    m_textureArray.Dispose();
                m_textureArray = null;

                Resources.TexId[] textIds = new Resources.TexId[m_textureArrayIndices.Count];
                foreach (var item in m_textureArrayIndices)
                    textIds[item.Value.Index] = item.Key;
                if (textIds.Length > 0)
                    m_textureArray = new Resources.MyTextureArray(textIds, "gpuParticles");

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

        internal static int Gather(MyGPUEmitterData[] data, out SharpDX.Direct3D11.ShaderResourceView textureArraySRV)
        {
            for (int i = 0; i < MAX_LIVE_EMITTERS; i++)
            {
                data[i].NumParticlesToEmitThisFrame = 0;
            }

            // sort emitters!
            List<MyLiveData> emitters = m_emitters.Values.ToList();
            //if (emitters.Count > MAX_LIVE_EMITTERS)
                emitters.Sort();

            int maxEmitterIndex = -1;
            uint textureIndex = 0;
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
                            emitter.ParticlesEmittedFraction;
                        emitter.GPUEmitter.Data.NumParticlesToEmitThisFrame = (int)toEmit;
                        emitter.ParticlesEmittedFraction = toEmit - emitter.GPUEmitter.Data.NumParticlesToEmitThisFrame;
                    }

                    if (emitter.TextureId != Resources.TexId.NULL)
                    {
                        MyRenderProxy.Assert(m_textureArrayIndices.ContainsKey(emitter.TextureId));
                        textureIndex = m_textureArrayIndices[emitter.TextureId].Index;
                    }
                    else textureIndex = 0;

                    int bufferIndex = emitter.BufferIndex;
                    data[bufferIndex] = emitter.GPUEmitter.Data;
                    Vector3 pos = emitter.GPUEmitter.WorldPosition - MyRender11.Environment.CameraPosition;
                    data[bufferIndex].RotationMatrix.M14 = pos.X;
                    data[bufferIndex].RotationMatrix.M24 = pos.Y;
                    data[bufferIndex].RotationMatrix.M34 = pos.Z;
                    data[bufferIndex].TextureIndex1 |= textureIndex << (ATLAS_INDEX_BITS + ATLAS_DIMENSION_BITS * 2);

                    if (bufferIndex > maxEmitterIndex)
                        maxEmitterIndex = bufferIndex;

                }
            }
            /*MyRenderStats.Generic.WriteFormat("GPU particles allocated: {0}", totalParticles, VRage.Stats.MyStatTypeEnum.CurrentValue, 300, 0);
            MyRenderStats.Generic.WriteFormat("GPU particles overload: {0}", overloadedParticles ? 1.0f : 0, VRage.Stats.MyStatTypeEnum.CurrentValue, 300, 0);
            MyRenderStats.Generic.WriteFormat("GPU emitters overload: {0}", overloadedEmitters ? 1.0f : 0, VRage.Stats.MyStatTypeEnum.CurrentValue, 300, 0);*/

            UpdateTextureArray();
            if (m_textureArray != null)
                textureArraySRV = m_textureArray.SRV;
            else textureArraySRV = null;

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

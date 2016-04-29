using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRageMath;
using System.Linq;

namespace VRageRender
{
    internal static class MyGPUEmitters
    {
        internal const int MAX_LIVE_EMITTERS = 32;
        internal const int MAX_PARTICLES = 400 * 1024;

        private const int ATLAS_INDEX_BITS = 12;
        private const int ATLAS_DIMENSION_BITS = 6;
        private const int ATLAS_TEXTURE_BITS = 8;
        private const int MAX_ATLAS_DIMENSION = 2 ^ ATLAS_DIMENSION_BITS;
        private const int MAX_ATLAS_INDEX = 2 ^ ATLAS_INDEX_BITS;

        private struct MyLiveData
        {
            public float ParticlesEmittedFraction;
            public int BufferIndex;
            public Resources.TexId TextureId;
            public MyGPUEmitter GPUEmitter;
            public float DieAt;
        }
        private static int m_totalParticles = 0;
        private static bool m_overloaded = false;
        private static Queue<int> m_freeBufferIndices;

        private class MyTextureArrayIndex
        {
            public uint Index;
            public uint Counter;
        }
        private static Dictionary<Resources.TexId, MyTextureArrayIndex> m_textureArrayIndices = new Dictionary<Resources.TexId, MyTextureArrayIndex>();
        private static Resources.MyTextureArray m_textureArray;
        private static bool m_textureArrayDirty;

        private static MyFreelist<MyLiveData> m_emitters = new MyFreelist<MyLiveData>(256);
        private static Dictionary<uint, GPUEmitterId> m_idIndex = new Dictionary<uint, GPUEmitterId>();

        struct GPUEmitterId
        {
            internal int Index;

            public static bool operator ==(GPUEmitterId x, GPUEmitterId y)
            {
                return x.Index == y.Index;
            }

            public static bool operator !=(GPUEmitterId x, GPUEmitterId y)
            {
                return x.Index != y.Index;
            }

            internal static readonly GPUEmitterId NULL = new GPUEmitterId { Index = -1 };

            #region Equals
            public class MyGPUEmitterIdComparerType : IEqualityComparer<GPUEmitterId>
            {
                public bool Equals(GPUEmitterId left, GPUEmitterId right)
                {
                    return left == right;
                }

                public int GetHashCode(GPUEmitterId gpuEmitterId)
                {
                    return gpuEmitterId.Index;
                }
            }
            public static MyGPUEmitterIdComparerType Comparer = new MyGPUEmitterIdComparerType();
            #endregion
        }

        internal static void Init()
        {
            m_emitters.Clear();
            m_idIndex.Clear();
            m_textureArrayIndices.Clear();
            m_freeBufferIndices = new Queue<int>();
            for (int i = 0; i < MAX_LIVE_EMITTERS; i++)
                m_freeBufferIndices.Enqueue(i);
            m_totalParticles = 0;
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
            var id = new GPUEmitterId { Index = m_emitters.Allocate() };
            
            MyRenderProxy.Assert(m_freeBufferIndices.Count > 0, "Too many live emitters!");

            m_emitters.Data[id.Index] = new MyLiveData 
            {
                BufferIndex = m_freeBufferIndices.Dequeue(),
                TextureId = Resources.TexId.NULL,
                DieAt = float.MaxValue
            };

            m_idIndex[GID] = id;
        }

        internal static void Remove(uint GID, bool instant = true)
        {
            var id = m_idIndex.Get(GID, GPUEmitterId.NULL);
            if (id != GPUEmitterId.NULL)
            {
                if (instant)
                    m_emitters.Data[id.Index].GPUEmitter.Data.Flags |= GPUEmitterFlags.Dead;
                else
                {
                    m_emitters.Data[id.Index].GPUEmitter.ParticlesPerSecond = 0;
                    m_emitters.Data[id.Index].DieAt = MyCommon.TimerMs + m_emitters.Data[id.Index].GPUEmitter.Data.ParticleLifeSpan * 1000;
                }
            }
        }

        private static void Remove(GPUEmitterId id)
        {
            var emitter = m_emitters.Data[id.Index];
            m_idIndex.Remove(emitter.GPUEmitter.GID);

            m_freeBufferIndices.Enqueue(emitter.BufferIndex);

            m_emitters.Free(id.Index);

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

                Resources.TexId[] textIds = new Resources.TexId[m_textureArrayIndices.Count];
                foreach (var item in m_textureArrayIndices)
                    textIds[item.Value.Index] = item.Key;
                m_textureArray = new Resources.MyTextureArray(textIds, "gpuParticles");

                m_textureArrayDirty = false;
            }
        }
        
        internal static void UpdateEmitters(MyGPUEmitter[] def)
        {
            m_overloaded = false;
            for (int i = 0; i < def.Length; i++)
            {
                var id = m_idIndex.Get(def[i].GID, GPUEmitterId.NULL);
                MyRenderProxy.Assert(id != GPUEmitterId.NULL);
                if (id != GPUEmitterId.NULL)
                {
                    m_totalParticles -= m_emitters.Data[id.Index].GPUEmitter.MaxParticles();

                    var texId = Resources.MyTextures.GetTexture(def[i].AtlasTexture, Resources.MyTextureEnum.GUI, true);
                    if (m_emitters.Data[id.Index].TextureId != texId)
                    {
                        RemoveTexture(m_emitters.Data[id.Index].TextureId);
                        AddTexture(texId);
                    }
                    m_emitters.Data[id.Index].TextureId = texId;
                    MyRenderProxy.Assert(m_emitters.Data[id.Index].TextureId != Resources.TexId.NULL);
                    def[i].Data.TextureIndex1 = GenerateTextureIndex(def[i]);
                    def[i].Data.TextureIndex2 = (uint)def[i].AtlasFrameModulo;
                    
                    m_emitters.Data[id.Index].GPUEmitter = def[i];

                    if ((m_totalParticles + def[i].MaxParticles()) > MAX_PARTICLES)
                    {
                        m_emitters.Data[id.Index].GPUEmitter.ParticlesPerSecond = 0;
                        m_overloaded = true;
                        MyRenderProxy.Assert(false, "GPU Particle system overloaded.");
                    }
                    m_totalParticles += m_emitters.Data[id.Index].GPUEmitter.MaxParticles();
                }
            }
        }
        internal static void UpdateEmittersPosition(uint[] GIDs, Vector3D[] worldPositions)
        {
            MyRenderProxy.Assert(GIDs.Length == worldPositions.Length);
            for (int i = 0; i < GIDs.Length; i++)
            {
                var id = m_idIndex.Get(GIDs[i], GPUEmitterId.NULL);
                m_emitters.Data[id.Index].GPUEmitter.WorldPosition = worldPositions[i];
            }
        }
        private static uint GenerateTextureIndex(MyGPUEmitter emitter)
        {
            MyRenderProxy.Assert(emitter.AtlasDimension.X < MAX_ATLAS_DIMENSION, "emitter.AtlasDimension.X < MAX_ATLAS_DIMENSION");
            MyRenderProxy.Assert(emitter.AtlasDimension.Y < MAX_ATLAS_DIMENSION, "emitter.AtlasDimension.Y < MAX_ATLAS_DIMENSION");
            uint atlasOffset = (uint)emitter.AtlasFrameOffset;
            MyRenderProxy.Assert(atlasOffset < MAX_ATLAS_INDEX, "atlasOffset < MAX_ATLAS_INDEX");
            MyRenderProxy.Assert(emitter.AtlasFrameModulo < ((1 << ATLAS_INDEX_BITS) - 1), "emitter.AtlasFrameModulo < ((1 << ATLAS_INDEX_BITS) - 1)");
            MyRenderProxy.Assert(
                (emitter.AtlasFrameOffset + emitter.AtlasFrameModulo - 1) < (emitter.AtlasDimension.X * emitter.AtlasDimension.Y), 
                "(emitter.AtlasFrameOffset + emitter.AtlasFrameModulo - 1) < (emitter.AtlasDimension.X * emitter.AtlasDimension.Y)");
            return atlasOffset | ((uint)emitter.AtlasDimension.X << (ATLAS_INDEX_BITS + ATLAS_DIMENSION_BITS)) |
                ((uint)emitter.AtlasDimension.Y << (ATLAS_INDEX_BITS));
        }
        internal static int Gather(MyGPUEmitterData[] data, out SharpDX.Direct3D11.ShaderResourceView textureArraySRV)
        {
            MyRenderStats.Generic.WriteFormat("GPU particles allocated: {0}", m_totalParticles, VRage.Stats.MyStatTypeEnum.CurrentValue, 300, 0);
            MyRenderStats.Generic.WriteFormat("GPU particles overload: {0}", m_overloaded ? 1.0f : 0, VRage.Stats.MyStatTypeEnum.CurrentValue, 300, 0);

            for (int i = 0; i < MAX_LIVE_EMITTERS; i++)
            {
                data[i].NumParticlesToEmitThisFrame = 0;
            }
            int maxEmitterIndex = -1;
            uint textureIndex = 0;
            foreach (var id in m_idIndex.Values)
            {
                if (MyCommon.TimerMs > m_emitters.Data[id.Index].DieAt)
                    m_emitters.Data[id.Index].GPUEmitter.Data.Flags |= GPUEmitterFlags.Dead;

                float toEmit = MyCommon.LastFrameDelta() * m_emitters.Data[id.Index].GPUEmitter.ParticlesPerSecond + 
                    m_emitters.Data[id.Index].ParticlesEmittedFraction;
                m_emitters.Data[id.Index].GPUEmitter.Data.NumParticlesToEmitThisFrame = (int)toEmit;
                m_emitters.Data[id.Index].ParticlesEmittedFraction = toEmit - m_emitters.Data[id.Index].GPUEmitter.Data.NumParticlesToEmitThisFrame;

                if (m_emitters.Data[id.Index].TextureId != Resources.TexId.NULL)
                {
                    MyRenderProxy.Assert(m_textureArrayIndices.ContainsKey(m_emitters.Data[id.Index].TextureId));
                    textureIndex = m_textureArrayIndices[m_emitters.Data[id.Index].TextureId].Index;
                }
                else textureIndex = 0;

                int bufferIndex = m_emitters.Data[id.Index].BufferIndex;
                data[bufferIndex] = m_emitters.Data[id.Index].GPUEmitter.Data;
                data[bufferIndex].Position = m_emitters.Data[id.Index].GPUEmitter.WorldPosition - MyEnvironment.CameraPosition;
                data[bufferIndex].TextureIndex1 |= textureIndex << (ATLAS_INDEX_BITS + ATLAS_DIMENSION_BITS * 2);

                if (bufferIndex > maxEmitterIndex)
                    maxEmitterIndex = bufferIndex;
            }
            
            UpdateTextureArray();
            if (m_textureArray != null)
                textureArraySRV = m_textureArray.SRV;
            else textureArraySRV = null;

            foreach (var id in m_idIndex.Values.ToArray())
                if ((m_emitters.Data[id.Index].GPUEmitter.Data.Flags & GPUEmitterFlags.Dead) > 0)
                {
                    m_totalParticles -= m_emitters.Data[id.Index].GPUEmitter.MaxParticles();
                    Remove(id);
                }
            return maxEmitterIndex + 1;
        }
    }
}

#region Using

using ParallelTasks;
using System;
using System.Collections.Generic;
using VRage.Generics;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

#endregion

namespace VRage.Game
{
    [VRage.Game.Components.MySessionComponentDescriptor(VRage.Game.Components.MyUpdateOrder.AfterSimulation)]
    public class MyParticlesManager : VRage.Game.Components.MySessionComponentBase
    {
        public static bool Enabled;
        private static bool m_paused = false;
        public static bool Paused
        {
            get { return m_paused; }
            set
            {
                if (m_paused != value)
                {
                    m_paused = value;
                    lock (m_particleEffectsForUpdate)
                    {
                        foreach (MyParticleEffect effect in m_particleEffectsForUpdate)
                            effect.SetDirty();
                    }
                }
            }
        }

        public static Func<Vector3D, Vector3> CalculateGravityInPoint;

        public static bool EnableCPUGenerations = true;


        #region Pools

        public static MyObjectsPool<MyParticleGeneration> GenerationsPool = new MyObjectsPool<MyParticleGeneration>(4096);
        public static MyObjectsPool<MyParticleGPUGeneration> GPUGenerationsPool = new MyObjectsPool<MyParticleGPUGeneration>(4096);
        public static MyObjectsPool<MyParticleLight> LightsPool = new MyObjectsPool<MyParticleLight>(32);
        public static MyObjectsPool<MyParticleSound> SoundsPool = new MyObjectsPool<MyParticleSound>(512);

        public static MyObjectsPool<MyParticleEffect> EffectsPool = new MyObjectsPool<MyParticleEffect>(2048);


        #endregion

        public static List<MyGPUEmitter> GPUEmitters = new List<MyGPUEmitter>();
        public static List<MyGPUEmitterLight> GPUEmittersLight = new List<MyGPUEmitterLight>();
        public static List<MyGPUEmitterTransformUpdate> GPUEmitterTransforms = new List<MyGPUEmitterTransformUpdate>();

        static List<MyParticleEffect> m_effectsToDelete = new List<MyParticleEffect>();
        static List<MyParticleEffect> m_particleEffectsForUpdate = new List<MyParticleEffect>();
        static List<MyParticleEffect> m_particleEffectsAll = new List<MyParticleEffect>();
        static List<VRageRender.MyBillboard> m_collectedBillboards = new List<VRageRender.MyBillboard>(16384);        

        public static List<MyParticleEffect> ParticleEffectsForUpdate
        {
            get { return m_particleEffectsForUpdate; }
        }

        static MyParticlesManager()
        {
            Enabled = true;
        }

        public static bool TryCreateParticleEffect(string effectName, out MyParticleEffect effect, bool userDraw = false)
        {
            int id;
            MyParticlesLibrary.GetParticleEffectsID(effectName, out id);

            return TryCreateParticleEffect(id, out effect, userDraw);
        }

        public static bool TryCreateParticleEffect(int id, out MyParticleEffect effect, bool userDraw = false)
        {
            if (id == -1 || !Enabled || !MyParticlesLibrary.EffectExists(id))
            {
                effect = null;
                return false;
            }

            effect = CreateParticleEffect(id, userDraw);
            return effect != null;
        }

        static MyParticleEffect CreateParticleEffect(int id, bool userDraw = false)
        {
            MyParticleEffect effect = MyParticlesLibrary.CreateParticleEffect(id);

            //Not in parallel
            userDraw = false;

            if (effect != null)
            {
                if (!userDraw)
                {
                    lock (m_particleEffectsForUpdate)
                    {
                        m_particleEffectsForUpdate.Add(effect);
                    }
                }

                effect.UserDraw = userDraw;

                m_particleEffectsAll.Add(effect);
            }

            return effect;
        }

        public static void RemoveParticleEffect(MyParticleEffect effect, bool fromBackground = false)
        {
            bool remove = true;

            if (!effect.UserDraw)
            {
                lock (m_particleEffectsForUpdate)
                {
                    remove = m_particleEffectsForUpdate.Contains(effect);
                    if (remove)
                        m_particleEffectsForUpdate.Remove(effect);
                }
            }

            m_particleEffectsAll.Remove(effect);
            MyParticlesLibrary.RemoveParticleEffectInstance(effect);
        }

        protected override void UnloadData()
        {
            foreach (MyParticleEffect effect in m_particleEffectsForUpdate)
            {
                m_effectsToDelete.Add(effect);
            }

            foreach (MyParticleEffect effect in m_effectsToDelete)
            {
                RemoveParticleEffect(effect);
            }
            m_effectsToDelete.Clear();

            System.Diagnostics.Debug.Assert(m_particleEffectsAll.Count == 0);
        }

        public override void LoadData()
        {
            base.LoadData();
            if (!Enabled)
                return;

            MyTransparentGeometry.LoadData();
        }


        public override void UpdateAfterSimulation()
        {
            if (!Enabled)
                return;

            UpdateEffects();

            //TestGPUParticles();
        }

        struct Statistics
        {
            public int GPUGens;
            public int CPUGens;
            public int Instances;
        }
        public static void LogEffects()
        {
            var stats = new Dictionary<string, Statistics>();
            foreach (MyParticleEffect effect in m_particleEffectsForUpdate)
            {
                Statistics value;
                if (stats.TryGetValue(effect.Name, out value))
                {
                    value.Instances++;
                    stats[effect.Name] = value;
                }
                else
                {
                    value = new Statistics();
                    foreach (var item in effect.GetGenerations())
                    {
                        if (item is MyParticleGPUGeneration)
                            value.GPUGens++;
                        else value.CPUGens++;
                    }
                    value.Instances = 1;
                    stats[effect.Name] = value;
                }
            }
            
            foreach (var item in stats)
                MyLog.Default.WriteLine(item.Key + ": #" + item.Value.Instances + " GPU " + item.Value.GPUGens + " CPU " + item.Value.CPUGens);
        }

        private static void UpdateEffects()
        {
            lock (m_particleEffectsForUpdate)
            {
                foreach (MyParticleEffect effect in m_particleEffectsForUpdate)
                {
                    if (effect.Update())
                        m_effectsToDelete.Add(effect);
                }
            }

            foreach (MyParticleEffect effect in m_effectsToDelete)
            {
                RemoveParticleEffect(effect, true);
            }
            m_effectsToDelete.Clear();
            //MyParticlesLibrary.Serialize("../../../../Content/Data/Particles.sbc");//re-export current particle library
        }

        public static void PrepareForDraw()
        {
            foreach (MyParticleEffect effect in m_particleEffectsForUpdate)
            {
                effect.PrepareForDraw();
            }
        }


        public static void DrawStart()
        {
            GPUEmitters.Clear();
            GPUEmittersLight.Clear();
            GPUEmitterTransforms.Clear();
        }

        public static void DrawEnd()
        {
            ProfilerShort.Begin("GPU_SendData");
            if (GPUEmitters.Count > 0)
            {
                MyRenderProxy.UpdateGPUEmitters(GPUEmitters.ToArray());

                GPUEmitters.Clear();
            }
            ProfilerShort.End();

            ProfilerShort.Begin("GPU_SendPositions");
            if (GPUEmitterTransforms.Count > 0)
            {
                MyRenderProxy.UpdateGPUEmittersTransform(GPUEmitterTransforms.ToArray());

                GPUEmitterTransforms.Clear();
            }
            ProfilerShort.End();

            ProfilerShort.Begin("GPU_SendLightData");
            if (GPUEmittersLight.Count > 0)
            {
                MyRenderProxy.UpdateGPUEmittersLight(GPUEmittersLight.ToArray());

                GPUEmittersLight.Clear();
            }
            ProfilerShort.End();
        }

        public override void Draw()
        {
            if (!Enabled)
                return;

            m_collectedBillboards.Clear();

            DrawStart();

            lock (m_particleEffectsForUpdate)
            {
                foreach (MyParticleEffect effect in m_particleEffectsForUpdate)
                {
                    effect.PrepareForDraw();
                    effect.Draw(m_collectedBillboards);
                }
            }

            DrawEnd();

            VRageRender.MyRenderProxy.AddBillboards(m_collectedBillboards);
        }
    }
}

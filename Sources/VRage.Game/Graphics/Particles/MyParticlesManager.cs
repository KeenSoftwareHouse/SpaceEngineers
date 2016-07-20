#region Using

using ParallelTasks;
using System;
using System.Collections.Generic;
using VRage.Generics;
using VRageMath;
using VRageRender;

#endregion

namespace VRage.Game
{
    [VRage.Game.Components.MySessionComponentDescriptor(VRage.Game.Components.MyUpdateOrder.AfterSimulation)]
    public class MyParticlesManager : VRage.Game.Components.MySessionComponentBase
    {
        public static bool Enabled;

        public static Func<Vector3D, Vector3> CalculateGravityInPoint;


        #region Pools

        public static MyObjectsPool<MyParticleGeneration> GenerationsPool = new MyObjectsPool<MyParticleGeneration>(4096);
        public static MyObjectsPool<MyParticleGPUGeneration> GPUGenerationsPool = new MyObjectsPool<MyParticleGPUGeneration>(4096);
        public static MyObjectsPool<MyParticleLight> LightsPool = new MyObjectsPool<MyParticleLight>(32);
        public static MyObjectsPool<MyParticleSound> SoundsPool = new MyObjectsPool<MyParticleSound>(512);

        public static MyObjectsPool<MyParticleEffect> EffectsPool = new MyObjectsPool<MyParticleEffect>(2048);


        #endregion

        public static List<MyGPUEmitter> GPUEmitters = new List<MyGPUEmitter>();
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

            MyPerformanceCounter.PerCameraDrawWrite.ParticleEffectsTotal = 0;
            MyPerformanceCounter.PerCameraDrawWrite.ParticleEffectsDrawn = 0;

            UpdateEffects();

            //TestGPUParticles();
        }




        private static void UpdateEffects()
        {
            lock (m_particleEffectsForUpdate)
            {
                foreach (MyParticleEffect effect in m_particleEffectsForUpdate)
                {
                    MyPerformanceCounter.PerCameraDrawWrite.ParticleEffectsTotal++;

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
            GPUEmitterTransforms.Clear();
        }

        public static void DrawEnd()
        {
            if (GPUEmitters.Count > 0)
            {
                var emitters = new MyGPUEmitter[GPUEmitters.Count];
                for (int i = 0; i < GPUEmitters.Count; i++)
                {
                    emitters[i] = GPUEmitters[i];
                }

                MyRenderProxy.UpdateGPUEmitters(emitters);

                GPUEmitters.Clear();
            }

            if (GPUEmitterTransforms.Count > 0)
            {
                var emitterUids = new uint[GPUEmitterTransforms.Count];
                var emitterTransforms = new MatrixD[GPUEmitterTransforms.Count];
                for (int i = 0; i < GPUEmitterTransforms.Count; i++)
                {
                    emitterUids[i] = GPUEmitterTransforms[i].GID;
                    emitterTransforms[i] = GPUEmitterTransforms[i].Transform;
                }

                MyRenderProxy.UpdateGPUEmittersTransform(emitterUids, emitterTransforms);

                GPUEmitterTransforms.Clear();
            }
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

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
    [VRage.Game.Components.MySessionComponentDescriptor(VRage.Game.Components.MyUpdateOrder.BeforeSimulation)]
    public class MyParticlesManager : VRage.Game.Components.MySessionComponentBase
    {
        public static bool Enabled;

        public static float BirthMultiplierOverall = 1.0f;

        public static int ParticlesTotal;//for display in MyGuiScreenDebugTiming

        public static Func<Vector3D, Vector3> CalculateGravityInPoint;


        #region Pools

        public static MyObjectsPool<MyParticleGeneration> GenerationsPool = new MyObjectsPool<MyParticleGeneration>(4096);
        public static MyObjectsPool<MyParticleLight> LightsPool = new MyObjectsPool<MyParticleLight>(32);

        public static MyObjectsPool<MyParticleEffect> EffectsPool = new MyObjectsPool<MyParticleEffect>(2048);


        #endregion

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
                else
                {
                    effect.AutoDelete = false;
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


        public override void UpdateBeforeSimulation()
        {
            if (!Enabled)
                return;

            MyPerformanceCounter.PerCameraDrawWrite.ParticleEffectsTotal = 0;
            MyPerformanceCounter.PerCameraDrawWrite.ParticleEffectsDrawn = 0;

            UpdateEffects();
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
        }

        public static void PrepareForDraw()
        {
            m_effectsForCustomDraw.Clear();

            PrepareEffectsForDraw();
        }

        private static void PrepareEffectsForDraw()
        {
            foreach (MyParticleEffect effect in m_particleEffectsForUpdate)
            {
                effect.PrepareForDraw();
            }
        }

        public override void Draw()
        {
            if (!Enabled)
                return;

            m_collectedBillboards.Clear();
            ParticlesTotal = 0;

            lock (m_particleEffectsForUpdate)
            {
                foreach (MyParticleEffect effect in m_particleEffectsForUpdate)
                {
                    effect.PrepareForDraw();
                    effect.Draw(m_collectedBillboards);
                }
            }

            foreach (MyParticleEffect effect in m_effectsForCustomDraw)
            {
                effect.Update();
                effect.PrepareForDraw();
                effect.Draw(m_collectedBillboards);
            }

            m_effectsForCustomDraw.Clear();

            VRageRender.MyRenderProxy.AddBillboards(m_collectedBillboards);
        }


        static List<MyParticleEffect> m_effectsForCustomDraw = new List<MyParticleEffect>();

        public static void CustomDraw(MyParticleEffect effect)
        {
            System.Diagnostics.Debug.Assert(effect != null);

            m_effectsForCustomDraw.Add(effect);
        }
    }
}

#region Using

using ParallelTasks;
using Sandbox.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Generics;
using VRageRender;

#endregion

namespace Sandbox.Graphics.TransparentGeometry.Particles
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MyParticlesManager : MySessionComponentBase
    {
        public static bool Enabled;
        //public static event EventHandler OnDraw = null;

        public static float BirthMultiplierOverall = 1.0f;

        static List<MyParticleEffect> m_effectsToDelete = new List<MyParticleEffect>();

        /// <summary>
        /// Event that occures when we want update particles
        /// </summary>
        //private static readonly AutoResetEvent m_updateParticlesEvent;
        //private static volatile bool m_updateCompleted = true;
        private static bool MultithreadedPrepareForDraw = true;
        static Task m_prepareForDrawTask;


        #region Pools

        public static MyObjectsPool<MyParticleGeneration> GenerationsPool = new MyObjectsPool<MyParticleGeneration>(4096);
        //TODO
        //public static MyObjectsPool<MyParticleEffect> EffectsPool = new MyObjectsPool<MyParticleEffect>(MyFakes.LOW_PARTICLE_EFFECTS_POOL ? 128 : 2048);
        public static MyObjectsPool<MyParticleEffect> EffectsPool = new MyObjectsPool<MyParticleEffect>(2048);


        #endregion

        static List<MyParticleEffect> m_particleEffectsForUpdate = new List<MyParticleEffect>();
        static List<MyParticleEffect> m_particleEffectsAll = new List<MyParticleEffect>();

        public static List<MyParticleEffect> ParticleEffectsForUpdate
        {
            get { return m_particleEffectsForUpdate; }
        }

        /// <summary>
        /// Event that occures when we want update particles
        /// </summary>
        //private static readonly AutoResetEvent m_prepareForDrawEvent;
        //private static volatile bool m_prepareForDrawCompleted = true;
        private static bool MultithreadedUpdater = false;
        static Task m_updaterTask;

        static ActionWork m_updateEffectsWork = new ActionWork(UpdateEffects);
        static ActionWork m_prepareEffectsWork = new ActionWork(PrepareEffectsForDraw);

        static MyParticlesManager()
        {
            Enabled = true;

            if (VRage.MyCompilationSymbols.PerformanceProfiling)
                MultithreadedUpdater = false;

            if (MultithreadedUpdater)
            {
                //m_updateParticlesEvent = new AutoResetEvent(false);
                //Task.Factory.StartNew(BackgroundUpdater, TaskCreationOptions.PreferFairness);
            }

            // TODO: Par 
            //MyRender.RegisterRenderModule(MyRenderModuleEnum.AnimatedParticlesPrepare, "Animated particles prepare", PrepareForDraw, MyRenderStage.PrepareForDraw, 0, true);

            if (VRage.MyCompilationSymbols.PerformanceProfiling)
                MultithreadedPrepareForDraw = false;


            if (MultithreadedPrepareForDraw)
            {
                //m_prepareForDrawEvent = new AutoResetEvent(false);
                //Task.Factory.StartNew(PrepareForDrawBackground, TaskCreationOptions.PreferFairness);
            }
        }


        public static bool TryCreateParticleEffect(int id, out MyParticleEffect effect, bool userDraw = false)
        {
            if (!MyParticlesLibrary.EffectExists(id))
            {
                effect = null;
                return false;
            }

            effect = CreateParticleEffect(id, userDraw);
            return effect != null;
        }

        static MyParticleEffect CreateParticleEffect(int id, bool userDraw = false)
        {
            //Because we can call Update() more times per frame
            WaitUntilUpdateCompleted();

            MyParticleEffect effect = MyParticlesLibrary.CreateParticleEffect(id);

            // This could more likely be caused by empty generation pool (which is allowed) then error in xml
            //System.Diagnostics.Debug.Assert(effect.GetGenerations().Count > 0);


            //Not in parallel
            userDraw = false;

            if (effect != null)
            {
                System.Diagnostics.Debug.Assert(m_updaterTask.IsComplete == true);

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
            //System.Diagnostics.Debug.Assert(m_updateCompleted == true);

            //Because XNA can call Update() more times per frame
            if (!fromBackground)
                WaitUntilUpdateCompleted();

            bool remove = true;

            if (!effect.UserDraw /*&& effect.Enabled*/)
            {
                lock (m_particleEffectsForUpdate)
                {
                    //System.Diagnostics.Debug.Assert(m_particleEffectsForUpdate.Contains(effect));
                    remove = m_particleEffectsForUpdate.Contains(effect);
                    if (remove)
                        m_particleEffectsForUpdate.Remove(effect);
                }
            }

            m_particleEffectsAll.Remove(effect);
            //if (remove)
            MyParticlesLibrary.RemoveParticleEffectInstance(effect);
        }

        protected override void UnloadData()
        {
            System.Diagnostics.Debug.Assert(m_updaterTask.IsComplete == true);

            WaitUntilUpdateCompleted();

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

            MyTransparentGeometry.LoadData();
        }


        public override void UpdateBeforeSimulation()
        {
            //if (MySandboxGame.IsPaused)
            //  return;

            MyPerformanceCounter.PerCameraDrawWrite.ParticleEffectsTotal = 0;
            MyPerformanceCounter.PerCameraDrawWrite.ParticleEffectsDrawn = 0;

            if (MultithreadedUpdater)
            {
                WaitUntilUpdateCompleted();

                m_updaterTask = Parallel.Start(m_updateEffectsWork, null);
            }
            else
            {
                UpdateEffects();
            }
        }

        private static void UpdateEffects()
        {
            foreach (MyParticleEffect effect in m_particleEffectsForUpdate)
            {
                MyPerformanceCounter.PerCameraDrawWrite.ParticleEffectsTotal++;

                if (effect.Update())
                    m_effectsToDelete.Add(effect);
            }

            foreach (MyParticleEffect effect in m_effectsToDelete)
            {
                RemoveParticleEffect(effect, true);
            }
            m_effectsToDelete.Clear();
        }

        public static void WaitUntilUpdateCompleted()
        {
            m_updaterTask.Wait();
        }

        public static void PrepareForDraw()
        {
            m_effectsForCustomDraw.Clear();

            if (MultithreadedPrepareForDraw)
            {
                m_prepareForDrawTask = Parallel.Start(m_prepareEffectsWork);

                //m_prepareForDrawCompleted = false;
                //m_prepareForDrawEvent.Set();
            }
            else
            {
                PrepareEffectsForDraw();
            }
        }

        private static void PrepareEffectsForDraw()
        {
            WaitUntilUpdateCompleted();

            foreach (MyParticleEffect effect in m_particleEffectsForUpdate)
            {
                effect.PrepareForDraw();
            }
        }

        private static void WaitUntilPrepareForDrawCompleted()
        {
            m_prepareForDrawTask.Wait();
        }


        static List<VRageRender.MyBillboard> m_collectedBillboards = new List<VRageRender.MyBillboard>(16384);
        public static int m_ParticlesTotal;//for display in MyGuiScreenDebugTiming

        public override void Draw()
        {
            WaitUntilPrepareForDrawCompleted();

            m_collectedBillboards.Clear();
            m_ParticlesTotal = 0;

            lock (m_particleEffectsForUpdate)
            {
                foreach (MyParticleEffect effect in m_particleEffectsForUpdate)
                {
                    effect.PrepareForDraw();
                    effect.Draw(m_collectedBillboards);
                }
            }

            //if (!MySandboxGame.IsPaused)
            {
                foreach (MyParticleEffect effect in m_effectsForCustomDraw)
                {
                    effect.Update();
                    effect.PrepareForDraw();
                    effect.Draw(m_collectedBillboards);
                }

                m_effectsForCustomDraw.Clear();
            }

            VRageRender.MyRenderProxy.AddBillboards(m_collectedBillboards);


            //if (OnDraw != null)
            //  OnDraw(null, null);

            System.Diagnostics.Debug.Assert(m_prepareForDrawTask.IsComplete);
        }


        static List<MyParticleEffect> m_effectsForCustomDraw = new List<MyParticleEffect>();

        public static void CustomDraw(MyParticleEffect effect)
        {
            System.Diagnostics.Debug.Assert(effect != null);

            m_effectsForCustomDraw.Add(effect);
        }
    }
}

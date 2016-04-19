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

        public static Func<Vector3D, Vector3> CalculateGravityInPoint;


        #region Pools

        public static MyObjectsPool<MyParticleGeneration> GenerationsPool = new MyObjectsPool<MyParticleGeneration>(4096);
        public static MyObjectsPool<MyParticleGPUGeneration> GPUGenerationsPool = new MyObjectsPool<MyParticleGPUGeneration>(4096);
        public static MyObjectsPool<MyParticleLight> LightsPool = new MyObjectsPool<MyParticleLight>(32);
        public static MyObjectsPool<MyParticleSound> SoundsPool = new MyObjectsPool<MyParticleSound>(512);

        public static MyObjectsPool<MyParticleEffect> EffectsPool = new MyObjectsPool<MyParticleEffect>(2048);


        #endregion

        public static List<MyGPUEmitter> GPUEmitters = new List<MyGPUEmitter>();
        public static List<MyGPUEmitterPositionUpdate> GPUEmitterPositions = new List<MyGPUEmitterPositionUpdate>();

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

            //TestGPUParticles();
        }


        public static void UpdateStart()
        {
            GPUEmitters.Clear();
            GPUEmitterPositions.Clear();
        }

        public static void UpdateEnd()
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

            if (GPUEmitterPositions.Count > 0)
            {
                var emitterUids = new uint[GPUEmitterPositions.Count];
                var emitterPos = new Vector3D[GPUEmitterPositions.Count];
                for (int i = 0; i < GPUEmitterPositions.Count; i++)
                {
                    emitterUids[i] = GPUEmitterPositions[i].GID;
                    emitterPos[i] = GPUEmitterPositions[i].WorldPosition;
                }

                MyRenderProxy.UpdateGPUEmittersPosition(emitterUids, emitterPos);

                GPUEmitterPositions.Clear();
            }
        }

        private static void UpdateEffects()
        {
            lock (m_particleEffectsForUpdate)
            {
                UpdateStart();                

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

            //TestGPUParticles();
        }


        static List<MyParticleEffect> m_effectsForCustomDraw = new List<MyParticleEffect>();

        public static void CustomDraw(MyParticleEffect effect)
        {
            System.Diagnostics.Debug.Assert(effect != null);

            m_effectsForCustomDraw.Add(effect);
        }

        static uint gid0, gid1;
        public static void TestGPUParticles()
        {
            if (VRage.Input.MyInput.Static.IsAnyShiftKeyPressed())
            {
                if (gid0 == 0)
                {
                    gid0 = MyRenderProxy.CreateGPUEmitter();
                    gid1 = MyRenderProxy.CreateGPUEmitter();
                }
                var Position = MyTransparentGeometry.Camera.Translation;
                var emitters = new MyGPUEmitter[2];
                // sparks
                emitters[0].GID = gid0;
                emitters[0].ParticlesPerSecond = 30000.0f;
                emitters[0].Data.Color0 = new Vector4(1.0f, 1.0f, 0.0f, 1f);
                emitters[0].Data.Color1 = new Vector4(1.0f, 0.0f, 0.0f, 1f);
                emitters[0].Data.Color2 = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
                emitters[0].Data.ColorKey1 = 0.5f;
                emitters[0].Data.ColorKey2 = 1.0f;
                emitters[0].Data.AlphaKey1 = 0.95f;
                emitters[0].Data.AlphaKey2 = 1.0f;
                emitters[0].Data.Bounciness = 0.4f;
                emitters[0].Data.Velocity = new Vector3(0.0f, 0.0f, -1.0f);
                emitters[0].Data.NumParticlesToEmitThisFrame = 0;
                emitters[0].Data.ParticleLifeSpan = 9.0f;
                emitters[0].Data.Size0 = 0.02f;
                emitters[0].Data.Size1 = 0.016f;
                emitters[0].Data.Size2 = 0.0f;
                emitters[0].Data.SizeKeys1 = 0.95f;
                emitters[0].Data.SizeKeys2 = 1.0f;
                emitters[0].Data.PositionVariance = new Vector3(0.1f, 0.1f, 0.1f);
                emitters[0].Data.VelocityVariance = 0.4f;
                emitters[0].Data.RotationVelocity = 0;
                emitters[0].Data.Acceleration = new Vector3(0, 0, -0.98f);
                emitters[0].Data.StreakMultiplier = 4.0f;
                emitters[0].Data.Flags = GPUEmitterFlags.Streaks | GPUEmitterFlags.Collide | GPUEmitterFlags.SleepState;
                emitters[0].Data.SoftParticleDistanceScale = 5;
                emitters[0].Data.AnimationFrameTime = 1.0f;
                emitters[0].Data.OITWeightFactor = 0.3f;
                emitters[0].AtlasTexture = "Textures\\Particles\\gpuAtlas0.dds";
                emitters[0].AtlasDimension = new Vector2I(2, 1);
                emitters[0].AtlasFrameOffset = 1;
                emitters[0].AtlasFrameModulo = 1;
                emitters[0].WorldPosition = Position + new Vector3D(8.0f, 0.0f, 8.0f);

                // smoke
                emitters[1].GID = gid1;
                emitters[1].ParticlesPerSecond = 100.0f;
                emitters[1].Data.Color0 = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
                emitters[1].Data.Color1 = new Vector4(0.6f, 0.6f, 0.65f, 1.0f);
                emitters[1].Data.Color2 = new Vector4(0.6f, 0.6f, 0.65f, 0.0f);
                emitters[1].Data.ColorKey1 = 0.5f;
                emitters[1].Data.ColorKey2 = 1.0f;
                emitters[1].Data.AlphaKey1 = 0.95f;
                emitters[1].Data.AlphaKey2 = 1.0f;
                emitters[1].Data.Velocity = new Vector3(0, 0, 1.0f);
                emitters[1].Data.NumParticlesToEmitThisFrame = 0;
                emitters[1].Data.ParticleLifeSpan = 150.0f;
                emitters[1].Data.Size0 = 0.50f;
                emitters[1].Data.Size1 = 2.2f;
                emitters[1].Data.SizeKeys1 = 1.0f;
                emitters[1].Data.SizeKeys2 = 1.0f;
                emitters[1].Data.PositionVariance = new Vector3(0.2f, 0.2f, 0.2f);
                emitters[1].Data.VelocityVariance = 0.3f;
                emitters[1].Data.RotationVelocity = 100.0f;
                emitters[1].Data.Acceleration = new Vector3(0.00707f, 0.00707f, -0.00003f * 9.8f);
                emitters[1].Data.Flags = GPUEmitterFlags.Light | GPUEmitterFlags.VolumetricLight;
                emitters[1].Data.SoftParticleDistanceScale = 2;
                emitters[1].Data.AnimationFrameTime = 1.0f;
                emitters[1].Data.OITWeightFactor = 1.0f;
                emitters[1].AtlasTexture = "Textures\\Particles\\gpuAtlas1.dds";
                emitters[1].AtlasDimension = new Vector2I(2, 1);
                emitters[1].AtlasFrameOffset = 0;
                emitters[1].AtlasFrameModulo = 1;
                emitters[1].WorldPosition = Position + new Vector3D(2.0f, 0.0f, 1.0f);
                MyRenderProxy.UpdateGPUEmitters(emitters);
            }
        }
    }
}

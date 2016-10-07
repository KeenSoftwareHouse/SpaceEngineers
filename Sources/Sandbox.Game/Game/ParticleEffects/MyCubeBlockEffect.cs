using Sandbox.Definitions;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using VRageRender.Import;

namespace Sandbox.Game.ParticleEffects
{
    //Effect controller
    public class MyCubeBlockEffect
    {
        public readonly int EffectId;
        private CubeBlockEffectBase m_effectDefinition;
        public bool CanBeDeleted = false;

        private List<MyCubeBlockParticleEffect> m_particleEffects;
        private MyEntity m_entity;

        public MyCubeBlockEffect(int EffectId, CubeBlockEffectBase effectDefinition, MyEntity block)
        {
            this.EffectId = EffectId;
            this.m_entity = block;
            this.m_effectDefinition = effectDefinition;
            this.m_particleEffects = new List<MyCubeBlockParticleEffect>();
            if (m_effectDefinition.ParticleEffects != null)
            {
                for (int i = 0; i < m_effectDefinition.ParticleEffects.Length; i++)
                {
                    this.m_particleEffects.Add(new MyCubeBlockParticleEffect(m_effectDefinition.ParticleEffects[i], m_entity));
                }
            }
        }

        public void Stop()
        {
            for (int i = 0; i < m_particleEffects.Count; i++)
            {
                m_particleEffects[i].Stop();
            }
            m_particleEffects.Clear();
        }

        public void Update()
        {
            int i;
            for (i = 0; i < m_particleEffects.Count; i++)
            {
                if (m_particleEffects[i].CanBeDeleted)
                {
                    m_particleEffects[i].Stop();
                    m_particleEffects.RemoveAt(i);
                    i--;
                }
                else
                {
                    m_particleEffects[i].Update();
                }
            }
        }
    }


    //particle effect controller
    class MyCubeBlockParticleEffect
    {
        private int m_particleId = -1;
        public int ParticleId { get { return m_particleId; } }

        private bool m_canBeDeleted = false;
        public bool CanBeDeleted { get { return m_canBeDeleted; } }

        private MyParticleEffect m_effect = null;
        public bool EffectIsRunning { get { return m_effect != null; } }

        private bool m_loop = false;
        private bool m_playedOnce = false;
        private bool m_playing = false;
        private float m_delay = 0;
        private float m_timer = 0;
        private float m_spawnTimeMin = 0;
        private float m_spawnTimeMax = 0;
        private float m_duration = 0f;
        private MyModelDummy m_originPoint;
        private MyEntity m_entity;

        public MyCubeBlockParticleEffect(CubeBlockEffect effectData, MyEntity entity)
        {
            MyParticlesLibrary.GetParticleEffectsID(effectData.Name, out m_particleId);
            if (m_particleId == -1)
                m_canBeDeleted = true;
            else
            {
                m_loop = effectData.Loop;
                m_delay = effectData.Delay;
                m_spawnTimeMin = Math.Max(0f, effectData.SpawnTimeMin);
                m_spawnTimeMax = Math.Max(m_spawnTimeMin, effectData.SpawnTimeMax);
                m_timer = m_delay;
                m_entity = entity;
                m_originPoint = GetEffectOrigin(effectData.Origin);
                m_duration = effectData.Duration;
                if (m_spawnTimeMax > 0f)
                    m_timer += MyUtils.GetRandomFloat(m_spawnTimeMin, m_spawnTimeMax);
            }
        }

        private MyModelDummy GetEffectOrigin(string origin)
        {
            if (m_entity.Model.Dummies.ContainsKey(origin))
                return m_entity.Model.Dummies[origin];
            return null;
        }

        public void Stop()
        {
            if (m_effect != null)
            {
                m_effect.Stop();
                m_effect = null;
            }
        }

        public void Update ()
        {
            if(m_particleId < 0)
                return;
            if (m_effect == null || m_effect.IsStopped)
            {
                if (m_playedOnce && m_loop == false)
                {
                    m_canBeDeleted = true;
                }
                else
                {
                    if (m_timer > 0f)
                    {
                        m_timer -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                    }
                    else
                    {
                        m_playedOnce = true;
                        m_canBeDeleted = !MyParticlesManager.TryCreateParticleEffect(m_particleId, out m_effect);
                        if (m_effect != null)
                        {
                            m_effect.WorldMatrix = m_entity.WorldMatrix;
                        }
                        if (m_spawnTimeMax > 0f)
                            m_timer = MyUtils.GetRandomFloat(m_spawnTimeMin, m_spawnTimeMax);
                        else
                            m_timer = 0;
                    }
                }
            }
            else
            {
                if (m_effect != null)
                {
                    float time = m_effect.GetElapsedTime();
                    if (m_duration > 0f && time >= m_duration)
                    {
                        m_effect.Stop();
                    }
                    else
                    {
                        if (m_originPoint != null)
                            m_effect.WorldMatrix = MatrixD.Multiply(MatrixD.Normalize(m_originPoint.Matrix), m_entity.WorldMatrix);
                        else
                            m_effect.WorldMatrix = m_entity.WorldMatrix;
                    }
                }
            }
        }
    }
}

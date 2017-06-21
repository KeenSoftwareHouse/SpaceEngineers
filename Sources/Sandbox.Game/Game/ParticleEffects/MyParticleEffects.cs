using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Game.Entities;
using VRage.Utils;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Game.Weapons;

using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Game.Entity;
using VRage.Game;
using VRage;

namespace Sandbox.Game
{
    public static class MyParticleEffects
    {
        public static void GenerateMuzzleFlash(Vector3D position, Vector3 dir, float radius, float length)
        {
            GenerateMuzzleFlash(position, dir, -1, ref MatrixD.Zero, radius, length);
        }

        public static void GenerateMuzzleFlash(Vector3D position, Vector3 dir, int renderObjectID, ref MatrixD worldToLocal, float radius, float length)
        {
            float angle = MyParticlesManager.Paused ? 0 : MyUtils.GetRandomFloat(0, MathHelper.PiOver2);

            float colorComponent = 1.3f;
            Vector4 color = new Vector4(colorComponent, colorComponent, colorComponent, 1);

            MyTransparentGeometry.AddLineBillboard("MuzzleFlashMachineGunSide", color, position, renderObjectID, ref worldToLocal,
                dir, length, 0.15f, VRageRender.MyBillboard.BlenType.Standard);
            MyTransparentGeometry.AddPointBillboard("MuzzleFlashMachineGunFront", color, position, renderObjectID, ref worldToLocal, radius, angle);
        }

        private class EffectSoundEmitter
        {
            public readonly uint ParticleSoundId;
            public bool Updated;
            public MyEntity3DSoundEmitter Emitter;
            public MySoundPair SoundPair;
            public float OriginalVolume;

            public EffectSoundEmitter(uint id, Vector3 position, MySoundPair sound)
            {
                ParticleSoundId = id;
                Updated = true;
                MyEntity entity = null;
                if (MyFakes.ENABLE_NEW_SOUNDS && MySession.Static.Settings.RealisticSound)//snap emitter to closest block - used for realistic sounds
                {
                    List<MyEntity> m_detectedObjects = new List<MyEntity>();
                    BoundingSphereD effectSphere = new BoundingSphereD(MySession.Static.LocalCharacter != null ? MySession.Static.LocalCharacter.PositionComp.GetPosition() : MySector.MainCamera.Position, 2f);
                    MyGamePruningStructure.GetAllEntitiesInSphere(ref effectSphere, m_detectedObjects);
                    float distBest = float.MaxValue;
                    float dist;
                    for (int i = 0; i < m_detectedObjects.Count; i++)
                    {
                        MyCubeBlock block = m_detectedObjects[i] as MyCubeBlock;
                        if (block != null)
                        {
                            dist = Vector3.DistanceSquared(MySession.Static.LocalCharacter.PositionComp.GetPosition(), block.PositionComp.GetPosition());
                            if (dist < distBest)
                            {
                                dist = distBest;
                                entity = block;
                            }
                        }
                    }
                    m_detectedObjects.Clear();
                }
                Emitter = new MyEntity3DSoundEmitter(entity);
                Emitter.SetPosition(position);
                if (sound == null)
                    sound = MySoundPair.Empty;
                Emitter.PlaySound(sound);
                if (Emitter.Sound != null)
                    OriginalVolume = Emitter.Sound.Volume;
                else
                    OriginalVolume = 1f;
                Emitter.Update();
                SoundPair = sound;
            }
        }
        private static List<EffectSoundEmitter> m_soundEmitters = new List<EffectSoundEmitter>();
        private static short UpdateCount = 0;
        public static void UpdateEffects()
        {
            int i, j;
            bool newSound;
            UpdateCount++;

            //effect sounds
            for (i = 0; i < m_soundEmitters.Count; i++)
            {
                m_soundEmitters[i].Updated = false;//not updated yet
            }
            foreach (var soundEffect in MyParticlesManager.SoundsPool.Active)
            {
                newSound = true;
                for (i = 0; i < m_soundEmitters.Count; i++)
                {
                    if (m_soundEmitters[i].ParticleSoundId == soundEffect.ParticleSoundId)
                    {
                        m_soundEmitters[i].Updated = true;
                        m_soundEmitters[i].Emitter.CustomVolume = m_soundEmitters[i].OriginalVolume * soundEffect.CurrentVolume;
                        m_soundEmitters[i].Emitter.CustomMaxDistance = soundEffect.CurrentRange;
                        newSound = false;
                        if (!m_soundEmitters[i].Emitter.Loop && soundEffect.NewLoop)
                        {
                            soundEffect.NewLoop = false;
                            m_soundEmitters[i].Emitter.PlaySound(m_soundEmitters[i].SoundPair, false);
                        }
                        break;
                    }
                }
                if (newSound && soundEffect.Enabled)//create new sound emitter
                {
                    if (soundEffect.Position != Vector3.Zero)
                    {
                        MySoundPair sound = new MySoundPair(soundEffect.SoundName);
                        if (sound != MySoundPair.Empty)
                        {
                            m_soundEmitters.Add(new EffectSoundEmitter(soundEffect.ParticleSoundId, soundEffect.Position, sound));
                        }
                    }
                }
            }
            for (i = 0; i < m_soundEmitters.Count; i++)
            {
                if (m_soundEmitters[i].Updated == false)//effect no longer exists
                {
                    m_soundEmitters[i].Emitter.StopSound(true);
                    m_soundEmitters.RemoveAt(i);
                    i--;
                }
                else if (UpdateCount == 100)
                    m_soundEmitters[i].Emitter.Update();
            }

            if (UpdateCount >= 100)
                UpdateCount = 0;
        }


        public static void CreateBasicHitParticles(string effectName, ref Vector3D hitPoint, ref Vector3 normal, ref Vector3D direction, IMyEntity physObject, MyEntity weapon, float scale, MyEntity ownerEntity = null)
        {
            Vector3D reflectedDirection = Vector3D.Reflect(direction, normal);
            MyUtilRandomVector3ByDeviatingVector randomVector = new MyUtilRandomVector3ByDeviatingVector(reflectedDirection);

            MyParticleEffect effect;
            if (MyParticlesManager.TryCreateParticleEffect(effectName, out effect))
            {
                MatrixD dirMatrix = MatrixD.CreateFromDir(normal);
                effect.WorldMatrix = MatrixD.CreateWorld(hitPoint, dirMatrix.Forward, dirMatrix.Up);

                //VRageRender.MyRenderProxy.DebugDrawSphere(hitPoint, 0.1f, Color.Wheat, 1, false);
                // VRageRender.MyRenderProxy.DebugDrawAxis(effect.WorldMatrix, 5, false);

                effect.UserScale = scale;
            }
        }
    }
}

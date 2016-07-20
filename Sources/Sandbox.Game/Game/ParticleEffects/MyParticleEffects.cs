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
        public static void GenerateMuzzleFlash(Vector3D position, Vector3 dir, float radius, float length, bool near = false)
        {
            GenerateMuzzleFlash(position, dir, -1, ref MatrixD.Zero, radius, length, near);
        }

        public static void GenerateMuzzleFlash(Vector3D position, Vector3 dir, int renderObjectID, ref MatrixD worldToLocal, float radius, float length, bool near = false)
        {
            float angle = MyUtils.GetRandomFloat(0, MathHelper.PiOver2);

            float colorComponent = 1.3f;
            Vector4 color = new Vector4(colorComponent, colorComponent, colorComponent, 1);

            MyTransparentGeometry.AddLineBillboard("MuzzleFlashMachineGunSide", color, position, renderObjectID, ref worldToLocal,
                dir, length, 0.15f, 0, near);
            MyTransparentGeometry.AddPointBillboard("MuzzleFlashMachineGunFront", color, position, renderObjectID, ref worldToLocal, radius, angle, 0, false, near);
        }

        public static void GenerateMuzzleFlashLocal(IMyEntity entity, Vector3 localPos, Vector3 localDir, float radius, float length, bool near = false)
        {
            float angle = MyUtils.GetRandomFloat(0, MathHelper.PiOver2);

            float colorComponent = 1.3f;
            Vector4 color = new Vector4(colorComponent, colorComponent, colorComponent, 1);

            VRageRender.MyRenderProxy.AddLineBillboardLocal(entity.Render.RenderObjectIDs[0], "MuzzleFlashMachineGunSide", color, localPos, 
                localDir, length, 0.15f, 0, near);

            VRageRender.MyRenderProxy.AddPointBillboardLocal(entity.Render.RenderObjectIDs[0], "MuzzleFlashMachineGunFront", color, localPos, radius, angle, 0, false, near);
        }


        private class EffectSoundEmitter
        {
            public readonly uint ParticleSoundId;
            public bool Updated;
            public MyEntity3DSoundEmitter Emitter;
            public float OriginalVolume;

            public EffectSoundEmitter(uint id, Vector3 position, MySoundPair sound)
            {
                ParticleSoundId = id;
                Updated = true;
                Emitter = new MyEntity3DSoundEmitter(null);
                Emitter.SetPosition(position);
                Emitter.PlaySound(sound);
                if (Emitter.Sound != null)
                    OriginalVolume = Emitter.Sound.Volume;
                else
                    OriginalVolume = 1f;
                Emitter.Update();
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
                        break;
                    }
                }
                if (newSound && soundEffect.Enabled && soundEffect.Position != Vector3.Zero)//create new sound emitter
                {
                    MySoundPair sound = new MySoundPair(soundEffect.SoundName);
                    if (sound != MySoundPair.Empty)
                    {
                        m_soundEmitters.Add(new EffectSoundEmitter(soundEffect.ParticleSoundId,Vector3.Zero,sound));
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

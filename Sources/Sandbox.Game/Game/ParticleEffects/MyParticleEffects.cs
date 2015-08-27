using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Graphics.TransparentGeometry.Particles;
using VRage.Utils;
using Sandbox.Graphics.TransparentGeometry;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Game.Weapons;

using Sandbox.ModAPI;
using VRage.ModAPI;

namespace Sandbox.Game
{
    public enum MyCustomHitMaterialMethodType
    {
        None = -2,
        Unknown = -1,
        Small = 0,
        Normal,
    }

    public enum MyCustomHitParticlesMethodType
    {
        None = -2,
        Basic = 2,
        BasicSmall = 3,
    }
    
    static class MyParticleEffects
    {
        static Dictionary<MyEntity, Dictionary<int, MyParticleEffect>> m_hitParticles = new Dictionary<MyEntity, Dictionary<int, MyParticleEffect>>(64);
        static readonly Dictionary<int, MyCustomHitParticlesMethod> m_generalParticleDelegates = new Dictionary<int, MyCustomHitParticlesMethod>(10);
        static readonly Dictionary<int, MyCustomHitParticlesMethod> m_autocannonParticleDelegates = new Dictionary<int, MyCustomHitParticlesMethod>(10);
        static readonly Dictionary<int, MyCustomHitMaterialMethod> m_hitMaterialParticleDelegates = new Dictionary<int, MyCustomHitMaterialMethod>(2);

        static MyParticleEffects()
        {
            m_generalParticleDelegates.Add((int)MyCustomHitParticlesMethodType.None, 
                delegate(ref Vector3D hitPoint, ref Vector3 normal, ref Vector3D direction, IMyEntity physObject, MyEntity weapon, float scale, MyEntity ownerEntity) { });
            m_generalParticleDelegates.Add((int)MyCustomHitParticlesMethodType.Basic, CreateBasicHitParticles);
            m_generalParticleDelegates.Add((int)MyCustomHitParticlesMethodType.BasicSmall, CreateBasicHitSmallParticles);

            m_autocannonParticleDelegates.Add((int)MyCustomHitParticlesMethodType.Basic, CreateBasicHitAutocannonParticles);

            m_hitMaterialParticleDelegates.Add((int)MyCustomHitMaterialMethodType.None, 
                delegate(ref Vector3D hitPoint, ref Vector3 normal, ref Vector3D direction, IMyEntity physObject, MySurfaceImpactEnum surfaceImpact, MyEntity weapon, float scale) { });
            m_hitMaterialParticleDelegates.Add((int)MyCustomHitMaterialMethodType.Normal, CreateHitMaterialParticles);
            m_hitMaterialParticleDelegates.Add((int)MyCustomHitMaterialMethodType.Small, CreateHitMaterialSmallParticles);
        }

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

        //  Create smoke and debris particle at the place of voxel/model hit
        public static void CreateCollisionParticles(Vector3D hitPoint, Vector3 direction, bool doSmoke, bool doSparks)
        {
            MatrixD dirMatrix = MatrixD.CreateFromDir(direction);
            if (doSmoke)
            {
                MyParticleEffect effect;
                if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Collision_Smoke, out effect))
                {
                    effect.WorldMatrix = MatrixD.CreateWorld(hitPoint, dirMatrix.Forward, dirMatrix.Up);
                }
            }
            if (doSparks)
            {
                MyParticleEffect effect;
                if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Collision_Sparks, out effect))
                {
                    effect.WorldMatrix = MatrixD.CreateWorld(hitPoint, dirMatrix.Forward, dirMatrix.Up);
                }
            }
        }

        //  Create smoke and debris particle at the place of voxel/model hit
        static void CreateBasicHitParticles(ref Vector3D hitPoint, ref Vector3 normal, ref Vector3D direction, IMyEntity physObject, MyEntity weapon, float scale, MyEntity ownerEntity = null)
        {
            Vector3D reflectedDirection = Vector3D.Reflect(direction, normal);
            MyUtilRandomVector3ByDeviatingVector randomVector = new MyUtilRandomVector3ByDeviatingVector(reflectedDirection);

            if (MySector.MainCamera.GetDistanceWithFOV(hitPoint) < 200)
            {
                MyParticleEffect effect;
                if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Hit_BasicAmmo, out effect))
                {
                    MatrixD dirMatrix = MatrixD.CreateFromDir(reflectedDirection);
                    effect.WorldMatrix = MatrixD.CreateWorld(hitPoint, dirMatrix.Forward, dirMatrix.Up);
                    effect.UserScale = scale;
                }
            }
        }

        static void CreateBasicHitSmallParticles(ref Vector3D hitPoint, ref Vector3 normal, ref Vector3D direction, IMyEntity physObject, MyEntity weapon, float scale, MyEntity ownerEntity = null)
        {
            Vector3D reflectedDirection = Vector3D.Reflect(direction, normal);
            MyUtilRandomVector3ByDeviatingVector randomVector = new MyUtilRandomVector3ByDeviatingVector(reflectedDirection);

            if (MySector.MainCamera.GetDistanceWithFOV(hitPoint) < 100)
            {
                MyParticleEffect effect;
                if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Hit_BasicAmmoSmall, out effect))
                {
                    MatrixD dirMatrix = MatrixD.CreateFromDir(reflectedDirection);
                    effect.WorldMatrix = MatrixD.CreateWorld(hitPoint, dirMatrix.Forward, dirMatrix.Up);
                    effect.UserScale = scale;
                }
            }
        }

        static MyParticleEffect GetEffectForWeapon(MyEntity weapon, int effectID)
        {
            Dictionary<int, MyParticleEffect> effects;
            m_hitParticles.TryGetValue(weapon, out effects);
            if (effects == null)
            {
                effects = new Dictionary<int, MyParticleEffect>();
                m_hitParticles.Add(weapon, effects);
            }

            MyParticleEffect effect;
            effects.TryGetValue(effectID, out effect);

            if (effect == null)
            {
                if (MyParticlesManager.TryCreateParticleEffect(effectID, out effect))
                {
                    effects.Add(effectID, effect);
                    effect.Tag = weapon;
                    effect.OnDelete += new EventHandler(effect_OnDelete);
                }
            }
            else
            {
                effect.Restart();
            }

            return effect;
        }

        static void effect_OnDelete(object sender, EventArgs e)
        {
            MyParticleEffect effect = (MyParticleEffect)sender;
            MyEntity weapon = (MyEntity)effect.Tag;
            Dictionary<int, MyParticleEffect> effects = m_hitParticles[weapon];
            effects.Remove(effect.GetID());
            if (effects.Count == 0)
                m_hitParticles.Remove(weapon);
        }

        //  Create smoke and debris particle at the place of voxel/model hit
        static void CreateBasicHitAutocannonParticles(ref Vector3D hitPoint, ref Vector3 normal, ref Vector3D direction, IMyEntity physObject, MyEntity weapon, float scale, MyEntity ownerEntity = null)
        {
            MyParticleEffect effect = GetEffectForWeapon(weapon, (int)MyParticleEffectsIDEnum.Hit_AutocannonBasicAmmo);

            Vector3 reflectedDirection = Vector3.Reflect(direction, normal);
            MyUtilRandomVector3ByDeviatingVector randomVector = new MyUtilRandomVector3ByDeviatingVector(reflectedDirection);
            MatrixD dirMatrix = MatrixD.CreateFromDir(reflectedDirection);
            effect.WorldMatrix = MatrixD.CreateWorld(hitPoint, dirMatrix.Forward, dirMatrix.Up);
            effect.UserScale = scale;
        }

        static void CreateHitMaterialParticles(ref Vector3D hitPoint, ref Vector3 normal, ref Vector3D direction, IMyEntity physObject, MySurfaceImpactEnum surfaceImpact, MyEntity weapon, float scale)
        {
            Vector3 reflectedDirection = Vector3.Reflect(direction, normal);

            if (weapon != null)
            {
                MyParticleEffect effect = null;
                switch (surfaceImpact)
                {
                    case MySurfaceImpactEnum.METAL:
                        effect = GetEffectForWeapon(weapon, (int)MyParticleEffectsIDEnum.MaterialHit_Metal);
                        break;
                    case MySurfaceImpactEnum.DESTRUCTIBLE:
                        effect = GetEffectForWeapon(weapon, (int)MyParticleEffectsIDEnum.MaterialHit_Destructible);
                        break;
                    case MySurfaceImpactEnum.INDESTRUCTIBLE:
                        effect = GetEffectForWeapon(weapon, (int)MyParticleEffectsIDEnum.MaterialHit_Indestructible);
                        break;
                    case MySurfaceImpactEnum.CHARACTER:
                        effect = GetEffectForWeapon(weapon, (int)MyParticleEffectsIDEnum.MaterialHit_Character);
                        break;
                    default:
                        System.Diagnostics.Debug.Assert(false);
                        break;
                }
                if (effect != null)
                {
                    MatrixD dirMatrix = MatrixD.CreateFromDir(reflectedDirection);
                    effect.WorldMatrix = MatrixD.CreateWorld(hitPoint, dirMatrix.Forward, dirMatrix.Up);
                    effect.UserScale = scale;
                }
            }
        }

        static void CreateHitMaterialSmallParticles(ref Vector3D hitPoint, ref Vector3 normal, ref Vector3D direction, IMyEntity physObject, MySurfaceImpactEnum surfaceImpact, MyEntity weapon, float scale)
        {
            if (weapon != null)
            {
                Vector3 reflectedDirection = Vector3.Reflect(direction, normal);

                MyParticleEffect effect = null;
                switch (surfaceImpact)
                {
                    case MySurfaceImpactEnum.METAL:
                        effect = GetEffectForWeapon(weapon, (int)MyParticleEffectsIDEnum.MaterialHit_MetalSmall);
                        break;
                    case MySurfaceImpactEnum.DESTRUCTIBLE:
                        effect = GetEffectForWeapon(weapon, (int)MyParticleEffectsIDEnum.MaterialHit_DestructibleSmall);
                        break;
                    case MySurfaceImpactEnum.INDESTRUCTIBLE:
                        effect = GetEffectForWeapon(weapon, (int)MyParticleEffectsIDEnum.MaterialHit_IndestructibleSmall);
                        break;
                    case MySurfaceImpactEnum.CHARACTER:
                        effect = GetEffectForWeapon(weapon, (int)MyParticleEffectsIDEnum.MaterialHit_CharacterSmall);
                        break;
                    default:
                        System.Diagnostics.Debug.Assert(false);
                        break;
                }
                if (effect != null)
                {
                    MatrixD dirMatrix = MatrixD.CreateFromDir(reflectedDirection);
                    effect.WorldMatrix = MatrixD.CreateWorld(hitPoint, dirMatrix.Forward, dirMatrix.Up);
                    effect.UserScale = scale;
                }
            }
        }

        public static MyCustomHitParticlesMethod GetCustomHitParticlesMethodById(int id)
        {
            MyCustomHitParticlesMethod output;
            bool found = m_generalParticleDelegates.TryGetValue(id, out output);
            MyDebug.AssertDebug(found, "No custom hit particles method was defined for this id. Ignore it.");
            if (!found)
            {
                found = m_generalParticleDelegates.TryGetValue((int)MyCustomHitParticlesMethodType.Basic, out output);
                MyDebug.AssertDebug(found, "No DEFAULT custom hit particles method was defined for this id. Definitions not loaded properly.");
            }
            return output;
        }

        public static MyCustomHitParticlesMethod GetCustomAutocannonHitParticlesMethodById(int id)
        {
            MyCustomHitParticlesMethod output;
            bool found = m_autocannonParticleDelegates.TryGetValue(id, out output);
            MyDebug.AssertDebug(found, "No custom hit particles method was defined for this id. Ignore it.");
            if (!found)
            {
                found = m_autocannonParticleDelegates.TryGetValue((int)MyCustomHitParticlesMethodType.Basic, out output);
                MyDebug.AssertDebug(found, "No DEFAULT custom hit particles method was defined for this id. Definitions not loaded properly.");
            }
            return output;
        }

        public static MyCustomHitMaterialMethod GetCustomHitMaterialMethodById(int id)
        {
            MyCustomHitMaterialMethod output;
            bool found = m_hitMaterialParticleDelegates.TryGetValue(id, out output);
            MyDebug.AssertDebug(found, "No custom hit particles method was defined for this id. Ignore it.");
            if (!found)
            {
                found = m_hitMaterialParticleDelegates.TryGetValue((int)MyCustomHitMaterialMethodType.Normal, out output);
                MyDebug.AssertDebug(found, "No DEFAULT custom hit particles method was defined for this id. Definitions not loaded properly.");
            }
            return output;
        }
    }
}

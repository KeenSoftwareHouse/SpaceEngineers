using System.Collections.Generic;
using VRageMath;
using VRage.Generics;
using Sandbox.Engine.Utils;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Engine.Physics;
using System;
using Sandbox.Game.Weapons.Guns;
using VRage.Game.Components;
using VRage.Network;
using VRage.Game.Entity;
using System.Diagnostics;
using Sandbox.Engine.Multiplayer;

namespace Sandbox.Game.Weapons
{
    [StaticEventOwner]
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class MyMissiles : MySessionComponentBase
    {
        //static MyObjectsPool<MyMissile> m_missiles = null;
        static HashSet<MyMissile> m_missiles = new HashSet<MyMissile>();

        public override Type[] Dependencies
        {
            get
            {
                return new Type[] { typeof(MyPhysics) };
            }
        }

        public override void LoadData()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyMissiles.LoadData");

            MySandboxGame.Log.WriteLine("MyMissiles.LoadContent() - START");
            MySandboxGame.Log.IncreaseIndent();

            //if (m_missiles == null)
            //{
            //    m_missiles = new MyObjectsPool<MyMissile>(MyMissileConstants.MAX_MISSILES_COUNT);

            //    foreach (var item in m_missiles.Unused)
            //    {
            //        item.Init();
            //    }
            //}

            //m_missiles.DeallocateAll();

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyMissiles.LoadContent() - END");
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }


        protected override void UnloadData()
        {
            //if (m_missiles != null)
            //{
            //    m_missiles.DeallocateAll();
            //    foreach (var item in m_missiles.Unused)
            //    {
            //        item.Done();
            //    }
            //    m_missiles = null;
            //}
        }

        static MyMissile CreateMissile(MyWeaponPropertiesWrapper weaponProperties)
        {
            MyMissile missile = new MyMissile();
            missile.Init(weaponProperties);
            m_missiles.Add(missile);
            return missile;
        }

        public static MyMissile AddUnsynced(MyWeaponPropertiesWrapper weaponProperties, Vector3D position, Vector3D initialVelocity, Vector3D direction, long ownerId)
        {
            MyMissile newMissile = CreateMissile(weaponProperties);
            if (Sync.IsServer)
            { 
                //"hack" to prevent self shooting of rocket launchers if there is lag on network
                Vector3D extendedPos = position+direction*4.0f;
                MyPhysics.HitInfo? info = MyPhysics.CastRay(position, extendedPos);

                //spawn rocket 4m in fron of launcher on DS (why 4 ? why not ;) ), but only if there is nothing in front of launcher
                if (info.HasValue == false)
                {
                    position = extendedPos;
                }
            }
            newMissile.Start(position, initialVelocity, direction, ownerId);

            return newMissile;
        }

        //  Add new missile to the list
        public static MyMissile Add(MyWeaponPropertiesWrapper weaponProperties, Vector3D position, Vector3D initialVelocity, Vector3D direction, long ownerId)
        {
            MyMissile newMissile = AddUnsynced(weaponProperties, position, initialVelocity, direction, ownerId);
            // REMOVE-ME: Still commented out as of 70997. Ported to new MP events. Rmoved old sync class MySyncMissiles
            //if (newMissile != null && Sync.IsServer)
            //    MyMultiplayer.RaiseStaticEvent(s => MyMissiles.MissileCreatedSuccess, missileData.Launcher, initialVelocity);

            return newMissile;
        }

        public static void Remove(MyMissile missile)
        {
            //m_missiles.Deallocate(missile);
            m_missiles.Remove(missile);
        }

        // TODO: Remove me if not needed
        [Obsolete]
        [Event, Broadcast]
        private static void MissileCreatedSuccess(long launcherId, Vector3D initialVelocity)
        {
            MyEntity shootingEntity;
            MyEntities.TryGetEntityById(launcherId, out shootingEntity);
            Debug.Assert(shootingEntity != null, "Could not find missile shooting entity");
            if (shootingEntity == null)
                return;

            var shootingLauncher = shootingEntity as IMyMissileGunObject;
            Debug.Assert(shootingLauncher != null, "Shooting entity was not an IMyMissileGunObject");
            if (shootingLauncher == null)
                return;

            shootingLauncher.ShootMissile(initialVelocity);
        }
    }
}

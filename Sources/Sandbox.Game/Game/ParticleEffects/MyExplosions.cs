#region Using

using System.Collections.Generic;
using Sandbox.Common;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using VRage.Generics;
using VRageMath;
using VRage.Game.Components;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using VRage.Network;


#endregion

namespace Sandbox.Game
{
    [StaticEventOwner]
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MyExplosions : MySessionComponentBase
    {
        static MyObjectsPool<MyExplosion> m_explosions = null;

        static List<MyExplosionInfo> m_explosionBuffer1 = new List<MyExplosionInfo>();
        static List<MyExplosionInfo> m_explosionBuffer2 = new List<MyExplosionInfo>();

        static List<MyExplosionInfo> m_explosionsRead = m_explosionBuffer1;
        static List<MyExplosionInfo> m_explosionsWrite = m_explosionBuffer2;

        static MyExplosions()
        {
        }

        public override void LoadData()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyExplosions.LoadData");
            MySandboxGame.Log.WriteLine("MyExplosions.LoadData() - START");
            MySandboxGame.Log.IncreaseIndent();

            if (m_explosions == null)
            {
                m_explosions = new MyObjectsPool<MyExplosion>(MyExplosionsConstants.MAX_EXPLOSIONS_COUNT);
            }

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyExplosions.LoadData() - END");
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        protected override void UnloadData()
        {
            if (m_explosions != null && m_explosions.ActiveCount > 0)
            {
                foreach (MyExplosion explosion in m_explosions.Active)
                {
                    if (explosion != null)
                    {
                        explosion.Close();
                    }
                }

                m_explosions.DeallocateAll();
            }

            m_explosionsRead.Clear();
            m_explosionsWrite.Clear();
        }

     
        //  Add new explosion to the list, but caller needs to start it using Start() method
        public static void AddExplosion(ref MyExplosionInfo explosionInfo, bool updateSync = true)
        {
            System.Diagnostics.Debug.Assert(explosionInfo.ExplosionSphere.Radius > 0);

            if (updateSync)
            {
                MyMultiplayer.RaiseStaticEvent(s => MyExplosions.ProxyExplosionRequest,
                    explosionInfo.ExplosionSphere.Center, 
                    (float)explosionInfo.ExplosionSphere.Radius, 
                    explosionInfo.ExplosionType,
                    explosionInfo.VoxelExplosionCenter,
                    explosionInfo.ParticleScale
                );
            }            
            
            m_explosionsWrite.Add(explosionInfo);            
        }

        //  We have only Update method for explosions, because drawing of explosion is mantained by particles and lights itself
        public override void UpdateBeforeSimulation()
        {
            SwapBuffers();

            foreach (var explosionInfo in m_explosionsRead)
            {
                MyExplosion explosion = null;
                m_explosions.AllocateOrCreate(out explosion);

                if (explosion != null)
                {
                    explosion.Start(explosionInfo);
                }
            }

            m_explosionsRead.Clear();

            //  Go over every active explosion and draw it, unless it isn't dead.
            foreach (var explosion in m_explosions.Active)
            {
                if (explosion.Update() == false)
                {
                    m_explosions.MarkForDeallocate(explosion);
                }
            }

            //  Deallocate/delete all lights that are turned off
            m_explosions.DeallocateAllMarked();
        }

        [Event, Reliable, Server,BroadcastExcept]
        private static void ProxyExplosionRequest(Vector3D center, float radius, MyExplosionTypeEnum type, Vector3D voxelCenter, float particleScale)
        {
            //Dont create explosion particles if message is bufferred, it is useless to create hundred explosion after scene load
            if (MySession.Static.Ready)
            {
                //  Create explosion
                MyExplosionInfo info = new MyExplosionInfo()
                {
                    PlayerDamage = 0,
                    //Damage = m_ammoProperties.Damage,
                    Damage = 200,
                    ExplosionType = type,
                    ExplosionSphere = new BoundingSphere(center, radius),
                    LifespanMiliseconds = MyExplosionsConstants.EXPLOSION_LIFESPAN,
                    CascadeLevel = 0,
                    HitEntity = null,
                    ParticleScale = particleScale,
                    OwnerEntity = null,
                    Direction = Vector3.Forward,
                    VoxelExplosionCenter = voxelCenter,
                    ExplosionFlags = MyExplosionFlags.CREATE_DEBRIS | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.CREATE_PARTICLE_EFFECT | MyExplosionFlags.CREATE_SHRAPNELS,
                    VoxelCutoutScale = 1.0f,
                    PlaySound = true,
                    ObjectsRemoveDelayInMiliseconds = 40
                };
                MyExplosions.AddExplosion(ref info, false);
            }
        }

        void SwapBuffers()
        {
            if (m_explosionBuffer1 == m_explosionsRead)
            {
                m_explosionsWrite = m_explosionBuffer1;
                m_explosionsRead = m_explosionBuffer2;
            }
            else
            {
                m_explosionsWrite = m_explosionBuffer2;
                m_explosionsRead = m_explosionBuffer1;
            }
        }

        public override void Draw()
        {
            //  Go over every active explosion and draw it, unless it isn't dead.
            foreach (MyExplosion item in m_explosions.Active)
            {
                item.DebugDraw();
            }
        }
    }
}

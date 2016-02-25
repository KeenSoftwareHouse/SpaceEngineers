#region Using

using System.Collections.Generic;
using Sandbox.Common;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using VRage.Generics;
using VRageMath;
using VRage.Game.Components;


#endregion

namespace Sandbox.Game
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MyExplosions : MySessionComponentBase
    {
        static MyObjectsPool<MyExplosion> m_explosions = null;

        static List<MyExplosionInfo> m_explosionBuffer1 = new List<MyExplosionInfo>();
        static List<MyExplosionInfo> m_explosionBuffer2 = new List<MyExplosionInfo>();

        static List<MyExplosionInfo> m_explosionsRead = m_explosionBuffer1;
        static List<MyExplosionInfo> m_explosionsWrite = m_explosionBuffer2;

        public static MySyncExplosions SyncObject = new MySyncExplosions();

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
            if (m_explosions != null)
            {
                foreach (MyExplosion explosion in m_explosions.Active)
                {
                    explosion.Close();
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
                SyncObject.RequestExplosion(
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

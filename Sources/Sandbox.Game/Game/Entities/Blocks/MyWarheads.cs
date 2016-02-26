#region Using

using System.Collections.Generic;
using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using VRageMath;
using Sandbox.Game.Multiplayer;
using System;
using VRage.Game.Components;
using VRage.Game.Entity;


#endregion

namespace Sandbox.Game
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    class MyWarheads : MySessionComponentBase
    {
        static HashSet<MyWarhead> m_warheads = new HashSet<MyWarhead>();
        static List<MyWarhead> m_warheadsToExplode = new List<MyWarhead>();

        static MyWarheads()
        {
        }

        public override void BeforeStart()
        {
 	        base.BeforeStart();
        }

        protected override void UnloadData()
        {
            m_warheads.Clear();
            m_warheadsToExplode.Clear();

            DebugWarheadShrinks.Clear();
            DebugWarheadGroupSpheres.Clear();
        }

     
        public static void AddWarhead(MyWarhead warhead)
        {
            if(m_warheads.Add(warhead))
                warhead.OnMarkForClose += warhead_OnClose;
        }

        public static void RemoveWarhead(MyWarhead warhead)
        {
            if(m_warheads.Remove(warhead))
                warhead.OnMarkForClose -= warhead_OnClose;
        }

        public static bool Contains(MyWarhead warhead)
        {
            return m_warheads.Contains(warhead);
        }

        static void warhead_OnClose(MyEntity obj)
        {
            m_warheads.Remove(obj as MyWarhead);
        }

        //  We have only Update method for explosions, because drawing of explosion is mantained by particles and lights itself
        public override void UpdateBeforeSimulation()
        {
            int frameMs = VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;

            foreach (var warhead in m_warheads)
            {
                if (!warhead.Countdown(frameMs))
                {
                    if(!warhead.MarkedToExplode)
                        continue;
                }

                warhead.RemainingMS -= frameMs;
                if (warhead.RemainingMS <= 0)
                {
                    m_warheadsToExplode.Add(warhead);
                }
            }

            foreach (var warhead in m_warheadsToExplode)
            {
                RemoveWarhead(warhead);
                //m_warheads.Remove(warhead);
                if (Sync.IsServer)
                    warhead.Explode();
            }

            m_warheadsToExplode.Clear();
        }

        public static List<BoundingSphere> DebugWarheadShrinks = new List<BoundingSphere>();
        public static List<BoundingSphere> DebugWarheadGroupSpheres = new List<BoundingSphere>();

        public override void Draw()
        {
            base.Draw();

            foreach (var bs in DebugWarheadShrinks)
            {
                VRageRender.MyRenderProxy.DebugDrawSphere(bs.Center, bs.Radius, Color.Blue, 1, false);
            }

            foreach (var bs in DebugWarheadGroupSpheres)
            {
                //VRageRender.MyRenderProxy.DebugDrawAABB(bbox, Color.Yellow.ToVector3(), 1, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(bs.Center, bs.Radius, Color.Yellow, 1, false);
            }
        }
    }
}

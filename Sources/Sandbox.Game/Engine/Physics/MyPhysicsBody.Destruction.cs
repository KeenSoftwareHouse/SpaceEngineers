#region Using

using System.Diagnostics;
using Sandbox.Engine.Physics;
using VRageMath;

using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;
using VRage.Utils;
using System.Linq;
using System.Collections.Generic;

using VRageRender;
using Sandbox.AppCode.Game;
using Sandbox.Game.Utils;
using Sandbox.Engine.Models;
using Havok;
using Sandbox.Graphics;
using Sandbox.Common;
using Sandbox.Game.World;
using Sandbox.Game.Gui;
using Sandbox.Game.Entities.Character;
using VRage;

using VRageMath.Spatial;
using Sandbox.Game;
using VRage.Profiler;

#endregion

namespace Sandbox.Engine.Physics
{
    //using MyHavokCluster = MyClusterTree<HkWorld>;
    using MyHavokCluster = MyClusterTree;
    using Sandbox.ModAPI;
    using Sandbox.Game.Entities.Cube;
    using Sandbox.Game.GameSystems;
    using Sandbox.Game.Multiplayer;
    using VRage.Library.Utils;
    using System;
    using Sandbox.Definitions;
    using VRage.ModAPI;
    using VRage.Game.Components;
    using VRage.Trace;

    /// <summary>
    /// Abstract engine physics body object.
    /// </summary>
    public partial class MyPhysicsBody
    {
        private HkdBreakableBody m_breakableBody;
        public override HkdBreakableBody BreakableBody
        {
            get { return m_breakableBody; }
            set
            {
                m_breakableBody = value;
                RigidBody = value;
            }
        }

        private List<HkdBreakableBodyInfo> m_tmpLst = new List<HkdBreakableBodyInfo>();

        public virtual void FracturedBody_AfterReplaceBody(ref HkdReplaceBodyEvent e)
        {
            System.Diagnostics.Debug.Assert(Sync.IsServer, "Client must not simulate destructions");
            if (!Sandbox.Game.Multiplayer.Sync.IsServer)
                return;

            ProfilerShort.Begin("DestructionFracture.AfterReplaceBody");
            Debug.Assert(BreakableBody != null);
            e.GetNewBodies(m_tmpLst);
            if (m_tmpLst.Count == 0)// || e.OldBody != DestructionBody)
            {
                ProfilerShort.End();
                return;
            }

            MyPhysics.RemoveDestructions(RigidBody);
            foreach (var b in m_tmpLst)
            {
                var bBody = MyFracturedPiecesManager.Static.GetBreakableBody(b);
                MatrixD m = bBody.GetRigidBody().GetRigidBodyMatrix();
                m.Translation = ClusterToWorld(m.Translation);
                var piece = MyDestructionHelper.CreateFracturePiece(bBody, ref m, (Entity as MyFracturedPiece).OriginalBlocks);
                if (piece == null)
                {
                    MyFracturedPiecesManager.Static.ReturnToPool(bBody);
                    continue;
                }
            }
            m_tmpLst.Clear();

            BreakableBody.AfterReplaceBody -= FracturedBody_AfterReplaceBody;

            MyFracturedPiecesManager.Static.RemoveFracturePiece(Entity as MyFracturedPiece, 0);

            ProfilerShort.End();
        }
    }
}
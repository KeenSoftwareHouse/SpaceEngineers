#region Using

using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;

using VRageMath;
using Sandbox.Game.World;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using System.Diagnostics;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Physics;
using Havok;
using System.Linq;
using Sandbox.Game.Weapons;
using VRageRender;
using VRage.Import;
using Sandbox.Common;
using Sandbox.Graphics;
using VRage.Groups;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;

#endregion

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// This should be replaced by MyEntityOwnershipComponent
    /// </summary>
    public class MyIDModule
    {
        long m_owner = 0; //PlayerId
        
        public long Owner
        {
            get { return m_owner; }
            set
            {
                //if (m_owner == 144242163021377641)
                //{
                //}
                m_owner = value;
            }
        }
        public MyOwnershipShareModeEnum ShareMode = MyOwnershipShareModeEnum.None;

        public VRage.Game.MyRelationsBetweenPlayerAndBlock GetUserRelationToOwner(long identityId)
        {
            return GetRelation(Owner, identityId, ShareMode);
        }

        public static VRage.Game.MyRelationsBetweenPlayerAndBlock GetRelation(long owner, long user, MyOwnershipShareModeEnum share = MyOwnershipShareModeEnum.None)
        {
            if (!MyFakes.SHOW_FACTIONS_GUI)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.NoOwnership;

            if (owner == 0)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.NoOwnership;

            if (owner == user)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.Owner;

            IMyFaction playerFaction = MySession.Static.Factions.TryGetPlayerFaction(user);
            IMyFaction faction = MySession.Static.Factions.TryGetPlayerFaction(owner);

            if ((playerFaction != null && playerFaction == faction && share == MyOwnershipShareModeEnum.Faction) || share == MyOwnershipShareModeEnum.All)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.FactionShare;

            if (playerFaction == null)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies;

            if (faction == null)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies;

            var factionRelation = MySession.Static.Factions.GetRelationBetweenFactions(faction.FactionId, playerFaction.FactionId);

            if (factionRelation == MyRelationsBetweenFactions.Neutral)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral;

            return VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies;
        }

    }
}


#region Using

using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Graphics.TransparentGeometry;

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

#endregion

namespace Sandbox.Game.Entities
{
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

        public MyRelationsBetweenPlayerAndBlock GetUserRelationToOwner(long identityId)
        {
            if (!MyFakes.SHOW_FACTIONS_GUI)
                return MyRelationsBetweenPlayerAndBlock.NoOwnership;

            if (Owner == 0)
                return MyRelationsBetweenPlayerAndBlock.NoOwnership;

            if (Owner == identityId)
                return MyRelationsBetweenPlayerAndBlock.Owner;

            IMyFaction playerFaction = MySession.Static.Factions.TryGetPlayerFaction(identityId);
            IMyFaction faction = MySession.Static.Factions.TryGetPlayerFaction(Owner);

            if ((playerFaction != null && playerFaction == faction && ShareMode == MyOwnershipShareModeEnum.Faction) || ShareMode == MyOwnershipShareModeEnum.All)
                return MyRelationsBetweenPlayerAndBlock.FactionShare;

            if (playerFaction == null)
                return MyRelationsBetweenPlayerAndBlock.Enemies;

            if (faction == null)
                return MyRelationsBetweenPlayerAndBlock.Enemies;

            var factionRelation = MySession.Static.Factions.GetRelationBetweenFactions(faction.FactionId, playerFaction.FactionId);

            if (factionRelation == MyRelationsBetweenFactions.Neutral)
                return MyRelationsBetweenPlayerAndBlock.Neutral;

            return MyRelationsBetweenPlayerAndBlock.Enemies;
        }
    }
}


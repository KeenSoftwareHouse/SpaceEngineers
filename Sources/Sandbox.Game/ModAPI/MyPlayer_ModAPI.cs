using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI;
using IMyControllableEntity = VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
using IMyGameControllableEntity = Sandbox.Game.Entities.IMyControllableEntity;

namespace Sandbox.Game.World
{
    partial class MyPlayer : IMyPlayer
    {
        IMyNetworkClient IMyPlayer.Client
        {
            get { return Client; }
        }

        VRage.Game.MyRelationsBetweenPlayerAndBlock IMyPlayer.GetRelationTo(long playerId)
        {
            return GetRelationTo(playerId);
        }

        HashSet<long> IMyPlayer.Grids 
        { 
            get { return Grids; } 
        }

        void IMyPlayer.RemoveGrid(long gridEntityId)
        {
            RemoveGrid(gridEntityId);
        }

        void IMyPlayer.AddGrid(long gridEntityId)
        {
            AddGrid(gridEntityId);
        }

        IMyEntityController IMyPlayer.Controller 
        { 
            get { return Controller;}
        }


        string IMyPlayer.DisplayName
        {
            get { return DisplayName; }
        }

        ulong IMyPlayer.SteamUserId
        {
            get { return this.Id.SteamId; }
        }


        VRageMath.Vector3D IMyPlayer.GetPosition()
        {
            return GetPosition();
        }

        // Warning: this is obsolete!
        long IMyPlayer.PlayerID
        {
            get { return Identity.IdentityId; }
        }

        long IMyPlayer.IdentityId
        {
            get { return Identity.IdentityId; }
        }

        bool IMyPlayer.IsAdmin
        {
            get { return IsAdmin; }
        }

        bool IMyPlayer.IsPromoted
        {
            get { return IsSpaceMaster; }
        }
    }
}

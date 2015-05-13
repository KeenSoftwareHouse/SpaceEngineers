using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.World
{
    public interface IMyRespawnComponent
    {
        bool HandleRespawnRequest(bool joinGame, bool newIdentity, long medicalRoom, string respawnShipId, MyPlayer.PlayerId playerId, Vector3D? spawnPosition);
        MyIdentity CreateNewIdentity(string identityName, MyPlayer.PlayerId playerId, string modelName);
    }
}

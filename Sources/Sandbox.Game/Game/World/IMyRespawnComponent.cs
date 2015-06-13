using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.World
{
    public interface IMyRespawnComponent
    {
        void InitFromCheckpoint(MyObjectBuilder_Checkpoint checkpoint);
        void SaveToCheckpoint(MyObjectBuilder_Checkpoint checkpoint);

        bool HandleRespawnRequest(bool joinGame, bool newIdentity, long medicalRoom, string respawnShipId, MyPlayer.PlayerId playerId, Vector3D? spawnPosition);
        MyIdentity CreateNewIdentity(string identityName, MyPlayer.PlayerId playerId, string modelName);
        void AfterRemovePlayer(MyPlayer player);
        void SetupCharacterDefault(MyPlayer player, MyWorldGenerator.Args args);
    }
}

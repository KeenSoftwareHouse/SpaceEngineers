using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SpaceEngineers.Game.Players;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace SpaceEngineers.Game.World
{
    [MyWorldGenerator.StartingStateType(typeof(MyObjectBuilder_WorldGeneratorPlayerStartingState_RespawnShip))]
    public class MyRespawnShipState : MyWorldGeneratorStartingStateBase
    {
        string m_respawnShipId;

        public override void Init(MyObjectBuilder_WorldGeneratorPlayerStartingState builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_WorldGeneratorPlayerStartingState_RespawnShip;
            m_respawnShipId = ob.RespawnShip;
        }

        public override MyObjectBuilder_WorldGeneratorPlayerStartingState GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_WorldGeneratorPlayerStartingState_RespawnShip;

            ob.RespawnShip = m_respawnShipId;

            return ob;
        }

        public override void SetupCharacter(MyWorldGenerator.Args generatorArgs)
        {
            string respawnShipId = m_respawnShipId;
            if (!MyDefinitionManager.Static.HasRespawnShip(m_respawnShipId))
                respawnShipId = MyDefinitionManager.Static.GetFirstRespawnShip();

            Debug.Assert(MySession.LocalHumanPlayer != null, "Local controller does not exist!");
            if (MySession.LocalHumanPlayer == null) return;

            MySpaceRespawnComponent.Static.SpawnAtShip(MySession.LocalHumanPlayer, respawnShipId);
        }

        public override Vector3D? GetStartingLocation()
        {
            return null;
        }
    }
}

using System.Diagnostics;
using Sandbox.Definitions;
using Sandbox.Game.World;
using VRage.Game;
using VRageMath;

namespace SpaceEngineers.Game.World.Generator
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

            Debug.Assert(MySession.Static.LocalHumanPlayer != null, "Local controller does not exist!");
            if (MySession.Static.LocalHumanPlayer == null) return;

            this.CreateAndSetPlayerFaction();

            MySpaceRespawnComponent.Static.SpawnAtShip(MySession.Static.LocalHumanPlayer, respawnShipId, null);
        }

        public override Vector3D? GetStartingLocation()
        {
            return null;
        }
    }
}

using Sandbox.Common.AI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.AI;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.AI
{
    public class MySpaceBotFactory : MyBotFactoryBase
    {
        public override int MaximumUncontrolledBotCount
        {
            get { return 10; }
        }

        public override int MaximumBotPerPlayer
        {
            get { return 10; }
        }

        public override bool CanCreateBotOfType(string behaviorType, bool load)
        {
            return true;
        }

        public override bool GetBotSpawnPosition(string behaviorType, out VRageMath.Vector3D spawnPosition)
        {
            if (MySession.LocalCharacter != null)
            {
                var pos = MySession.LocalCharacter.PositionComp.GetPosition();
                Vector3 up;
                Vector3D right, forward;

                up = MyGravityProviderSystem.CalculateNaturalGravityInPoint(pos);
                if (up.LengthSquared() < 0.0001f) up = Vector3.Up;
                else up = Vector3D.Normalize(up);
                forward = Vector3.CalculatePerpendicularVector(up);
                right = Vector3.Cross(forward, up);
                spawnPosition = MyUtils.GetRandomDiscPosition(ref pos, 5.0f, ref forward, ref right);
                return true;
            }

            spawnPosition = Vector3D.Zero;
            return false;
        }

        public override bool GetBotGroupSpawnPositions(string behaviorType, int count, List<Vector3D> spawnPositions)
        {
            throw new NotImplementedException();
        }
    }
}

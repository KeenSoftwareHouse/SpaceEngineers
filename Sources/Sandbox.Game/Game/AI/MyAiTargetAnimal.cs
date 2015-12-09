using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI
{
    public class MyAiTargetAnimal : MyAiTargetBase
    {
        public MyAiTargetAnimal(IMyEntityBot bot)
            :
            base(bot)
        {

        }

        public bool GetRandomDirectedPosition(Vector3D initPosition, Vector3D direction, out Vector3D outPosition)
        {
            outPosition = MySession.LocalCharacter.PositionComp.GetPosition();
            return true;
        }
    }
}

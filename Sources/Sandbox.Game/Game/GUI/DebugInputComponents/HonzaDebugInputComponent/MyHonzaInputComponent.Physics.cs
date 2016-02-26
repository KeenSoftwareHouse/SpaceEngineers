#region Using

using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using VRage.Audio;
using VRage.Game.Components;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;

#endregion

namespace Sandbox.Game.Gui
{
    public partial class MyHonzaInputComponent
    {
        public class PhysicsComponent : MyDebugComponent
        {
            public override string GetName()
            {
                return "Honza";
            }
        }
    }
}
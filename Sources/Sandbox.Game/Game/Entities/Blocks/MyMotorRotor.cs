using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Models;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_MotorRotor))]
    public class MyMotorRotor : MyAttachableTopBlockBase, IMyMotorRotor
    {
        #region ModAPI implementation
        ModAPI.IMyMotorBase ModAPI.IMyMotorRotor.Stator
        {
            get { return Stator as MyMotorStator; }
        }

        bool ModAPI.Ingame.IMyMotorRotor.IsAttached
        {
            get { return Stator != null; }
        }
        #endregion
    }
}
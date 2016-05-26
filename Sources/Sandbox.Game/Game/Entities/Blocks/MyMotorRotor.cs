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
        public Vector3 DummyPosLoc { get; private set; }

        public MyMechanicalConnectionBlockBase Stator { get { return m_parentBlock; } }

        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            var rotorOb = builder as MyObjectBuilder_MotorRotor;

            base.Init(builder, cubeGrid);

            LoadDummies();
        }

        private void LoadDummies()
        {
            var finalModel = VRage.Game.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model);
            foreach (var dummy in finalModel.Dummies)
            {
                if (dummy.Key.ToLower().Contains("wheel"))
                {
                    Matrix dummyLocal = Matrix.Normalize(dummy.Value.Matrix) * this.PositionComp.LocalMatrix;
                    DummyPosLoc = dummyLocal.Translation;
                    break;
                }
            }
        }

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
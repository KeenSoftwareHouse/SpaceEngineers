using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Multiplayer;
using VRage.Game;
using VRage.Game.Models;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_MotorRotor))]
    public class MyMotorRotor : MyCubeBlock
    {
        public Vector3 DummyPosLoc { get; private set; }

        private MyMotorBase m_statorBlock;

        public MyMotorBase Stator { get { return m_statorBlock; } }

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

        internal void Attach(MyMotorBase stator)
        {
            m_statorBlock = stator;
        }

        internal void Detach(bool isWelding)
        {
            if (isWelding == false)
            {
                m_statorBlock = null;
            }
        }

        public override void OnUnregisteredFromGridSystems()
        {
            if (m_statorBlock != null)
            {
                var statorBlock = m_statorBlock;          
                statorBlock.Detach();
                statorBlock.SyncDetach();
            }
            base.OnUnregisteredFromGridSystems();

            if (Sync.IsServer)
            {
                CubeGrid.OnGridSplit -= CubeGrid_OnGridSplit;
            }
        }

        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();

            if (Sync.IsServer)
            {
                CubeGrid.OnGridSplit += CubeGrid_OnGridSplit;
            }
        }

        protected void CubeGrid_OnGridSplit(MyCubeGrid grid1, MyCubeGrid grid2)
        {
            if (m_statorBlock != null)
            {
                m_statorBlock.OnGridSplit();
            }
        }

    }
}
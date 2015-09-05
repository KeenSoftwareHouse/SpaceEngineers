using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Multiplayer;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_MotorRotor))]
    public class MyMotorRotor : MyCubeBlock
    {
        public Vector3 DummyPosLoc { get; private set; }

        private MyMotorBase m_statorBlock;
        private long m_statorBlockId;

        public MyMotorBase Stator { get { return m_statorBlock; } }

        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            var rotorOb = builder as MyObjectBuilder_MotorRotor;

            base.Init(builder, cubeGrid);

            LoadDummies();
        }

        private void LoadDummies()
        {
            var finalModel = Engine.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model);
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

        internal void Detach()
        {
            m_statorBlock = null;
        }

        public override void OnUnregisteredFromGridSystems()
        {
            if (m_statorBlock != null)
            {
                m_statorBlock.Detach();
            }
            base.OnUnregisteredFromGridSystems();
        }

        public override void OnRemovedByCubeBuilder()
        {
            if (m_statorBlock != null)
            {
                var tmpStatorBlock = m_statorBlock;
                m_statorBlock.Detach(); // This will call our detach and set m_statorBlock to null
                if (Sync.IsServer)
                    tmpStatorBlock.CubeGrid.RemoveBlock(tmpStatorBlock.SlimBlock, updatePhysics: true);
            }
            base.OnRemovedByCubeBuilder();
        }

    }
}

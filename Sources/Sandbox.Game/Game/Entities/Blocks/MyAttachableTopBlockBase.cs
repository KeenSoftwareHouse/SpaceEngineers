using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRageMath;

namespace Sandbox.Game.Entities.Blocks
{
    abstract public class MyAttachableTopBlockBase : MyCubeBlock
    {
        public Vector3 DummyPosLoc { get; private set; }

        long? m_parentId;

        protected MyMechanicalConnectionBlockBase m_parentBlock;

        public virtual void Attach(MyMechanicalConnectionBlockBase parent)
        {
            m_parentBlock = parent;
        }

        public virtual void Detach(bool isWelding)
        {
            if (isWelding == false)
            {
                m_parentBlock = null;
            }
        }

        public override void OnUnregisteredFromGridSystems()
        {
            if (m_parentBlock != null)
            {
                var parent = m_parentBlock;
                m_parentId = m_parentBlock.EntityId;
                parent.Detach();
                parent.SyncDetach();               
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
                if (m_parentId != null)
                {
                    MyMechanicalConnectionBlockBase parent = null;
                    MyEntities.TryGetEntityById<MyMechanicalConnectionBlockBase>(m_parentId.Value, out parent);
                    if (parent != null && parent.CubeGrid != null && parent.Closed == false)
                    {
                        parent.ReattachTop(this);
                    }
                }
                CubeGrid.OnGridSplit += CubeGrid_OnGridSplit;
            }
        }

        protected void CubeGrid_OnGridSplit(MyCubeGrid grid1, MyCubeGrid grid2)
        {
            if (m_parentBlock != null)
            {
                m_parentBlock.OnGridSplit();
            }
        }

        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
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
    }
}

using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Game.Entities.Blocks
{
    abstract public class MyAttachableTopBlockBase : MyCubeBlock
    {
        protected MyMechanicalConnectionBlockBase m_parentBlock;

        public virtual void Attach(MyMechanicalConnectionBlockBase stator)
        {
            m_parentBlock = stator;
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
                var parentBlock = m_parentBlock;
                parentBlock.Detach();
                parentBlock.SyncDetach();
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
            if (m_parentBlock != null)
            {
                m_parentBlock.OnGridSplit();
            }
        }
    }
}

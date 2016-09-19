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

        private MyMechanicalConnectionBlockBase m_parentBlock;
        public MyMechanicalConnectionBlockBase Stator { get { return m_parentBlock; } }

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Gui;

namespace Sandbox.Game.GUI.DebugInputComponents
{
    public partial class MyVoxelDebugInputComponent : MyMultiDebugInputComponent
    {
        public MyVoxelDebugInputComponent()
        {
            m_components = new MyDebugComponent[]{
                new IntersectBBComponent(this),
                new IntersectRayComponent(this),
                new ToolsComponent(this),
                new StorageWriteCacheComponent(this),
                new PhysicsComponent(this), 
            };
        }

        #region Base
        private MyDebugComponent[] m_components;

        public override MyDebugComponent[] Components
        {
            get { return m_components; }
        }

        public override string GetName()
        {
            return "Voxels";
        }
        #endregion
    }
}

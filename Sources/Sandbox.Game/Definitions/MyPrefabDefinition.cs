using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.Game.Entities.Cube;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_PrefabDefinition))]
    public class MyPrefabDefinition : MyDefinitionBase
    {
        private MyObjectBuilder_CubeGrid[] m_cubeGrids;
        public MyObjectBuilder_CubeGrid[] CubeGrids
        {
            get
            {
                if (!Initialized) MyDefinitionManager.Static.ReloadPrefabsFromFile(PrefabPath);
                return m_cubeGrids;
            }
        }

        private BoundingSphere m_boundingSphere;
        public BoundingSphere BoundingSphere
        {
            get
            {
                if (!Initialized) MyDefinitionManager.Static.ReloadPrefabsFromFile(PrefabPath);
                return m_boundingSphere;
            }
        }

        private BoundingBox m_boundingBox;
        public BoundingBox BoundingBox
        {
            get
            {
                if (!Initialized) MyDefinitionManager.Static.ReloadPrefabsFromFile(PrefabPath);
                return m_boundingBox;
            }
        }

        public string PrefabPath;
        public bool Initialized = false;

        protected override void Init(MyObjectBuilder_DefinitionBase baseBuilder)
        {
            base.Init(baseBuilder);
            var builder = baseBuilder as MyObjectBuilder_PrefabDefinition;
            PrefabPath = builder.PrefabPath;
            Initialized = false;
        }

        public void InitLazy(MyObjectBuilder_DefinitionBase baseBuilder)
        {
            var builder = baseBuilder as MyObjectBuilder_PrefabDefinition;

            Debug.Assert(builder.CubeGrid != null || builder.CubeGrids != null, "No cube grids defined in prefab " + PrefabPath);
            if (builder.CubeGrid == null && builder.CubeGrids == null) return;

            // Backwards compatiblity
            if (builder.CubeGrid != null)
                m_cubeGrids = new MyObjectBuilder_CubeGrid[1] { builder.CubeGrid };
            else
                m_cubeGrids = builder.CubeGrids;

            m_boundingSphere = new BoundingSphere(Vector3.Zero, float.MinValue);
            m_boundingBox = BoundingBox.CreateInvalid();
         
            foreach (var grid in m_cubeGrids)
            {
                BoundingBox localBB = grid.CalculateBoundingBox();
                Matrix gridTransform = grid.PositionAndOrientation.HasValue ? (Matrix)grid.PositionAndOrientation.Value.GetMatrix() : Matrix.Identity;
                m_boundingBox.Include(localBB.Transform(gridTransform));
            }

            m_boundingSphere = BoundingSphere.CreateFromBoundingBox(m_boundingBox);

            foreach (var gridBuilder in m_cubeGrids)
            {
                gridBuilder.CreatePhysics = true;
                gridBuilder.XMirroxPlane = null;
                gridBuilder.YMirroxPlane = null;
                gridBuilder.ZMirroxPlane = null;
            }
            
            Initialized = true;
        }
    }
}

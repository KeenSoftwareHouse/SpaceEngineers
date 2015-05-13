using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.Game.Entities.Cube;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_PrefabDefinition))]
    public class MyPrefabDefinition : MyDefinitionBase
    {
        public MyObjectBuilder_CubeGrid[] CubeGrids;
        public BoundingSphere BoundingSphere;
        public BoundingBox BoundingBox;
        public string PrefabPath;

        protected override void Init(MyObjectBuilder_DefinitionBase baseBuilder)
        {
            base.Init(baseBuilder);

            Id = baseBuilder.Id;

            var builder = baseBuilder as MyObjectBuilder_PrefabDefinition;

            BoundingSphere = new BoundingSphere(Vector3.Zero, float.MinValue);

            PrefabPath = builder.PrefabPath;

            if (builder.CubeGrid == null && builder.CubeGrids == null)
            {
                return;
            }
            // Backwards compatiblity
            if (builder.CubeGrid != null)
                CubeGrids = new MyObjectBuilder_CubeGrid[1] { builder.CubeGrid };
            else
                CubeGrids = builder.CubeGrids;

            BoundingBox = BoundingBox.CreateInvalid();
         
            foreach (var grid in CubeGrids)
            {
                BoundingBox localBB = grid.CalculateBoundingBox();
                Matrix gridTransform = grid.PositionAndOrientation.HasValue ? (Matrix)grid.PositionAndOrientation.Value.GetMatrix() : Matrix.Identity;
                BoundingBox.Include(localBB.Transform(gridTransform));
            }

            BoundingSphere = BoundingSphere.CreateFromBoundingBox(BoundingBox);

            foreach (var gridBuilder in CubeGrids)
            {
                gridBuilder.CreatePhysics = true;
                gridBuilder.XMirroxPlane = null;
                gridBuilder.YMirroxPlane = null;
                gridBuilder.ZMirroxPlane = null;
            }
        }
    }
}

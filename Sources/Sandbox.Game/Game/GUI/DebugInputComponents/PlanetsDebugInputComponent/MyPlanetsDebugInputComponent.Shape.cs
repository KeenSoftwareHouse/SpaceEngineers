using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.FileSystem;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
    public partial class MyPlanetsDebugInputComponent : MyMultiDebugInputComponent
    {
        private class ShapeComponent : MyDebugComponent
        {
            private MyPlanetsDebugInputComponent m_comp;

            public ShapeComponent(MyPlanetsDebugInputComponent comp)
            {
                m_comp = comp;
            }

            public override void Update100()
            {
                base.Update100();
                MyPlanetShapeProvider.PruningStats.CycleWork();
                MyPlanetShapeProvider.CacheStats.CycleWork();
                MyPlanetShapeProvider.CullStats.CycleWork();
            }

            #region Draw

            public override void Draw()
            {
                Text("Planet Shape request culls: {0}", MyPlanetShapeProvider.CullStats.History);
                Text("Planet Shape coefficient cache hits: {0}", MyPlanetShapeProvider.CacheStats.History);
                Text("Planet Shape pruning tree hits: {0}", MyPlanetShapeProvider.PruningStats.History);
                Text("Planet Shape Requests: {0}", MyPlanetShapeProvider.FormatedUnculledHistory);

                MultilineText("Known lods:\n {0}", MyPlanetShapeProvider.GetKnownLodSizes());

                base.Draw();
            }

            #endregion

            public override string GetName()
            {
                return "Shape";
            }
        }
    }
}

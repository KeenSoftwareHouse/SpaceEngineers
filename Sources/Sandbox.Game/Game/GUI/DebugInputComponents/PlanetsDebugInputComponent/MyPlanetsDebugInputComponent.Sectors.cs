using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Utils;
using VRage.Input;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public partial class MyPlanetsDebugInputComponent : MyMultiDebugInputComponent
    {
        private class SectorsComponent : MyDebugComponent
        {
            private MyPlanetsDebugInputComponent m_comp;

            public SectorsComponent(MyPlanetsDebugInputComponent comp)
            {
                m_comp = comp;

                AddShortcut(MyKeys.S, true, true, false, false, () => "Toggle sector work cycle", () => ToggleSectors());
            }

            private bool ToggleSectors()
            {
                MyPlanet.RUN_SECTORS = !MyPlanet.RUN_SECTORS;
                return true;
            }

            public override void Draw()
            {
                base.Draw();

                var p = m_comp.CameraPlanet;
                if (p != null && p.SectorsCreated != null)
                {
                    Text(Color.Yellow, 1.1f, "Statistics for this planet: (last/min/avg/max)");
                    Text("Created: {0}", FormatWorkTracked(p.SectorsCreated.Stats()));
                    Text("Closed: {0}", FormatWorkTracked(p.SectorsClosed.Stats()));
                    Text("Scans: {0}", FormatWorkTracked(p.SectorScans.Stats()));
                    Text("Scanned: {0}", FormatWorkTracked(p.SectorsScanned.Stats()));
                    Text("Operations: {0}", FormatWorkTracked(p.SectorOperations.Stats()));
                    Text("Active: {0}", p.EnvironmentSectors.Count);
                }
            }

            private string FormatWorkTracked(Vector4I workStats)
            {
                return String.Format("{0:D3}/{1:D3}/{2:D3}/{3:D3}", workStats.X, workStats.Y, workStats.Z, workStats.W);
            }

            public override string GetName()
            {
                return "Sectors";
            }
        }
    }
}

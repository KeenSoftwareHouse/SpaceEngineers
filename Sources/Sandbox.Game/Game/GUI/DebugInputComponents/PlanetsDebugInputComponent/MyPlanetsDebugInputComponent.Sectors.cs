using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.World;
using VRage.Input;
using VRageMath;
using VRageRender;

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

                AddShortcut(MyKeys.NumPad8, true, false, false, false, () => "Toggle update range", () => m_updateRange = !m_updateRange);
            }

            private bool ToggleSectors()
            {
                MyPlanet.RUN_SECTORS = !MyPlanet.RUN_SECTORS;
                return true;
            }

            public override void Draw()
            {
                base.Draw();

                var planet = m_comp.CameraPlanet;

                if (planet == null) return;

                var envs = planet.Components.Get<MyPlanetEnvironmentComponent>();
                if (envs == null) return;

                var textPrinted = false;

                var current = MyPlanetEnvironmentSessionComponent.ActiveSector;

                if (current != null && current.DataView != null)
                {
                    var storage = current.DataView.LogicalSectors;
                    Text(Color.White, 1.5f, "Current sector: {0}", current.ToString());
                    Text("Storage sectors:");
                    foreach (var log in storage)
                    {
                        Text("   {0}", log.DebugData);
                    }
                }

                Text("Horizon Distance: {0}", m_radius);

                if (m_updateRange)
                    UpdateViewRange(planet);

                foreach (var provider in envs.Providers)
                {
                    var sects = provider.LogicalSectors.ToArray();
                    if (sects.Length > 0 && !textPrinted)
                    {
                        textPrinted = true;
                        Text(Color.Yellow, 1.5f, "Synchronized:");
                    }
                    foreach (var sector in sects)
                        if (sector != null && sector.ServerOwned)
                        {
                            Text("Sector {0}", sector.ToString());
                        }
                }

                Text("Physics");
                foreach (var sector in planet.Components.Get<MyPlanetEnvironmentComponent>().PhysicsSectors.Values)
                {
                    Text(Color.White, 0.8f, "Sector {0}", sector.ToString());
                }

                Text("Graphics");
                foreach (var proxy in planet.Components.Get<MyPlanetEnvironmentComponent>().Proxies.Values)
                {
                    if (proxy.EnvironmentSector != null)
                        Text(Color.White, 0.8f, "Sector {0}", proxy.EnvironmentSector.ToString());
                }

                MyRenderProxy.DebugDrawCylinder(m_center, m_orientation, (float)m_radius, m_height, Color.Orange, 1, true, false);
            }

            #region View Range

            private bool m_updateRange = true;

            private Vector3D m_center;
            private double m_radius;
            private double m_height;

            private QuaternionD m_orientation;

            private void UpdateViewRange(MyPlanet planet)
            {
                var pos = MySector.MainCamera.Position;

                double dist = double.MaxValue;

                foreach (var p in MyPlanets.GetPlanets())
                {
                    double dsq = Vector3D.DistanceSquared(pos, p.WorldMatrix.Translation);
                    if (dsq < dist)
                    {
                        planet = p;
                        dist = dsq;
                    }
                }

                var radius = planet.MinimumRadius;
                double altitude;

                m_height = planet.MaximumRadius - radius;

                var center = planet.WorldMatrix.Translation;


                m_radius = HyperSphereHelpers.DistanceToTangentProjected(ref center, ref pos, radius, out altitude);

                var up = center - pos;
                up.Normalize();

                m_center = pos + up * altitude;

                var forward = Vector3D.CalculatePerpendicularVector(up);

                m_orientation = QuaternionD.CreateFromForwardUp(forward, up);
            }

            #endregion

            // keep this arround, maybe I should add it there instead?
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

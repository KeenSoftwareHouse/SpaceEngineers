using Sandbox.Common;
using Sandbox.Engine.Voxels;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public partial class MyPlanetsDebugInputComponent : MyMultiDebugInputComponent
    {
        private class InfoComponent : MyDebugComponent
        {
            private MyPlanetsDebugInputComponent m_comp;

            private Vector3 m_lastCameraPosition = Vector3.Invalid;

            private Queue<float> m_speeds = new Queue<float>(60);

            public InfoComponent(MyPlanetsDebugInputComponent comp)
            {
                m_comp = comp;
            }

            public override void Draw()
            {
                base.Draw();

                if (MySession.Static == null) return;

                if (m_comp.CameraPlanet != null)
                {
                    var provider = m_comp.CameraPlanet.Provider;

                    if (provider == null) return;

                    Vector3 camPos = MySector.MainCamera.Position;

                    float instantSpeed = 0;
                    float averageSpeed = 0;

                    if (m_lastCameraPosition.IsValid())
                    {
                        instantSpeed = (camPos - m_lastCameraPosition).Length() * VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND;
                        if (m_speeds.Count == 60) m_speeds.Dequeue();
                        m_speeds.Enqueue(instantSpeed);

                        foreach (var s in m_speeds)
                        {
                            averageSpeed += s;
                        }

                        averageSpeed /= m_speeds.Count;
                    }

                    m_lastCameraPosition = camPos;

                    Vector3 pos = camPos;

                    pos -= m_comp.CameraPlanet.PositionLeftBottomCorner;

                    MyPlanetStorageProvider.SurfacePropertiesExtended properties;

                    provider.ComputeCombinedMaterialAndSurfaceExtended(pos, out properties);

                    Section("Position");
                    Text("Position: {0}", properties.Position);
                    Text("Speed: {0:F2}ms -- {1:F2}m/s", instantSpeed, averageSpeed);
                    Text("Latitude: {0}", MathHelper.ToDegrees(Math.Asin(properties.Latitude)));
                    Text("Longitude: {0}", MathHelper.ToDegrees(MathHelper.MonotonicAcos(properties.Longitude)));
                    Text("Altitude: {0}", properties.Altitude);
                    VSpace(5f);
                    Text("Height: {0}", properties.Depth);
                    Text("HeightRatio: {0}", properties.HeightRatio);
                    Text("Slope: {0}", MathHelper.ToDegrees(Math.Acos(properties.Slope)));
                    Text("Air Density: {0}", m_comp.CameraPlanet.GetAirDensity(camPos));
                    Text("Oxygen: {0}", m_comp.CameraPlanet.GetOxygenForPosition(camPos));

                    Section("Cube Position");
                    Text("Face: {0}", MyCubemapHelpers.GetNameForFace(properties.Face));
                    Text("Texcoord: {0}", properties.Texcoord);
                    Text("Texcoord Position: {0}", (Vector2I)(properties.Texcoord * 2048));

                    Section("Material");
                    Text("Material: {0}", properties.Material != null ? properties.Material.Id.SubtypeName : "null");
                    Text("Material Origin: {0}", properties.Origin);
                    Text("Biome: {0}", properties.Biome != null ? properties.Biome.Name : "");
                    MultilineText("EffectiveRule: {0}", properties.EffectiveRule);
                    Text("Ore: {0}", properties.Ore);


                    Section("Map values");
                    Text("BiomeValue: {0}", properties.BiomeValue);
                    Text("MaterialValue: {0}", properties.MaterialValue);
                    Text("OcclusionValue: {0}", properties.OcclusionValue);
                    Text("OreValue: {0}", properties.OreValue);
                }
            }

            public override string GetName()
            {
                return "Info";
            }
        }
    }
}

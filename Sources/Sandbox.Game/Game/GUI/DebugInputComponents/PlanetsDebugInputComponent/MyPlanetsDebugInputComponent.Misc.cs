using Sandbox.Common;
using Sandbox.Engine.Voxels;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using VRage.Utils;
using VRageMath;
using VRage.Game.Entity;

namespace Sandbox.Game.Gui
{
    public partial class MyPlanetsDebugInputComponent : MyMultiDebugInputComponent
    {
        private class MiscComponent : MyDebugComponent
        {
            private MyPlanetsDebugInputComponent m_comp;

            private Vector3 m_lastCameraPosition = Vector3.Invalid;

            private Queue<float> m_speeds = new Queue<float>(60);

            public MiscComponent(MyPlanetsDebugInputComponent comp)
            {
                m_comp = comp;
            }

            public override void Draw()
            {
                base.Draw();

                if (MySession.Static == null) return;

                Text("Game time: {0}", MySession.Static.ElapsedGameTime);

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

                Section("Controlled Entity/Camera");
                Text("Speed: {0:F2}ms -- {1:F2}m/s", instantSpeed, averageSpeed);

                if (MySession.Static.LocalHumanPlayer == null || MySession.Static.LocalHumanPlayer.Controller.ControlledEntity == null) return;

                var controlled = MySession.Static.LocalHumanPlayer.Controller.ControlledEntity;
                var centity = (MyEntity)controlled;

                if (centity is MyCubeBlock) centity = ((MyCubeBlock)centity).CubeGrid;

                StringBuilder sb = new StringBuilder();
                if (centity.Physics != null)
                {
                    sb.Clear();
                    sb.Append("Mass: ");
                    MyValueFormatter.AppendWeightInBestUnit(centity.Physics.Mass, sb);
                    Text(sb.ToString());
                }

                MyEntityThrustComponent component;
                if (centity.Components.TryGet(out component))
                {
                    sb.Clear();
                    sb.Append("Current Thrust: ");
                    MyValueFormatter.AppendForceInBestUnit(component.FinalThrust.Length(), sb);
                    sb.AppendFormat(" : {0}", component.FinalThrust);
                    Text(sb.ToString());
                }
            }

            public override string GetName()
            {
                return "Misc";
            }
        }
    }
}

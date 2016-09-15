using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRage;
using VRage.Game.Entity;
using VRage.Input;
using VRage.Profiler;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.GUI.DebugInputComponents
{
    public partial class MyVoxelDebugInputComponent
    {
        private class IntersectRayComponent : MyDebugComponent
        {
            private MyVoxelDebugInputComponent m_comp;

            private bool m_moveProbe = true;

            private bool m_showVoxelProbe = false;

            private float m_rayLength = 25;

            private MyVoxelBase m_probedVoxel;

            private LineD m_probedLine;

            private Vector3D m_forward;
            private Vector3D m_up;

            private int m_probeCount = 1;

            private float m_probeGap = 1;

            public IntersectRayComponent(MyVoxelDebugInputComponent comp)
            {
                m_comp = comp;

                AddShortcut(MyKeys.OemOpenBrackets, true, false, false, false, () => "Toggle voxel probe ray.", () => ToggleProbeRay());
                AddShortcut(MyKeys.OemBackslash, true, false, false, false, () => "Freeze/Unfreeze probe", () => FreezeProbe());
            }

            private bool ToggleProbeRay()
            {
                m_showVoxelProbe = !m_showVoxelProbe;
                return true;
            }

            private bool FreezeProbe()
            {
                m_moveProbe = !m_moveProbe;
                return true;
            }

            public override bool HandleInput()
            {
                int scroll = MyInput.Static.DeltaMouseScrollWheelValue();
                if (scroll != 0 && m_showVoxelProbe)
                {
                    if (MyInput.Static.IsAnyCtrlKeyPressed())
                    {
                        if (MyInput.Static.IsAnyShiftKeyPressed())
                            m_rayLength += scroll / 12f;
                        else
                            m_rayLength += scroll / 120f;

                        m_probedLine.To = m_probedLine.From + m_rayLength * m_probedLine.Direction;
                        m_probedLine.Length = m_rayLength;
                        return true;
                    }

                    if (MyInput.Static.IsKeyPress(MyKeys.G))
                    {
                        m_probeGap = MathHelper.Clamp(m_probeGap + scroll / 240f, .5f, 32f);
                        return true;
                    }

                    if (MyInput.Static.IsKeyPress(MyKeys.N))
                    {
                        m_probeCount = MathHelper.Clamp(m_probeCount + scroll / 120, 1, 33);
                        return true;
                    }
                }

                return base.HandleInput();
            }

            #region Draw

            private void Probe(Vector3D pos)
            {
                var ray = m_probedLine;
                ray.From += pos;
                ray.To += pos;

                var entities = new List<MyLineSegmentOverlapResult<MyEntity>>();

                MyGamePruningStructure.GetTopmostEntitiesOverlappingRay(ref ray, entities, MyEntityQueryType.Static);

                double closest = double.PositiveInfinity;

                foreach (var e in entities)
                {
                    var voxel = e.Element as MyVoxelBase;
                    if (voxel != null && e.Distance < closest)
                    {
                        m_probedVoxel = voxel;
                    }
                }

                if (m_probedVoxel is MyVoxelPhysics) m_probedVoxel = ((MyVoxelPhysics)m_probedVoxel).Parent;

                if (m_probedVoxel != null && m_probedVoxel.Storage.DataProvider != null)
                {
                    MyRenderProxy.DebugDrawLine3D(ray.From, ray.To, Color.Green, Color.Green, true);

                    Vector3D start = Vector3D.Transform(ray.From, m_probedVoxel.PositionComp.WorldMatrixInvScaled);
                    start += m_probedVoxel.SizeInMetresHalf;
                    var end = Vector3D.Transform(ray.To, m_probedVoxel.PositionComp.WorldMatrixInvScaled);
                    end += m_probedVoxel.SizeInMetresHalf;
                    var voxRay = new LineD(start, end);

                    double startOffset;
                    double endOffset;
                    // Intersect provider for nau
                    var cont = m_probedVoxel.Storage.DataProvider.Intersect(ref voxRay, out startOffset, out endOffset);

                    var from = voxRay.From;
                    voxRay.From = from + voxRay.Direction * voxRay.Length * startOffset;
                    voxRay.To = from + voxRay.Direction * voxRay.Length * endOffset;

                    if (m_probeCount == 1)
                    {
                        Text(Color.Yellow, 1.5f, "Probing voxel map {0}:{1}", m_probedVoxel.StorageName, m_probedVoxel.EntityId);
                        Text("Local Pos: {0}", start);
                        Text("Intersects: {0}", cont);
                    }

                    if (cont)
                    {
                        start = voxRay.From - m_probedVoxel.SizeInMetresHalf;
                        start = Vector3D.Transform(start, m_probedVoxel.PositionComp.WorldMatrix);
                        end = voxRay.To - m_probedVoxel.SizeInMetresHalf;
                        end = Vector3D.Transform(end, m_probedVoxel.PositionComp.WorldMatrix);

                        MyRenderProxy.DebugDrawLine3D(start, end, Color.Red, Color.Red, true);
                    }
                }
                else
                {
                    if (m_probeCount == 1)
                        Text(Color.Yellow, 1.5f, "No voxel found");

                    MyRenderProxy.DebugDrawLine3D(ray.From, ray.To, Color.Yellow, Color.Yellow, true);
                }
            }

            public override void Draw()
            {
                base.Draw();

                if (MySession.Static == null) return;

                if (m_showVoxelProbe)
                {
                    Text("Probe Controlls:");
                    Text("\tCtrl + Mousewheel: Chage probe size");
                    Text("\tCtrl + Shift+Mousewheel: Chage probe size (x10)");
                    Text("\tN + Mousewheel: Chage probe count");
                    Text("\tG + Mousewheel: Chage probe gap");

                    Text("Probe Size: {0}", m_rayLength);
                    Text("Probe Count: {0}", m_probeCount * m_probeCount);

                    if (m_moveProbe)
                    {
                        m_up = MySector.MainCamera.UpVector;
                        m_forward = MySector.MainCamera.ForwardVector;
                        var start = MySector.MainCamera.Position - m_up * 0.5f + m_forward * 0.5f;
                        m_probedLine = new LineD(start, start + m_rayLength * m_forward);
                    }

                    ProfilerShort.Begin("Raycast voxel storage");

                    var right = Vector3D.Cross(m_forward, m_up);

                    float half = m_probeCount / 2f;

                    for (int x = 0; x < m_probeCount; ++x)
                    {
                        for (int y = 0; y < m_probeCount; ++y)
                        {
                            var pos = ((x - half) * m_probeGap * right) + ((y - half) * m_probeGap * m_up);

                            Probe(pos);
                        }
                    }

                    ProfilerShort.End();
                }
            }

            #endregion

            public override string GetName()
            {
                return "Intersect Ray";
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRage.Input;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.GUI.DebugInputComponents
{
    public partial class MyVoxelDebugInputComponent
    {
        private class IntersectBBComponent : MyDebugComponent
        {
            private MyVoxelDebugInputComponent m_comp;

            private bool m_moveProbe = true;

            private bool m_showVoxelProbe = false;

            private byte m_valueToSet = 128;

            private enum ProbeMode
            {
                Content = (int) MyStorageDataTypeEnum.Content,
                Material = (int) MyStorageDataTypeEnum.Material,
                Intersect = 2
            }

            private ProbeMode m_mode = ProbeMode.Intersect;
            private float m_probeSize = 1;
            private int m_probeLod = 0;

            List<MyVoxelBase> m_voxels = new List<MyVoxelBase>();
            private MyStorageData m_target = new MyStorageData();

            private MyVoxelBase m_probedVoxel;

            private Vector3 m_probePosition;


            public IntersectBBComponent(MyVoxelDebugInputComponent comp)
            {
                m_comp = comp;

                AddShortcut(MyKeys.OemOpenBrackets, true, false, false, false, () => "Toggle voxel probe box.", () => ToggleProbeBox());
                AddShortcut(MyKeys.OemCloseBrackets, true, false, false, false, () => "Toggle probe mode", () => SwitchProbeMode());
                AddShortcut(MyKeys.OemBackslash, true, false, false, false, () => "Freeze/Unfreeze probe", () => FreezeProbe());

                AddShortcut(MyKeys.OemSemicolon, true, false, false, false, () => "Increase Probe Size.", () => ResizeProbe(1, 0));
                AddShortcut(MyKeys.OemSemicolon, true, true, false, false, () => "Decrease Probe Size.", () => ResizeProbe(-1, 0));

                AddShortcut(MyKeys.OemSemicolon, true, false, true, false, () => "Increase Probe Size (x128).", () => ResizeProbe(128, 0));
                AddShortcut(MyKeys.OemSemicolon, true, true, true, false, () => "Decrease Probe Size (x128).", () => ResizeProbe(-128, 0));

                AddShortcut(MyKeys.OemQuotes, true, false, false, false, () => "Increase LOD Size.", () => ResizeProbe(0, 1));
                AddShortcut(MyKeys.OemQuotes, true, true, false, false, () => "Decrease LOD Size.", () => ResizeProbe(0, -1));
            }

            private bool ResizeProbe(int sizeDelta, int lodDelta)
            {
                m_probeLod = MathHelper.Clamp(m_probeLod + lodDelta, 0, 16);
                if (m_mode != ProbeMode.Intersect)
                    m_probeSize = MathHelper.Clamp(m_probeSize + (sizeDelta << m_probeLod), 1 << m_probeLod, 32*(1 << m_probeLod));
                else
                    m_probeSize = MathHelper.Clamp(m_probeSize + (sizeDelta << m_probeLod), 1, float.PositiveInfinity);
                return true;
            }

            private bool ToggleProbeBox()
            {
                m_showVoxelProbe = !m_showVoxelProbe;
                ResizeProbe(0, 0);
                return true;
            }

            private bool SwitchProbeMode()
            {
                m_mode = (ProbeMode) (((int) m_mode + 1)%3);
                ResizeProbe(0, 0);
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
                if (scroll != 0 && MyInput.Static.IsAnyCtrlKeyPressed() && m_showVoxelProbe)
                {
                    m_valueToSet += (byte) (scroll/120);
                    return true;
                }

                return base.HandleInput();
            }

            #region Draw

            public override void Draw()
            {
                base.Draw();

                if (MySession.Static == null) return;

                if (m_showVoxelProbe)
                {
                    float halfSize = m_probeSize*.5f;
                    float lodSize = 1 << m_probeLod;

                    if (m_moveProbe)
                        m_probePosition = MySector.MainCamera.Position + MySector.MainCamera.ForwardVector*m_probeSize*3;

                    BoundingBox bb;
                    BoundingBoxD bbp; // Box used for drawing and finding the probe and drawing

                    bb = new BoundingBox(m_probePosition - halfSize, m_probePosition + halfSize);
                    bbp = (BoundingBoxD) bb;

                    m_voxels.Clear();
                    MyGamePruningStructure.GetAllVoxelMapsInBox(ref bbp, m_voxels);

                    MyVoxelBase map = null;
                    double distance = double.PositiveInfinity;

                    foreach (var vox in m_voxels)
                    {
                        var d = Vector3D.Distance(vox.WorldMatrix.Translation, m_probePosition);
                        if (d < distance)
                        {
                            distance = d;
                            map = vox;
                        }
                    }

                    ContainmentType cont = ContainmentType.Disjoint;

                    if (map != null)
                    {
                        map = map.RootVoxel;

                        Vector3 localPos = Vector3.Transform(m_probePosition, map.PositionComp.WorldMatrixInvScaled);
                        localPos += map.SizeInMetresHalf;

                        // Create similar bounding box in storage space
                        bb = new BoundingBox(localPos - halfSize, localPos + halfSize);

                        m_probedVoxel = map;

                        Section("Probing {1}: {0}", map.StorageName, map.GetType().Name);
                        Text("Probe mode: {0}", m_mode);

                        if (m_mode == ProbeMode.Intersect)
                        {
                            Text("Local Pos: {0}", localPos);
                            Text("Probe Size: {0}", m_probeSize);
                            cont = map.Storage.Intersect(ref bb, false);
                            Text("Result: {0}", cont.ToString());
                            bbp = (BoundingBoxD)bb;
                        }
                        else
                        {
                            Vector3I min = Vector3I.Floor(bb.Min / lodSize + .5f);
                            Vector3I max = min + ((int)m_probeSize >> m_probeLod) - 1;

                            bbp = new BoundingBoxD(min << m_probeLod, (max + 1) << m_probeLod);
                            bbp.Translate(new Vector3D(-.5));

                            Text("Probe Size: {0}({1})", (max - min).X + 1, m_probeSize);
                            Text("Probe LOD: {0}", m_probeLod);

                            var requestData = (MyStorageDataTypeEnum)(int)m_mode;
                            MyVoxelRequestFlags flags = MyVoxelRequestFlags.ContentChecked;

                            m_target.Resize(max - min + 1);
                            m_target.Clear(MyStorageDataTypeEnum.Content, 0);
                            m_target.Clear(MyStorageDataTypeEnum.Material, 0);
                            map.Storage.ReadRange(m_target, (MyStorageDataTypeFlags) (1 << (int) requestData), m_probeLod, ref min, ref max, ref flags);

                            if (requestData == MyStorageDataTypeEnum.Content)
                            {
                                if (flags.HasFlag(MyVoxelRequestFlags.EmptyContent)) cont = ContainmentType.Disjoint;
                                else if (flags.HasFlag(MyVoxelRequestFlags.FullContent)) cont = ContainmentType.Contains;
                                else
                                {
                                    int val = m_target.ValueWhenAllEqual(requestData);
                                    if (val == -1) cont = ContainmentType.Intersects;
                                    else if (val >= MyVoxelConstants.VOXEL_ISO_LEVEL) cont = ContainmentType.Contains;
                                    else cont = ContainmentType.Disjoint;
                                }

                                DrawContentsInfo(m_target);
                            }
                            else
                            {
                                cont = ContainmentType.Disjoint;
                                DrawMaterialsInfo(m_target);
                            }

                            Text(Color.Yellow, 1.5f, "Voxel Editing:");
                            Text("Value to set (Ctrl+Mousewheel): {0}", m_valueToSet);
                            if (m_probeLod != 0)
                            {
                                Text(Color.Red, "Writing to storage is only possible when probe is set to LOD 0");
                            }
                            else
                            {
                                Text("Use primary mouse button to set.");
                                Text("Position/Extents: {0}/{1}", bbp.Min, bbp.Extents);

                                if (MyInput.Static.IsLeftMousePressed())
                                {
                                    if (requestData == MyStorageDataTypeEnum.Content)
                                        m_target.BlockFillContent(Vector3I.Zero, m_target.Size3D - Vector3I.One, m_valueToSet);
                                    else
                                        m_target.BlockFillMaterial(Vector3I.Zero, m_target.Size3D - Vector3I.One, m_valueToSet);

                                    map.Storage.WriteRange(m_target, (MyStorageDataTypeFlags) (1 << (int) requestData), ref min, ref max);
                                }
                            }
                        }
                    }
                    else
                    {
                        Section("No Voxel Found");
                        Text("Probe mode: {0}", m_mode);
                        Text("Probe Size: {0}", m_probeSize);
                    }

                    Color c = ColorForContainment(cont);
                    if (map != null)
                    {
                        bbp = bbp.Translate(-map.SizeInMetresHalf);
                        MyOrientedBoundingBoxD oobb = new MyOrientedBoundingBoxD(bbp, map.WorldMatrix);
                        MyRenderProxy.DebugDrawOBB(oobb, c, 0.5f, true, false);
                    }
                    else
                    {
                        MyRenderProxy.DebugDrawAABB(bbp, c, 0.5f, 1.0f, true);
                    }
                }
            }

            private void DrawContentsInfo(MyStorageData data)
            {
                byte min, max;
                uint sum = 0;
                uint nonZero = 0;
                uint nonFull = 0;

                min = byte.MaxValue;
                max = 0;

                int cellCount = data.SizeLinear/data.StepLinear;

                for (int i = 0; i < data.SizeLinear; i += data.StepLinear)
                {
                    byte content = data.Content(i);
                    if (min > content) min = content;
                    if (max < content) max = content;

                    sum += content;

                    if (content != 0) nonZero++;
                    if (content != 255) nonFull++;
                }

                Section("Probing Contents ({0} {1})", cellCount, cellCount > 1 ? "voxels" : "voxel");

                Text("Min: {0}", min);
                Text("Average: {0}", sum/cellCount);
                Text("Max: {0}", max);

                VSpace(5);

                Text("Non-Empty: {0}", nonZero);
                Text("Non-Full: {0}", nonFull);
            }

            struct MatInfo : IComparable<MatInfo>
            {
                public byte Material;
                public int Count;



                public int CompareTo(MatInfo other)
                {
                    return Count - other.Count;
                }
            }

            private unsafe void DrawMaterialsInfo(MyStorageData data)
            {
                int* counts = stackalloc int[256];

                int cellCount = data.SizeLinear/data.StepLinear;

                for (int i = 0; i < data.SizeLinear; i += data.StepLinear)
                {
                    byte mat = data.Material(i);
                    counts[mat]++;
                }

                Section("Probing Materials ({0} {1})", cellCount, cellCount > 1 ? "voxels" : "voxel");

                List<MatInfo> hits = new List<MatInfo>();

                for (int i = 0; i < 256; i++)
                {
                    if (counts[i] > 0)
                    {
                        hits.Add(new MatInfo()
                        {
                            Material = (byte) i,
                            Count = counts[i]
                        });
                    }
                }

                hits.Sort();

                int maxMaterial = MyDefinitionManager.Static.VoxelMaterialCount;

                foreach (var info in hits)
                {
                    if (info.Material == MyVoxelConstants.NULL_MATERIAL)
                        Text("    Null Material: {0}", info.Count);
                    else if (info.Material > maxMaterial)
                        Text("    Invalid Material({1}): {0}", info.Count, info.Material);
                    else
                        Text("    {1}: {0}", info.Count, MyDefinitionManager.Static.GetVoxelMaterialDefinition(info.Material).Id.SubtypeName);
                }
            }

            private Color ColorForContainment(ContainmentType cont)
            {
                return cont == ContainmentType.Disjoint ? Color.Green : (cont == ContainmentType.Contains ? Color.Yellow : Color.Red);
            }

            #endregion

            public override string GetName()
            {
                return "Intersect BB";
            }
        }
    }
}

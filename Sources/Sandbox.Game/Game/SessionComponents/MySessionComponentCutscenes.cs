using Sandbox;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VRage.Audio;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.VisualScripting;
using VRage.Game.VisualScripting.ScriptBuilder;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, 900, typeof(MyObjectBuilder_CutsceneSessionComponent))]
    public class MySessionComponentCutscenes : MySessionComponentBase
    {
        private MyObjectBuilder_CutsceneSessionComponent m_objectBuilder;
        private Dictionary<string, Cutscene> m_cutsceneLibrary = new Dictionary<string, Cutscene>();

        public MatrixD CameraMatrix { get { return m_currentCameraMatrix; } }

        public bool IsCutsceneRunning { get { return m_currentCutscene != null; } }

        private Cutscene m_currentCutscene = null;
        private CutsceneSequenceNode m_currentNode = null;
        private float m_currentTime = 0f;
        private float m_currentFOV = 70;
        private int m_currentNodeIndex = 0;
        private bool m_nodeActivated = false;
        private float MINIMUM_FOV = 10;
        private float MAXIMUM_FOV = 300;
        private float m_eventDelay = float.MaxValue;
        private bool m_releaseCamera = false;
        private bool m_overlayEnabled = false;

        private MatrixD m_nodeStartMatrix;
        private float m_nodeStartFOV = 70;
        private MatrixD m_nodeEndMatrix;
        private MatrixD m_currentCameraMatrix;
        private MyEntity m_lookTarget = null;
        private MyEntity m_rotateTarget = null;
        private MyEntity m_moveTarget = null;
        private MyEntity m_attachedPositionTo = null;
        private Vector3D m_attachedPositionOffset = Vector3D.Zero;
        private MyEntity m_attachedRotationTo = null;
        private MatrixD m_attachedRotationOffset;
        private Vector3D m_lastUpVector = Vector3D.Up;
        private List<MatrixD> m_waypoints = new List<MatrixD>();

        private IMyCameraController m_originalCameraController = null;
        private MyCutsceneCamera m_cameraEntity = new MyCutsceneCamera();

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            m_objectBuilder = sessionComponent as MyObjectBuilder_CutsceneSessionComponent; // Safe cast

            if (m_objectBuilder != null && m_objectBuilder.Cutscenes != null && m_objectBuilder.Cutscenes.Length > 0)
            {
                foreach (var cutscene in m_objectBuilder.Cutscenes)
                {
                    if (cutscene.Name != null && cutscene.Name.Length > 0 && !m_cutsceneLibrary.ContainsKey(cutscene.Name))
                        m_cutsceneLibrary.Add(cutscene.Name, cutscene);
                }
            }
        }

        public override void BeforeStart()
        {
            // Needs to be done after entities are loaded.
            if (m_objectBuilder != null)
            {
                // Add the voxel precaching points
                foreach (var waypointName in m_objectBuilder.VoxelPrecachingWaypointNames)
                {
                    MyEntity entity;
                    if (MyEntities.TryGetEntityByName(waypointName, out entity))
                    {
                        MyRenderProxy.PointsForVoxelPrecache.Add(entity.PositionComp.GetPosition());
                    }
                }
            }
        }

        public override void UpdateBeforeSimulation()
        {
            if (m_releaseCamera && MySession.Static.ControlledEntity != null)
            {
                m_releaseCamera = false;
                MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, MySession.Static.ControlledEntity.Entity);
                MyHud.CutsceneHud = false;
                MyGuiScreenGamePlay.DisableInput = false;
            }
            if (IsCutsceneRunning)
            {
                if (MySession.Static.CameraController != m_cameraEntity)
                {
                    m_originalCameraController = MySession.Static.CameraController;
                    MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, m_cameraEntity);
                }
                if (m_currentCutscene.SequenceNodes != null && m_currentCutscene.SequenceNodes.Length > m_currentNodeIndex)
                {
                    m_currentNode = m_currentCutscene.SequenceNodes[m_currentNodeIndex];
                    CutsceneUpdate();
                }
                else
                {
                    //cutscene done
                    if (m_currentCutscene.NextCutscene != null && m_currentCutscene.NextCutscene.Length > 0)
                        PlayCutscene(m_currentCutscene.NextCutscene);
                    else
                        CutsceneEnd();
                }
                m_cameraEntity.WorldMatrix = m_currentCameraMatrix;

                /*
                 * DEBUG curve
                if (false && m_waypoints.Count > 2)
                {
                    int i = 0;
                    for (float timeRatio = 0f; timeRatio <= 1f; timeRatio += 0.01f)
                    {
                        float segmentTime = 1f / (m_waypoints.Count - 1);
                        int segment = (int)Math.Floor(timeRatio / segmentTime);
                        float segmentRatio = (timeRatio - segment * segmentTime) / segmentTime;
                        Vector3D pos = Vector3D.Zero;
                        if (segment == 0)
                        {
                            pos = MathHelper.CalculateBezierPoint(segmentRatio, m_waypoints[segment], m_waypoints[segment], m_waypoints[segment + 1] - (m_waypoints[segment + 2] - m_waypoints[segment]) / 4, m_waypoints[segment + 1]);
                        }
                        else if (segment >= m_waypoints.Count - 2)
                        {
                            pos = MathHelper.CalculateBezierPoint(segmentRatio, m_waypoints[segment], m_waypoints[segment] + (m_waypoints[segment + 1] - m_waypoints[segment - 1]) / 4, m_waypoints[segment + 1], m_waypoints[segment + 1]);
                        }
                        else
                        {
                            pos = MathHelper.CalculateBezierPoint(segmentRatio, m_waypoints[segment], m_waypoints[segment] + (m_waypoints[segment + 1] - m_waypoints[segment - 1]) / 4, m_waypoints[segment+1] - (m_waypoints[segment + 2] - m_waypoints[segment]) / 4, m_waypoints[segment + 1]);
                        }
                        //VRageRender.MyRenderProxy.DebugDrawSphere((Vector3)pos, 0.2f, Color.Aquamarine, 1f, true);
                        VRageRender.MyRenderProxy.DebugDrawText3D(pos, i.ToString(), Color.GreenYellow, 1, true);
                        i++;
                    }
                    foreach(var w in m_waypoints)
                        VRageRender.MyRenderProxy.DebugDrawSphere(w, 1.5f, Color.Orange, 1f, true);
                }*/
            }
        }

        public void CutsceneUpdate()
        {
            if (!m_nodeActivated)
            {
                //new node
                MySandboxGame.Log.WriteLineAndConsole(m_currentCutscene.Name + ": " + m_currentNodeIndex.ToString());
                m_nodeActivated = true;
                m_nodeStartMatrix = m_currentCameraMatrix;
                m_nodeEndMatrix = m_currentCameraMatrix;
                m_nodeStartFOV = m_currentFOV;
                m_moveTarget = null;
                m_rotateTarget = null;
                m_waypoints.Clear();

                m_eventDelay = float.MaxValue;
                if (m_currentNode.Event != null && m_currentNode.Event.Length > 0 && MyVisualScriptLogicProvider.CutsceneNodeEvent != null)
                {
                    if (m_currentNode.EventDelay <= 0)
                        MyVisualScriptLogicProvider.CutsceneNodeEvent(m_currentNode.Event);
                    else
                        m_eventDelay = m_currentNode.EventDelay;
                }

                //rotation
                if (m_currentNode.LookAt != null && m_currentNode.LookAt.Length > 0)
                {
                    MyEntity entity = MyVisualScriptLogicProvider.GetEntityByName(m_currentNode.LookAt);
                    if (entity != null)
                    {
                        m_nodeStartMatrix = MatrixD.CreateLookAtInverse(m_currentCameraMatrix.Translation, m_rotateTarget.PositionComp.GetPosition(), m_currentCameraMatrix.Up);
                        m_nodeEndMatrix = m_nodeStartMatrix;
                    }
                }

                if (m_currentNode.SetRorationLike != null && m_currentNode.SetRorationLike.Length > 0)
                {
                    MyEntity entity = MyVisualScriptLogicProvider.GetEntityByName(m_currentNode.SetRorationLike);
                    if (entity != null)
                    {
                        m_nodeStartMatrix = entity.WorldMatrix;
                        m_nodeEndMatrix = m_nodeStartMatrix;
                    }
                }

                if (m_currentNode.RotateLike != null && m_currentNode.RotateLike.Length > 0)
                {
                    MyEntity entity = MyVisualScriptLogicProvider.GetEntityByName(m_currentNode.RotateLike);
                    if (entity != null)
                        m_nodeEndMatrix = entity.WorldMatrix;
                }

                if (m_currentNode.RotateTowards != null && m_currentNode.RotateTowards.Length > 0)
                    m_rotateTarget = m_currentNode.RotateTowards.Length > 0 ? MyVisualScriptLogicProvider.GetEntityByName(m_currentNode.RotateTowards) : null;

                if (m_currentNode.LockRotationTo != null)
                    m_lookTarget = m_currentNode.LockRotationTo.Length > 0 ? MyVisualScriptLogicProvider.GetEntityByName(m_currentNode.LockRotationTo) : null;

                //position
                m_nodeStartMatrix.Translation = m_currentCameraMatrix.Translation;
                m_nodeEndMatrix.Translation = m_currentCameraMatrix.Translation;

                if (m_currentNode.SetPositionTo != null && m_currentNode.SetPositionTo.Length > 0)
                {
                    MyEntity entity = MyVisualScriptLogicProvider.GetEntityByName(m_currentNode.SetPositionTo);
                    if (entity != null)
                    {
                        m_nodeStartMatrix.Translation = entity.WorldMatrix.Translation;
                        m_nodeEndMatrix.Translation = entity.WorldMatrix.Translation;
                    }
                }

                if (m_currentNode.AttachTo != null)
                {
                    if (m_currentNode.AttachTo != null)
                    {
                        m_attachedPositionTo = m_currentNode.AttachTo.Length > 0 ? MyVisualScriptLogicProvider.GetEntityByName(m_currentNode.AttachTo) : null;
                        m_attachedPositionOffset = m_attachedPositionTo != null ? Vector3D.Transform(m_currentCameraMatrix.Translation, m_attachedPositionTo.PositionComp.WorldMatrixInvScaled) : Vector3D.Zero;
                        m_attachedRotationTo = m_attachedPositionTo;
                        m_attachedRotationOffset = m_currentCameraMatrix * m_attachedRotationTo.PositionComp.WorldMatrixInvScaled;
                        m_attachedRotationOffset.Translation = Vector3D.Zero;
                    }
                }
                else
                {
                    if (m_currentNode.AttachPositionTo != null)
                    {
                        m_attachedPositionTo = m_currentNode.AttachPositionTo.Length > 0 ? MyVisualScriptLogicProvider.GetEntityByName(m_currentNode.AttachPositionTo) : null;
                        m_attachedPositionOffset = m_attachedPositionTo != null ? Vector3D.Transform(m_currentCameraMatrix.Translation, m_attachedPositionTo.PositionComp.WorldMatrixInvScaled) : Vector3D.Zero;
                    }

                    if (m_currentNode.AttachRotationTo != null)
                    {
                        m_attachedRotationTo = m_currentNode.AttachRotationTo.Length > 0 ? MyVisualScriptLogicProvider.GetEntityByName(m_currentNode.AttachRotationTo) : null;
                        m_attachedRotationOffset = m_currentCameraMatrix * m_attachedRotationTo.PositionComp.WorldMatrixInvScaled;
                        m_attachedRotationOffset.Translation = Vector3D.Zero;
                    }
                }

                if (m_currentNode.MoveTo != null && m_currentNode.MoveTo.Length > 0)
                    m_moveTarget = m_currentNode.MoveTo.Length > 0 ? MyVisualScriptLogicProvider.GetEntityByName(m_currentNode.MoveTo) : null;

                //waypoints
                if (m_currentNode.Waypoints != null && m_currentNode.Waypoints.Length > 0)
                {
                    MyEntity entity;
                    bool first = true;
                    foreach (var waypoint in m_currentNode.Waypoints)
                    {
                        if (waypoint.Name.Length > 0)
                        {
                            entity = MyVisualScriptLogicProvider.GetEntityByName(waypoint.Name);
                            if (entity != null)
                            {
                                m_waypoints.Add(entity.WorldMatrix);
                                if (first)
                                {
                                    m_lastUpVector = entity.WorldMatrix.Up;
                                    first = false;
                                }
                            }
                        }
                    }
                    if (m_waypoints.Count > 0)
                    {
                        if (m_waypoints.Count < 3)
                        {
                            m_nodeEndMatrix.Translation = m_waypoints[m_waypoints.Count - 1].Translation;
                            m_waypoints.Clear();
                        }
                        else if (m_waypoints.Count == 2)
                        {
                            m_nodeStartMatrix = m_waypoints[0];
                            m_nodeEndMatrix = m_waypoints[1];
                        }
                    }
                }
                m_currentCameraMatrix = m_nodeStartMatrix;
            }

            //update time
            m_currentTime += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            float timeRatio = m_currentNode.Time > 0 ? MathHelper.Clamp(m_currentTime / m_currentNode.Time, 0f, 1f) : 1f;

            //event
            if (m_currentTime >= m_eventDelay)
            {
                m_eventDelay = float.MaxValue;
                MyVisualScriptLogicProvider.CutsceneNodeEvent(m_currentNode.Event);
            }

            //end position
            if (m_moveTarget != null)
                m_nodeEndMatrix.Translation = m_moveTarget.PositionComp.GetPosition();

            //update position
            Vector3D newPos = m_currentCameraMatrix.Translation;
            if (m_attachedPositionTo != null)
            {
                if (!m_attachedPositionTo.Closed)
                {
                    newPos = Vector3D.Transform(m_attachedPositionOffset, m_attachedPositionTo.PositionComp.WorldMatrix);
                }
            }
            else if (m_waypoints.Count > 2)
            {
                double segmentTime = 1f / (m_waypoints.Count - 1);
                int segment = (int)Math.Floor(timeRatio / segmentTime);
                if (segment > m_waypoints.Count - 2)
                    segment = m_waypoints.Count - 2;
                double segmentRatio = (timeRatio - segment * segmentTime) / segmentTime;
                if (segment == 0)
                {
                    //first path segment
                    newPos = MathHelper.CalculateBezierPoint(segmentRatio,
                        m_waypoints[segment].Translation,
                        m_waypoints[segment].Translation,
                        m_waypoints[segment + 1].Translation - (m_waypoints[segment + 2].Translation - m_waypoints[segment].Translation) / 4,
                        m_waypoints[segment + 1].Translation);
                }
                else if (segment >= m_waypoints.Count - 2)
                {
                    //last path segment
                    newPos = MathHelper.CalculateBezierPoint(segmentRatio,
                        m_waypoints[segment].Translation,
                        m_waypoints[segment].Translation + (m_waypoints[segment + 1].Translation - m_waypoints[segment - 1].Translation) / 4,
                        m_waypoints[segment + 1].Translation,
                        m_waypoints[segment + 1].Translation);
                }
                else
                {
                    //middle path segment
                    newPos = MathHelper.CalculateBezierPoint(segmentRatio,
                        m_waypoints[segment].Translation,
                        m_waypoints[segment].Translation + (m_waypoints[segment + 1].Translation - m_waypoints[segment - 1].Translation) / 4,
                        m_waypoints[segment + 1].Translation - (m_waypoints[segment + 2].Translation - m_waypoints[segment].Translation) / 4,
                        m_waypoints[segment + 1].Translation);
                }
            }
            else if (m_nodeStartMatrix.Translation != m_nodeEndMatrix.Translation)
            {
                newPos = new Vector3D(
                    MathHelper.SmoothStep(m_nodeStartMatrix.Translation.X, m_nodeEndMatrix.Translation.X, timeRatio),
                    MathHelper.SmoothStep(m_nodeStartMatrix.Translation.Y, m_nodeEndMatrix.Translation.Y, timeRatio),
                    MathHelper.SmoothStep(m_nodeStartMatrix.Translation.Z, m_nodeEndMatrix.Translation.Z, timeRatio));
            }

            //end rotation
            if (m_rotateTarget != null)
                m_nodeEndMatrix = MatrixD.CreateLookAtInverse(m_currentCameraMatrix.Translation, m_rotateTarget.PositionComp.GetPosition(), m_nodeStartMatrix.Up);

            //update rotation
            if (m_lookTarget != null)
            {
                if (!m_lookTarget.Closed)
                    m_currentCameraMatrix = MatrixD.CreateLookAtInverse(newPos, m_lookTarget.PositionComp.GetPosition(), m_waypoints.Count > 2 ? m_lastUpVector : m_currentCameraMatrix.Up);
            }
            else if (m_attachedRotationTo != null)
            {
                m_currentCameraMatrix = m_attachedRotationOffset * m_attachedRotationTo.WorldMatrix;
            }
            else if (m_waypoints.Count > 2)
            {
                float segmentTime = 1f / (m_waypoints.Count - 1);
                int segment = (int)Math.Floor(timeRatio / segmentTime);
                if (segment > m_waypoints.Count - 2)
                    segment = m_waypoints.Count - 2;
                float segmentRatio = (timeRatio - segment * segmentTime) / segmentTime;

                QuaternionD quat1 = QuaternionD.CreateFromRotationMatrix(m_waypoints[segment]);
                QuaternionD quat2 = QuaternionD.CreateFromRotationMatrix(m_waypoints[segment + 1]);
                QuaternionD res = QuaternionD.Slerp(quat1, quat2, MathHelper.SmoothStepStable((double)segmentRatio));
                m_currentCameraMatrix = MatrixD.CreateFromQuaternion(res);
            }
            else if (!m_nodeStartMatrix.EqualsFast(ref m_nodeEndMatrix))
            {
                QuaternionD quat1 = QuaternionD.CreateFromRotationMatrix(m_nodeStartMatrix);
                QuaternionD quat2 = QuaternionD.CreateFromRotationMatrix(m_nodeEndMatrix);
                QuaternionD res = QuaternionD.Slerp(quat1, quat2, MathHelper.SmoothStepStable((double)timeRatio));
                m_currentCameraMatrix = MatrixD.CreateFromQuaternion(res);
            }
            m_currentCameraMatrix.Translation = newPos;

            //FOV
            if (m_currentNode.ChangeFOVTo > MINIMUM_FOV)
                m_currentFOV = MathHelper.SmoothStep(m_nodeStartFOV, MathHelper.Clamp(m_currentNode.ChangeFOVTo, MINIMUM_FOV, MAXIMUM_FOV), timeRatio);
            m_cameraEntity.FOV = m_currentFOV;

            //next node
            if (m_currentTime >= m_currentNode.Time)
                CutsceneNext(false);
        }

        public void CutsceneEnd(bool releaseCamera = true)
        {
            if (m_currentCutscene != null)
            {
                if (MyVisualScriptLogicProvider.CutsceneEnded != null)
                    MyVisualScriptLogicProvider.CutsceneEnded(m_currentCutscene.Name);
                m_currentCutscene = null;
                if (releaseCamera)
                {
                    m_cameraEntity.FOV = MathHelper.ToDegrees(MySandboxGame.Config.FieldOfView);
                    m_releaseCamera = true;
                }
                MyHudCameraOverlay.Enabled = m_overlayEnabled;
            }
        }

        public void CutsceneNext(bool setToZero)
        {
            //finish update

            m_nodeActivated = false;
            m_currentNodeIndex++;
            m_currentTime -= setToZero ? m_currentTime : m_currentNode.Time;
        }

        public void CutsceneSkip()
        {
            if (m_currentCutscene != null)
            {
                if (m_currentCutscene.CanBeSkipped)
                {
                    if (m_currentCutscene.FireEventsDuringSkip && MyVisualScriptLogicProvider.CutsceneNodeEvent != null)
                    {
                        if (m_currentNode != null && m_currentNode.EventDelay > 0 && m_eventDelay != float.MaxValue)
                            MyVisualScriptLogicProvider.CutsceneNodeEvent(m_currentNode.Event);
                        for (int i = m_currentNodeIndex + 1; i < m_currentCutscene.SequenceNodes.Length; i++)
                        {
                            if (!string.IsNullOrEmpty(m_currentCutscene.SequenceNodes[i].Event))
                                MyVisualScriptLogicProvider.CutsceneNodeEvent(m_currentCutscene.SequenceNodes[i].Event);
                        }
                    }
                    m_currentNodeIndex = m_currentCutscene.SequenceNodes.Length;
                    MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                }
                else
                {
                    MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
                }
            }
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            m_cutsceneLibrary.Clear();
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            m_objectBuilder.Cutscenes = new Cutscene[m_cutsceneLibrary.Count];
            int i = 0;
            foreach (Cutscene cutscene in m_cutsceneLibrary.Values)
            {
                m_objectBuilder.Cutscenes[i] = cutscene;
                i++;
            }
            return m_objectBuilder;
        }

        public bool PlayCutscene(string cutsceneName)
        {
            MySandboxGame.Log.WriteLineAndConsole("Cutscene start: " + cutsceneName);
            if (m_cutsceneLibrary.ContainsKey(cutsceneName))
            {
                if (IsCutsceneRunning)
                    CutsceneEnd(false);

                m_currentCutscene = m_cutsceneLibrary[cutsceneName];
                m_currentNode = null;
                m_currentNodeIndex = 0;
                m_currentTime = 0;
                m_nodeActivated = false;
                m_lookTarget = null;
                m_attachedPositionTo = null;
                m_attachedRotationTo = null;
                m_rotateTarget = null;
                m_moveTarget = null;
                m_currentFOV = MathHelper.Clamp(m_currentCutscene.StartingFOV, MINIMUM_FOV, MAXIMUM_FOV);
                MyGuiScreenGamePlay.DisableInput = true;
                if (MyCubeBuilder.Static.IsActivated)
                    MyCubeBuilder.Static.Deactivate();
                MyHud.CutsceneHud = true;
                m_overlayEnabled = MyHudCameraOverlay.Enabled;
                MyHudCameraOverlay.Enabled = false;

                MatrixD startMatrix = MatrixD.Identity;
                MyEntity entity = m_currentCutscene.StartEntity.Length > 0 ? MyVisualScriptLogicProvider.GetEntityByName(m_currentCutscene.StartEntity) : null;
                if (entity != null)
                    startMatrix = entity.WorldMatrix;

                if (m_currentCutscene.StartLookAt.Length > 0 && !m_currentCutscene.StartLookAt.Equals(m_currentCutscene.StartEntity))
                {
                    entity = MyVisualScriptLogicProvider.GetEntityByName(m_currentCutscene.StartLookAt);
                    if (entity != null)
                        startMatrix = MatrixD.CreateLookAtInverse(startMatrix.Translation, entity.PositionComp.GetPosition(), startMatrix.Up);
                }
                m_nodeStartMatrix = startMatrix;
                m_currentCameraMatrix = startMatrix;
                m_originalCameraController = MySession.Static.CameraController;
                m_cameraEntity.WorldMatrix = startMatrix;
                MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, m_cameraEntity);

                return true;
            }
            else
            {
                Debug.Fail("There is no cutscene named \"" + cutsceneName + "\".");
                CutsceneEnd();
                return false;
            }
        }

        public Dictionary<string, Cutscene> GetCutscenes()
        {
            return m_cutsceneLibrary;
        }

        public Cutscene GetCutscene(string name)
        {
            return m_cutsceneLibrary[name];
        }

    }
}

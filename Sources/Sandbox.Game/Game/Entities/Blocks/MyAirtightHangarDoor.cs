using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

using Havok;
using VRage.Components;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_AirtightHangarDoor))]
    class MyAirtightHangarDoor : MyAirtightDoorGeneric
    {
        List<MyEntitySubpart> m_subparts = new List<MyEntitySubpart>(4);

        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            base.Init(builder, cubeGrid);
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            InitSubparts();
            UpdateDoorPosition();
        }
        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
        }

        protected override void UpdateDoorPosition()
        {
            if (this.CubeGrid.Physics == null)
                return;

            float opening = (m_currOpening - 1) * m_subparts.Count * m_subpartMovementDistance;
            float maxOpening = 0;
            foreach (var subpart in m_subparts)
            {
                maxOpening -= m_subpartMovementDistance;
                if (subpart != null && subpart.Physics != null)
                {
                    subpart.PositionComp.LocalMatrix = Matrix.CreateTranslation(new Vector3(0f, (opening < maxOpening ? maxOpening : opening), 0f));
                    if (subpart.Physics.LinearVelocity != this.CubeGrid.Physics.LinearVelocity)
                        subpart.Physics.LinearVelocity = this.CubeGrid.Physics.LinearVelocity;

                    if (subpart.Physics.AngularVelocity != this.CubeGrid.Physics.AngularVelocity)
                        subpart.Physics.AngularVelocity = this.CubeGrid.Physics.AngularVelocity;
                }
            }
        }
        private void InitSubparts()
        {
            if (!CubeGrid.CreatePhysics)
                return;
            int i = 1;
            StringBuilder partName = new StringBuilder();
            MyEntitySubpart foundPart;
            m_subparts.Clear();
            while (true)
            {
                partName.Clear().Append("HangarDoor_door").Append(i++);
                Subparts.TryGetValue(partName.ToString(), out foundPart);
                if (foundPart == null)
                    break;
                m_subparts.Add(foundPart);
            };

            UpdateDoorPosition();

            if (CubeGrid.Projector != null)
            {
                //This is a projected grid, don't add collisions for subparts
                return;
            }
            foreach (var subpart in m_subparts)
            {
                if (subpart != null && subpart.Physics == null)
                {
                    if ((subpart.ModelCollision.HavokCollisionShapes != null) && (subpart.ModelCollision.HavokCollisionShapes.Length > 0))
                    {
                        var shape = subpart.ModelCollision.HavokCollisionShapes[0];
                        subpart.Physics = new Engine.Physics.MyPhysicsBody(subpart, RigidBodyFlag.RBF_DOUBLED_KINEMATIC | RigidBodyFlag.RBF_KINEMATIC);
                        subpart.Physics.IsPhantom = false;
                        Vector3 center = subpart.PositionComp.LocalVolume.Center;
                        subpart.Physics.CreateFromCollisionObject(shape, center, WorldMatrix, null, MyPhysics.KinematicDoubledCollisionLayer);
                        subpart.Physics.Enabled = true;
                    }
                }
            }

            CubeGrid.OnHavokSystemIDChanged -= CubeGrid_HavokSystemIDChanged;
            CubeGrid.OnHavokSystemIDChanged += CubeGrid_HavokSystemIDChanged;
            CubeGrid.OnPhysicsChanged -= CubeGrid_OnPhysicsChanged;
            CubeGrid.OnPhysicsChanged += CubeGrid_OnPhysicsChanged;
            if (CubeGrid.Physics!=null)
                UpdateHavokCollisionSystemID(CubeGrid.Physics.HavokCollisionSystemID);
        }

        void CubeGrid_OnPhysicsChanged(MyEntity obj)
        {
            if (m_subparts == null || m_subparts.Count == 0)
            {
                return;
            }

            if (m_subparts[0].Physics == null)
            {
                return;
            }
            if (obj.Physics != null &&obj.Physics.HavokCollisionSystemID != m_subparts[0].Physics.HavokCollisionSystemID)
                UpdateHavokCollisionSystemID(obj.Physics.HavokCollisionSystemID);
        }

        internal void UpdateHavokCollisionSystemID(int HavokCollisionSystemID)
        {
            foreach (var subpart in m_subparts)
            {
                if (subpart != null && subpart.Physics != null)
                {
                    if ((subpart.ModelCollision.HavokCollisionShapes != null) && (subpart.ModelCollision.HavokCollisionShapes.Length > 0))
                    {
                        var info = HkGroupFilter.CalcFilterInfo(MyPhysics.KinematicDoubledCollisionLayer, HavokCollisionSystemID, 1, 1);
                        subpart.Physics.RigidBody.SetCollisionFilterInfo(info);
                        if (subpart.Physics.HavokWorld != null)
                            subpart.Physics.HavokWorld.RefreshCollisionFilterOnEntity(subpart.Physics.RigidBody);
                        if (subpart.Physics.RigidBody2 != null)
                        {
                            info = HkGroupFilter.CalcFilterInfo(MyPhysics.DynamicDoubledCollisionLayer, HavokCollisionSystemID, 1, 1);
                            subpart.Physics.RigidBody2.SetCollisionFilterInfo(info);
                            if (subpart.Physics.HavokWorld != null)
                                subpart.Physics.HavokWorld.RefreshCollisionFilterOnEntity(subpart.Physics.RigidBody2);
                        }

                        /*if (this.CubeGrid.Physics != null && this.CubeGrid.Physics.HavokWorld != null)
                        {
                            this.CubeGrid.Physics.HavokWorld.RefreshCollisionFilterOnEntity(m_subpartDoor1.Physics.RigidBody);
                            this.CubeGrid.Physics.HavokWorld.RefreshCollisionFilterOnEntity(m_subpartDoor1.Physics.RigidBody2);
                        }*/
                    }
                }
            }
        }

        public override void OnBuildSuccess(long builtBy) 
        {
            if (CubeGrid.Physics != null)
            {
                UpdateHavokCollisionSystemID(CubeGrid.Physics.HavokCollisionSystemID);
            }
            base.OnBuildSuccess(builtBy);
        }

        public override void OnCubeGridChanged(MyCubeGrid oldGrid)
        {
            oldGrid.OnHavokSystemIDChanged -= CubeGrid_HavokSystemIDChanged;
            CubeGrid.OnHavokSystemIDChanged += CubeGrid_HavokSystemIDChanged;
            oldGrid.OnPhysicsChanged -= CubeGrid_OnPhysicsChanged;
            CubeGrid.OnPhysicsChanged += CubeGrid_OnPhysicsChanged; 
            if (CubeGrid.Physics != null)//when splitting blocks or creating new, then this is null and IDs are set from grit activation
                                       //when merging blocks, however, no grid is activated, so IDs must be changed from here
                UpdateHavokCollisionSystemID(CubeGrid.Physics.HavokCollisionSystemID);
            base.OnCubeGridChanged(oldGrid);
        }

        void CubeGrid_HavokSystemIDChanged(int id)
        {
            if (CubeGrid.Physics != null)
            {
                UpdateHavokCollisionSystemID(CubeGrid.Physics.HavokCollisionSystemID);
            }
        }

        protected override void Closing()
        {
            CubeGrid.OnHavokSystemIDChanged -= CubeGrid_HavokSystemIDChanged;
            base.Closing();
        }
    }
}

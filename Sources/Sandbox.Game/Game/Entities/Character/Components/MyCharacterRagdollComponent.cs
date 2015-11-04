using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Components;
using VRage.FileSystem;
using VRageMath;

namespace Sandbox.Game.Entities.Character.Components
{
    public class MyCharacterRagdollComponent : MyCharacterComponent
    {
        public MyRagdollMapper RagdollMapper;
        private IMyGunObject<Weapons.MyDeviceBase> m_previousWeapon;
        private MyPhysicsBody m_previousPhysics;

        public bool ResetJetpackRagdoll { get; set; }

        public bool IsRagdollActivated
        {
            get
            {
                if (Character.Physics == null) return false;
                return Character.Physics.IsRagdollModeActive;
            }
        }

        /// <summary>
        /// Loads Ragdoll data
        /// </summary>
        /// <param name="ragDollFile"></param>
        public bool InitRagdoll()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("RagdollComponent.InitRagdoll");
            if (Character.Physics.Ragdoll != null)
            {
                Character.Physics.CloseRagdollMode();
                Character.Physics.Ragdoll.ResetToRigPose();
                
                Character.Physics.Ragdoll.SetToKeyframed();
                return true;
            }

            Character.Physics.Ragdoll = new HkRagdoll();

            bool dataLoaded = false;

            if (Character.Model.HavokData != null && Character.Model.HavokData.Length > 0)
            {
                try
                {
                    dataLoaded = Character.Physics.Ragdoll.LoadRagdollFromBuffer(Character.Model.HavokData);
                }
                catch (Exception e)
                {
                    Debug.Fail("Error loading ragdoll from buffer: " + e.Message);
                    Character.Physics.CloseRagdoll();
                    Character.Physics.Ragdoll = null;
                }
            }
            else if (Character.Definition.RagdollDataFile != null)
            {
                String ragDollFile = System.IO.Path.Combine(MyFileSystem.ContentPath, Character.Definition.RagdollDataFile);
                if (System.IO.File.Exists(ragDollFile))
                {
                    dataLoaded = Character.Physics.Ragdoll.LoadRagdollFromFile(ragDollFile);
                }
                else
                {
                    System.Diagnostics.Debug.Fail("Cannot find ragdoll file: " + ragDollFile);
                }
            }

            if (Character.Definition.RagdollRootBody != String.Empty)
            {
                if (!Character.Physics.Ragdoll.SetRootBody(Character.Definition.RagdollRootBody))
                {
                    Debug.Fail("Can not set root body with name: " + Character.Definition.RagdollRootBody + " on model " + Character.ModelName + ". Please check your definitions.");
                }
            }

            if (!dataLoaded)
            {
                Character.Physics.Ragdoll.Dispose();
                Character.Physics.Ragdoll = null;
            }
            foreach (var body in Character.Physics.Ragdoll.RigidBodies)
                body.UserObject = Character;

            if (Character.Physics.Ragdoll != null && MyPerGameSettings.Destruction) //scaling weights and IT
            {
                Character.Physics.Ragdoll.SetToDynamic();
                var mp = new HkMassProperties();
                foreach (var body in Character.Physics.Ragdoll.RigidBodies)
                {
                    mp.Mass = MyDestructionHelper.MassToHavok(body.Mass);
                    mp.InertiaTensor = Matrix.CreateScale(1.0f / 25.0f) * body.InertiaTensor;
                    body.SetMassProperties(ref mp);
                }
                Character.Physics.Ragdoll.SetToKeyframed();
            }

            if (Character.Physics.Ragdoll != null && MyFakes.ENABLE_RAGDOLL_DEFAULT_PROPERTIES)
            {
                Character.Physics.SetRagdollDefaults();
            }

            return dataLoaded;

        }

        public void InitRagdollMapper()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("RagdollComponent.InitRagdollMapper");
            if (Character.Bones.Count == 0) return;
            if (Character.Physics == null || Character.Physics.Ragdoll == null) return;

            RagdollMapper = new MyRagdollMapper(Character, Character.Bones);

            RagdollMapper.Init(Character.Definition.RagdollBonesMappings);
        }

        /// <summary>
        /// Sets the ragdoll pose to bones pose
        /// </summary> 
        private void UpdateRagdoll()
        {
            //if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("RagdollComponent.UpdateRagdoll");
            if (Character.Physics == null || Character.Physics.Ragdoll == null || RagdollMapper == null) return;
            if (!MyPerGameSettings.EnableRagdollModels) return;          

            if (!RagdollMapper.IsActive || !Character.Physics.IsRagdollModeActive) return;

            if (!RagdollMapper.IsKeyFramed && !RagdollMapper.IsPartiallySimulated) return;

            RagdollMapper.UpdateRagdollPosition();
            RagdollMapper.UpdateRagdollPose();
            RagdollMapper.SetVelocities();

            RagdollMapper.DebugDraw(Character.WorldMatrix);
        }

        private void ActivateJetpackRagdoll()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("RagdollComponent.ActivateJetpackRagdoll");
            if (RagdollMapper == null || Character.Physics == null || Character.Physics.Ragdoll == null) return;
            if (!MyPerGameSettings.EnableRagdollModels) return;
            if (!MyPerGameSettings.EnableRagdollInJetpack) return;
            if (Character.Physics.HavokWorld == null) return;

            List<string> bodies = new List<string>();
            string[] bodiesArray;

            if (Character.CurrentWeapon == null)
            {
                if (Character.Definition.RagdollPartialSimulations.TryGetValue("Jetpack", out bodiesArray))
                {
                    bodies.AddArray(bodiesArray);
                }
                else
                {
                    // Fallback if missing definitions
                    bodies.Add("Ragdoll_SE_rig_LUpperarm001");
                    bodies.Add("Ragdoll_SE_rig_LForearm001");
                    bodies.Add("Ragdoll_SE_rig_LPalm001");
                    bodies.Add("Ragdoll_SE_rig_RUpperarm001");
                    bodies.Add("Ragdoll_SE_rig_RForearm001");
                    bodies.Add("Ragdoll_SE_rig_RPalm001");

                    bodies.Add("Ragdoll_SE_rig_LThigh001");
                    bodies.Add("Ragdoll_SE_rig_LCalf001");
                    bodies.Add("Ragdoll_SE_rig_LFoot001");
                    bodies.Add("Ragdoll_SE_rig_RThigh001");
                    bodies.Add("Ragdoll_SE_rig_RCalf001");
                    bodies.Add("Ragdoll_SE_rig_RFoot001");
                }
            }
            else
            {
                if (Character.Definition.RagdollPartialSimulations.TryGetValue("Jetpack_Weapon", out bodiesArray))
                {
                    bodies.AddArray(bodiesArray);
                }
                else
                {
                    bodies.Add("Ragdoll_SE_rig_LThigh001");
                    bodies.Add("Ragdoll_SE_rig_LCalf001");
                    bodies.Add("Ragdoll_SE_rig_LFoot001");
                    bodies.Add("Ragdoll_SE_rig_RThigh001");
                    bodies.Add("Ragdoll_SE_rig_RCalf001");
                    bodies.Add("Ragdoll_SE_rig_RFoot001");
                }
            }

            List<int> simulatedBodies = new List<int>();

            foreach (var body in bodies)
            {
                simulatedBodies.Add(RagdollMapper.BodyIndex(body));
            }

            if (!Character.Physics.IsRagdollModeActive)
            {
                Character.Physics.SwitchToRagdollMode(false);                
            }

            RagdollMapper.ResetRagdollVelocities();

            if (Character.Physics.IsRagdollModeActive)
            {
                RagdollMapper.ActivatePartialSimulation(simulatedBodies);
            }

            // This is hack, ragdoll in jetpack sometimes can't settle and simulation is broken, if we find another way how to avoid that, this can be disabled
            if (!MyFakes.ENABLE_JETPACK_RAGDOLL_COLLISIONS)
            {
                // Because after cluster's reorder, the bodies can collide!
                Character.Physics.DisableRagdollBodiesCollisions();
            }            
        }

        private void DeactivateJetpackRagdoll()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("RagdollComponent.DeactivateJetpackRagdoll");
            if (RagdollMapper == null || Character.Physics == null || Character.Physics.Ragdoll == null) return;
            if (!MyPerGameSettings.EnableRagdollModels) return;           
            if (!MyPerGameSettings.EnableRagdollInJetpack) return;

            if (RagdollMapper.IsPartiallySimulated)
            {
                RagdollMapper.DeactivatePartialSimulation();
            }

            if (Character.Physics.IsRagdollModeActive)
            {
                Character.Physics.CloseRagdollMode();
            }
        }

        /// <summary>
        /// Sets the bones pose to ragdoll pose
        /// </summary>
        private void SimulateRagdoll()
        {
            //if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("RagdollComponent.SimulateRagdoll");
            if (!MyPerGameSettings.EnableRagdollModels) return;
            if (Character.Physics == null || RagdollMapper == null) return;

            if (Character.Physics.Ragdoll == null || !Character.Physics.Ragdoll.InWorld || !RagdollMapper.IsActive) return;

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Bones To Ragdoll");

            try
            {
                RagdollMapper.UpdateRagdollAfterSimulation();

                if (!Character.IsCameraNear && !MyFakes.ENABLE_PERMANENT_SIMULATIONS_COMPUTATION) return;

                RagdollMapper.UpdateCharacterPose(Character.IsDead ? 1.0f : 0.1f, Character.IsDead ? 1.0f : 0.0f);

                RagdollMapper.DebugDraw(Character.WorldMatrix);
            }
            finally
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }

            // save bone changes
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Save bones and pos update");

            for (int i = 0; i < Character.Bones.Count; i++)
            {
                MyCharacterBone bone = Character.Bones[i];
                Character.BoneRelativeTransforms[i] = bone.ComputeBoneTransform();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public void InitDeadBodyPhysics()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("RagdollComponent.InitDeadBodyPhysics");
            if (Character.Physics.IsRagdollModeActive) Character.Physics.CloseRagdollMode();
            if (RagdollMapper.IsActive) RagdollMapper.Deactivate();
            Character.Physics.SwitchToRagdollMode();
            RagdollMapper.Activate();
            RagdollMapper.SetRagdollToKeyframed();
            RagdollMapper.UpdateRagdollPose();
            RagdollMapper.SetRagdollToDynamic();            
        }

        public void UpdateCharacterPhysics()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("RagdollComponent.UpdateCharacterPhysics");
            InitRagdoll();
            if ((Character.Definition.RagdollBonesMappings.Count > 1) && Character.Physics.Ragdoll != null)
            {
                InitRagdollMapper();
            }
        }


        #region Component Overrided Methods

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (Sync.IsServer && Character.IsDead && MyFakes.ENABLE_RAGDOLL_CLIENT_SYNC)
            {
                RagdollMapper.SyncRigidBodiesTransforms(Character.WorldMatrix);
            }
        }

        public override void UpdateBeforeSimulation()
        {
            //if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("RagdollComponent.UpdateBeforeSimulation");
            base.UpdateBeforeSimulation();

            // TODO: This should be changed so the ragdoll gets registered in the generators, now for SE, apply gravity explictly
            // Apply Gravity on Ragdoll 
            // OM: This should be called only in SE, in ME this is handled by world!
            if (Character.Physics.Ragdoll != null && Character.Physics.Ragdoll.InWorld && (!Character.Physics.Ragdoll.IsKeyframed || RagdollMapper.IsPartiallySimulated) && (MyPerGameSettings.Game == GameEnum.SE_GAME))
            {
                Vector3 gravity = MyGravityProviderSystem.CalculateTotalGravityInPoint(Character.PositionComp.WorldAABB.Center) + Character.Physics.HavokWorld.Gravity * MyCharacter.CHARACTER_GRAVITY_MULTIPLIER;
                Character.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, gravity * (MyPerGameSettings.Destruction ? MyDestructionHelper.MassToHavok(Character.Definition.Mass) : Character.Definition.Mass), null, null);                
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Ragdoll");
            UpdateRagdoll();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public override void UpdateAfterSimulation()
        {
            //if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("RagdollComponent.UpdateAfterSimulation");
            base.UpdateAfterSimulation();          
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Simulate Ragdoll");
            SimulateRagdoll();    
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            CheckChangesOnCharacter();
        }

        private void CheckChangesOnCharacter()
        {
            //if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("RagdollComponent.CheckChangesOnCharacter");
            if (MyPerGameSettings.EnableRagdollInJetpack)
            {
                if (Character.CurrentWeapon != m_previousWeapon)
                {
                    DeactivateJetpackRagdoll();
                    ActivateJetpackRagdoll();
                }
                m_previousWeapon = Character.CurrentWeapon;
                var movementState = Character.GetCurrentMovementState();
	            var jetpack = Character.JetpackComp;
                if ((jetpack != null && jetpack.TurnedOn) && movementState == Common.ObjectBuilders.MyCharacterMovementEnum.Flying && Character.Physics.Enabled)
                {
                    if (!IsRagdollActivated || !RagdollMapper.IsActive)
                    {
                        DeactivateJetpackRagdoll();
                        ActivateJetpackRagdoll();
                    }
                }
                else if (RagdollMapper != null && RagdollMapper.IsPartiallySimulated)
                {
                    DeactivateJetpackRagdoll();
                }
            }
            if (Character.Physics != m_previousPhysics)
            {
                UpdateCharacterPhysics();
            }

            m_previousPhysics = Character.Physics;

            if (Character.IsDead && !IsRagdollActivated && Character.Physics.Enabled)
            {
                InitDeadBodyPhysics();
            }            
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
        }

        public override string ComponentTypeDebugString
        {
            get { return "Character Ragdoll Component"; }
        }

        public override void OnAddedToContainer()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("RagdollComponent.OnAddedToContainer");
            base.OnAddedToContainer();
            NeedsUpdateAfterSimulation = true;
            NeedsUpdateBeforeSimulation = true;
            NeedsUpdateBeforeSimulation100 = true;

            if (Character.Physics != null && MyPerGameSettings.EnableRagdollModels && Character.Model.HavokData != null && Character.Model.HavokData.Length > 0)
            {
                if (InitRagdoll() && (Character.Definition.RagdollBonesMappings.Count > 1))
                {
                    InitRagdollMapper();
                }
            }
            else
            {
                Container.Remove<MyCharacterRagdollComponent>();
            }
        }

        #endregion
    }
}

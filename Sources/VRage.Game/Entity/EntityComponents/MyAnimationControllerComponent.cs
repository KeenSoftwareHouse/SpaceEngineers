using System;
using System.Collections.Generic;
using VRageRender.Animations;
using VRage.Game.SessionComponents;
using VRage.Utils;
using VRageMath;

namespace VRage.Game.Components
{
    /// <summary>
    /// Entity component containing animation controller.
    /// </summary>
    public class MyAnimationControllerComponent : MyEntityComponentBase
    {
        // Animation controller. Contains definition of animation layers (state machines, its nodes are animation trees). 
        private readonly VRageRender.Animations.MyAnimationController m_controller = new VRageRender.Animations.MyAnimationController();
        // Array of character bones (skinned entity bones with additional description).
        private VRageRender.Animations.MyCharacterBone[] m_characterBones;
        // Final matrices - relative.
        private Matrix[] m_boneRelativeTransforms;
        // Final matrices - absolute.
        private Matrix[] m_boneAbsoluteTransforms;

        // Reference to last bone result in raw form.
        private List<MyAnimationClip.BoneState> m_lastBoneResult;

        // Last frame actions - only used in non official build.
#if OFFICIAL_BUILD == false
        private List<MyStringId> m_lastFrameActions = null;
        private List<MyStringId> m_currentFrameActions = null;
#endif

        private bool m_componentValid = false;
        readonly FastResourceLock m_componentValidLock = new FastResourceLock();

        // Callback informing that we need to reload bones.
        public Action ReloadBonesNeeded;

        public event Action<MyStringId> ActionTriggered;

        // ------------------------------------------------------------------------

        /// <summary>
        /// Name of the component type for debug purposes (e.g.: "Position")
        /// </summary>
        public override string ComponentTypeDebugString { get { return "AnimationControllerComp"; } }

        /// <summary>
        /// Component was added in the entity component container.
        /// </summary>
        public override void OnAddedToContainer()
        {
            MySessionComponentAnimationSystem.Static.RegisterEntityComponent(this);
        }

        /// <summary>
        /// Component will be removed from entity component container.
        /// </summary>
        public override void OnBeforeRemovedFromContainer()
        {
            MySessionComponentAnimationSystem.Static.UnregisterEntityComponent(this);
        }

        public void MarkAsValid()
        {
            using (m_componentValidLock.AcquireExclusiveUsing())
            {
                m_componentValid = true;
            }
        }

        public void MarkAsInvalid()
        {
            using (m_componentValidLock.AcquireExclusiveUsing())
            {
                m_componentValid = false;
            }
        }

        // ------------------------------------------------------------------------

        /// <summary>
        /// Get the animation controller instance.
        /// </summary>
        public VRageRender.Animations.MyAnimationController Controller
        {
            get { return m_controller; }
        }

        /// <summary>
        /// Get the variable storage of animation controller instance. Shortcut.
        /// </summary>
        public MyAnimationVariableStorage Variables
        {
            get { return m_controller.Variables; }
        }

        /// <summary>
        /// Get reference to array of character bones and its contents.
        /// </summary>
        public MyCharacterBone[] CharacterBones
        {
            get { return m_characterBones; }
        }

        /// <summary>
        /// Get the instance of inverse kinematics.
        /// </summary>
        public MyAnimationInverseKinematics InverseKinematics
        {
            get { return m_controller.InverseKinematics; }
        }

        public VRageRender.Animations.MyCharacterBone[] CharacterBonesSorted { get; private set; }

        // Final matrices - relative.
        public Matrix[] BoneRelativeTransforms { get { return m_boneRelativeTransforms; } }
        // Final matrices - absolute.
        public Matrix[] BoneAbsoluteTransforms { get { return m_boneAbsoluteTransforms; } }
        // Last result in raw form. Allocated from internal pool -> this variable or its content may change during update.
        public List<MyAnimationClip.BoneState> LastRawBoneResult { get { return m_lastBoneResult; } }
        // Definition identifier (source of this controller component).
        public MyDefinitionId SourceId { get; set; }
        // Return all actions triggered in last frame.
        public List<MyStringId> LastFrameActions
        {
            get
            {
#if OFFICIAL_BUILD
                return null;
#else
                return m_lastFrameActions;
#endif
            }
        }

        public void SetCharacterBones(MyCharacterBone[] characterBones,
            Matrix[] relativeTransforms, Matrix[] abosoluteTransforms)
        {
            m_characterBones = characterBones;
            CharacterBonesSorted = new MyCharacterBone[m_characterBones.Length];
            Array.Copy(m_characterBones, CharacterBonesSorted, m_characterBones.Length);
            // sort the bones, deeper in hierarchy they are, later they are evaluated
            Array.Sort(CharacterBonesSorted, (x, y) => x.Depth.CompareTo(y.Depth));

            m_boneRelativeTransforms = relativeTransforms;
            m_boneAbsoluteTransforms = abosoluteTransforms;

            Controller.ResultBonesPool.Reset(m_characterBones);
        }

        // Update animation state (position and orientation of bones).
        // Called from MySessionComponentAnimationSystem.
        public void Update()
        {
            using (m_componentValidLock.AcquireSharedUsing())
            {
                if (!m_componentValid)
                    return;

                VRageRender.Animations.MyAnimationUpdateData updateData = new VRageRender.Animations.MyAnimationUpdateData();
                updateData.DeltaTimeInSeconds = VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                updateData.CharacterBones = m_characterBones;
                updateData.Controller = null;  // will be set inside m_controller.Update automatically if null
                updateData.BonesResult = null; // will be set inside m_controller.Update (result!)

                m_controller.Update(ref updateData);
                if (updateData.BonesResult != null)
                {
                    for (int i = 0; i < updateData.BonesResult.Count; i++)
                    {
                        CharacterBones[i].SetCompleteTransform(ref updateData.BonesResult[i].Translation,
                            ref updateData.BonesResult[i].Rotation);
                    }
                }
                m_lastBoneResult = updateData.BonesResult;

#if OFFICIAL_BUILD == false
                var helperLast = m_lastFrameActions;
                m_lastFrameActions = m_currentFrameActions;
                m_currentFrameActions = helperLast;
                if (m_currentFrameActions != null)
                    m_currentFrameActions.Clear();
#endif
            }
        }

        public void UpdateTransformations()
        {
            if (m_characterBones == null)
                return;

            MyCharacterBone.ComputeAbsoluteTransforms(CharacterBonesSorted);
        }

        public void UpdateInverseKinematics()
        {
            m_controller.UpdateInverseKinematics(ref m_characterBones);
        }

        // Find character bone having given name. If found, output parameter index is set.
        // Returns reference to the bone or null if not found.
        public VRageRender.Animations.MyCharacterBone FindBone(string name, out int index)
        {
            if (name != null)
            {
                for (int i = 0; i < m_characterBones.Length; i++)
                {
                    if (m_characterBones[i].Name == name)
                    {
                        index = i;
                        return m_characterBones[i];
                    }
                }
            }
            // vrage todo: MyFakes are not in VRage... what with it?
            //if (MyFakes.ENABLE_BONES_AND_ANIMATIONS_DEBUG)
            //{
            //    Debug.Fail("Warning! Bone with name: " + name + " was not found in the skeleton of model name: " + this.Model.AssetName + ". Pleace check your bone definitions in SBC file.");
            //}
            index = -1;
            return null;
        }

        /// <summary>
        /// Trigger an action in this layer. 
        /// If there is a transition having given (non-null) name, it is followed immediatelly.
        /// Conditions of transition are ignored.
        /// This is a shortcut to Controller.TriggerAction.
        /// </summary>
        public void TriggerAction(MyStringId actionName)
        {
            using (m_componentValidLock.AcquireSharedUsing())
            {
                if (m_componentValid)
                {
#if OFFICIAL_BUILD == false
                    if (m_currentFrameActions == null)
                        m_currentFrameActions = new List<MyStringId>(16);
                    m_currentFrameActions.Add(actionName);
#endif
                    Controller.TriggerAction(actionName);

                    if(ActionTriggered != null)
                        ActionTriggered(actionName);
                }
            }
        }

        public void Clear()
        {
            MarkAsInvalid();
            InverseKinematics.Clear();
            Controller.DeleteAllLayers();
            Controller.Variables.Clear();
            Controller.ResultBonesPool.Reset(null);
            m_characterBones = null;
            m_boneRelativeTransforms = null;
            m_boneAbsoluteTransforms = null;
            CharacterBonesSorted = null;
        }
    }
}

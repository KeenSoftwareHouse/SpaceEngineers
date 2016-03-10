using System.Collections.Generic;
using VRage.Animations;
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
        private VRage.Animations.MyAnimationController m_controller = new VRage.Animations.MyAnimationController();
        // Array of character bones (skinned entity bones with additional description).
        private VRage.Animations.MyCharacterBone[] m_characterBones;
        // Final matrices - relative.
        private Matrix[] m_boneRelativeTransforms;
        // Final matrices - absolute.
        private Matrix[] m_boneAbsoluteTransforms;

        // Reference to last bone result in raw form.
        private List<MyAnimationClip.BoneState> m_lastBoneResult;

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

        // ------------------------------------------------------------------------

        /// <summary>
        /// Get the animation controller instance.
        /// </summary>
        public VRage.Animations.MyAnimationController Controller
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
        /// Get or set reference to array of character bones and its contents.
        /// </summary>
        public VRage.Animations.MyCharacterBone[] CharacterBones
        {
            get { return m_characterBones; }
            set 
            { 
                m_characterBones = value;
                if (value != null)
                {
                    m_boneRelativeTransforms = new Matrix[value.Length];
                    m_boneAbsoluteTransforms = new Matrix[value.Length];
                    for (int i = 0; i < value.Length; i++)
                    {
                        m_boneRelativeTransforms[i] = Matrix.Identity;
                        m_boneAbsoluteTransforms[i] = Matrix.Identity;
                    }
                    Controller.ResultBonesPool.Reset(m_characterBones);
                }
            }
        }

        // Final matrices - relative.
        public Matrix[] BoneRelativeTransforms { get { return m_boneRelativeTransforms; } }
        // Final matrices - absolute.
        public Matrix[] BoneAbsoluteTransforms { get { return m_boneAbsoluteTransforms; } }
        // Last result in raw form. Allocated from internal pool -> this variable or its content may change during update.
        public List<MyAnimationClip.BoneState> LastRawBoneResult { get { return m_lastBoneResult; } }

        // ------------------------------------------------------------------------

        // Update animation state (position and orientation of bones).
        // Called from MySessionComponentAnimationSystem.
        public void Update()
        {
            VRage.Animations.MyAnimationUpdateData updateData = new Animations.MyAnimationUpdateData();
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
        }

        // Find character bone having given name. If found, output parameter index is set.
        // Returns reference to the bone or null if not found.
        public VRage.Animations.MyCharacterBone FindBone(string name, out int index)
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
            Controller.TriggerAction(actionName);
        }

        public void Clear()
        {
            Controller.DeleteAllLayers();
            Controller.Variables.Clear();
            Controller.ResultBonesPool.Reset(null);
            CharacterBones = null;
        }
    }
}

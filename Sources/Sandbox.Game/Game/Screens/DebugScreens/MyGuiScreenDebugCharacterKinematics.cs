using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using VRage;
using VRageMath;
using VRage.Utils;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Character.Components;

namespace Sandbox.Game.Screens.DebugScreens
{
    [MyDebugScreen("Render", "Character kinematics")]
    class MyGuiScreenDebugCharacterKinematics : MyGuiScreenDebugBase
    {

        MyGuiControlSlider belowReachableDistance;
        MyGuiControlSlider aboveReachableDistance;
        MyGuiControlSlider verticalChangeUpGain;
        MyGuiControlSlider verticalChangeDownGain;
        MyGuiControlSlider ankleHeight;
        MyGuiControlSlider footWidth;
        MyGuiControlSlider footLength;
        MyGuiControlCombobox characterMovementStateCombo;
        MyGuiControlCheckbox enabledIKState;
        public static bool ikSettingsEnabled;
        MyFeetIKSettings ikSettings;
        public bool updating = false;

        public MyRagdollMapper PlayerRagdollMapper
        {
            get
            {
                MyCharacter playerCharacter = MySession.LocalCharacter;
                var ragdollComponent = playerCharacter.Components.Get<MyCharacterRagdollComponent>();
                if (ragdollComponent == null) return null;
                return ragdollComponent.RagdollMapper;
            }
        }
        

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugCharacterKinematics";
        }

        public MyGuiScreenDebugCharacterKinematics()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);
            m_currentPosition.Y += 0.01f;
            m_scale = 0.7f;

            AddCaption("Character kinematics debug draw", Color.Yellow.ToVector4());
            AddShareFocusHint();

            AddCheckBox("Enable permanent IK/Ragdoll simulation ", null, MemberHelper.GetMember(() => MyFakes.ENABLE_PERMANENT_SIMULATIONS_COMPUTATION));

            AddCheckBox("Draw IK Settings ", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_SETTINGS));
            AddCheckBox("Draw ankle final position", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_ANKLE_FINALPOS));
            AddCheckBox("Draw raycast lines and foot lines", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_RAYCASTLINE));
            AddCheckBox("Draw bones", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_BONES));
            AddCheckBox("Draw raycast hits", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_RAYCASTHITS));
            AddCheckBox("Draw ankle desired positions", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_ANKLE_DESIREDPOSITION));
            AddCheckBox("Draw closest support position", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_CLOSESTSUPPORTPOSITION));
            AddCheckBox("Draw IK solvers debug", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_IKSOLVERS));
            AddCheckBox("Enable/Disable Feet IK", null, MemberHelper.GetMember(() => MyFakes.ENABLE_FOOT_IK));

            AddCheckBox("Draw Ragdoll Rig Pose", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_ORIGINAL_RIG));
            AddCheckBox("Draw Bones Rig Pose", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_BONES_ORIGINAL_RIG));
            AddCheckBox("Draw Ragdoll Pose", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_POSE));
            AddCheckBox("Draw Bones", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_COMPUTED_BONES));
            AddCheckBox("Draw bones intended transforms", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_BONES_DESIRED));
            AddCheckBox("Draw Hip Ragdoll and Char. Position", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_HIPPOSITIONS));

            AddCheckBox("Enable Ragdoll", null, MemberHelper.GetMember(() => MyPerGameSettings.EnableRagdollModels));
            AddCheckBox("Enable Ragdoll Animation", null, MemberHelper.GetMember(() => MyFakes.ENABLE_RAGDOLL_ANIMATION));
            AddCheckBox("Enable Bones Translation", null, MemberHelper.GetMember(() => MyFakes.ENABLE_RAGDOLL_BONES_TRANSLATION));
            
            // MW:TODO change it
            //AddSlider("Ragdoll simulation time", 10f, 20*60f, () => MyPerGameSettings.CharacterDefaultLootingCounter, (x) => MyPerGameSettings.CharacterDefaultLootingCounter=x); 
            

            StringBuilder caption = new StringBuilder("Kill Ragdoll");
            AddButton(caption, killRagdollAction);

            StringBuilder captionActivate = new StringBuilder("Activate Ragdoll");
            AddButton(captionActivate, activateRagdollAction);

            StringBuilder captionRagdollDynamic = new StringBuilder("Switch to Dynamic / Keyframed");
            AddButton(captionRagdollDynamic, switchRagdoll);

            //characterMovementStateCombo = AddCombo<Sandbox.Common.ObjectBuilders.MyCharacterMovementEnum>(null,  MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_MOVEMENT_STATE));
            enabledIKState = AddCheckBox("Enable IK for this state", null, MemberHelper.GetMember(() => MyGuiScreenDebugCharacterKinematics.ikSettingsEnabled));
            belowReachableDistance = AddSlider("Reachable distance below character", 0f, 0f, 2f, null);           
            aboveReachableDistance = AddSlider("Reachable distance above character", 0f, 0f, 2f, null);
            verticalChangeUpGain = AddSlider("Shift Up Gain", 0.1f, 0f, 1f, null);
            verticalChangeDownGain = AddSlider("Sift Down Gain", 0.1f, 0f, 1f, null);
            ankleHeight = AddSlider("Ankle height", 0.1f, 0.001f, 0.3f, null);
            footWidth = AddSlider("Foot width", 0.1f, 0.001f, 0.3f, null);
            footLength = AddSlider("Foot length", 0.3f, 0.001f, 0.2f, null);
            RegisterEvents();
        }

        private void switchRagdoll(MyGuiControlButton obj)
        {
            MyCharacter playerCharacter = MySession.LocalCharacter;
            if (PlayerRagdollMapper.IsActive)
            {
                if (playerCharacter.Physics.Ragdoll.IsKeyframed)
                {
                    playerCharacter.Physics.Ragdoll.EnableConstraints();
                    PlayerRagdollMapper.SetRagdollToDynamic();                    
                }
                else
                {
                    playerCharacter.Physics.Ragdoll.DisableConstraints();
                    PlayerRagdollMapper.SetRagdollToKeyframed();
                }
                
            }
        }

        private void activateRagdollAction(MyGuiControlButton obj)
        {
            MyCharacter playerCharacter = MySession.LocalCharacter;
            if (PlayerRagdollMapper == null)
            {
                var component = new MyCharacterRagdollComponent();
                playerCharacter.Components.Add<MyCharacterRagdollComponent>(component);
                component.InitRagdoll();
            }
            if (PlayerRagdollMapper.IsActive) PlayerRagdollMapper.Deactivate();
            //playerCharacter.RagdollMapper.Activate(playerCharacter.Physics.HavokWorld, MyPhysics.CollisionLayerWithoutCharacter, playerCharacter.Physics.CharacterSystemGroupCollisionFilterID);
            //m_playerCharacter.Physics.CharacterProxy.SwitchToRagdollMode(m_playerCharacter.Physics.HavokWorld, m_playerCharacter.WorldMatrix);
            //m_playerCharacter.Physics.CharacterProxy.SwitchToRagdollMode(m_playerCharacter.Physics.HavokWorld);
            playerCharacter.Physics.SwitchToRagdollMode(false);
            PlayerRagdollMapper.Activate();
            PlayerRagdollMapper.SetRagdollToKeyframed();
            playerCharacter.Physics.Ragdoll.DisableConstraints();

        }

        private void killRagdollAction(MyGuiControlButton obj)
        {
            MyCharacter playerCharacter = MySession.LocalCharacter;
            //m_playerCharacter.Physics.CharacterProxy.RagdollMapper.SetRagdollToDynamic();
            //m_playerCharacter.Physics.CharacterProxy.RagdollMapper.EnableRagdollConstraints();
            MyFakes.CHARACTER_CAN_DIE_EVEN_IN_CREATIVE_MODE = true;
            playerCharacter.DoDamage(1000000, MyDamageType.Suicide, true);
            //MyFakes.CHARACTER_CAN_DIE_EVEN_IN_CREATIVE_MODE = false;
        }

        void ItemChanged(MyGuiControlSlider slider)
        {
            if (updating) return;

            ikSettings.Enabled = enabledIKState.IsChecked;
            ikSettings.BelowReachableDistance = belowReachableDistance.Value;
            ikSettings.AboveReachableDistance = aboveReachableDistance.Value;
            ikSettings.VerticalShiftUpGain = verticalChangeUpGain.Value;
            ikSettings.VerticalShiftDownGain = verticalChangeDownGain.Value;
            ikSettings.FootSize.Y = ankleHeight.Value;
            ikSettings.FootSize.X = footWidth.Value;
            ikSettings.FootSize.Z = footLength.Value;

            MyCharacter playerCharacter = MySession.LocalCharacter;

            MyCharacterMovementEnum selected = MyCharacterMovementEnum.Standing;//(MyCharacterMovementEnum)characterMovementStateCombo.GetSelectedKey();
            playerCharacter.Definition.FeetIKSettings[selected] = ikSettings;
        }

        void RegisterEvents()
        {
            //characterMovementStateCombo.ItemSelected += characterMovementStateCombo_ItemSelected;
            belowReachableDistance.ValueChanged += ItemChanged;
            aboveReachableDistance.ValueChanged += ItemChanged;
            verticalChangeUpGain.ValueChanged += ItemChanged;
            verticalChangeDownGain.ValueChanged += ItemChanged;
            ankleHeight.ValueChanged += ItemChanged;
            footWidth.ValueChanged += ItemChanged;
            footLength.ValueChanged += ItemChanged;
            enabledIKState.IsCheckedChanged += IsCheckedChanged;
        }

        private void IsCheckedChanged(MyGuiControlCheckbox obj)
        {
            ItemChanged(null);
        }

        private void characterMovementStateCombo_ItemSelected()
        {            
            
            MyCharacter playerCharacter = MySession.LocalCharacter;

            MyCharacterMovementEnum selected = (MyCharacterMovementEnum)characterMovementStateCombo.GetSelectedKey();

            MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_MOVEMENT_STATE = selected;

            if (!playerCharacter.Definition.FeetIKSettings.TryGetValue(selected, out ikSettings))
            {
                ikSettings = new MyFeetIKSettings();
                ikSettings.Enabled = false;
                ikSettings.AboveReachableDistance = 0.1f;
                ikSettings.BelowReachableDistance = 0.1f;
                ikSettings.VerticalShiftDownGain = 0.1f;
                ikSettings.VerticalShiftUpGain = 0.1f;
                ikSettings.FootSize = new Vector3(0.1f, 0.1f, 0.2f);
            }

            updating = true;

            enabledIKState.IsChecked = ikSettings.Enabled;
            belowReachableDistance.Value = ikSettings.BelowReachableDistance;
            aboveReachableDistance.Value = ikSettings.AboveReachableDistance;
            verticalChangeUpGain.Value = ikSettings.VerticalShiftUpGain;
            verticalChangeDownGain.Value = ikSettings.VerticalShiftDownGain;
            ankleHeight.Value = ikSettings.FootSize.Y;
            footWidth.Value = ikSettings.FootSize.X;
            footLength.Value = ikSettings.FootSize.Z;
            
            updating = false;
        }

       

        void UnRegisterEvents()
        {
            characterMovementStateCombo.ItemSelected -= characterMovementStateCombo_ItemSelected;
            belowReachableDistance.ValueChanged -= ItemChanged;
            aboveReachableDistance.ValueChanged -= ItemChanged;
            verticalChangeUpGain.ValueChanged -= ItemChanged;
            verticalChangeDownGain.ValueChanged -= ItemChanged;
            ankleHeight.ValueChanged -= ItemChanged;
            footWidth.ValueChanged -= ItemChanged;
            footLength.ValueChanged -= ItemChanged;
            enabledIKState.IsCheckedChanged -= IsCheckedChanged;
        }
    }
}

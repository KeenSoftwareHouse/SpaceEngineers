using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using VRage;
using VRageMath;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Character.Components;
using VRage.Game;

namespace Sandbox.Game.Screens.DebugScreens
{
#if !XB1_TMP

    [MyDebugScreen("VRage", "Character feet IK")]
    class MyGuiScreenDebugFeetIK : MyGuiScreenDebugBase
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
                MyCharacter playerCharacter = MySession.Static.LocalCharacter;
                var ragdollComponent = playerCharacter.Components.Get<MyCharacterRagdollComponent>();
                if (ragdollComponent == null) return null;
                return ragdollComponent.RagdollMapper;
            }
        }
        

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugFeetIK";
        }

        public MyGuiScreenDebugFeetIK()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);
            m_currentPosition.Y += 0.01f;
            m_scale = 0.7f;

            AddCaption("Character feet IK debug draw", Color.Yellow.ToVector4());
            AddShareFocusHint();

            AddCheckBox("Draw IK Settings ", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_SETTINGS));
            AddCheckBox("Draw ankle final position", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_ANKLE_FINALPOS));
            AddCheckBox("Draw raycast lines and foot lines", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_RAYCASTLINE));
            AddCheckBox("Draw bones", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_BONES));
            AddCheckBox("Draw raycast hits", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_RAYCASTHITS));
            AddCheckBox("Draw ankle desired positions", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_ANKLE_DESIREDPOSITION));
            AddCheckBox("Draw closest support position", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_CLOSESTSUPPORTPOSITION));
            AddCheckBox("Draw IK solvers debug", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_IKSOLVERS));
            AddCheckBox("Enable/Disable Feet IK", null, MemberHelper.GetMember(() => MyFakes.ENABLE_FOOT_IK));

            //characterMovementStateCombo = AddCombo<Sandbox.Common.ObjectBuilders.MyCharacterMovementEnum>(null,  MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_MOVEMENT_STATE));
            enabledIKState = AddCheckBox("Enable IK for this state", null, MemberHelper.GetMember(() => ikSettingsEnabled));
            belowReachableDistance = AddSlider("Reachable distance below character", 0f, 0f, 2f, null);           
            aboveReachableDistance = AddSlider("Reachable distance above character", 0f, 0f, 2f, null);
            verticalChangeUpGain = AddSlider("Shift Up Gain", 0.1f, 0f, 1f, null);
            verticalChangeDownGain = AddSlider("Sift Down Gain", 0.1f, 0f, 1f, null);
            ankleHeight = AddSlider("Ankle height", 0.1f, 0.001f, 0.3f, null);
            footWidth = AddSlider("Foot width", 0.1f, 0.001f, 0.3f, null);
            footLength = AddSlider("Foot length", 0.3f, 0.001f, 0.2f, null);
            RegisterEvents();
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

            MyCharacter playerCharacter = MySession.Static.LocalCharacter;

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

            MyCharacter playerCharacter = MySession.Static.LocalCharacter;

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

#endif
}

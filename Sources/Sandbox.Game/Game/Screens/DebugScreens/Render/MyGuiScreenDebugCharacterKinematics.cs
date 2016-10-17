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
using VRage.Game;

namespace Sandbox.Game.Screens.DebugScreens
{

#if !XB1

    [MyDebugScreen("Render", "Character kinematics")]
    class MyGuiScreenDebugCharacterKinematics : MyGuiScreenDebugBase
    {
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

            AddCheckBox("Draw Ragdoll Rig Pose", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_ORIGINAL_RIG));
            AddCheckBox("Draw Bones Rig Pose", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_BONES_ORIGINAL_RIG));
            AddCheckBox("Draw Ragdoll Pose", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_POSE));
            AddCheckBox("Draw Bones", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_COMPUTED_BONES));
            AddCheckBox("Draw bones intended transforms", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_BONES_DESIRED));
            AddCheckBox("Draw Hip Ragdoll and Char. Position", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_HIPPOSITIONS));

            AddCheckBox("Enable Ragdoll", null, MemberHelper.GetMember(() => MyPerGameSettings.EnableRagdollModels));
            AddCheckBox("Enable Ragdoll Animation", null, MemberHelper.GetMember(() => MyFakes.ENABLE_RAGDOLL_ANIMATION));
            AddCheckBox("Enable Bones Translation", null, MemberHelper.GetMember(() => MyFakes.ENABLE_RAGDOLL_BONES_TRANSLATION));
            
            StringBuilder caption = new StringBuilder("Kill Ragdoll");
            AddButton(caption, killRagdollAction);

            StringBuilder captionActivate = new StringBuilder("Activate Ragdoll");
            AddButton(captionActivate, activateRagdollAction);

            StringBuilder captionRagdollDynamic = new StringBuilder("Switch to Dynamic / Keyframed");
            AddButton(captionRagdollDynamic, switchRagdoll);
        }

        private void switchRagdoll(MyGuiControlButton obj)
        {
            MyCharacter playerCharacter = MySession.Static.LocalCharacter;
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
            MyCharacter playerCharacter = MySession.Static.LocalCharacter;
            if (PlayerRagdollMapper == null)
            {
                var component = new MyCharacterRagdollComponent();
                playerCharacter.Components.Add<MyCharacterRagdollComponent>(component);
                component.InitRagdoll();
            }
            if (PlayerRagdollMapper.IsActive) PlayerRagdollMapper.Deactivate();
            //playerCharacter.RagdollMapper.Activate(playerCharacter.GetPhysicsBody().HavokWorld, MyPhysics.CollisionLayers.CollisionLayerWithoutCharacter, playerCharacter.Physics.CharacterSystemGroupCollisionFilterID);
            //m_playerCharacter.Physics.CharacterProxy.SwitchToRagdollMode(m_playerCharacter.GetPhysicsBody().HavokWorld, m_playerCharacter.WorldMatrix);
            //m_playerCharacter.Physics.CharacterProxy.SwitchToRagdollMode(m_playerCharacter.GetPhysicsBody().HavokWorld);
            playerCharacter.Physics.SwitchToRagdollMode(false);
            PlayerRagdollMapper.Activate();
            PlayerRagdollMapper.SetRagdollToKeyframed();
            playerCharacter.Physics.Ragdoll.DisableConstraints();

        }

        private void killRagdollAction(MyGuiControlButton obj)
        {
            MyCharacter playerCharacter = MySession.Static.LocalCharacter;
            //m_playerCharacter.Physics.CharacterProxy.RagdollMapper.SetRagdollToDynamic();
            //m_playerCharacter.Physics.CharacterProxy.RagdollMapper.EnableRagdollConstraints();
            MyFakes.CHARACTER_CAN_DIE_EVEN_IN_CREATIVE_MODE = true;
            playerCharacter.DoDamage(1000000, MyDamageType.Suicide, true);
            //MyFakes.CHARACTER_CAN_DIE_EVEN_IN_CREATIVE_MODE = false;
        }

    }

#endif
}

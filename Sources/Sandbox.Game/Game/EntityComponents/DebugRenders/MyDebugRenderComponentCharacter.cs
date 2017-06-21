using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Lights;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Utils;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Sandbox.Game.Entities.Character;

using Sandbox.ModAPI;
using VRageRender.Animations;
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentCharacter : MyDebugRenderComponent
    {
        MyCharacter m_character = null;
        public MyDebugRenderComponentCharacter(MyCharacter character)
            : base(character)
        {
            m_character = character;
        }
        #region debug Draw

        List<Matrix> m_simulatedBonesDebugDraw = new List<Matrix>();
        List<Matrix> m_simulatedBonesAbsoluteDebugDraw = new List<Matrix>();
        private long m_counter = 0;
        private float m_lastDamage = 0;
        private float m_lastCharacterVelocity;

        public override void DebugDraw()
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_SHOW_DAMAGE)
            {
                m_counter++;
                if (m_character.CharacterAccumulatedDamage != m_lastDamage) m_counter = 0;
                VRageRender.MyRenderProxy.DebugDrawText3D(((MyEntity)m_character).WorldMatrix.Translation + ((MyEntity)m_character).WorldMatrix.Up, "Total damage:" + m_lastDamage + " velocity:" + m_lastCharacterVelocity, Color.Red, 1.5f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                if (m_counter > 200)
                {
                    m_character.CharacterAccumulatedDamage = 0;
                    m_lastCharacterVelocity = 0;
                    m_counter = 0;
                }
                m_lastDamage = m_character.CharacterAccumulatedDamage;
                m_lastCharacterVelocity = Math.Max(m_lastCharacterVelocity, m_character.Physics.LinearVelocity.Length());
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_MISC && m_character.CurrentWeapon != null)
            {
                VRageRender.MyRenderProxy.DebugDrawAxis(((MyEntity)m_character.CurrentWeapon).WorldMatrix, 1.4f, false);
                VRageRender.MyRenderProxy.DebugDrawText3D(((MyEntity)m_character.CurrentWeapon).WorldMatrix.Translation, "Weapon", Color.White, 0.7f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

                VRageRender.MyRenderProxy.DebugDrawSphere((m_character.AnimationController.CharacterBones[m_character.WeaponBone].AbsoluteTransform * m_character.PositionComp.WorldMatrix).Translation, 0.02f, Color.White, 1, false);
                VRageRender.MyRenderProxy.DebugDrawText3D((m_character.AnimationController.CharacterBones[m_character.WeaponBone].AbsoluteTransform * m_character.PositionComp.WorldMatrix).Translation, "Weapon Bone", Color.White, 1f, false);
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_MISC && m_character.IsUsing != null)
            {
                Matrix characterMatrix = m_character.IsUsing.WorldMatrix;
                characterMatrix.Translation = Vector3.Zero;
                characterMatrix = characterMatrix * Matrix.CreateFromAxisAngle(characterMatrix.Up, MathHelper.Pi);

                Vector3 position = m_character.IsUsing.PositionComp.GetPosition() - m_character.IsUsing.WorldMatrix.Up * MyDefinitionManager.Static.GetCubeSize(MyCubeSize.Large) / 2.0f;
                position = position + characterMatrix.Up * 0.28f - characterMatrix.Forward * 0.22f;
                characterMatrix.Translation = position;
                VRageRender.MyRenderProxy.DebugDrawAxis(characterMatrix, 1.4f, false);
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_SUIT_BATTERY_CAPACITY)
            {
                var world = m_character.PositionComp.WorldMatrix;

                VRageRender.MyRenderProxy.DebugDrawText3D(world.Translation + 2f * world.Up, string.Format("{0} MWh", m_character.SuitBattery.ResourceSource.RemainingCapacity),
                    Color.White, 1f, true, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            }

            m_simulatedBonesDebugDraw.Clear();
            m_simulatedBonesAbsoluteDebugDraw.Clear();

            if (MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_BONES)
            {
                m_character.AnimationController.UpdateTransformations();
                for (int s = 0; s < m_character.AnimationController.CharacterBones.Length; s++)
                {
                    MyCharacterBone bone2 = m_character.AnimationController.CharacterBones[s];
                    if (bone2.Parent == null)
                        continue;

                    //bone2.ComputeAbsoluteTransform();

                    var p2m = Matrix.CreateScale(0.1f) * bone2.AbsoluteTransform * m_character.PositionComp.WorldMatrix;
                    Vector3 p2 = p2m.Translation;

                    MyCharacterBone bone1 = bone2.Parent;
                    //bone1.Rotation = Quaternion.Identity;
                    //bone1.Translation = Vector3.Zero;

                    // bone1.ComputeAbsoluteTransform();
                    Vector3 p1 = (bone1.AbsoluteTransform * m_character.PositionComp.WorldMatrix).Translation;

                    VRageRender.MyRenderProxy.DebugDrawLine3D(p1, p2, Color.White, Color.White, false);

                    Vector3 pCenter = (p1 + p2) * 0.5f;
                    VRageRender.MyRenderProxy.DebugDrawText3D(pCenter, bone2.Name + " (" + s.ToString() + ")", Color.Red, 0.5f, false);

                    VRageRender.MyRenderProxy.DebugDrawAxis(p2m, 0.1f, false);
                }
            }
        }

        #endregion
    }
}

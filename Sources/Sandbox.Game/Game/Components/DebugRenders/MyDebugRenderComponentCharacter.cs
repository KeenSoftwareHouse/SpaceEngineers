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
using Sandbox.Graphics.TransparentGeometry;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Sandbox.Game.Entities.Character;
using Sandbox.Common.Components;
using Sandbox.ModAPI;

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

        public override bool DebugDraw()
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

                VRageRender.MyRenderProxy.DebugDrawSphere((m_character.Bones[m_character.WeaponBone].AbsoluteTransform * m_character.PositionComp.WorldMatrix).Translation, 0.02f, Color.White, 1, false);
                VRageRender.MyRenderProxy.DebugDrawText3D((m_character.Bones[m_character.WeaponBone].AbsoluteTransform * m_character.PositionComp.WorldMatrix).Translation, "Weapon Bone", Color.White, 1f, false);
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

                VRageRender.MyRenderProxy.DebugDrawText3D(world.Translation + 2f * world.Up, string.Format("{0} MWh", m_character.SuitBattery.RemainingCapacity),
                    Color.White, 1f, true, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            }

            // return true;

            /*
            Matrix headMatrix = m_headMatrix * WorldMatrix;
            VRageRender.MyRenderProxy.DebugDrawAxis(headMatrix, 2.4f, false);

            VRageRender.MyRenderProxy.DebugDrawAxis(m_weaponDummyMatrix, 2.4f, false);

            VRageRender.MyRenderProxy.DebugDrawAxis(Matrix.CreateWorld(headMatrix.Translation, Vector3.Forward, Vector3.Up), 2.4f, false);
              */
            //return true;

            m_simulatedBonesDebugDraw.Clear();
            m_simulatedBonesAbsoluteDebugDraw.Clear();
            //Physics.CharacterProxy.GetPoseModelSpace(m_simulatedBonesDebugDraw);
            //Physics.CharacterProxy.GetPoseLocalSpace(m_simulatedBones);

            if (MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_BONES)
            {
                if (MyFakes.USE_HAVOK_ANIMATION_HANDS)
                {

                    for (int s = 0; s < m_simulatedBonesDebugDraw.Count; s++)
                    {
                        MyCharacterBone bone2 = m_character.Bones[s];

                        Matrix absolute2 = m_simulatedBonesDebugDraw[s];// *bone2.BindTransform;

                        if (bone2.Parent == null)
                        {
                            m_simulatedBonesAbsoluteDebugDraw.Add(absolute2);
                            continue;
                        }


                        MyCharacterBone bone1 = bone2.Parent;

                        Matrix absolute1 = m_simulatedBonesAbsoluteDebugDraw[m_character.Bones.IndexOf(bone1)];
                        //absolute2 = absolute2 * absolute1;

                        m_simulatedBonesAbsoluteDebugDraw.Add(absolute2);

                        var p2m = absolute2 * m_character.PositionComp.WorldMatrix;
                        Vector3 p2 = p2m.Translation;


                        //bone1.Rotation = Quaternion.Identity;
                        //bone1.Translation = Vector3.Zero;

                        Vector3 p1 = (absolute1 * m_character.PositionComp.WorldMatrix).Translation;

                        VRageRender.MyRenderProxy.DebugDrawLine3D(p1, p2, Color.White, Color.White, false);

                        Vector3 pCenter = (p1 + p2) * 0.5f;
                        VRageRender.MyRenderProxy.DebugDrawText3D(pCenter, bone2.Name + " (" + s.ToString() + ")", Color.White, 0.5f, false);

                        VRageRender.MyRenderProxy.DebugDrawAxis(p2m, 0.5f, false);
                    }
                }
                else
                {

                    for (int s = 0; s < m_character.Bones.Count; s++)
                    {
                        MyCharacterBone bone2 = m_character.Bones[s];
                        if (bone2.Parent == null)
                            continue;

                        bone2.ComputeAbsoluteTransform();

                        var p2m = Matrix.CreateScale(0.1f) *  bone2.AbsoluteTransform * m_character.PositionComp.WorldMatrix;
                        Vector3 p2 = p2m.Translation;

                        MyCharacterBone bone1 = bone2.Parent;
                        //bone1.Rotation = Quaternion.Identity;
                        //bone1.Translation = Vector3.Zero;

                        // bone1.ComputeAbsoluteTransform();
                        Vector3 p1 = (bone1.AbsoluteTransform * m_character.PositionComp.WorldMatrix).Translation;

                        VRageRender.MyRenderProxy.DebugDrawLine3D(p1, p2, Color.White, Color.White, false);

                        Vector3 pCenter = (p1 + p2) * 0.5f;
                        VRageRender.MyRenderProxy.DebugDrawText3D(pCenter, bone2.Name + " (" + s.ToString() + ")", Color.White, 0.5f, false);

                        VRageRender.MyRenderProxy.DebugDrawAxis(p2m, 0.1f, false);

                        if (s == 0)
                        {
                            //  MyDebugDraw.DrawAxis((bone2.AbsoluteTransform * WorldMatrix), 1000, 1, false);
                        }
                    }
                }
            }
            return true;
        }

        #endregion
    }
}

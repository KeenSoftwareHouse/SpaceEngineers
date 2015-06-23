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
using VRage.Components;
using VRage.ModAPI;

namespace Sandbox.Game.Components
{
    class MyRenderComponentCharacter : MyRenderComponent
    {
        #region Jetpack thrust

        public class MyJetpackThrust
        {
            public int Bone;
            public Vector3 Forward;
            public float Offset;
            public MyLight Light;

            public float ThrustLength;
            public float ThrustRadius;
            public float ThrustThickness;

            public Matrix ThrustMatrix;
            public string ThrustMaterial;
            public float ThrustGlareSize;
        }

        List<MyJetpackThrust> m_jetpackThrusts = new List<MyJetpackThrust>(8);

        #endregion
        
        public List<MyJetpackThrust> JetpackThrusts
        {
            get { return m_jetpackThrusts; }           
        }
        

        #region lights properties

        MyLight m_light;
        MyLight m_leftGlare;
        MyLight m_rightGlare;
        int m_leftLightIndex = -1;
        int m_rightLightIndex = -1;
        Matrix m_reflectorAngleMatrix;
        float m_oldReflectorAngle = -1;
        Vector3 m_lightLocalPosition;
        float m_lightGlareSize = 0.0f;

        #endregion

        #region character properties

        private const float HIT_INDICATOR_LENGTH = 0.8f;
        float m_currentHitIndicatorCounter = 0;
        
        bool m_sentSkeletonMessage = false;
        MyCharacter m_character = null;

        #endregion

        #region overrides

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_character = Container.Entity as MyCharacter;
        }

        public override void AddRenderObjects()
        {
            if (m_model == null)
                return;

            if (IsRenderObjectAssigned(0))
                return;

            System.Diagnostics.Debug.Assert(m_model == null || !string.IsNullOrEmpty(m_model.AssetName));

            SetRenderObjectID(0, VRageRender.MyRenderProxy.CreateRenderCharacter
                (
                 Container.Entity.GetFriendlyName() + " " + Container.Entity.EntityId.ToString(),
                 m_model.AssetName,
                 Container.Entity.PositionComp.WorldMatrix,
                 m_diffuseColor,
                 ColorMaskHsv,
                 GetRenderFlags()
                ));
            m_sentSkeletonMessage = false;

            UpdateCharacterSkeleton();
        }


        private void UpdateCharacterSkeleton()
        {
            if (!m_sentSkeletonMessage)
            {
                m_sentSkeletonMessage = true;
                var skeletonDescription = new MySkeletonBoneDescription[m_character.Bones.Count];

                for (int i = 0; i < m_character.Bones.Count; i++)
                {
                    skeletonDescription[i].Parent = -1;
                    if (m_character.Bones[i].Parent != null)
                    {
                        for (int j = 0; j < m_character.Bones.Count; j++)
                        {
                            if (m_character.Bones[j].Name == m_character.Bones[i].Parent.Name)
                            {
                                skeletonDescription[i].Parent = j;
                                break;
                            }
                        }
                    }

                    if (m_character.Bones[i].Parent != null)
                    {
                        Debug.Assert(skeletonDescription[i].Parent > -1, "Can't find bone with parent name!");
                    }

                    skeletonDescription[i].SkinTransform = m_character.Bones[i].SkinTransform;
                }

                VRageRender.MyRenderProxy.SetCharacterSkeleton(RenderObjectIDs[0], skeletonDescription, Model.Animations.Skeleton.ToArray());
            }
        }

        public override void Draw()
        {
            base.Draw();

            UpdateCharacterSkeleton();

            VRageRender.MyRenderProxy.SetCharacterTransforms(RenderObjectIDs[0], m_character.BoneRelativeTransforms);

            Matrix headMatrix = m_character.GetHeadMatrix(false);

            
            Vector3 position = m_light.Position;
            Vector3 forwardVector = m_light.ReflectorDirection;
            Vector3 leftVector = headMatrix.Left;
            Vector3 upVector = headMatrix.Up;

            float reflectorLength = MyCharacter.REFLECTOR_BILLBOARD_LENGTH * 0.4f * 0.16f;
            float reflectorThickness = MyCharacter.REFLECTOR_BILLBOARD_THICKNESS * 0.08f;
            //float reflectorRadiusForAdditive = 0.25f;//0.65f;

            Vector3 color = new Vector3(m_light.ReflectorColor);

            Vector3 glarePosition = position + forwardVector * 0.28f;
            var dot = Vector3.Dot(Vector3.Normalize(MySector.MainCamera.Position - glarePosition), forwardVector);
            float angle = 1 - Math.Abs(dot);
            float alphaGlareAlphaBlended = (float)Math.Pow(1 - angle, 2);
            float alphaGlareAdditive = (float)Math.Pow(1 - angle, 2);
            float alphaCone = (1 - (float)Math.Pow(1 - angle, 30)) * 0.5f;

            float reflectorRadiusForAlphaBlended = MathHelper.Lerp(0.1f, 0.5f, alphaGlareAlphaBlended); //3.5f;

            //  Multiply alpha by reflector level (and not the color), because if we multiply the color and let alpha unchanged, reflector cune will be drawn as very dark cone, but still visible
            var reflectorLevel = m_character.CurrentLightPower;
            m_light.ReflectorIntensity = reflectorLevel;

            alphaCone *= reflectorLevel * 0.2f;
            alphaGlareAlphaBlended *= reflectorLevel * 0.1f;
            alphaGlareAdditive *= reflectorLevel * 0.8f;

            float distance = Vector3.Distance(m_character.PositionComp.GetPosition(), MySector.MainCamera.Position);

            if (!m_character.IsInFirstPersonView && distance < MyCharacter.LIGHT_GLARE_MAX_DISTANCE && reflectorLevel > 0)
            {
                float alpha = MathHelper.Clamp((MyCharacter.LIGHT_GLARE_MAX_DISTANCE - 10.0f) / distance, 0, 1);

                if (reflectorLength > 0 && reflectorThickness > 0)
                {
                    if (m_leftGlare != null)
                    {
                        MyTransparentGeometry.AddLineBillboard("ReflectorConeCharacter", new Vector4(color, 1.0f) * alphaCone * alpha,
                            m_leftGlare.Position,
                                                     m_leftGlare.ReflectorDirection, reflectorLength, reflectorThickness);
                    }

                    if (m_rightGlare != null)
                    {
                        MyTransparentGeometry.AddLineBillboard("ReflectorConeCharacter", new Vector4(color, 1.0f) * alphaCone * alpha,
                            m_rightGlare.Position,
                                                     m_rightGlare.ReflectorDirection, reflectorLength, reflectorThickness);
                    }
                }

                if (m_leftGlare != null)
                {
                    MyTransparentGeometry.AddPointBillboard("ReflectorGlareAlphaBlended", new Vector4(color, 1.0f) * alphaGlareAlphaBlended * alpha,
                                                  m_leftGlare.Position, reflectorRadiusForAlphaBlended * 0.3f, 0);
                }

                if (m_rightGlare != null)
                {
                    MyTransparentGeometry.AddPointBillboard("ReflectorGlareAlphaBlended", new Vector4(color, 1.0f) * alphaGlareAlphaBlended * alpha,
                                                  m_rightGlare.Position, reflectorRadiusForAlphaBlended * 0.3f, 0);
                }
            }

            DrawJetpackThrusts(m_character.UpdateCalled());

            if (m_character.Hierarchy.Parent != null)
            {
                if (m_leftGlare != null && m_leftGlare.LightOn == true)
                {
                    m_leftGlare.LightOn = false;
                    m_leftGlare.GlareOn = false;
                    m_leftGlare.UpdateLight();
                }

                if (m_rightGlare != null && m_rightGlare.LightOn == true)
                {
                    m_rightGlare.LightOn = false;
                    m_rightGlare.GlareOn = false;
                    m_rightGlare.UpdateLight();
                }
            }
            else
            {
                if (m_leftGlare != null && m_leftGlare.LightOn == false)
                {
                    m_leftGlare.LightOn = true;
                    m_leftGlare.GlareOn = true;
                    m_leftGlare.UpdateLight();
                }

                if (m_rightGlare != null && m_rightGlare.LightOn == true)
                {
                    m_rightGlare.LightOn = true;
                    m_rightGlare.GlareOn = true;
                    m_rightGlare.UpdateLight();
                }
            }

            if (MySession.ControlledEntity == m_character)
            {
                if (m_character.IsDead && m_character.CurrentRespawnCounter > 0)
                {
                    DrawBlood(1);
                }

                if (!m_character.IsDead && m_currentHitIndicatorCounter > 0)
                {
                    m_currentHitIndicatorCounter -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                    if (m_currentHitIndicatorCounter < 0)
                        m_currentHitIndicatorCounter = 0;

                    float alpha = m_currentHitIndicatorCounter / HIT_INDICATOR_LENGTH;

                    DrawBlood(alpha);
                }

                if (m_character.HealthRatio <= MyCharacter.LOW_HEALTH_RATIO && !m_character.IsDead)
                {
                    float alpha = MathHelper.Clamp(MyCharacter.LOW_HEALTH_RATIO - m_character.HealthRatio, 0, 1) / MyCharacter.LOW_HEALTH_RATIO + 0.3f;
                    DrawBlood(alpha);
                }
            }

            //DebugDraw();
        }

        #endregion
   
        private void DrawBlood(float alpha)
        {
            RectangleF dest = new RectangleF(0, 0, MyGuiManager.GetFullscreenRectangle().Width, MyGuiManager.GetFullscreenRectangle().Height);
            Rectangle? source = null;

            VRageRender.MyRenderProxy.DrawSprite("Textures\\Gui\\Blood.dds", ref dest, false, ref source, new Color(new Vector4(1, 1, 1, alpha)), 0,
                new Vector2(1, 0), ref Vector2.Zero, VRageRender.Graphics.SpriteEffects.None, 0);
        }

        #region character lights

        private void DrawJetpackThrusts(bool updateCalled)
        {
            if (m_character.CanDrawThrusts() == false)
            {
                return;
            }

            //VRageRender.MyRenderProxy.DebugDrawLine3D(WorldMatrix.Translation, WorldMatrix.Translation + Physics.LinearAcceleration, Color.White, Color.Green, false);

            foreach (MyJetpackThrust thrust in m_jetpackThrusts)
            {
                float strength = 0;
                Vector3D position = Vector3D.Zero;
                var worldToLocal = MatrixD.Invert(Container.Entity.PositionComp.WorldMatrix);

                if (m_character.JetpackEnabled && m_character.IsJetpackPowered() && !m_character.IsInFirstPersonView)
                {
                    var thrustMatrix = (MatrixD)thrust.ThrustMatrix * Container.Entity.PositionComp.WorldMatrix;
                    Vector3D forward = Vector3D.TransformNormal(thrust.Forward, thrustMatrix);
                    position = thrustMatrix.Translation;
                    position += forward * thrust.Offset;

                    float flameScale = 0.05f;
                    if (updateCalled)
                        thrust.ThrustRadius = MyUtils.GetRandomFloat(0.9f, 1.1f) * flameScale;
                    strength = Vector3.Dot(forward, -Container.Entity.Physics.LinearAcceleration);
                    strength = MathHelper.Clamp(strength * 0.5f, 0.1f, 1f);

                    if (strength > 0 && thrust.ThrustRadius > 0)
                    {
                        float angle = 1 - Math.Abs(Vector3.Dot(MyUtils.Normalize(MySector.MainCamera.Position - position), forward));
                        float alphaCone = (1 - (float)Math.Pow(1 - angle, 30)) * 0.5f;

                        if (updateCalled)
                        {
                            thrust.ThrustLength = strength * 10 * MyUtils.GetRandomFloat(0.6f, 1.0f) * flameScale;
                            thrust.ThrustThickness = MyUtils.GetRandomFloat(thrust.ThrustRadius * 0.90f, thrust.ThrustRadius);
                        }

                        //  We move polyline particle backward, because we are stretching ball texture and it doesn't look good if stretched. This will hide it.
                        MyTransparentGeometry.AddLineBillboard(thrust.ThrustMaterial, thrust.Light.Color * alphaCone, position /*- forward * thrust.ThrustLength * 0.25f*/,
                            GetRenderObjectID(), ref worldToLocal, forward, thrust.ThrustLength, thrust.ThrustThickness);
                    }

                    if (thrust.ThrustRadius > 0)
                        MyTransparentGeometry.AddPointBillboard(thrust.ThrustMaterial, thrust.Light.Color, position, GetRenderObjectID(), ref worldToLocal, thrust.ThrustRadius, 0);
                }
                else
                {
                    if (updateCalled || (m_character.IsUsing != null))
                        thrust.ThrustRadius = 0;
                }

                if (thrust.Light != null)
                {
                    if (thrust.ThrustRadius > 0)
                    {
                        thrust.Light.LightOn = true;
                        thrust.Light.Intensity = 1.3f + thrust.ThrustLength * 1.2f;

                        thrust.Light.Range = thrust.ThrustRadius * 7f + thrust.ThrustLength / 10;

                        thrust.Light.Position = Vector3D.Transform(position, MatrixD.Invert(Container.Entity.PositionComp.WorldMatrix));
                        thrust.Light.ParentID = GetRenderObjectID();

                        thrust.Light.GlareOn = true;

                        thrust.Light.GlareIntensity = 0.5f + thrust.ThrustLength * 4;

                        thrust.Light.GlareMaterial = "GlareJetpack";
                        thrust.Light.GlareType = VRageRender.Lights.MyGlareTypeEnum.Normal;

                        thrust.Light.GlareSize = (thrust.ThrustRadius * 2.4f + thrust.ThrustLength * 0.2f) * thrust.ThrustGlareSize;

                        thrust.Light.GlareQuerySize = 0.3f;
                        thrust.Light.UpdateLight();
                    }
                    else
                    {
                        thrust.Light.GlareOn = false;
                        thrust.Light.LightOn = false;
                        thrust.Light.UpdateLight();
                    }
                }
            }
        }

        public void InitJetpackThrusts(MyCharacterDefinition definition)
        {
            m_jetpackThrusts.Clear();

            foreach (var thrustDefinition in definition.Thrusts)
            {
                int index;
                var thrustBone = m_character.FindBone(thrustDefinition.ThrustBone, out index);
                if (thrustBone != null)
                {
                    InitJetpackThrust(index, Vector3.Forward, thrustDefinition.SideFlameOffset, thrustDefinition); // UP is now in -Z
                    InitJetpackThrust(index, Vector3.Left, thrustDefinition.SideFlameOffset, thrustDefinition);
                    InitJetpackThrust(index, Vector3.Right, thrustDefinition.SideFlameOffset, thrustDefinition);
                    InitJetpackThrust(index, Vector3.Backward, thrustDefinition.SideFlameOffset, thrustDefinition); // DOWN is now in Z                    
                    InitJetpackThrust(index, Vector3.Up, thrustDefinition.FrontFlameOffset, thrustDefinition); // FORWARD is now in Y
                }
            }
        }

        private void InitJetpackThrust(int bone, Vector3 forward, float offset, MyJetpackThrustDefinition thrustDefinition)
        {
            var thrust = new MyJetpackThrust()
            {
                Bone = bone,
                Forward = forward,
                Offset = offset,
                ThrustMaterial = thrustDefinition.ThrustMaterial,
                ThrustGlareSize = thrustDefinition.ThrustGlareSize
            };

            thrust.Light = MyLights.AddLight();
            thrust.Light.ReflectorDirection = Container.Entity.PositionComp.WorldMatrix.Forward;
            thrust.Light.ReflectorUp = Container.Entity.PositionComp.WorldMatrix.Up;
            thrust.Light.ReflectorRange = 1;
            thrust.Light.Color = thrustDefinition.ThrustColor;
            thrust.Light.Start(MyLight.LightTypeEnum.PointLight, 1);

            m_jetpackThrusts.Add(thrust);
        }

        public void InitLight(MyCharacterDefinition definition)
        {
            m_light = MyLights.AddLight();

            m_lightGlareSize = definition.LightGlareSize;

            m_light.Start(MyLight.LightTypeEnum.PointLight | MyLight.LightTypeEnum.Spotlight, 1.5f);
            m_light.ShadowDistance = 20;
            m_light.ReflectorFalloff = 5;
            m_light.LightOwner = MyLight.LightOwnerEnum.SmallShip;
            m_light.UseInForwardRender = true;
            m_light.ReflectorTexture = definition.ReflectorTexture;
            m_light.Range = 1;

            MyCharacterBone leftGlareBone = null;
            if (definition.LeftLightBone != String.Empty) leftGlareBone = m_character.FindBone(definition.LeftLightBone, out m_leftLightIndex);
            if (leftGlareBone != null)
            {
                m_leftGlare = MyLights.AddLight();
                m_leftGlare.Start(MyLight.LightTypeEnum.None, 1.5f);
                m_leftGlare.LightOn = false;
                m_leftGlare.LightOwner = MyLight.LightOwnerEnum.SmallShip;
                m_leftGlare.UseInForwardRender = false;
                m_leftGlare.GlareOn = true;
                m_leftGlare.GlareQuerySize = 0.2f;
                m_leftGlare.GlareType = VRageRender.Lights.MyGlareTypeEnum.Directional;
                m_leftGlare.GlareMaterial = definition.LeftGlare;
            }

            MyCharacterBone rightGlareBone = null;
            if (definition.RightLightBone != String.Empty) rightGlareBone = m_character.FindBone(definition.RightLightBone, out m_rightLightIndex);
            if (rightGlareBone != null)
            {
                m_rightGlare = MyLights.AddLight();
                m_rightGlare.Start(MyLight.LightTypeEnum.None, 1.5f);
                m_rightGlare.LightOn = false;
                m_rightGlare.LightOwner = MyLight.LightOwnerEnum.SmallShip;
                m_rightGlare.UseInForwardRender = false;
                m_rightGlare.GlareOn = true;
                m_rightGlare.GlareQuerySize = 0.2f;
                m_rightGlare.GlareType = VRageRender.Lights.MyGlareTypeEnum.Directional;
                m_rightGlare.GlareMaterial = definition.RightGlare;
            }
        }

        public void UpdateLightProperties(float currentLightPower)
        {
            if (m_light != null)
            {
                m_light.ReflectorRange = MyCharacter.REFLECTOR_RANGE;
                m_light.ReflectorColor = MyCharacter.REFLECTOR_COLOR;
                m_light.ReflectorIntensity = MyCharacter.REFLECTOR_INTENSITY * currentLightPower;

                m_light.Color = MyCharacter.POINT_COLOR;
                m_light.SpecularColor = new Vector3(MyCharacter.POINT_COLOR_SPECULAR);
                m_light.Range = MyCharacter.POINT_LIGHT_RANGE;
                m_light.Intensity = MyCharacter.POINT_LIGHT_INTENSITY * currentLightPower;

                //MyTrace.Send(TraceWindow.Default, m_currentLightPower.ToString());

                m_light.UpdateLight();

                if (m_leftGlare != null)
                {
                    m_leftGlare.GlareIntensity = m_light.Intensity;
                    m_leftGlare.UpdateLight();
                }

                if (m_rightGlare != null)
                {
                    m_rightGlare.GlareIntensity = m_light.Intensity;
                    m_rightGlare.UpdateLight();
                }
            }
        }

        public void UpdateLightPosition()
        {
            if (m_light != null)
            {
                m_lightLocalPosition = new Vector3(0, 0, 0.3f);

                MatrixD headMatrix = m_character.GetHeadMatrix(false, true, false);
                MatrixD headMatrixAnim = m_character.GetHeadMatrix(false, true, true);

                if (m_oldReflectorAngle != MyCharacter.REFLECTOR_DIRECTION)
                {
                    m_oldReflectorAngle = MyCharacter.REFLECTOR_DIRECTION;
                    m_reflectorAngleMatrix = MatrixD.CreateFromAxisAngle(headMatrix.Forward, MathHelper.ToRadians(MyCharacter.REFLECTOR_DIRECTION));
                }

                m_light.ReflectorDirection = Vector3.Transform(headMatrix.Forward, m_reflectorAngleMatrix);
                m_light.ReflectorUp = headMatrix.Up;
                m_light.ReflectorTexture = "Textures\\Lights\\dual_reflector_2.dds";
                m_light.ReflectorColor = MyCharacter.REFLECTOR_COLOR;
                m_light.UpdateReflectorRangeAndAngle(MyCharacter.REFLECTOR_CONE_ANGLE, MyCharacter.REFLECTOR_RANGE);
                m_light.Position = Vector3D.Transform(m_lightLocalPosition, headMatrixAnim);
                m_light.UpdateLight();

                Matrix[] boneMatrices = m_character.BoneTransforms;

                if (m_leftGlare != null)
                {
                    MatrixD leftGlareMatrix = m_reflectorAngleMatrix * MatrixD.Normalize(boneMatrices[m_leftLightIndex]) * m_character.PositionComp.WorldMatrix;

                    m_leftGlare.Position = leftGlareMatrix.Translation;
                    m_leftGlare.Range = 1;
                    m_leftGlare.ReflectorDirection = -leftGlareMatrix.Up;//, m_reflectorAngleMatrix);
                    m_leftGlare.ReflectorUp = leftGlareMatrix.Forward;
                    m_leftGlare.GlareIntensity = m_light.Intensity;
                    m_leftGlare.GlareSize = m_lightGlareSize;
                    m_leftGlare.GlareQuerySize = 0.1f;
                    m_leftGlare.GlareMaxDistance = MyCharacter.LIGHT_GLARE_MAX_DISTANCE;
                    m_leftGlare.UpdateLight();
                }

                if (m_rightGlare != null)
                {
                    MatrixD rightGlareMatrix = m_reflectorAngleMatrix * MatrixD.Normalize(boneMatrices[m_rightLightIndex]) * m_character.PositionComp.WorldMatrix;
                    m_rightGlare.Position = rightGlareMatrix.Translation;
                    m_rightGlare.Range = 1;
                    m_rightGlare.ReflectorDirection = -rightGlareMatrix.Up;
                    m_rightGlare.ReflectorUp = rightGlareMatrix.Forward;
                    m_rightGlare.GlareIntensity = m_light.Intensity;
                    m_rightGlare.GlareSize = m_lightGlareSize;
                    m_rightGlare.GlareQuerySize = 0.1f;
                    m_rightGlare.GlareMaxDistance = MyCharacter.LIGHT_GLARE_MAX_DISTANCE;
                    m_rightGlare.UpdateLight();
                }
            }
        }

        public void UpdateLight(float lightPower, bool updateRenderObject)
        {
            if (lightPower <= 0.0f)
            {
                m_light.ReflectorOn = false;
                m_light.LightOn = false;
            }
            else
            {
                m_light.ReflectorOn = true;
                m_light.LightOn = true;
            }

            if (updateRenderObject)
            {
                UpdateLightPosition();

                VRageRender.MyRenderProxy.UpdateModelProperties(
                RenderObjectIDs[0],
                0,
                Model.AssetName,
                -1,
                "Light",
                null,
                null,
                null,
                null,
                lightPower);

                UpdateLightProperties(lightPower);
            }
        }

        public void UpdateThrustMatrices(Matrix[] boneMatrices)
        {
            foreach (var thrust in m_jetpackThrusts)
            {
                thrust.ThrustMatrix = Matrix.Normalize(boneMatrices[thrust.Bone]);
            }
        }

        public void UpdateShadowIgnoredObjects()
        {
            if (m_light != null)
            {
                VRageRender.MyRenderProxy.ClearLightShadowIgnore(m_light.RenderObjectID);
                VRageRender.MyRenderProxy.SetLightShadowIgnore(m_light.RenderObjectID, RenderObjectIDs[0]);
            }         
        }

        public void UpdateShadowIgnoredObjects(IMyEntity Parent)
        {
            foreach (var renderObjectId in Parent.Render.RenderObjectIDs)
            {
                VRageRender.MyRenderProxy.SetLightShadowIgnore(m_light.RenderObjectID, renderObjectId);
            }
        }

        public void Damage()
        {
            m_currentHitIndicatorCounter = HIT_INDICATOR_LENGTH;
        }

        public void CleanLights()
        {
            if (m_light != null)
            {
                MyLights.RemoveLight(m_light);
                m_light = null;
            }
            if (m_leftGlare != null)
            {
                MyLights.RemoveLight(m_leftGlare);
                m_leftGlare = null;
            }
            if (m_rightGlare != null)
            {
                MyLights.RemoveLight(m_rightGlare);
                m_rightGlare = null;
            }

            foreach (var thrust in m_jetpackThrusts)
            {
                MyLights.RemoveLight(thrust.Light);
            }
            m_jetpackThrusts.Clear();
        }

        #endregion
    }
}

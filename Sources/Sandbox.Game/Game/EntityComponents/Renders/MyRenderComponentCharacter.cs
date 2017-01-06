using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Lights;
using Sandbox.Game.World;
using Sandbox.Game.Weapons;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Utils;
using VRage.ModAPI;
using VRageRender.Animations;
using VRage.Game;
using VRageRender;

namespace Sandbox.Game.Components
{
    class MyRenderComponentCharacter : MyRenderComponentSkinnedEntity
    {
		private readonly MyStringHash m_characterMaterial = MyStringHash.GetOrCompute("Character");
	    private int m_lastWalkParticleCheckTime;
	    private int m_walkParticleSpawnCounterMs = 1000;
	    private const int m_walkParticleGravityDelay = 10000;
	    private const int m_walkParticleJetpackOffDelay = 2000;
	    private const int m_walkParticleDefaultDelay = 1000;

	    public MyRenderComponentCharacter()
	    {
		    m_lastWalkParticleCheckTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
	    }

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

		#region Walking effects

	    internal void TrySpawnWalkingParticles(ref HkContactPointEvent value)
	    {
		    if (!MyFakes.ENABLE_WALKING_PARTICLES)
			    return;
            
		    var oldCheckTime = m_lastWalkParticleCheckTime;
		    m_lastWalkParticleCheckTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
		    m_walkParticleSpawnCounterMs -= m_lastWalkParticleCheckTime - oldCheckTime;
		    if (m_walkParticleSpawnCounterMs > 0)
			    return;

			var naturalGravityMultiplier = MyGravityProviderSystem.CalculateHighestNaturalGravityMultiplierInPoint(Entity.PositionComp.WorldMatrix.Translation);
		    if (naturalGravityMultiplier <= 0f)
		    {
			    m_walkParticleSpawnCounterMs = m_walkParticleGravityDelay;
			    return;
		    }

		    var character = Entity as MyCharacter;
            if (character.JetpackRunning)
		    {
			    m_walkParticleSpawnCounterMs = m_walkParticleJetpackOffDelay;
			    return;
		    }

		    var currentMovementState = character.GetCurrentMovementState();
		    if (currentMovementState.GetDirection() == MyCharacterMovement.NoDirection || currentMovementState == MyCharacterMovementEnum.Falling)
		    {
			    m_walkParticleSpawnCounterMs = m_walkParticleDefaultDelay;
			    return;
		    }

		    var otherPhysicsBody = value.GetOtherEntity(character).Physics as MyVoxelPhysicsBody;//value.Base.BodyA.UserObject == character.Physics ? value.Base.BodyB.UserObject : value.Base.BodyA.UserObject)) as MyVoxelPhysicsBody;
		    if (otherPhysicsBody == null)
			    return;
	       
			MyStringId movementType;

		    const int walkParticleWalkDelay = 500;
			const int walkParticleRunDelay = 275;
			const int walkParticleSprintDelay = 250;
		    switch (currentMovementState.GetSpeed())
		    {
			    case MyCharacterMovement.NormalSpeed:
				    movementType = MyMaterialPropertiesHelper.CollisionType.Walk;
				    m_walkParticleSpawnCounterMs = walkParticleWalkDelay;
				    break;
				case MyCharacterMovement.Fast:
					movementType = MyMaterialPropertiesHelper.CollisionType.Run;
				    m_walkParticleSpawnCounterMs = walkParticleRunDelay;
				    break;
				case MyCharacterMovement.VeryFast:
					movementType = MyMaterialPropertiesHelper.CollisionType.Sprint;
				    m_walkParticleSpawnCounterMs = walkParticleSprintDelay;
				    break;
				default:
				    movementType = MyMaterialPropertiesHelper.CollisionType.Walk;
				    m_walkParticleSpawnCounterMs = m_walkParticleDefaultDelay;
				    break;

		    }

            var spawnPosition = otherPhysicsBody.ClusterToWorld(value.ContactPoint.Position);

            MyVoxelMaterialDefinition voxelMaterialDefinition = otherPhysicsBody.m_voxelMap.GetMaterialAt(ref spawnPosition);
		    if (voxelMaterialDefinition == null)
			    return;

		    MyMaterialPropertiesHelper.Static.TryCreateCollisionEffect(
				movementType,
				spawnPosition,
				value.ContactPoint.Normal,
				m_characterMaterial,
				MyStringHash.GetOrCompute(voxelMaterialDefinition.MaterialTypeName));
	    }

		#endregion

		public List<MyJetpackThrust> JetpackThrusts
        {
            get { return m_jetpackThrusts; }           
        }
        

        #region lights properties

        MyLight m_light;
        Vector3D m_leftGlarePosition;
        Vector3D m_rightGlarePosition;
        int m_leftLightIndex = -1;
        int m_rightLightIndex = -1;
        Matrix m_reflectorAngleMatrix;
        float m_oldReflectorAngle = -1;
        Vector3 m_lightLocalPosition;

        #endregion

        #region character properties

        private const float HIT_INDICATOR_LENGTH = 0.8f;
        float m_currentHitIndicatorCounter = 0;


        #endregion

        #region overrides


        public override void Draw()
        {
            base.Draw();

            MyCharacter character = m_skinnedEntity as MyCharacter;
            Vector3 position = m_light.Position;
            Vector3 forwardVector = m_light.ReflectorDirection;

            float reflectorLength = MyCharacter.REFLECTOR_BILLBOARD_LENGTH * 0.4f * 0.16f;
            float reflectorThickness = MyCharacter.REFLECTOR_BILLBOARD_THICKNESS * 0.08f;

            Vector3 color = new Vector3(m_light.ReflectorColor);

            Vector3 glarePosition = position + forwardVector * 0.28f;
            var dot = Vector3.Dot(Vector3.Normalize(MySector.MainCamera.Position - glarePosition), forwardVector);
            float angle = 1 - Math.Abs(dot);
            float alphaGlareAlphaBlended = (float)Math.Pow(1 - angle, 2);
            float alphaCone = (1 - (float)Math.Pow(1 - angle, 30)) * 0.5f;

            float reflectorRadiusForAlphaBlended = MathHelper.Lerp(0.1f, 0.5f, alphaGlareAlphaBlended); //3.5f;

            //  Multiply alpha by reflector level (and not the color), because if we multiply the color and let alpha unchanged, reflector cune will be drawn as very dark cone, but still visible
            var reflectorLevel = character.CurrentLightPower;
            alphaCone *= reflectorLevel * 0.2f;
            alphaGlareAlphaBlended *= reflectorLevel * 0.1f;

            float distance = Vector3.Distance(character.PositionComp.GetPosition(), MySector.MainCamera.Position);

            if ((character != MySession.Static.LocalCharacter || !character.IsInFirstPersonView) && 
                distance < MyCharacter.LIGHT_GLARE_MAX_DISTANCE && reflectorLevel > 0 &&
                m_leftLightIndex != -1 && m_rightLightIndex != -1)
            {
                float alpha = MathHelper.Clamp((MyCharacter.LIGHT_GLARE_MAX_DISTANCE - 10.0f) / distance, 0, 1);

                if (reflectorLength > 0 && reflectorThickness > 0 && alpha > 0)
                {
                    MyTransparentGeometry.AddLineBillboard("ReflectorConeCharacter", new Vector4(color, 1.0f) * alphaCone * alpha,
                        m_leftGlarePosition, m_light.ReflectorDirection, reflectorLength, reflectorThickness, MyBillboard.BlenType.AdditiveBottom);
                    MyTransparentGeometry.AddLineBillboard("ReflectorConeCharacter", new Vector4(color, 1.0f) * alphaCone * alpha,
                        m_rightGlarePosition, m_light.ReflectorDirection, reflectorLength, reflectorThickness, MyBillboard.BlenType.AdditiveBottom);
                }

                if (dot > 0)
                {
                    MyTransparentGeometry.AddPointBillboard("ReflectorGlareAlphaBlended", new Vector4(color, 1.0f) * alphaGlareAlphaBlended * alpha,
                        m_leftGlarePosition, reflectorRadiusForAlphaBlended * 0.3f, 0, -1, MyBillboard.BlenType.AdditiveTop);
                    MyTransparentGeometry.AddPointBillboard("ReflectorGlareAlphaBlended", new Vector4(color, 1.0f) * alphaGlareAlphaBlended * alpha,
                        m_rightGlarePosition, reflectorRadiusForAlphaBlended * 0.3f, 0, -1, MyBillboard.BlenType.AdditiveTop);
                }
            }

            DrawJetpackThrusts(character.UpdateCalled());

            //Maybe this check is not needed at all? In every case we want the DrawBlood effect when damaged
            if ( MySession.Static.ControlledEntity == character ||
                 MySession.Static.ControlledEntity is MyCockpit && ((MyCockpit)MySession.Static.ControlledEntity).Pilot == character ||
                 MySession.Static.ControlledEntity is MyLargeTurretBase && ((MyLargeTurretBase)MySession.Static.ControlledEntity).Pilot == character
                )
            {
                if (character.IsDead && character.CurrentRespawnCounter > 0)
                {
                    DrawBlood(1);
                }

                if (!character.IsDead && m_currentHitIndicatorCounter > 0)
                {
                    m_currentHitIndicatorCounter -= VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                    if (m_currentHitIndicatorCounter < 0)
                        m_currentHitIndicatorCounter = 0;

                    float alpha = m_currentHitIndicatorCounter / HIT_INDICATOR_LENGTH;

                    DrawBlood(alpha);
                }

                if (character.StatComp != null)
				{
                    var healthRatio = character.StatComp.HealthRatio;
                    if (healthRatio <= MyCharacterStatComponent.LOW_HEALTH_RATIO && !character.IsDead)
					{
						float alpha = MathHelper.Clamp(MyCharacterStatComponent.LOW_HEALTH_RATIO - healthRatio, 0, 1) / MyCharacterStatComponent.LOW_HEALTH_RATIO + 0.3f;
						DrawBlood(alpha);
					}
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
                new Vector2(1, 0), ref Vector2.Zero, SpriteEffects.None, 0);
        }

        #region character lights
        
        public static float JETPACK_LIGHT_INTENSITY_BASE = 0f;
        public static float JETPACK_LIGHT_INTENSITY_LENGTH = 200f;
        public static float JETPACK_LIGHT_RANGE_RADIUS = 1.8f;
        public static float JETPACK_LIGHT_RANGE_LENGTH = 0.6f;
        public static float JETPACK_GLARE_INTENSITY_BASE = 0.0f;
        public static float JETPACK_GLARE_INTENSITY_LENGTH = 15.0f;
        public static float JETPACK_GLARE_SIZE_RADIUS = 2.4f;
        public static float JETPACK_GLARE_SIZE_LENGTH = 0.2f;

        private void DrawJetpackThrusts(bool updateCalled)
        {
            MyCharacter character = m_skinnedEntity as MyCharacter;
	        if (character == null || character.GetCurrentMovementState() == MyCharacterMovementEnum.Died)
		        return;

	        var jetpack = character.JetpackComp;

            if (jetpack == null || !jetpack.CanDrawThrusts)
                return;

            var thrustComponent = Container.Get<MyEntityThrustComponent>();
            if (thrustComponent == null)
                return;

            //VRageRender.MyRenderProxy.DebugDrawLine3D(WorldMatrix.Translation, WorldMatrix.Translation + Physics.LinearAcceleration, Color.White, Color.Green, false);

            var worldToLocal = MatrixD.Invert(Container.Entity.PositionComp.WorldMatrix);
            foreach (MyJetpackThrust thrust in m_jetpackThrusts)
            {
	            Vector3D position = Vector3D.Zero;

                if ((jetpack.TurnedOn && jetpack.IsPowered) && (!character.IsInFirstPersonView ||character != MySession.Static.LocalCharacter))
                {
                    var thrustMatrix = (MatrixD)thrust.ThrustMatrix * Container.Entity.PositionComp.WorldMatrix;
                    Vector3D forward = Vector3D.TransformNormal(thrust.Forward, thrustMatrix);
                    position = thrustMatrix.Translation;
                    position += forward * thrust.Offset;

                    float flameScale = 0.05f;
                    if (updateCalled)
                        thrust.ThrustRadius = MyUtils.GetRandomFloat(0.9f, 1.1f) * flameScale;

                    float strength = Vector3.Dot(forward, -Vector3.Transform(thrustComponent.FinalThrust, Entity.WorldMatrix.GetOrientation())/Entity.Physics.Mass);

                    strength = MathHelper.Clamp(strength * 0.09f, 0.1f, 1f);

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
                        MyTransparentGeometry.AddLineBillboard(thrust.ThrustMaterial, thrust.Light.Color * alphaCone * strength, position,
                            GetRenderObjectID(), ref worldToLocal, forward, thrust.ThrustLength, thrust.ThrustThickness, MyBillboard.BlenType.AdditiveBottom);
                    }

                    if (thrust.ThrustRadius > 0)
                        MyTransparentGeometry.AddPointBillboard(thrust.ThrustMaterial, thrust.Light.Color, position, GetRenderObjectID(), ref worldToLocal, 
                            thrust.ThrustRadius, 0, -1, MyBillboard.BlenType.AdditiveBottom);
                }
                else
                {
                    if (updateCalled || (character.IsUsing != null))
                        thrust.ThrustRadius = 0;
                }

                if (thrust.Light != null)
                {
                    if (thrust.ThrustRadius > 0)
                    {
                        thrust.Light.LightOn = true;
                        thrust.Light.Intensity = JETPACK_LIGHT_INTENSITY_BASE + thrust.ThrustLength * JETPACK_LIGHT_INTENSITY_LENGTH;

                        thrust.Light.Range = thrust.ThrustRadius * JETPACK_LIGHT_RANGE_RADIUS + thrust.ThrustLength * JETPACK_LIGHT_RANGE_LENGTH;
                        thrust.Light.Position = Vector3D.Transform(position, MatrixD.Invert(Container.Entity.PositionComp.WorldMatrix));
                        thrust.Light.ParentID = GetRenderObjectID();

                        thrust.Light.GlareOn = true;

                        thrust.Light.GlareIntensity = JETPACK_GLARE_INTENSITY_BASE + thrust.ThrustLength * JETPACK_GLARE_INTENSITY_LENGTH;

                        thrust.Light.GlareMaterial = "GlareJetpack";
                        thrust.Light.GlareType = VRageRender.Lights.MyGlareTypeEnum.Normal;

                        thrust.Light.GlareSize = (thrust.ThrustRadius * JETPACK_GLARE_SIZE_RADIUS + thrust.ThrustLength * JETPACK_GLARE_SIZE_LENGTH) * thrust.ThrustGlareSize;

                        thrust.Light.GlareQuerySize = 0.1f;
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

			if (definition.Jetpack == null)
				return;

            foreach (var thrustDefinition in definition.Jetpack.Thrusts)
            {
                int index;
                var thrustBone = m_skinnedEntity.AnimationController.FindBone(thrustDefinition.ThrustBone, out index);
	            if (thrustBone == null)
					continue;

	            InitJetpackThrust(index, Vector3.Forward, thrustDefinition.SideFlameOffset, ref definition.Jetpack.ThrustProperties); // UP is now in -Z
	            InitJetpackThrust(index, Vector3.Left, thrustDefinition.SideFlameOffset, ref definition.Jetpack.ThrustProperties);
	            InitJetpackThrust(index, Vector3.Right, thrustDefinition.SideFlameOffset, ref definition.Jetpack.ThrustProperties);
	            InitJetpackThrust(index, Vector3.Backward, thrustDefinition.SideFlameOffset, ref definition.Jetpack.ThrustProperties); // DOWN is now in Z                    
	            InitJetpackThrust(index, Vector3.Up, thrustDefinition.FrontFlameOffset, ref definition.Jetpack.ThrustProperties); // FORWARD is now in Y
            }
        }

        private void InitJetpackThrust(int bone, Vector3 forward, float offset, ref MyObjectBuilder_ThrustDefinition thrustProperties)
        {
            var thrust = new MyJetpackThrust()
            {
                Bone = bone,
                Forward = forward,
                Offset = offset,
                ThrustMaterial = thrustProperties.FlamePointMaterial,
                ThrustGlareSize = thrustProperties.FlameGlareSize
            };

            thrust.Light = MyLights.AddLight();
            thrust.Light.ReflectorDirection = Container.Entity.PositionComp.WorldMatrix.Forward;
            thrust.Light.ReflectorUp = Container.Entity.PositionComp.WorldMatrix.Up;
            thrust.Light.ReflectorRange = 1;
            thrust.Light.Color = thrustProperties.FlameIdleColor;
            thrust.Light.Start(MyLight.LightTypeEnum.PointLight, 1);

            m_jetpackThrusts.Add(thrust);
        }

        public void InitLight(MyCharacterDefinition definition)
        {
            m_light = MyLights.AddLight();

            m_light.Start(MyLight.LightTypeEnum.PointLight | MyLight.LightTypeEnum.Spotlight, 0.5f);

            /// todo: defaults should be supplied from Environemnt.sbc
            m_light.GlossFactor = 0;
            m_light.DiffuseFactor = 3.14f;
            m_light.UseInForwardRender = true;
            m_light.LightOwner = MyLight.LightOwnerEnum.SmallShip;
            m_light.ShadowDistance = 20;
            m_light.ReflectorFalloff = 10;

            m_light.ReflectorTexture = "Textures\\Lights\\dual_reflector_2.dds";
            m_light.ReflectorColor = MyCharacter.REFLECTOR_COLOR;
            m_light.ReflectorConeMaxAngleCos = MyCharacter.REFLECTOR_CONE_ANGLE;
            m_light.ReflectorRange = MyCharacter.REFLECTOR_RANGE;
            m_light.ReflectorGlossFactor = MyCharacter.REFLECTOR_GLOSS_FACTOR;
            m_light.ReflectorDiffuseFactor = MyCharacter.REFLECTOR_DIFFUSE_FACTOR;
            m_light.Color = MyCharacter.POINT_COLOR;
            m_light.SpecularColor = new Vector3(MyCharacter.POINT_COLOR_SPECULAR);
            m_light.Range = MyCharacter.POINT_LIGHT_RANGE;

            m_skinnedEntity.AnimationController.FindBone(definition.LeftLightBone, out m_leftLightIndex);

            m_skinnedEntity.AnimationController.FindBone(definition.RightLightBone, out m_rightLightIndex);
        }

        public void UpdateLightProperties(float currentLightPower)
        {
            if (m_light != null)
            {
                m_light.ReflectorIntensity = MyCharacter.REFLECTOR_INTENSITY * currentLightPower;
                m_light.Intensity = MyCharacter.POINT_LIGHT_INTENSITY * currentLightPower;

                //MyTrace.Send(TraceWindow.Default, m_currentLightPower.ToString());

                m_light.UpdateLight();
            }
        }

        public void UpdateLightPosition()
        {
            if (m_light != null)
            {
                MyCharacter character = m_skinnedEntity as MyCharacter;

                m_lightLocalPosition = character.Definition.LightOffset;

                MatrixD headMatrix = character.GetHeadMatrix(false, true, false);
                MatrixD headMatrixAnim = character.GetHeadMatrix(false, true, true, true);

                if (m_oldReflectorAngle != MyCharacter.REFLECTOR_DIRECTION)
                {
                    m_oldReflectorAngle = MyCharacter.REFLECTOR_DIRECTION;
                    m_reflectorAngleMatrix = MatrixD.CreateFromAxisAngle(headMatrix.Forward, MathHelper.ToRadians(MyCharacter.REFLECTOR_DIRECTION));
                }

                m_light.ReflectorDirection = Vector3.Transform(headMatrix.Forward, m_reflectorAngleMatrix);
                m_light.ReflectorUp = headMatrix.Up;
                m_light.Position = Vector3D.Transform(m_lightLocalPosition, headMatrixAnim);
                m_light.UpdateLight();

                Matrix[] boneMatrices = character.BoneAbsoluteTransforms;

                if (m_leftLightIndex != -1)
                {
                    MatrixD leftGlareMatrix = m_reflectorAngleMatrix * MatrixD.Normalize(boneMatrices[m_leftLightIndex]) * m_skinnedEntity.PositionComp.WorldMatrix;
                    m_leftGlarePosition = leftGlareMatrix.Translation;
                }

                if (m_rightLightIndex != -1)
                {
                    MatrixD rightGlareMatrix = m_reflectorAngleMatrix * MatrixD.Normalize(boneMatrices[m_rightLightIndex]) * m_skinnedEntity.PositionComp.WorldMatrix;
                    m_rightGlarePosition = rightGlareMatrix.Translation;
                }
            }
        }

        public void UpdateLight(float lightPower, bool updateRenderObject)
        {
            if (m_light == null)
                return;

            bool enabled = (lightPower > 0.0f);
            if (m_light.LightOn != enabled)
            {
                m_light.ReflectorOn = enabled;
                m_light.LightOn = enabled;
            }
            if (updateRenderObject)
            {
                UpdateLightPosition();

                VRageRender.MyRenderProxy.UpdateModelProperties(
                    RenderObjectIDs[0],
                    0,
                    -1,
                    "Light",
                    null,
                    null,
                    null);

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

            foreach (var thrust in m_jetpackThrusts)
            {
                MyLights.RemoveLight(thrust.Light);
            }
            m_jetpackThrusts.Clear();
        }

        #endregion
    }
}

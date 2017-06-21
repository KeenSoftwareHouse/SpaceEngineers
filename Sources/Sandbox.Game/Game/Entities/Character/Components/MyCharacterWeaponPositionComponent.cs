using Sandbox.Game.Weapons;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;
using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using VRageRender.Animations;
using VRage.Utils;
using VRageRender;

namespace Sandbox.Game.Entities.Character.Components
{
    /// <summary>
    /// Weapon positioning.
    /// </summary>
    public class MyCharacterWeaponPositionComponent : MyCharacterComponent
    {
        // --- Logical position --- (the position used by game logic)

        public Vector3D LogicalPositionLocalSpace { get; private set; }
        public Vector3D LogicalPositionWorld { get; private set; }
        public Vector3D LogicalOrientationWorld { get; private set; }
        public Vector3D LogicalCrosshairPoint { get; private set; }

        // --- Graphical position --- (the position displayed locally, depends on bone transforms, 1st/3rd person camera)

        //public Vector3D GraphicalPositionLocalSpace { get; private set; }
        public Vector3D GraphicalPositionWorld { get; private set; }
        public float ArmsIkWeight { get; private set; }

        // --- Helper variables ---

        private float m_animationToIKDelay = 0.3f; //s
        private float m_currentAnimationToIkTime = 0.3f;
        private int m_animationToIkState; //0 - none, -1 IK to Animation, 1 AnimationToIK

        private Vector4 m_weaponPositionVariantWeightCounters = new Vector4(1, 0, 0, 0);
        private float m_sprintStatusWeight = 0.0f;
        private float m_sprintStatusGainSpeed = 1.0f / (MyEngineConstants.UPDATE_STEPS_PER_SECOND * 0.25f);

        private float m_backkickSpeed;
        private float m_backkickPos;

        private bool m_lastStateWasFalling;
        private bool m_lastStateWasCrouching;
        private float m_suppressBouncingForTimeSec;
        private float m_lastLocalRotX;

        private readonly MyAverageFiltering m_spineRestPositionX = new MyAverageFiltering(16);
        private readonly MyAverageFiltering m_spineRestPositionY = new MyAverageFiltering(16);
        private readonly MyAverageFiltering m_spineRestPositionZ = new MyAverageFiltering(16);
        
        // --- TODO: magic numbers, should be moved to definition
        static readonly Vector3 m_weaponIronsightTranslation = new Vector3(0.0f, -0.11f, -0.22f);
        static readonly Vector3 m_toolIronsightTranslation = new Vector3(0.0f, -0.21f, -0.25f);
        static readonly float m_suppressBouncingDelay = 0.5f;
        
        /// <summary>
        /// Initialize from character object builder.
        /// </summary>
        public virtual void Init(MyObjectBuilder_Character characterBuilder)
        {
            
        }

        /// <summary>
        /// Update weapon position, either logical and graphical.
        /// </summary>
        public void Update(bool timeAdvanced = true)
        {
            if (Character.Definition == null)
                return;

            UpdateLogicalWeaponPosition();
            if (!Engine.Platform.Game.IsDedicated)
            {
                if (timeAdvanced)
                {
                    m_backkickSpeed *= 0.85f;
                    m_backkickPos = m_backkickPos * 0.5f + m_backkickSpeed;
                }

                UpdateIkTransitions();
                UpdateGraphicalWeaponPosition();
            }

            m_lastStateWasFalling = Character.IsFalling;
            m_lastStateWasCrouching = Character.IsCrouching;
            if (timeAdvanced)
            {
                m_suppressBouncingForTimeSec -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if (m_suppressBouncingForTimeSec < 0)
                    m_suppressBouncingForTimeSec = 0;
            }
        }

        /// <summary>
        /// Update and get current weights of the weapon variants.
        /// </summary>
        /// <returns>Weights. Normalized.</returns>
        Vector4D UpdateAndGetWeaponVariantWeights(MyHandItemDefinition handItemDefinition)
        {
            float characterSpeed;
            Character.AnimationController.Variables.GetValue(MyAnimationVariableStorageHints.StrIdSpeed, out characterSpeed);
            bool isWalkingState = MyCharacter.IsRunningState(Character.GetCurrentMovementState()) && characterSpeed > Character.Definition.MaxWalkSpeed;
            bool isShooting = (Character.IsShooting(MyShootActionEnum.PrimaryAction) || Character.IsShooting(MyShootActionEnum.SecondaryAction)) && (!Character.IsSprinting);
            bool isInIronSight = Character.ZoomMode == MyZoomModeEnum.IronSight && (!Character.IsSprinting);

            float deltaW = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS / handItemDefinition.BlendTime;
            float deltaShootW = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS / handItemDefinition.ShootBlend;
            // blend into the current variant
            // if currently shooting/ironsight -> use "shooting" blend speed
            m_weaponPositionVariantWeightCounters.X += !isWalkingState && !isShooting && !isInIronSight ? deltaW : (isShooting || isInIronSight ? -deltaShootW : -deltaW);
            m_weaponPositionVariantWeightCounters.Y += isWalkingState && !isShooting && !isInIronSight ? deltaW : (isShooting || isInIronSight ? -deltaShootW : -deltaW);
            m_weaponPositionVariantWeightCounters.Z += isShooting && !isInIronSight ? deltaShootW : (isInIronSight ? -deltaShootW : -deltaW);
            m_weaponPositionVariantWeightCounters.W += isInIronSight ? deltaShootW : (isShooting ? -deltaShootW : -deltaW);
            m_weaponPositionVariantWeightCounters = Vector4.Clamp(m_weaponPositionVariantWeightCounters, Vector4.Zero, Vector4.One);

            Vector4D rtnWeights = new Vector4D(MathHelper.SmoothStep(0, 1, m_weaponPositionVariantWeightCounters.X),
                MathHelper.SmoothStep(0, 1, m_weaponPositionVariantWeightCounters.Y),
                MathHelper.SmoothStep(0, 1, m_weaponPositionVariantWeightCounters.Z),
                MathHelper.SmoothStep(0, 1, m_weaponPositionVariantWeightCounters.W));

            double weightSum = rtnWeights.X + rtnWeights.Y + rtnWeights.Z + rtnWeights.W;
            return rtnWeights / weightSum;
        }

        /// <summary>
        /// Update shown position of the weapon.
        /// </summary>
        private void UpdateGraphicalWeaponPosition()
        {
            var animController = Character.AnimationController;
            MyHandItemDefinition handItemDefinition = Character.HandItemDefinition;
            if (handItemDefinition == null || Character.CurrentWeapon == null || animController.CharacterBones == null)
                return;
            // ---------

            // gather useful variables
            bool isLocallyControlled = Character.ControllerInfo.IsLocallyControlled();
            bool isInFirstPerson = (Character.IsInFirstPersonView || Character.ForceFirstPersonCamera) && isLocallyControlled;
            bool flying = Character.JetpackRunning;
            if (m_lastStateWasFalling && flying)
            {
                m_currentAnimationToIkTime = m_animationToIKDelay * (float)Math.Cos(Character.HeadLocalXAngle - m_lastLocalRotX);
            }
            if (m_lastStateWasCrouching != Character.IsCrouching)
            {
                m_suppressBouncingForTimeSec = m_suppressBouncingDelay;
            }
            if (m_suppressBouncingForTimeSec > 0)
            {
                m_spineRestPositionX.Clear();
                m_spineRestPositionY.Clear();
                m_spineRestPositionZ.Clear();
            }

            m_lastLocalRotX = Character.HeadLocalXAngle;
            // get head matrix
            MatrixD weaponMatrixPositioned = Character.GetHeadMatrix(false, !flying, false, true, preferLocalOverSync: true) * Character.PositionComp.WorldMatrixInvScaled;
            if (!isInFirstPerson && animController.CharacterBones.IsValidIndex(Character.HeadBoneIndex))
            {
                // apply feet ik (head bone is stabilized)
                weaponMatrixPositioned.M42 += animController.CharacterBonesSorted[0].Translation.Y;
            }

            // ---------

            // mix positioning matrices (variants: stand/walk/shoot/ironsight), all in character local space 

            // standing (IK)
            MatrixD standingMatrix = isInFirstPerson ? handItemDefinition.ItemLocation : handItemDefinition.ItemLocation3rd;
            // walking (IK)
            MatrixD walkingMatrix = isInFirstPerson ? handItemDefinition.ItemWalkingLocation : handItemDefinition.ItemWalkingLocation3rd;
            // shooting (IK)
            MatrixD shootingMatrix = isInFirstPerson ? handItemDefinition.ItemShootLocation : handItemDefinition.ItemShootLocation3rd;
            // ironsight (IK)
            MatrixD ironsightMatrix = handItemDefinition.ItemIronsightLocation;
            // animation pose
            MatrixD weaponAnimMatrix = animController.CharacterBones.IsValidIndex(Character.WeaponBone)
                ? GetWeaponRelativeMatrix() * animController.CharacterBones[Character.WeaponBone].AbsoluteTransform
                : GetWeaponRelativeMatrix();

            ironsightMatrix.Translation = m_weaponIronsightTranslation;
            if (Character.CurrentWeapon is MyEngineerToolBase)
            {
                ironsightMatrix.Translation = m_toolIronsightTranslation;
            }
            // get weights of all state variants
            Vector4D variantWeights = UpdateAndGetWeaponVariantWeights(handItemDefinition);
            // interpolate matrices to get the resulting one
            MatrixD weaponMatrixLocal = variantWeights.X * standingMatrix
                + variantWeights.Y * walkingMatrix
                + variantWeights.Z * shootingMatrix
                + variantWeights.W * ironsightMatrix;
            weaponMatrixLocal = MatrixD.Normalize(weaponMatrixLocal);

            // weapon positioning - IK weight
            double weaponDataPosWeight = 0;
            if (handItemDefinition.ItemPositioning == MyItemPositioningEnum.TransformFromData && isInFirstPerson
                || handItemDefinition.ItemPositioning3rd == MyItemPositioningEnum.TransformFromData && !isInFirstPerson)
                weaponDataPosWeight += variantWeights.X;
            if (handItemDefinition.ItemPositioningWalk == MyItemPositioningEnum.TransformFromData && isInFirstPerson
                || handItemDefinition.ItemPositioningWalk3rd == MyItemPositioningEnum.TransformFromData && !isInFirstPerson)
                weaponDataPosWeight += variantWeights.Y;
            if (handItemDefinition.ItemPositioningShoot == MyItemPositioningEnum.TransformFromData && isInFirstPerson
                || handItemDefinition.ItemPositioningShoot3rd == MyItemPositioningEnum.TransformFromData && !isInFirstPerson)
                weaponDataPosWeight += variantWeights.Z;
            
            weaponDataPosWeight += variantWeights.W;
            weaponDataPosWeight /= variantWeights.X + variantWeights.Y + variantWeights.Z + variantWeights.W;
            // now computing hand IK weight 
            double armsIkWeight = 0;
            if (handItemDefinition.ItemPositioning != MyItemPositioningEnum.TransformFromAnim && isInFirstPerson
                || handItemDefinition.ItemPositioning3rd != MyItemPositioningEnum.TransformFromAnim && !isInFirstPerson)
                armsIkWeight += variantWeights.X;
            if (handItemDefinition.ItemPositioningWalk != MyItemPositioningEnum.TransformFromAnim && isInFirstPerson
                || handItemDefinition.ItemPositioningWalk3rd != MyItemPositioningEnum.TransformFromAnim && !isInFirstPerson)
                armsIkWeight += variantWeights.Y;
            if (handItemDefinition.ItemPositioningShoot != MyItemPositioningEnum.TransformFromAnim && isInFirstPerson
                || handItemDefinition.ItemPositioningShoot3rd != MyItemPositioningEnum.TransformFromAnim && !isInFirstPerson)
                armsIkWeight += variantWeights.Z;
            armsIkWeight /= variantWeights.X + variantWeights.Y + variantWeights.Z + variantWeights.W;

            ApplyWeaponBouncing(handItemDefinition, ref weaponMatrixLocal, (float)(1.0 - 0.95 * variantWeights.W));
            
            // apply head transform on top of it
            if (!isInFirstPerson)
            {
                weaponMatrixPositioned.M43 += 0.5 * weaponMatrixLocal.M43 * Math.Max(0, weaponMatrixPositioned.M32);   // offset not to interfere with body
                weaponMatrixPositioned.M42 += 0.5 * weaponMatrixLocal.M42 * Math.Max(0, weaponMatrixPositioned.M32);   // offset not to interfere with body
                weaponMatrixPositioned.M42 -= 0.25 * Math.Max(0, weaponMatrixPositioned.M32);   // offset not to interfere with body
                weaponMatrixPositioned.M43 -= 0.05 * Math.Min(0, weaponMatrixPositioned.M32);   // offset not to interfere with body
                weaponMatrixPositioned.M41 -= 0.25 * Math.Max(0, weaponMatrixPositioned.M32);   // offset not to interfere with body
            }
            
            MatrixD weaponMatrixPositionedLocal = weaponMatrixLocal * weaponMatrixPositioned;
            // displace sensor (maybe move to logical part? does not seem to be used at all)
            var characterWeaponEngToolBase = Character.CurrentWeapon as MyEngineerToolBase;
            if (characterWeaponEngToolBase != null)
            {
                characterWeaponEngToolBase.SensorDisplacement = -weaponMatrixLocal.Translation;
            }
            
            // mix plain animation with (anim+ik) result - for example, medieval uses plain animations
            double ikRatio = weaponDataPosWeight * m_currentAnimationToIkTime / m_animationToIKDelay;
            MatrixD weaponFinalWorld = MatrixD.Lerp(weaponAnimMatrix, weaponMatrixPositionedLocal, ikRatio) * Character.WorldMatrix;
            // propagate result to fields
            GraphicalPositionWorld = weaponFinalWorld.Translation;
            ArmsIkWeight = (float)armsIkWeight;
            ((MyEntity)Character.CurrentWeapon).WorldMatrix = weaponFinalWorld;

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
            {
                MyRenderProxy.DebugDrawAxis(weaponFinalWorld, 0.5f, false);
            }
        }

        /// <summary>
        /// Apply bouncing movement on weapon.
        /// </summary>
        /// <param name="handItemDefinition">definition of hand item</param>
        /// <param name="weaponMatrixLocal">current weapon matrix (character local space)</param>
        private void ApplyWeaponBouncing(MyHandItemDefinition handItemDefinition, ref MatrixD weaponMatrixLocal, float fpsBounceMultiplier)
        {
            if (!Character.AnimationController.CharacterBones.IsValidIndex(Character.SpineBoneIndex))
                return;

            bool isLocallyControlled = Character.ControllerInfo.IsLocallyControlled();
            bool isInFirstPerson = (Character.IsInFirstPersonView || Character.ForceFirstPersonCamera) && isLocallyControlled;

            var spineBone = Character.AnimationController.CharacterBones[Character.SpineBoneIndex];
            Vector3 spinePos = spineBone.AbsoluteTransform.Translation - Character.AnimationController.CharacterBonesSorted[0].Translation;
            m_spineRestPositionX.Add(spinePos.X);
            m_spineRestPositionY.Add(spinePos.Y);
            m_spineRestPositionZ.Add(spinePos.Z);
            Vector3 spineAbsRigPos = spineBone.GetAbsoluteRigTransform().Translation;
            Vector3 spineRestPos = new Vector3(spineAbsRigPos.X, m_spineRestPositionY.Get(), spineAbsRigPos.Z);

            Vector3 bounceOffset = (spinePos - spineRestPos) * fpsBounceMultiplier;
            bounceOffset.Z = isInFirstPerson ? bounceOffset.Z : 0;
            m_sprintStatusWeight += Character.IsSprinting ? m_sprintStatusGainSpeed : -m_sprintStatusGainSpeed;
            m_sprintStatusWeight = MathHelper.Clamp(m_sprintStatusWeight, 0, 1);
            if (isInFirstPerson)
            {
                // multiply only when in first person
                bounceOffset *= 1 + Math.Max(0, handItemDefinition.RunMultiplier - 1) * m_sprintStatusWeight;
                bounceOffset.X *= handItemDefinition.XAmplitudeScale;
                bounceOffset.Y *= handItemDefinition.YAmplitudeScale;
                bounceOffset.Z *= handItemDefinition.ZAmplitudeScale;
            }
            else
            {
                bounceOffset *= handItemDefinition.AmplitudeMultiplier3rd;
            }

            bounceOffset.Z += m_backkickPos;

            weaponMatrixLocal.Translation += bounceOffset;
        }

        private Matrix GetWeaponRelativeMatrix()
        {
            if (Character.CurrentWeapon != null && Character.HandItemDefinition != null && Character.AnimationController.CharacterBones.IsValidIndex(Character.WeaponBone))
            {
                return Matrix.Invert(Character.HandItemDefinition.RightHand);
            }
            else
            {
                return Matrix.Identity;
            }
        }

        /// <summary>
        /// Update logical weapon position (model of the weapon will be placed there, bullet will spawn on logical position).
        /// </summary>
        private void UpdateLogicalWeaponPosition()
        {
            Vector3 templogicalPositionLocalSpace; 
            // head position
            if (Character.IsCrouching) 
                templogicalPositionLocalSpace = new Vector3(0, Character.Definition.CharacterCollisionCrouchHeight - Character.Definition.CharacterHeadHeight, 0);
            else
                templogicalPositionLocalSpace = new Vector3(0, Character.Definition.CharacterCollisionHeight - Character.Definition.CharacterHeadHeight, 0);

            // fetch ironsight position
            Vector3 ironsightLocation = m_weaponIronsightTranslation;
            if (Character.CurrentWeapon is MyEngineerToolBase)
            {
                ironsightLocation = m_toolIronsightTranslation;
            }

            // hand item position
            var handItemDef = Character.HandItemDefinition;
            if (handItemDef != null)
                templogicalPositionLocalSpace += Character.ZoomMode == MyZoomModeEnum.IronSight ? ironsightLocation : handItemDef.ItemShootLocation.Translation;

            templogicalPositionLocalSpace.Z = 0;

            // store results
            LogicalPositionLocalSpace = templogicalPositionLocalSpace;
            LogicalPositionWorld = Vector3D.Transform(LogicalPositionLocalSpace, Character.PositionComp.WorldMatrix);

            bool flying = Character.JetpackRunning;

            float headRotXRads = MathHelper.ToRadians(Character.HeadLocalXAngle);
            if (!flying)
                LogicalOrientationWorld = Character.PositionComp.WorldMatrix.Forward * Math.Cos(headRotXRads)
                                          + Character.PositionComp.WorldMatrix.Up * Math.Sin(headRotXRads);
            else
                LogicalOrientationWorld = Character.PositionComp.WorldMatrix.Forward;

            LogicalCrosshairPoint = LogicalPositionWorld + LogicalOrientationWorld * 2000;

            if (Character.CurrentWeapon != null && Character.ControllerInfo.IsLocallyControlled() == false)
            {
                // MZ fix: weapon position not updated on DS
                MyEngineerToolBase tool = Character.CurrentWeapon as MyEngineerToolBase;
                if (tool != null)
                {

                    tool.UpdateSensorPosition();
                }
                else
                {
                    MyHandDrill drill = Character.CurrentWeapon as MyHandDrill;
                    if (drill != null)
                    {
                        drill.WorldPositionChanged(null);
                    }
                }
            }
        }

        /// <summary>
        /// Update interpolation parameters.
        /// </summary>
        internal void UpdateIkTransitions()
        {
            m_animationToIkState = (Character.HandItemDefinition == null || Character.CurrentWeapon == null) ? -1 : 1;

            m_currentAnimationToIkTime += m_animationToIkState * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            if (m_currentAnimationToIkTime >= m_animationToIKDelay)
            {
                m_currentAnimationToIkTime = m_animationToIKDelay;
            }
            else if (m_currentAnimationToIkTime <= 0)
            {
                m_currentAnimationToIkTime = 0;
            }
        }

        public void AddBackkick(float backkickForce)
        {
            const float magic = 1f; // todo: rethink this
            m_backkickSpeed = Math.Max(m_backkickSpeed, backkickForce * magic);
        }
    }
}

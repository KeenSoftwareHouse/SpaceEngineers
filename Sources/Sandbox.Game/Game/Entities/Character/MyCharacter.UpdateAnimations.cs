#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Utils;
using Sandbox.Graphics;
using VRage.Animations;
using VRage.Import;
using VRageMath;
using VRage.Utils;
using VRageRender;
using VRage;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Havok;
using VRage;
using VRage.Library.Utils;
using VRage.FileSystem;


#endregion

namespace Sandbox.Game.Entities.Character
{
    #region Enums

    [Flags]
    public enum MyPlayAnimationMode
    {
        Immediate = 1 << 0,
        WaitForPreviousEnd = 1 << 1,
        JustFirstFrame = 1 << 2,
        Play = 1 << 3,
        Stop = 1 << 4,
    }

    #endregion

    #region Structs

    public struct MyAnimationCommand
    {
        public string AnimationSubtypeName;
        public bool Loop;
        public MyPlayAnimationMode Mode;
        public MyBonesArea Area;
        public float BlendTime;
        public float TimeScale;
    }

    public struct MyFeetIKSettings
    {
        public bool Enabled;
        public float BelowReachableDistance; // distance reachable below the character's rigid body
        public float AboveReachableDistance; // distance reachable above character's ground (how high foot can be placed)
        public float VerticalShiftUpGain; // how quickly can shift character's root
        public float VerticalShiftDownGain; // how quickly can crouch..
        public Vector3 FootSize; // x = foot width, y = foot height, z = foot lenght/size
    }

    #endregion

    public partial class MyCharacter
    {
        #region Fields

        static int MAX_BONES = 120;

        /// <summary>
        /// The model bones
        /// </summary>
        private List<MyCharacterBone> m_bones = new List<MyCharacterBone>();

        public List<MyCharacterBone> Bones { get { return m_bones; } }

        /// <summary>
        /// An associated animation clip player
        /// </summary>
        private AnimationPlayer m_player;
        private AnimationPlayer m_playerNextAnim;

        MyAnimationPlayerBlendPair m_headPlayer;
        MyAnimationPlayerBlendPair m_spinePlayer;
        MyAnimationPlayerBlendPair m_leftHandPlayer;
        MyAnimationPlayerBlendPair m_rightHandPlayer;
        MyAnimationPlayerBlendPair m_leftFingersPlayer;
        MyAnimationPlayerBlendPair m_rightFingersPlayer;

        //Animation blending logic
        Queue<MyAnimationCommand> m_commandQueue = new Queue<MyAnimationCommand>();

        float m_currentBlendTime = 0;
        float m_totalBlendTime = 0;

        //0 - none, 1 - blend in, -1 - blend out
        int m_currentUpperState = 0;
        float m_currentUpperBlendTime = 0;
        float m_totalUpperBlendTime = 0;

        MatrixD m_helperMatrix;

        public Matrix[] BoneTransforms;

        float m_verticalFootError = 0;
        float m_cummulativeVerticalFootError = 0;

        #endregion

        #region Animations init


        void InitAnimations()
        {
            ObtainBones();

            Debug.Assert(TestCharacterBoneDefinitions(), "Warning! Bone definitions in model " + this.ModelName + " are incorrect.");

            BoneTransforms = new Matrix[m_bones.Count];

            m_helperMatrix = Matrix.CreateRotationY((float)Math.PI);
            
            m_player = new AnimationPlayer();
            m_playerNextAnim = new AnimationPlayer();

            string[] bones = null;
            m_characterDefinition.BoneSets.TryGetValue("Head", out bones);
            m_headPlayer = new MyAnimationPlayerBlendPair(this, bones);

            bones = null;
            m_characterDefinition.BoneSets.TryGetValue("Spine", out bones);
            m_spinePlayer = new MyAnimationPlayerBlendPair(this, bones);

            bones = null;
            m_characterDefinition.BoneSets.TryGetValue("LeftHand", out bones);
            m_leftHandPlayer = new MyAnimationPlayerBlendPair(this, bones);

            bones = null;
            m_characterDefinition.BoneSets.TryGetValue("RightHand", out bones);
            m_rightHandPlayer = new MyAnimationPlayerBlendPair(this, bones);

            bones = null;
            m_characterDefinition.BoneSets.TryGetValue("LeftFingers", out bones);
            m_leftFingersPlayer = new MyAnimationPlayerBlendPair(this, bones);

            bones = null;
            m_characterDefinition.BoneSets.TryGetValue("RightFingers", out bones);
            m_rightFingersPlayer = new MyAnimationPlayerBlendPair(this, bones);

            FindBone(m_characterDefinition.HeadBone, out m_headBoneIndex);
            FindBone(m_characterDefinition.Camera3rdBone, out m_camera3rdBoneIndex);
            if (m_camera3rdBoneIndex == -1)
                m_camera3rdBoneIndex = m_headBoneIndex;            
            FindBone(m_characterDefinition.LeftHandIKStartBone, out m_leftHandIKStartBone);
            FindBone(m_characterDefinition.LeftHandIKEndBone, out m_leftHandIKEndBone);
            FindBone(m_characterDefinition.RightHandIKStartBone, out m_rightHandIKStartBone);
            FindBone(m_characterDefinition.RightHandIKEndBone, out m_rightHandIKEndBone);

            FindBone(m_characterDefinition.LeftUpperarmBone, out m_leftUpperarmBone);
            FindBone(m_characterDefinition.LeftForearmBone, out m_leftForearmBone);
            FindBone(m_characterDefinition.RightUpperarmBone, out m_rightUpperarmBone);
            FindBone(m_characterDefinition.RightForearmBone, out m_rightForearmBone);

            FindBone(m_characterDefinition.WeaponBone, out m_weaponBone);
            FindBone(m_characterDefinition.LeftHandItemBone, out m_leftHandItemBone);
            FindBone(m_characterDefinition.RighHandItemBone, out m_rightHandItemBone);
            FindBone(m_characterDefinition.SpineBone, out m_spineBone);

            FindBone(m_characterDefinition.ModelRootBoneName, out m_rootBone);
            FindBone(m_characterDefinition.LeftHipBoneName, out m_leftHipBone);
            FindBone(m_characterDefinition.LeftKneeBoneName, out m_leftKneeBone);
            FindBone(m_characterDefinition.LeftAnkleBoneName, out m_leftAnkleBone);
            FindBone(m_characterDefinition.RightHipBoneName, out m_rightHipBone);
            FindBone(m_characterDefinition.RightKneeBoneName, out m_rightKneeBone);
            FindBone(m_characterDefinition.RightAnkleBoneName, out m_rightAnkleBone);           

            //footDimensions = new Vector3(m_characterDefinition.FootWidth, m_characterDefinition.AnkleHeight, m_characterDefinition.FootLenght);

            UpdateAnimation();
        }


        // FOR DEBUG ONLY - TEST THE LOADED DEFINITIONS OF BONES
        private bool TestCharacterBoneDefinitions()
        {
           bool isOk = true;
            // Test only mandatory bones - add if needed
           if (m_characterDefinition.HeadBone == null)
           {
               Debug.Fail("Warning! Headbone definition is missing! Model: " + this.ModelName);
               isOk = false;                
           }
           //if (m_characterDefinition.ModelRootBoneName == null)
           //{
           //    Debug.Fail("Warning! Rootbone definition is missing! Model: " + this.ModelName);
           //    isOk = false;
           //}
           if (m_characterDefinition.SpineBone == null)
           {
               Debug.Fail("Warning! Spine bone definition is missing! Model: " + this.ModelName);
               isOk = false;
           }
           return isOk;
        }

        /// <summary>
        /// Get the bones from the model and create a bone class object for
        /// each bone. We use our bone class to do the real animated bone work.
        /// </summary>
        private void ObtainBones()
        {
            m_bones.Clear();
            foreach (MyModelBone bone in Model.Bones)
            {
                Matrix boneTransform = bone.Transform;

                if (Model.DataVersion < 01047001)
                {
                    if (bone.Name.Contains("Dummy"))
                    {   //0.009860006
                        boneTransform = boneTransform * Matrix.CreateScale(0.009860006f);
                    }
                }

                // Create the bone object and add to the heirarchy
                MyCharacterBone newBone = new MyCharacterBone(bone.Name, boneTransform, bone.Parent != -1 ? m_bones[bone.Parent] : null);

                if (Model.DataVersion < 01050001)
                {
                    newBone.CompatibilityMode = true;
                }

                // Add to the bones for this model
                m_bones.Add(newBone);
            }
        }

        /// <summary>
        /// Find a bone in this model by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public MyCharacterBone FindBone(string name, out int index)
        {
            index = -1;
            if (name == null) return null;
            foreach (MyCharacterBone bone in m_bones)
            {
                index++;

                if (bone.Name == name)
                    return bone;
            }
            if (MyFakes.ENABLE_BONES_AND_ANIMATIONS_DEBUG)
            {
                Debug.Fail("Warning! Bone with name: " + name + " was not found in the skeleton of model name: " + this.ModelName + ". Pleace check your bone definitions in SBC file.");
            }
            return null;
        }

        Matrix[] m_boneRelativeTransforms;
        public Matrix[] BoneRelativeTransforms { get { return m_boneRelativeTransforms; } }
        void CalculateTransforms()
        {
            ProfilerShort.Begin("MyCharacter.CalculateTransforms");

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Clear bones");
            foreach (var bone in m_bones)
            {
                bone.Translation = Vector3.Zero;
                bone.Rotation = Quaternion.Identity;
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update bones m_player");
            if (m_player.IsInitialized)
            {
                m_player.UpdateBones();
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update bones m_playerNextAnim");
            if (m_playerNextAnim.IsInitialized)
            {
                m_playerNextAnim.UpdateBones();
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();


            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update bones hands");
            m_headPlayer.UpdateBones();
            m_spinePlayer.UpdateBones();
            m_leftHandPlayer.UpdateBones();
            m_rightHandPlayer.UpdateBones();
            m_leftFingersPlayer.UpdateBones();
            m_rightFingersPlayer.UpdateBones();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            m_simulatedBones.Clear();

            if (MyFakes.USE_HAVOK_ANIMATION_FOOT || MyFakes.USE_HAVOK_ANIMATION_HANDS || MyFakes.USE_HAVOK_ANIMATION_HEAD)
            {
                //VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Simulate character physics");

                //List<Quaternion> rotations = new List<Quaternion>();
                //List<Vector3> translations = new List<Vector3>();
                //foreach (var bone in m_bones)
                //{
                //    Matrix transform = bone.ComputeBoneTransform();
                //    Quaternion rotation = Quaternion.CreateFromRotationMatrix(transform);
                //    rotation.Normalize();

                //    Vector3 translation = transform.Translation;

                //    rotations.Add(rotation);
                //    translations.Add(translation);
                //}

                //Physics.CharacterProxy.UpdatePose(rotations, translations);

                //if (this == MySession.ControlledObject)
                //{
                //    Matrix invWorld = Matrix.Invert(WorldMatrix);
                //    Vector3 lookAtTarget = Vector3.Transform(MySector.MainCamera.Position, invWorld);

                //    Matrix leftHand = m_currentWeapon == null? Matrix.Identity : Matrix.CreateTranslation(Vector3.Transform(m_leftHandToolLocalMatrix.Translation, ((MyEntity)m_currentWeapon).WorldMatrix * invWorld));
                //    Matrix rightHand = m_currentWeapon == null ? Matrix.Identity : Matrix.CreateTranslation(Vector3.Transform(m_rightHandToolLocalMatrix.Translation, ((MyEntity)m_currentWeapon).WorldMatrix * invWorld));

                //    float leftFootError;
                //    float rightFootError;
                //    Physics.CharacterProxy.SimulatePose(
                //        m_currentWeapon != null && MyFakes.USE_HAVOK_ANIMATION_HANDS,
                //        !m_weaponIsDrivenByBone && m_currentWeapon != null && MyFakes.USE_HAVOK_ANIMATION_HANDS,
                //        false && MyFakes.USE_HAVOK_ANIMATION_HEAD,
                //        false && MyFakes.USE_HAVOK_ANIMATION_FOOT /*m_currentMovementState == MyCharacterMovementEnum.Standing*/,
                //        leftHand, rightHand, 
                //        Vector3.Zero,
                //        WorldMatrix, out leftFootError, out rightFootError);

                //    m_verticalFootError = System.Math.Min(leftFootError, rightFootError);
                //}

                //Physics.CharacterProxy.GetPoseModelSpace(m_simulatedBones);

                //VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();


                //VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("CopyTransforms");
                //for (int i = 0; i < m_simulatedBones.Count; i++)
                //{
                //    BoneTransformsWrite[i] = m_simulatedBones[i];
                //}
                //VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            }
            else
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ComputeAbsoluteTransforms");
                for (int i = 0; i < m_bones.Count; i++)
                {
                    MyCharacterBone bone = m_bones[i];
                    bone.ComputeAbsoluteTransform();
                }
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                if (m_leftHandItem != null)
                {
                    UpdateLeftHandItemPosition();
                }

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Calculate Hand IK");
                if (m_currentWeapon != null)
                {
                    if (!MyPerGameSettings.UseAnimationInsteadOfIK)
                    {
                        UpdateWeaponPosition(); //mainly IK and some zoom + ironsight stuff
                        if (m_handItemDefinition.SimulateLeftHand && m_leftHandIKStartBone != -1 && m_leftHandIKEndBone != -1 && (!UseAnimationForWeapon && m_animationToIKState == 0))
                        {
                            MatrixD leftHand = (MatrixD)m_handItemDefinition.LeftHand * ((MyEntity)m_currentWeapon).WorldMatrix;
                            CalculateHandIK(m_leftHandIKStartBone, m_leftForearmBone, m_leftHandIKEndBone, ref leftHand);
                            //CalculateHandIK(m_leftHandIKStartBone, m_leftHandIKEndBone, ref leftHand);
                        }

                        if (m_handItemDefinition.SimulateRightHand && m_rightHandIKStartBone != -1 && m_rightHandIKEndBone != -1 && (!UseAnimationForWeapon || m_animationToIKState != 0))
                        {
                            MatrixD rightHand = (MatrixD)m_handItemDefinition.RightHand * ((MyEntity)m_currentWeapon).WorldMatrix;
                            CalculateHandIK(m_rightHandIKStartBone, m_rightForearmBone, m_rightHandIKEndBone, ref rightHand);
                            //CalculateHandIK(m_rightHandIKStartBone, m_rightHandIKEndBone, ref rightHand);
                        }
                    }
                    else
                    {
                        Debug.Assert(m_rightHandItemBone != -1, "Invalid bone for weapon.");
                        if (m_rightHandItemBone != -1)
                        {
                            //use animation for right hand item
                            MatrixD rightHandItemMatrix = m_bones[m_rightHandItemBone].AbsoluteTransform * WorldMatrix;
                            //var rightHandItemMatrix = ((MyEntity)m_currentWeapon).PositionComp.WorldMatrix; //use with UpdateWeaponPosition() but not working for barbarians
                            Vector3D up = rightHandItemMatrix.Up;
                            rightHandItemMatrix.Up = rightHandItemMatrix.Forward;
                            rightHandItemMatrix.Forward = up;
                            rightHandItemMatrix.Right = -rightHandItemMatrix.Right;
                            ((MyEntity)m_currentWeapon).PositionComp.WorldMatrix = rightHandItemMatrix;
                        }
                    }
                }
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                if (m_boneRelativeTransforms == null || m_bones.Count != m_boneRelativeTransforms.Length)
                    m_boneRelativeTransforms = new Matrix[m_bones.Count];

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Saving bone transforms");
                for (int i = 0; i < m_bones.Count; i++)
                {
                    MyCharacterBone bone = m_bones[i];
                    m_boneRelativeTransforms[i] = bone.ComputeBoneTransform();
                    bone.ComputeAbsoluteTransform();
                    BoneTransforms[i] = bone.AbsoluteTransform;
                    m_simulatedBones.Add(bone.AbsoluteTransform);
                }
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }

            ProfilerShort.End();
        }


        void CalculateHandIK(int startBoneIndex, int endBoneIndex, ref MatrixD targetTransform)
        {
            MyCharacterBone endBone = m_bones[endBoneIndex];
            MyCharacterBone startBone = m_bones[startBoneIndex];

            if (Model.DataVersion < 01047001)
            {
                Vector3D pos = targetTransform.Translation;
                targetTransform = MatrixD.CreateRotationZ(MathHelper.Pi) * Matrix.CreateRotationX(MathHelper.Pi) * targetTransform;
                targetTransform.Translation = pos;
            }

            // Solve IK Problem
            List<MyCharacterBone> bones = new List<MyCharacterBone>();

            for (int i = startBoneIndex; i <= endBoneIndex; i++) bones.Add(m_bones[i]);
            MatrixD invWorld = MatrixD.Invert(WorldMatrix);
            Matrix localFinalTransform = targetTransform * invWorld;
            Vector3 finalPos = localFinalTransform.Translation;
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_IKSOLVERS)
            {
                VRageRender.MyRenderProxy.DebugDrawText3D(targetTransform.Translation, "Hand target transform", Color.Purple, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(targetTransform.Translation, 0.03f, Color.Purple, 1, false);
                VRageRender.MyRenderProxy.DebugDrawAxis((MatrixD)targetTransform, 0.03f, false);
            }

            MyInverseKinematics.SolveCCDIk(ref finalPos, bones, 0.0005f, 5, 0.5f, ref localFinalTransform, endBone);
            //MyInverseKinematics.SolveTwoJointsIk(ref finalPos, bones[0], bones[1], bones[2], ref localFinalTransform, WorldMatrix, bones[3],false);

        }

        void CalculateHandIK(int upperarm, int forearm, int palm,  ref MatrixD targetTransform)
        {
            if (MyFakes.ENABLE_BONES_AND_ANIMATIONS_DEBUG)
            {
                Debug.Assert(m_bones.IsValidIndex(upperarm), "UpperArm index for IK is invalid");
                Debug.Assert(m_bones.IsValidIndex(forearm), "ForeArm index for IK is invalid");
                Debug.Assert(m_bones.IsValidIndex(palm), "Palm index for IK is invalid");
            }


            //MyCharacterBone endBone = m_bones[endBoneIndex];
            //MyCharacterBone startBone = m_bones[startBoneIndex];

            if (Model.DataVersion < 01047001)
            {
                Vector3D pos = targetTransform.Translation;
                targetTransform = MatrixD.CreateRotationZ(MathHelper.Pi) * Matrix.CreateRotationX(MathHelper.Pi) * targetTransform;
                targetTransform.Translation = pos;
            }

            // Solve IK Problem
            //List<MyCharacterBone> bones = new List<MyCharacterBone>();

            //for (int i = startBoneIndex; i <= endBoneIndex; i++) bones.Add(m_bones[i]);
            MatrixD invWorld = MatrixD.Invert(WorldMatrix);
            Matrix localFinalTransform = targetTransform * invWorld;
            Vector3 finalPos = localFinalTransform.Translation;
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_IKSOLVERS)
            {
                VRageRender.MyRenderProxy.DebugDrawText3D(targetTransform.Translation, "Hand target transform", Color.Purple, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(targetTransform.Translation, 0.03f, Color.Purple, 1, false);
                VRageRender.MyRenderProxy.DebugDrawAxis((MatrixD)targetTransform, 0.03f, false);
            }

            //MyInverseKinematics.SolveCCDIk(ref finalPos, bones, 0.0005f, 5, 0.5f, ref localFinalTransform, endBone);
            if (m_bones.IsValidIndex(upperarm) && m_bones.IsValidIndex(forearm) && m_bones.IsValidIndex(palm))
            {
                MyInverseKinematics.SolveTwoJointsIkCCD(ref finalPos, m_bones[upperarm], m_bones[forearm], m_bones[palm], ref localFinalTransform, WorldMatrix, m_bones[palm], false);
            }

        }

        /// <summary>
        /// This updates the foot placement in the world using raycasting and finding closest support. 
        /// </summary>
        /// <param name="upDirection">This direction is used to raycast from feet - must be normalized!</param>
        /// <param name="underFeetReachableDistance">How below the original position can character reach down with legs</param>
        /// <param name="maxFootHeight">How high from the original position can be foot placed</param>
        /// <param name="verticalChangeGainUp">How quickly we raise up the character</param>
        /// <param name="verticalChangeGainDown">How quickly we crouch down</param>
        /// <param name="maxDistanceSquared">This is the maximal error in foot placement</param>
        /// <param name="footDimensions">This is foot dimensions, in Y axis is the ankle's height</param>
        /// <param name="footPlacementDistanceSquared">This is the distance limit between calculated and current position to start IK on foot placement</param>
        void UpdateFeetPlacement(Vector3 upDirection, float belowCharacterReachableDistance, float aboveCharacterReachableDistance, float verticalShiftUpGain, float verticalShiftDownGain, Vector3 footDimensions)
        {
            Debug.Assert(footDimensions != Vector3.Zero,"void UpdateFeetPlacement(...) : foot dimensions can not be zero!");

            float ankleHeight = footDimensions.Y;
                        
            // get the current foot matrix and location
            Matrix invWorld = PositionComp.WorldMatrixInvScaled;
            MyCharacterBone rootBone = m_bones[m_rootBone]; // root bone is used to transpose the model up or down
            Matrix modelRootBoneMatrix = rootBone.AbsoluteTransform;
            float verticalShift = modelRootBoneMatrix.Translation.Y;
            Matrix leftFootMatrix = m_bones[m_leftAnkleBone].AbsoluteTransform;
            Matrix rightFootMatrix = m_bones[m_rightAnkleBone].AbsoluteTransform;

            // ok first we get the closest support to feet and we need to know from where to raycast for each foot in world coords
            // we need to raycast from original ground position of the feet, no from the character shifted position            
            // we cast from the ground of the model space, assuming the model's local space up vector is in Y axis
            Vector3 leftFootGroundPosition = new Vector3(leftFootMatrix.Translation.X, 0, leftFootMatrix.Translation.Z);
            Vector3 rightFootGroundPosition = new Vector3(rightFootMatrix.Translation.X, 0, rightFootMatrix.Translation.Z);
            Vector3 fromL = Vector3.Transform(leftFootGroundPosition, WorldMatrix);  // we get this position in the world
            Vector3 fromR = Vector3.Transform(rightFootGroundPosition, WorldMatrix);            

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GetClosestFootPosition");

            // find the closest ground support, raycasting from Up to Down
            var contactLeft = MyInverseKinematics.GetClosestFootSupportPosition(this, null, fromL, upDirection, footDimensions, WorldMatrix, belowCharacterReachableDistance, aboveCharacterReachableDistance, Physics.CharacterCollisionFilter);        // this returns world coordinates of support for left foot
            var contactRight = MyInverseKinematics.GetClosestFootSupportPosition(this, null, fromR, upDirection, footDimensions, WorldMatrix, belowCharacterReachableDistance, aboveCharacterReachableDistance, Physics.CharacterCollisionFilter);
            
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();


            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Characters root shift estimation");

            // if we got hit only for one feet, we do nothing, but slowly return back root bone (character vertical shift) position if it was changed from original
            // that happends very likely when the support below is too far for one leg
            if (contactLeft == null || contactRight == null)
            {
                modelRootBoneMatrix.Translation -= modelRootBoneMatrix.Translation * verticalShiftUpGain;
                rootBone.SetBindTransform(modelRootBoneMatrix);
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                return;
            }

            // Here we recalculate if we shift the root of the character to reach bottom or top
            // get the desired foot world coords

            Vector3 supportL = contactLeft.Value.Position;
            Vector3 supportR = contactRight.Value.Position;

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_CLOSESTSUPPORTPOSITION)
            {
                VRageRender.MyRenderProxy.DebugDrawText3D(supportL, "Foot support position", Color.Blue, 1, false);
                VRageRender.MyRenderProxy.DebugDrawText3D(supportR, "Foot support position", Color.Blue, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(supportL, 0.03f, Color.Blue, 0, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(supportR, 0.03f, Color.Blue, 0, false);               
            }

            // Get the vector between actual feet position and possible support in local model coords
            //                                  local model space coord of desired position + shift it up of ankle heights
            Vector3 leftAnkleDesiredPosition = Vector3.Transform(supportL, invWorld) + ((leftFootMatrix.Translation.Y - verticalShift) * upDirection);
            Vector3 rightAnkleDesiredPosition = Vector3.Transform(supportR, invWorld) + ((rightFootMatrix.Translation.Y - verticalShift) * upDirection);

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_ANKLE_DESIREDPOSITION)
            {
                VRageRender.MyRenderProxy.DebugDrawText3D(supportL, "Ankle desired position", Color.Purple, 1, false);
                VRageRender.MyRenderProxy.DebugDrawText3D(supportR, "Ankle desired position", Color.Purple, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(Vector3.Transform(leftAnkleDesiredPosition, WorldMatrix), 0.03f, Color.Purple, 0, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(Vector3.Transform(rightAnkleDesiredPosition, WorldMatrix), 0.03f, Color.Purple, 0, false);
            }

            // Get the height of found support related to character's position in model's local space, assuming it's Y axis
            float leftAnkleDesiredHeight = leftAnkleDesiredPosition.Y;
            float rightAnkleDesiredHeight = rightAnkleDesiredPosition.Y;

            // if we the distances are too big, so we will not be able to set the position, we can skip it
            if (Math.Abs(leftAnkleDesiredHeight - rightAnkleDesiredHeight) > aboveCharacterReachableDistance)
            {
                modelRootBoneMatrix.Translation -= modelRootBoneMatrix.Translation * verticalShiftUpGain;
                rootBone.SetBindTransform(modelRootBoneMatrix);
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                return;
            }
            
            // if we got one of the supports below the character root, we must check whether we can reach it, if yes, we need to crouch to reach it            
            if ((((leftAnkleDesiredHeight > -belowCharacterReachableDistance) && (leftAnkleDesiredHeight < ankleHeight)) ||      // left support is below model and is reachable
                ((rightAnkleDesiredHeight > -belowCharacterReachableDistance) && (rightAnkleDesiredHeight < ankleHeight))) &&    // right support is below model and is reachable
                // finally check if character is shifted down, the other feet won't get too high
                (Math.Max(leftAnkleDesiredHeight, rightAnkleDesiredHeight) - Math.Min(leftAnkleDesiredHeight, rightAnkleDesiredHeight) < aboveCharacterReachableDistance))               
            {
                // then we can try to reach down according to the difference
                float distanceBelow = Math.Min(leftAnkleDesiredHeight, rightAnkleDesiredHeight)-ankleHeight;
                Vector3 verticalTranslation = upDirection * distanceBelow;
                Vector3 translation = modelRootBoneMatrix.Translation;
                translation.Interpolate3(modelRootBoneMatrix.Translation, verticalTranslation, verticalShiftDownGain);
                modelRootBoneMatrix.Translation = translation;
                rootBone.SetBindTransform(modelRootBoneMatrix);                
            }               
            else // if both supports are up, we need to get up, however, that should be done by rigid body as well.. we limit it only by reachable distance, so it is bounded to rigid body position
                if ((leftAnkleDesiredHeight > ankleHeight) && (leftAnkleDesiredHeight < aboveCharacterReachableDistance) &&
                    (rightAnkleDesiredHeight > ankleHeight) && (rightAnkleDesiredHeight < aboveCharacterReachableDistance))
            {
                // move up to reach the highest support
                float distanceAbove = Math.Max(leftAnkleDesiredHeight, rightAnkleDesiredHeight) - ankleHeight;
                Vector3 verticalTranslation = upDirection * distanceAbove;
                Vector3 translation = modelRootBoneMatrix.Translation;
                translation.Interpolate3(modelRootBoneMatrix.Translation, verticalTranslation, verticalShiftUpGain);
                modelRootBoneMatrix.Translation = translation;
                rootBone.SetBindTransform(modelRootBoneMatrix);
            }
            // finally if we can not get into right vertical position for foot placement, slowly reset the vertical shift
            else
            {
                modelRootBoneMatrix.Translation -= modelRootBoneMatrix.Translation * verticalShiftUpGain;
                rootBone.SetBindTransform(modelRootBoneMatrix);
            }

            // Hard limit to root's shift in vertical position
            //if (characterVerticalShift < -underFeetReachableDistance)
            //{
            //    modelRootBoneMatrix.Translation = -upDirection * underFeetReachableDistance;
            //    rootBone.SetBindTransform(modelRootBoneMatrix);
            //    characterVerticalShift = -underFeetReachableDistance; // get the new height
            //}
            //if (characterVerticalShift > underFeetReachableDistance)
            //{
            //    modelRootBoneMatrix.Translation = upDirection * underFeetReachableDistance;
            //    rootBone.SetBindTransform(modelRootBoneMatrix);
            //    characterVerticalShift = underFeetReachableDistance; // get the new height
            //}

            // Then we need to recalculate all other bones matrices so we get proper data for children, since we changed the root position
            //foreach (var b in m_bones) b.ComputeAbsoluteTransform();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("CalculateFeetPlacement");

            // Then recalculate feet positions only if we can reach the final and if we are in the limits
            if ((-belowCharacterReachableDistance < leftAnkleDesiredHeight) && (leftAnkleDesiredHeight < aboveCharacterReachableDistance)) // and the found foot support height is not over limit                
                CalculateFeetPlacement(
                    m_leftHipBone, 
                    m_leftKneeBone, 
                    m_leftAnkleBone,
                    leftAnkleDesiredPosition,
                    contactLeft.Value.Normal,
                    footDimensions,
                    leftFootMatrix.Translation.Y - verticalShift <= ankleHeight);

            if ((-belowCharacterReachableDistance < rightAnkleDesiredHeight) && (rightAnkleDesiredHeight < aboveCharacterReachableDistance))
                CalculateFeetPlacement(
                    m_rightHipBone,
                    m_rightKneeBone,
                    m_rightAnkleBone,
                    rightAnkleDesiredPosition,
                    contactRight.Value.Normal,
                    footDimensions,
                    rightFootMatrix.Translation.Y - verticalShift <= ankleHeight);

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_BONES)
            {
                List<Matrix> left = new List<Matrix> { m_bones[m_leftHipBone].AbsoluteTransform, m_bones[m_leftKneeBone].AbsoluteTransform, m_bones[m_leftAnkleBone].AbsoluteTransform };
                List<Matrix> right = new List<Matrix> { m_bones[m_rightHipBone].AbsoluteTransform, m_bones[m_rightKneeBone].AbsoluteTransform, m_bones[m_rightAnkleBone].AbsoluteTransform };
                debugDrawBones(left);
                debugDrawBones(right);
                VRageRender.MyRenderProxy.DebugDrawText3D(WorldMatrix.Translation, "Rigid body", Color.Yellow, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(WorldMatrix.Translation, 0.05f, Color.Yellow, 0, false);
                VRageRender.MyRenderProxy.DebugDrawText3D((modelRootBoneMatrix * WorldMatrix).Translation, "Character root bone", Color.Yellow, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere((modelRootBoneMatrix * WorldMatrix).Translation, 0.07f, Color.Red, 0, false);
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Storing bones transforms");

            // After the feet placement we need to save new bone transformations
            if (m_boneRelativeTransforms == null || m_bones.Count != m_boneRelativeTransforms.Length)
                m_boneRelativeTransforms = new Matrix[m_bones.Count];

            for (int i = 0; i < m_bones.Count; i++)
            {
                MyCharacterBone bone = m_bones[i];
                m_boneRelativeTransforms[i] = bone.ComputeBoneTransform();
                //bone.ComputeAbsoluteTransform();
                //BoneTransforms[i] = bone.AbsoluteTransform;
                //m_simulatedBones.Add(bone.AbsoluteTransform);
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }


        void debugDrawBones(List<Matrix> boneTransforms)
        {
            Vector3 p1 = Vector3.Zero;
            Vector3 p2;
            foreach (var boneMat in boneTransforms)
            {

                VRageRender.MyRenderProxy.DebugDrawAxis(boneMat * WorldMatrix, 1f, false);
                p2 = (boneMat * PositionComp.WorldMatrix).Translation;
               
                if (!Vector3.IsZero(p1)) VRageRender.MyRenderProxy.DebugDrawLine3D(p1, p2, Color.Yellow, Color.Yellow, false);
                               
                p1 = p2;
            }
        }


        /// <summary>
        /// This calculates new bone rotations for legs so the foot is placed on supported ground or object, which is found by RayCast
        /// </summary>
        /// <param name="hipBoneIndex">it is the index of the hip bone in m_bones list</param>
        /// <param name="kneeBoneIndex">it is the index of the hip bone in m_bones list</param>
        /// <param name="FeetBoneIndex">it is the index of the hip bone in m_bones list</param>
        /// <param name="footPosition">this is the model space of the final foot placement, it is then recalculated to local model space</param>
        void CalculateFeetPlacement(int hipBoneIndex, int kneeBoneIndex, int ankleBoneIndex, Vector3 footPosition, Vector3 footNormal, Vector3 footDimensions, bool setFootTransform)
        {
                        
            Vector3 endPos = footPosition;
           
            List<MyCharacterBone> boneTransforms = new List<MyCharacterBone>();
            boneTransforms.Add(m_bones[hipBoneIndex]);
            boneTransforms.Add(m_bones[kneeBoneIndex]);
            boneTransforms.Add(m_bones[ankleBoneIndex]);

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Calculate foot transform");

            Matrix finalAnkleTransform = Matrix.Identity;

            if (setFootTransform)
            {           
                // compute rotation based on normal of foot support position
                Vector3 currentUp = WorldMatrix.Up;
                currentUp.Normalize();
                Vector3 crossResult = Vector3.Cross(currentUp, footNormal);
                crossResult.Normalize();
                double cosAngle = currentUp.Dot(footNormal);
                cosAngle = MathHelper.Clamp(cosAngle, -1, 1);
                double turnAngle = Math.Acos(cosAngle);	// get the angle
                Matrix rotation = Matrix.CreateFromAxisAngle((Vector3)crossResult, (float)turnAngle);

                // now rotate the model world to the rotation
                if (rotation.IsValid()) finalAnkleTransform = WorldMatrix * rotation;
                else finalAnkleTransform = WorldMatrix;
                // but the position of this world will be different
                finalAnkleTransform.Translation = Vector3.Transform(footPosition, WorldMatrix);

                // compute transformation in model space
                MatrixD invWorld = MatrixD.Invert(WorldMatrix);
                finalAnkleTransform = finalAnkleTransform * invWorld;

                // get the original ankleSpace
                MatrixD originalAngleTransform = m_bones[ankleBoneIndex].BindTransform * m_bones[ankleBoneIndex].Parent.AbsoluteTransform;

                // now it needs to be related to rig transform
                finalAnkleTransform = originalAngleTransform.GetOrientation() * finalAnkleTransform.GetOrientation();
             
            }

            finalAnkleTransform.Translation = footPosition;

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
          

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("IK Calculation");

            MyInverseKinematics.SolveTwoJointsIk(ref endPos, m_bones[hipBoneIndex], m_bones[kneeBoneIndex], m_bones[ankleBoneIndex], ref finalAnkleTransform, WorldMatrix, setFootTransform ? m_bones[ankleBoneIndex] : null, false);

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();


            // draw our foot placement transformation
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_ANKLE_FINALPOS)
            {
                Matrix debug = finalAnkleTransform * WorldMatrix;
                Matrix debug2 = m_bones[ankleBoneIndex].AbsoluteTransform * WorldMatrix;
                VRageRender.MyRenderProxy.DebugDrawText3D(debug.Translation, "Final ankle position", Color.Red, 1.0f, false);
                VRageRender.MyRenderProxy.DebugDrawOBB(Matrix.CreateScale(footDimensions) * debug, Color.Red, 1, false, false);
                VRageRender.MyRenderProxy.DebugDrawText3D(debug2.Translation, "Actual ankle position", Color.Green, 1.0f, false);
                VRageRender.MyRenderProxy.DebugDrawOBB(Matrix.CreateScale(footDimensions) * debug2, Color.Green, 1, false, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(debug.Translation, debug.Translation + debug.Forward * 0.5f, Color.Yellow, Color.Yellow, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(debug.Translation, debug.Translation + debug.Up * 0.2f, Color.Purple, Color.Purple, false);
            }
           
        }

        #endregion

        #region Animations update

        void FlushAnimationQueue()
        {
            while (m_commandQueue.Count > 0)
                UpdateAnimation();
        }

        void UpdateAnimation()
        {
            float stepTime = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            if (m_player.IsInitialized)
            {
                m_player.Advance(stepTime);
            }

            if (m_playerNextAnim.IsInitialized)
            {
                m_currentBlendTime += stepTime;
            }

            m_headPlayer.Advance();
            m_spinePlayer.Advance();
            m_leftHandPlayer.Advance();
            m_rightHandPlayer.Advance();
            m_leftFingersPlayer.Advance();
            m_rightFingersPlayer.Advance();

            if (m_leftHandPlayer.GetState() == MyAnimationPlayerBlendPair.AnimationBlendState.Stopped && m_leftHandItem != null)
            {
                m_leftHandItem.Close();
                m_leftHandItem = null;
            }

            if (m_commandQueue.Count > 0)
            {
                MyAnimationCommand command = m_commandQueue.Peek();

                if ((command.Mode & MyPlayAnimationMode.Play) == MyPlayAnimationMode.Play)
                {
                    m_commandQueue.Dequeue();

                    MyAnimationDefinition animDefinition;
                    if (!TryGetAnimationDefinition(command.AnimationSubtypeName, out animDefinition))
                        return;

                    if (animDefinition.AllowWithWeapon)
                    {
                        if (!UseAnimationForWeapon)
                        {
                            StoreWeaponRelativeMatrix();
                            UseAnimationForWeapon = true;
                            ResetWeaponAnimationState = true;
                        }
                    }

                    if (!animDefinition.LeftHandItem.TypeId.IsNull)
                    {
                        if (m_leftHandItem != null)
                        {
                            (m_leftHandItem as IMyHandheldGunObject<Sandbox.Game.Weapons.MyDeviceBase>).OnControlReleased();
                            m_leftHandItem.Close();
                        }

                        m_leftHandItem = MyEntityFactory.CreateEntity(animDefinition.LeftHandItem.TypeId);
                        var ob = MyEntityFactory.CreateObjectBuilder(m_leftHandItem);
                        m_leftHandItem.Init(ob);

                        (m_leftHandItem as IMyHandheldGunObject<Sandbox.Game.Weapons.MyDeviceBase>).OnControlAcquired(this);
                        UpdateLeftHandItemPosition();

                        MyEntities.Add(m_leftHandItem);
                       
                    }
                    bool firstFrame = (command.Mode & MyPlayAnimationMode.JustFirstFrame) == MyPlayAnimationMode.JustFirstFrame;
                    if ((animDefinition.InfluenceArea & MyBonesArea.Head) == MyBonesArea.Head)
                        m_headPlayer.Play(animDefinition, command.Loop, command.BlendTime, command.TimeScale, firstFrame);

                    if ((animDefinition.InfluenceArea & MyBonesArea.Spine) == MyBonesArea.Spine)
                        m_spinePlayer.Play(animDefinition, command.Loop, command.BlendTime, command.TimeScale, firstFrame);

                    if ((animDefinition.InfluenceArea & MyBonesArea.LeftHand) == MyBonesArea.LeftHand)
                        m_leftHandPlayer.Play(animDefinition, command.Loop, command.BlendTime, command.TimeScale, firstFrame);

                    if ((animDefinition.InfluenceArea & MyBonesArea.RightHand) == MyBonesArea.RightHand)
                        m_rightHandPlayer.Play(animDefinition, command.Loop, command.BlendTime, command.TimeScale, firstFrame);

                    if ((animDefinition.InfluenceArea & MyBonesArea.LeftFingers) == MyBonesArea.LeftFingers)
                        m_leftFingersPlayer.Play(animDefinition, command.Loop, command.BlendTime, command.TimeScale, firstFrame);

                    if ((animDefinition.InfluenceArea & MyBonesArea.RightFingers) == MyBonesArea.RightFingers)
                        m_rightFingersPlayer.Play(animDefinition, command.Loop, command.BlendTime, command.TimeScale, firstFrame);

                    if ((animDefinition.InfluenceArea & MyBonesArea.Body) == MyBonesArea.Body)
                        PlayAnimation(animDefinition, command.Loop, command.BlendTime, command.TimeScale, command.Mode);
                }

                else if ((command.Mode & MyPlayAnimationMode.Stop) == MyPlayAnimationMode.Stop)
                {
                    m_commandQueue.Dequeue();

                    if ((command.Area & MyBonesArea.Head) == MyBonesArea.Head)
                        m_headPlayer.Stop(command.BlendTime);

                    if ((command.Area & MyBonesArea.Spine) == MyBonesArea.Spine)
                        m_spinePlayer.Stop(command.BlendTime);

                    if ((command.Area & MyBonesArea.LeftHand) == MyBonesArea.LeftHand)
                        m_leftHandPlayer.Stop(command.BlendTime);

                    if ((command.Area & MyBonesArea.RightHand) == MyBonesArea.RightHand)
                        m_rightHandPlayer.Stop(command.BlendTime);

                    if ((command.Area & MyBonesArea.LeftFingers) == MyBonesArea.LeftFingers)
                        m_leftFingersPlayer.Stop(command.BlendTime);

                    if ((command.Area & MyBonesArea.RightFingers) == MyBonesArea.RightFingers)
                        m_rightFingersPlayer.Stop(command.BlendTime);
                }

                else if((command.Mode & (MyPlayAnimationMode.Immediate | MyPlayAnimationMode.JustFirstFrame | MyPlayAnimationMode.WaitForPreviousEnd)) != 0)
                //if ((command.Mode == MyPlayAnimationMode.Immediate) || (command.Mode == MyPlayAnimationMode.JustFirstFrame) || (command.Mode == MyPlayAnimationMode.WaitForPreviousEnd))
                    if (m_currentBlendTime <= 0)
                    {
                        m_commandQueue.Dequeue();
                        
                        MyAnimationDefinition animDefinition;
                        if (!TryGetAnimationDefinition(command.AnimationSubtypeName, out animDefinition))
                            return;

                        PlayAnimation(animDefinition, command.Loop, command.BlendTime, command.TimeScale, command.Mode);
                    }
            }

            float blendRatio = 0;
            if (m_playerNextAnim.IsInitialized && m_currentBlendTime > 0)
            {
                //0.. full current animation
                //1.. full next animation
                blendRatio = 1;
                if (m_totalBlendTime > 0)
                    blendRatio = MathHelper.Clamp(m_currentBlendTime / m_totalBlendTime, 0, 1);

                if (blendRatio > 0)
                {
                    m_playerNextAnim.Advance(stepTime);
                }
            }

            //Advance to next animation
            if (blendRatio == 1 && m_playerNextAnim.IsInitialized)
            {
                m_player.Initialize(m_playerNextAnim);
                m_playerNextAnim.Done();
                blendRatio = 0;
                m_currentBlendTime = 0;
            }

            if (m_player.IsInitialized)
            {
                m_player.Weight = 1; 
            }

            if (m_playerNextAnim.IsInitialized)
            {
                m_playerNextAnim.Weight = blendRatio;
            }

            m_headPlayer.UpdateAnimation();
            m_spinePlayer.UpdateAnimation();
            m_leftHandPlayer.UpdateAnimation();
            m_rightHandPlayer.UpdateAnimation();
            m_leftFingersPlayer.UpdateAnimation();
            m_rightFingersPlayer.UpdateAnimation();

            // check if animations on hands stopped and whether we need to reset the state for weapon positioning
            if (ResetWeaponAnimationState)
            {
                if ((m_rightHandPlayer.GetState() == MyAnimationPlayerBlendPair.AnimationBlendState.Stopped) &&
                    (m_rightFingersPlayer.GetState() == MyAnimationPlayerBlendPair.AnimationBlendState.Stopped))
                {
                    ResetWeaponAnimationState = false;
                    UseAnimationForWeapon = false;
                }
            }
        }


        /// <summary>
        /// Play an animation clip
        /// </summary>
        /// <param name="clip">The clip to play</param>
        /// <returns>The player that will play this clip</returns>
        private void PlayAnimation(MyAnimationDefinition animationDefinition, bool loop, float blendTime, float timeScale, MyPlayAnimationMode mode)
        {
            string model = animationDefinition.AnimationModel;
            int clipIndex = animationDefinition.ClipIndex;

            if (string.IsNullOrEmpty(model))
                return;

            if (animationDefinition.Status == MyAnimationDefinition.AnimationStatus.Unchecked)
            {
                var fsPath = System.IO.Path.IsPathRooted(model) ? model : System.IO.Path.Combine(MyFileSystem.ContentPath, model);
                if (!MyFileSystem.FileExists(fsPath))
                {
                    animationDefinition.Status = MyAnimationDefinition.AnimationStatus.Failed;
                    return;
                }
            }

            animationDefinition.Status = MyAnimationDefinition.AnimationStatus.OK;

            MyModel animation = MyModels.GetModelOnlyAnimationData(model);
            AnimationClip clip = animation.Animations.Clips[clipIndex];

            // Create a clip player and assign it to this model
            m_playerNextAnim.Initialize(clip, this, 1, timeScale, mode == MyPlayAnimationMode.JustFirstFrame);
            m_playerNextAnim.Looping = loop;

            m_currentBlendTime = 0;

            float actualTimeToEnd = 0;

            if (m_player.IsInitialized)
            {
                actualTimeToEnd = m_player.Duration - m_player.Position;

                //from idle to anything
                if (actualTimeToEnd > 0 && m_player.Looping)
                    actualTimeToEnd = blendTime;
            }

            //blend always that time it was required
            m_totalBlendTime = blendTime;

            if (mode == MyPlayAnimationMode.WaitForPreviousEnd)
            {
                m_currentBlendTime = m_totalBlendTime - actualTimeToEnd;
            }
        }


        private void StopUpperAnimation(float blendTime)
        {
            m_headPlayer.Stop(blendTime);
            m_spinePlayer.Stop(blendTime);
            m_leftHandPlayer.Stop(blendTime);
            m_rightHandPlayer.Stop(blendTime);            
        }

        private void StopFingersAnimation(float blendTime)
        {
            m_leftFingersPlayer.Stop(blendTime);
            m_rightFingersPlayer.Stop(blendTime);
        }

        internal void AddCommand(MyAnimationCommand command, bool sync = false)
        {
            if (command.Mode == MyPlayAnimationMode.Immediate)
            {
                m_commandQueue.Clear();
            }

            m_commandQueue.Enqueue(command);

            if (sync)
                SyncObject.SendAnimationCommand(ref command);
        }


        public void SetSpineAdditionalRotation(Quaternion rotationUsed, Quaternion rotationForClients, bool updateSync = true)
        {
            bool valueChanged = m_player.SpineAdditionalRotation != rotationUsed;
            m_player.SpineAdditionalRotation = rotationUsed;
            m_playerNextAnim.SpineAdditionalRotation = rotationUsed;

            m_headPlayer.SpineAdditionalRotation = rotationUsed;
            m_spinePlayer.SpineAdditionalRotation = rotationUsed;
            m_leftHandPlayer.SpineAdditionalRotation = rotationUsed;
            m_rightHandPlayer.SpineAdditionalRotation = rotationUsed;

            if (updateSync && valueChanged)
            {
                SyncObject.ChangeHeadOrSpine(m_headLocalXAngle, m_headLocalYAngle,
                    rotationForClients, m_player.HandAdditionalRotation, m_player.HandAdditionalRotation, m_player.UpperHandAdditionalRotation);
            }  
        }

        public void SetHeadAdditionalRotation(Quaternion rotation, bool updateSync = true)
        {
            bool valueChanged = m_player.HeadAdditionalRotation != rotation;
            m_player.HeadAdditionalRotation = rotation;
            m_playerNextAnim.HeadAdditionalRotation = rotation;

            m_headPlayer.HeadAdditionalRotation = rotation;
            m_spinePlayer.HeadAdditionalRotation = rotation;            
            m_leftHandPlayer.HeadAdditionalRotation = rotation;
            m_rightHandPlayer.HeadAdditionalRotation = rotation;

            if (updateSync && valueChanged)
            {
                SyncObject.ChangeHeadOrSpine(m_headLocalXAngle, m_headLocalYAngle,
                    m_player.SpineAdditionalRotation, m_player.HandAdditionalRotation, m_player.HandAdditionalRotation, m_player.UpperHandAdditionalRotation);
            }  
        }

        public void SetHandAdditionalRotation(Quaternion rotation, bool updateSync = true)
        {
            bool valueChanged = m_player.HandAdditionalRotation != rotation;
            m_player.HandAdditionalRotation = rotation;
            m_playerNextAnim.HandAdditionalRotation = rotation;

            m_headPlayer.HandAdditionalRotation = rotation;
            m_spinePlayer.HandAdditionalRotation = rotation;
            m_leftHandPlayer.HandAdditionalRotation = rotation;
            m_rightHandPlayer.HandAdditionalRotation = rotation;


            if (updateSync && valueChanged)
            {
                SyncObject.ChangeHeadOrSpine(m_headLocalXAngle, m_headLocalYAngle,
                    m_player.SpineAdditionalRotation, m_player.HandAdditionalRotation, m_player.HandAdditionalRotation, m_player.UpperHandAdditionalRotation);
            }  
        }

        public void SetUpperHandAdditionalRotation(Quaternion rotation, bool updateSync = true)
        {
            bool valueChanged = m_player.UpperHandAdditionalRotation != rotation;
            m_player.UpperHandAdditionalRotation = rotation;
            m_playerNextAnim.UpperHandAdditionalRotation = rotation;

            m_headPlayer.UpperHandAdditionalRotation = rotation;
            m_spinePlayer.UpperHandAdditionalRotation = rotation;
            m_leftHandPlayer.UpperHandAdditionalRotation = rotation;
            m_rightHandPlayer.UpperHandAdditionalRotation = rotation;

            if (updateSync && valueChanged)
            {
                SyncObject.ChangeHeadOrSpine(m_headLocalXAngle, m_headLocalYAngle,
                    Quaternion.Zero, m_player.HandAdditionalRotation, m_player.HandAdditionalRotation, m_player.UpperHandAdditionalRotation);
            }  
        }

        public bool HasAnimation(string animationName)
        {
            return Definition.AnimationNameToSubtypeName.ContainsKey(animationName);
        }

        bool TryGetAnimationDefinition(string animationSubtypeName, out MyAnimationDefinition animDefinition)
        {
            if (animationSubtypeName == null)
            {
                animDefinition = null;
                return false;
            }

            animDefinition = MyDefinitionManager.Static.TryGetAnimationDefinition(animationSubtypeName);
            if (animDefinition == null)
            {
                //Try backward compatibility
                //Backward compatibility
                string oldPath = System.IO.Path.Combine(MyFileSystem.ContentPath, animationSubtypeName);
                if (MyFileSystem.FileExists(oldPath))
                {
                    animDefinition = new MyAnimationDefinition()
                    { 
                        AnimationModel = oldPath,
                        ClipIndex = 0,
                    };
                    return true;
                }

                animDefinition = null;
                return false;
            }

            return true;
        }

        #endregion

        #region Animation commands

        public void PlayCharacterAnimation(
           string animationName,
           bool loop,
           MyPlayAnimationMode mode,
           float blendTime,           
           float timeScale = 1,
           bool sync = false
           )
        {
            if (animationName == null)
            {
                System.Diagnostics.Debug.Fail("Cannot play null animation!");
                return;
            }

            string animationSubtype = null;
            if (!m_characterDefinition.AnimationNameToSubtypeName.TryGetValue(animationName, out animationSubtype))
            {

                // TODO: Rethinkif we have different skeleton model, we can't play default character animations. 
                // This may still cause problems if character model has humanoid skeleton, but with different bone names and animation subtype wasn't defined.
                if (m_characterDefinition.Skeleton != "Humanoid") return;

                animationSubtype = animationName;
            }

            AddCommand(new MyAnimationCommand()
                {
                    AnimationSubtypeName = animationSubtype,
                    Loop = loop,
                    Mode = mode,
                    BlendTime = blendTime,
                    TimeScale = timeScale
                }, sync);

            FlushAnimationQueue();
        }

        public void StopUpperCharacterAnimation(
          float blendTime
          )
        {
            AddCommand(new MyAnimationCommand()
                {
                    AnimationSubtypeName = null,
                    Loop = false,
                    Mode = MyPlayAnimationMode.Stop,
                    Area = MyBonesArea.LeftHand | MyBonesArea.RightHand,
                    BlendTime = 0,
                    TimeScale = 1
                });
        }


        public void PlayFingersCharacterAnimation(
         string animation,
         bool loop,
         float blendTime,
         float timeScale = 1
            )
        {
            if (animation == null)
            {
                System.Diagnostics.Debug.Fail("Cannot play null animation!");
                return;
            }

            AddCommand(new MyAnimationCommand()
            {
                AnimationSubtypeName = animation,
                Loop = loop,
                Mode = MyPlayAnimationMode.Play,
                Area = MyBonesArea.LeftFingers | MyBonesArea.RightFingers,
                BlendTime = blendTime,
                TimeScale = timeScale,
            });
        }

        public void StopFingersCharacterAnimation(
          float blendTime
          )
        {
            AddCommand(new MyAnimationCommand()
            {
                AnimationSubtypeName = null,
                Loop = false,
                Mode = MyPlayAnimationMode.Stop,
                Area = MyBonesArea.LeftFingers | MyBonesArea.RightFingers,
                BlendTime = 0,
                TimeScale = 1
            });
        }

        #endregion

        public bool ResetWeaponAnimationState { get; set; }
    }
}

using System;
using System.Collections.Generic;
using VRage.Collections;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace VRageRender.Animations
{
    /// <summary>
    /// Interface providing terrain height in model space.
    /// </summary>
    public interface IMyTerrainHeightProvider
    {
        /// <summary>
        /// Get terrain height in model space.
        /// </summary>
        /// <param name="bonePosition">bone position in model space</param>
        /// <param name="terrainHeight">terrain height in model space</param>
        /// <param name="terrainNormal">terrain normal in (character) model space</param>
        /// <returns>true if the intersection was found</returns>
        bool GetTerrainHeight(Vector3 bonePosition, out float terrainHeight, out Vector3 terrainNormal);

        /// <summary>
        /// Get reference terrain height - (flat terrain height) in model space.
        /// </summary>
        float GetReferenceTerrainHeight();
    }

    /// <summary>
    /// Tiny structure describing IK chain.
    /// </summary>
    public class MyAnimationIkChain
    {
        public int BoneIndex = -1;    // index of end bone
        public string BoneName;  // name of the end bone
        public int ChainLength;  // length of ik chain (number of affected bones that are parent to end bone)
        public bool AlignBoneWithTerrain;  // align the end bone with terrain (only in the direction of the pole vector)
        public Matrix? EndBoneTransform;   // overriding transform of the end bone
        public float MinEndPointRotation = -20; // minimum rotation of the end bone when aligning to terrain
        public float MaxEndPointRotation = 90;  // maximum rotation of the end bone when aligning to terrain
    }

    /// <summary>
    /// Tiny structure describing IK chain + remembering last state.
    /// </summary>
    public class MyAnimationIkChainExt : MyAnimationIkChain
    {
        public float LastTerrainHeight = 0;
        public Vector3 LastTerrainNormal = Vector3.Up;
        public Vector3 LastPoleVector = Vector3.Left;

        public Matrix LastAligningRotationMatrix = Matrix.Identity;
        public float AligningSmoothness = 0.125f;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MyAnimationIkChainExt() { }
        /// <summary>
        /// Copy constructor.
        /// </summary>
        public MyAnimationIkChainExt(MyAnimationIkChain initFromChain)
        {
            BoneIndex = initFromChain.BoneIndex;
            BoneName = initFromChain.BoneName;
            ChainLength = initFromChain.ChainLength;
            AlignBoneWithTerrain = initFromChain.AlignBoneWithTerrain;
            EndBoneTransform = initFromChain.EndBoneTransform;
            MinEndPointRotation = initFromChain.MinEndPointRotation;
            MaxEndPointRotation = initFromChain.MaxEndPointRotation;
        }
    }

    /// <summary>
    /// Class providing various IK solutions.
    /// Feet IK is 
    /// </summary>
    public class MyAnimationInverseKinematics
    {
        /// <summary>
        /// List of all feet bones.
        /// </summary>
        private readonly List<MyAnimationIkChainExt> m_feet = new List<MyAnimationIkChainExt>(2);
        /// <summary>
        /// List of all ignored bones (that should not move during IK!).
        /// </summary>
        private bool[] m_ignoredBonesTable;
        /// <summary>
        /// All ignored bones (that should not move during IK!). Names of ignored bones.
        /// </summary>
        private readonly HashSet<string> m_ignoredBoneNames = new HashSet<string>();
        /// <summary>
        /// Character offset - used when the character is slightly above the terrain due to the capsule
        /// </summary>
        private float m_characterDirDownOffset = 0;
        /// <summary>
        /// Maximum character offsets.
        /// </summary>
        private const float m_characterDirDownOffsetMin = -0.3f;
        private const float m_characterDirDownOffsetMax = 0.2f;
        /// <summary>
        /// Character offset smoothing.
        /// </summary>
        private float m_characterDirDownOffsetSmoothness = 0.5f;
        /// <summary>
        /// Current influence of the feet IK.
        /// </summary>
        private float m_currentFeetIkInfluence = 0.0f;

        private const float m_poleVectorChangeSmoothness = 0.85f;

        private const int m_offsetFilteringSampleCount = 30;
        private int m_offsetFilteringCursor = 0;
        private float m_filteredOffsetValue = 0;
        private readonly List<float> m_offsetFiltering = new List<float>(m_offsetFilteringSampleCount);

        // --------- static fields ---------

        // Preallocated array for computations.
        private static readonly int[] m_boneIndicesPreallocated = new int[64];
        // Debug Transform - world transform of controlled entity. Useful for debug drawings.
        public static MatrixD DebugTransform;
        // Show debug drawings.
        private static bool m_showDebugDrawings = false;
        // Preallocated backup array.
        private readonly List<Matrix> m_ignoredBonesBackup = new List<Matrix>(8);
        // Vertical offset of root bone caused by feet IK.
        private float m_rootBoneVerticalOffset;

        /// <summary>
        /// List of all feet bones.
        /// </summary>
        public ListReader<MyAnimationIkChainExt> Feet
        {
            get { return m_feet; }
        }

        /// <summary>
        /// Interface providing results from raycasts.
        /// </summary>
        public IMyTerrainHeightProvider TerrainHeightProvider
        {
            get;
            set;
        }

        // Vertical offset of root bone caused by feet IK.
        public float RootBoneVerticalOffset
        {
            get { return m_rootBoneVerticalOffset; }
        }

        // ------------------------------------------------------------------------------------
        /// <summary>
        /// Solve feet positions. 
        /// </summary>
        /// <param name="enabled">Feet resolving is enabled - this is a parameter because of blending over time.</param>
        /// <param name="characterBones">Character bones storage.</param>
        /// <param name="allowMovingWithBody">If feet cannot reach, move the body</param>
        public void SolveFeet(bool enabled, MyCharacterBone[] characterBones, bool allowMovingWithBody)
        {
            m_currentFeetIkInfluence = MathHelper.Clamp(m_currentFeetIkInfluence + (enabled ? 0.1f : -0.1f), 0.0f, 1.0f);
            if (m_currentFeetIkInfluence <= 0 || TerrainHeightProvider == null || characterBones == null || characterBones.Length == 0 || m_feet.Count == 0)
                return;
            RecreateIgnoredBonesTableIfNeeded(characterBones);
            BackupIgnoredBones(characterBones);

            if (allowMovingWithBody)
            {
                MoveTheBodyDown(characterBones);  // move the body down!
            }

            RestoreIgnoredBones(characterBones);

            float refTerrainHeight = TerrainHeightProvider.GetReferenceTerrainHeight();
            float maximumNegativeOffsetValue = m_characterDirDownOffsetMax;
            bool foundAnyOffset = false;
            foreach (var foot in m_feet)
            {
                if (foot.BoneIndex == -1)
                {
                    foreach (var bone in characterBones)
                        if (bone.Name == foot.BoneName)
                        {
                            foot.BoneIndex = bone.Index;
                            break;
                        }

                    if (foot.BoneIndex == -1) // alright alright, skip this bone
                        continue;
                }
                var footBone = characterBones[foot.BoneIndex];
                var firstBone = footBone;
                var secondBone = footBone;
                for (int i = 0; i < foot.ChainLength; i++)
                {
                    secondBone = firstBone;
                    firstBone = firstBone.Parent;
                }
                firstBone.ComputeAbsoluteTransform(); // will propagate the transform to children

                // obtain terrain height
                Vector3 bonePos = footBone.AbsoluteTransform.Translation;
                float terrainHeight;
                Vector3 terrainNormal;
                if (TerrainHeightProvider.GetTerrainHeight(bonePos, out terrainHeight, out terrainNormal))
                {
                    terrainNormal = Vector3.Lerp(foot.LastTerrainNormal, terrainNormal, 0.2f);
                }
                else
                {
                    terrainHeight = foot.LastTerrainHeight;
                    terrainNormal = Vector3.Lerp(foot.LastTerrainNormal, Vector3.Up, 0.1f);
                    foot.LastTerrainHeight *= 0.9f;
                }
                foot.LastTerrainHeight = terrainHeight;
                foot.LastTerrainNormal = terrainNormal;
                float terrainHeightDelta = terrainHeight - refTerrainHeight;

                // compute IK!
                {
                    float originalBoneY = bonePos.Y;
                    float unclampedNewBoneY = terrainHeightDelta + (bonePos.Y - m_filteredOffsetValue) / terrainNormal.Y; // we expect the normal to be normalized
                    bonePos.Y = Math.Min(secondBone.AbsoluteTransform.Translation.Y, unclampedNewBoneY);  // do not allow the feet to be above ankle
                    bonePos.Y = MathHelper.Lerp(originalBoneY, bonePos.Y, m_currentFeetIkInfluence);
                    maximumNegativeOffsetValue = MathHelper.Clamp(terrainHeightDelta, m_characterDirDownOffsetMin, maximumNegativeOffsetValue); // store worst negative offset
                    foundAnyOffset = true;
                    
                    if (originalBoneY > bonePos.Y)
                    {
                        bonePos.Y = originalBoneY;
                    }
                    
                    SolveIkTwoBones(characterBones, foot, ref bonePos, ref terrainNormal, fromBindPose: false);
                }
            }

            if (!foundAnyOffset)
                maximumNegativeOffsetValue = 0;

            m_characterDirDownOffset = MathHelper.Lerp(maximumNegativeOffsetValue, m_characterDirDownOffset, m_characterDirDownOffsetSmoothness * m_offsetFiltering.Count / m_offsetFilteringSampleCount);
        }

        private void RestoreIgnoredBones(MyCharacterBone[] characterBones)
        {
            int j = 0;
            for (int i = 0; i < characterBones.Length; i++)
                if (m_ignoredBonesTable[i])
                {
                    characterBones[i].SetCompleteTransformFromAbsoluteMatrix(m_ignoredBonesBackup[j++], false);
                    characterBones[i].ComputeAbsoluteTransform();
                }
        }

        private void BackupIgnoredBones(MyCharacterBone[] characterBones)
        {
            m_ignoredBonesBackup.Clear();
            for (int i = 0; i < characterBones.Length; i++)
                if (m_ignoredBonesTable[i])
                    m_ignoredBonesBackup.Add(characterBones[i].AbsoluteTransform);
        }

        private void MoveTheBodyDown(MyCharacterBone[] characterBones)
        {
            if (m_offsetFiltering.Count == m_offsetFilteringSampleCount)
            {
                m_offsetFiltering[m_offsetFilteringCursor++] = m_characterDirDownOffset;
                if (m_offsetFilteringCursor == m_offsetFilteringSampleCount)
                    m_offsetFilteringCursor = 0;
            }
            else
            {
                m_offsetFiltering.Add(m_characterDirDownOffset);
            }

            float filteredOffsetValue = float.MinValue;
            foreach (float offset in m_offsetFiltering)
            {
                filteredOffsetValue = Math.Max(filteredOffsetValue, offset);
            }
            m_filteredOffsetValue = filteredOffsetValue;

            if (m_offsetFilteringCursor >= m_offsetFilteringSampleCount)
                m_offsetFilteringCursor = 0;
            if (filteredOffsetValue * filteredOffsetValue > MyMathConstants.EPSILON)
            {
                MyCharacterBone bone = characterBones[0];
                while (bone.Parent != null)
                    bone = bone.Parent;

                var rootBoneTranslation = bone.Translation;
                m_rootBoneVerticalOffset = filteredOffsetValue * m_currentFeetIkInfluence;
                rootBoneTranslation.Y += m_rootBoneVerticalOffset;
                bone.Translation = rootBoneTranslation;
                bone.ComputeAbsoluteTransform(); // move the skeleton down
            }
        }

        // ------------------------------------------------------------------------------------
        public void ClearCharacterOffsetFilteringSamples()
        {
            m_offsetFiltering.Clear();
        }

        // ------------------------------------------------------------------------------------
        /// <summary>
        /// Solve IK for chain of two bones + change rotation of end bone.
        /// </summary>
        /// <param name="characterBones">bone storage</param>
        /// <param name="ikChain">description of bone chain</param>
        /// <param name="finalPosition">desired position of end bone</param>
        /// <param name="finalNormal">desired normal of end bone - would be projected on plane first bone-second bone-third bone</param>
        /// <param name="fromBindPose">solve this starting from the bind pose</param>
        /// <returns>true on success</returns>
        public static bool SolveIkTwoBones(MyCharacterBone[] characterBones, MyAnimationIkChainExt ikChain, ref Vector3 finalPosition, ref Vector3 finalNormal, bool fromBindPose)
        {
            int boneIndex = ikChain.BoneIndex;
            float finalMinRot = MathHelper.ToRadians(ikChain.MinEndPointRotation);
            float finalMaxRot = MathHelper.ToRadians(ikChain.MaxEndPointRotation);
            Vector3 lastPoleVector = ikChain.LastPoleVector;
            int chainLength = ikChain.ChainLength;
            bool alignBoneWithTerrain = ikChain.AlignBoneWithTerrain;

            MyCharacterBone thirdBone = characterBones[boneIndex];
            if (thirdBone == null)
                return false;
            MyCharacterBone secondBone = thirdBone.Parent;
            for (int i = 2; i < chainLength; i++)
            {
                secondBone = secondBone.Parent;
            }
            if (secondBone == null)
                return false;
            MyCharacterBone firstBone = secondBone.Parent;
            if (firstBone == null)
                return false;

            if (fromBindPose)
            {
                firstBone.SetCompleteBindTransform();
                secondBone.SetCompleteBindTransform();
                thirdBone.SetCompleteBindTransform();
                firstBone.ComputeAbsoluteTransform(true);
            }
            
            Matrix thirdBoneTransformBackup = thirdBone.AbsoluteTransform;

            //Vector3 firstBoneTransformRightDir = firstBone.AbsoluteTransform.Right;
            Vector3 firstBoneOrigin = firstBone.AbsoluteTransform.Translation;
            Vector3 secondBoneOrigin = secondBone.AbsoluteTransform.Translation;
            Vector3 thirdBoneOrigin = thirdBone.AbsoluteTransform.Translation;
            // Vector3D finalPosition comes from parameter
            Vector3 secondMinusFirst = secondBoneOrigin - firstBoneOrigin;
            Vector3 finalMinusFirst = finalPosition - firstBoneOrigin;
            //Vector3 finalMinusSecond = finalPosition - secondBoneOrigin;

            Vector3 poleVectorNormalized;
            Vector3 thirdMinusFirst = thirdBoneOrigin - firstBoneOrigin;
            Vector3.Cross(ref secondMinusFirst, ref thirdMinusFirst, out poleVectorNormalized);

            // project to 2D (only vectors)
            poleVectorNormalized.Normalize();
            poleVectorNormalized = Vector3.Normalize(Vector3.Lerp(poleVectorNormalized, lastPoleVector, m_poleVectorChangeSmoothness));
            Vector3 planeDirY = Vector3.Normalize(finalMinusFirst); // finalMinusFirst? thirdMinusFirst?
            Vector3 planeDirX = Vector3.Normalize(Vector3.Cross(planeDirY, poleVectorNormalized));
            //Vector2 firstBoneOrigin2D = new Vector2(0, 0);
            Vector2 secondBoneOrigin2D = new Vector2(planeDirX.Dot(ref secondMinusFirst), planeDirY.Dot(ref secondMinusFirst));
            Vector2 thirdBoneOrigin2D = new Vector2(planeDirX.Dot(ref thirdMinusFirst), planeDirY.Dot(ref thirdMinusFirst));
            Vector2 finalPosition2D = new Vector2(planeDirX.Dot(ref finalMinusFirst), planeDirY.Dot(ref finalMinusFirst));
            Vector2 terrainNormal2D = new Vector2(planeDirX.Dot(ref finalNormal), planeDirY.Dot(ref finalNormal));

            float firstBoneLength = (secondBoneOrigin2D/* - 0*/).Length();
            float secondBoneLength = (thirdBoneOrigin2D - secondBoneOrigin2D).Length();
            float finalDistance = finalPosition2D.Length();

            if (firstBoneLength + secondBoneLength <= finalDistance)
            {
                finalPosition2D = (firstBoneLength + secondBoneLength) * finalPosition2D / finalDistance;
                // too far
                //return false;
            }

            // mid-joint ->  wanted position in 2D
            Vector2 newSecondBoneOrigin2D;
            {
                newSecondBoneOrigin2D.Y = (finalPosition2D.Y * finalPosition2D.Y - secondBoneLength * secondBoneLength + firstBoneLength * firstBoneLength) / (2.0f * finalPosition2D.Y);
                float srqtArg = firstBoneLength * firstBoneLength - newSecondBoneOrigin2D.Y * newSecondBoneOrigin2D.Y;
                newSecondBoneOrigin2D.X = (float)Math.Sqrt(srqtArg > 0 ? srqtArg : 0);
            }

            // project back
            Vector3 newSecondBoneOrigin = firstBoneOrigin + planeDirX * newSecondBoneOrigin2D.X + planeDirY * newSecondBoneOrigin2D.Y;
            Vector3 newSecondMinusFirst = newSecondBoneOrigin - firstBoneOrigin;
            Vector3 newThirdMinusSecond = finalPosition - newSecondBoneOrigin;
            Vector3 newTerrainNormal = planeDirX * terrainNormal2D.X + planeDirY * terrainNormal2D.Y;
            newTerrainNormal.Normalize();

            // set the rotations in the bones

            // first bone ---------------------------------
            Matrix firstBoneAbsoluteFinal = firstBone.AbsoluteTransform;
            Quaternion rotFirstDelta = Quaternion.CreateFromTwoVectors(secondMinusFirst, newSecondMinusFirst);
            firstBoneAbsoluteFinal.Right = Vector3.Transform(firstBoneAbsoluteFinal.Right, rotFirstDelta);
            firstBoneAbsoluteFinal.Up = Vector3.Transform(firstBoneAbsoluteFinal.Up, rotFirstDelta);
            firstBoneAbsoluteFinal.Forward = Vector3.Transform(firstBoneAbsoluteFinal.Forward, rotFirstDelta);

            firstBone.SetCompleteTransformFromAbsoluteMatrix(ref firstBoneAbsoluteFinal, true);
            firstBone.ComputeAbsoluteTransform();

            // second bone ---------------------------------
            Matrix secondBoneAbsoluteFinal = secondBone.AbsoluteTransform;
            Quaternion rotSecondDelta = Quaternion.CreateFromTwoVectors(thirdBone.AbsoluteTransform.Translation - secondBone.AbsoluteTransform.Translation, newThirdMinusSecond);
            secondBoneAbsoluteFinal.Right = Vector3.Transform(secondBoneAbsoluteFinal.Right, rotSecondDelta);
            secondBoneAbsoluteFinal.Up = Vector3.Transform(secondBoneAbsoluteFinal.Up, rotSecondDelta);
            secondBoneAbsoluteFinal.Forward = Vector3.Transform(secondBoneAbsoluteFinal.Forward, rotSecondDelta);

            secondBone.SetCompleteTransformFromAbsoluteMatrix(ref secondBoneAbsoluteFinal, true);
            secondBone.ComputeAbsoluteTransform();

            //// third bone ----------------------------------

            if (ikChain.EndBoneTransform.HasValue)
            {
                MatrixD localTransformRelated = ikChain.EndBoneTransform.Value * MatrixD.Invert((MatrixD)thirdBone.BindTransform * thirdBone.Parent.AbsoluteTransform);
                thirdBone.Rotation = Quaternion.CreateFromRotationMatrix(Matrix.Normalize((Matrix)localTransformRelated.GetOrientation()));
                thirdBone.Translation = (Vector3)localTransformRelated.Translation;
                thirdBone.ComputeAbsoluteTransform();
            }
            else if (alignBoneWithTerrain)
            {
                Matrix footRotation;
                Vector3 finalRotPoleVec;
                Vector3.Cross(ref newTerrainNormal, ref Vector3.Up, out finalRotPoleVec);
                float deltaAngle = MyUtils.GetAngleBetweenVectors(newTerrainNormal, Vector3.Up);
                if (finalRotPoleVec.Dot(poleVectorNormalized) > 0)
                    deltaAngle = -deltaAngle;
                deltaAngle = MathHelper.Clamp(deltaAngle, finalMinRot, finalMaxRot);

                Matrix.CreateFromAxisAngle(ref poleVectorNormalized, deltaAngle, out footRotation);
                ikChain.LastAligningRotationMatrix = Matrix.Lerp(ikChain.LastAligningRotationMatrix, footRotation, ikChain.AligningSmoothness);
                Matrix thirdBoneAbsoluteFinal = thirdBoneTransformBackup.GetOrientation() * ikChain.LastAligningRotationMatrix;
                thirdBoneAbsoluteFinal.Translation = thirdBone.AbsoluteTransform.Translation;

                thirdBone.SetCompleteTransformFromAbsoluteMatrix(ref thirdBoneAbsoluteFinal, true);
                thirdBone.ComputeAbsoluteTransform();
            }

            // Debugging of the solver.
            if (m_showDebugDrawings)
            {
                MyRenderProxy.DebugDrawLine3D(Vector3D.Transform(firstBoneOrigin, ref DebugTransform), Vector3D.Transform(secondBoneOrigin, ref DebugTransform),
                    Color.Yellow, Color.Red, false);
                MyRenderProxy.DebugDrawLine3D(Vector3D.Transform(secondBoneOrigin, ref DebugTransform), Vector3D.Transform(thirdBoneOrigin, ref DebugTransform),
                    Color.Yellow, Color.Red, false);
                MyRenderProxy.DebugDrawSphere(Vector3D.Transform(finalPosition, ref DebugTransform), 0.05f, Color.Cyan, 1.0f, false);
                MyRenderProxy.DebugDrawLine3D(Vector3D.Transform(secondBoneOrigin, ref DebugTransform), Vector3D.Transform(secondBoneOrigin + poleVectorNormalized, ref DebugTransform),
                    Color.PaleGreen, Color.PaleGreen, false);

                MyRenderProxy.DebugDrawLine3D(Vector3D.Transform(firstBoneOrigin, ref DebugTransform), Vector3D.Transform(firstBoneOrigin + planeDirX, ref DebugTransform),
                    Color.White, Color.White, false);
                MyRenderProxy.DebugDrawLine3D(Vector3D.Transform(firstBoneOrigin, ref DebugTransform), Vector3D.Transform(firstBoneOrigin + planeDirY, ref DebugTransform),
                    Color.White, Color.White, false);

                MyRenderProxy.DebugDrawSphere(Vector3D.Transform(newSecondBoneOrigin, ref DebugTransform), 0.05f, Color.Green, 1.0f, false);
                MyRenderProxy.DebugDrawAxis(firstBone.AbsoluteTransform * DebugTransform, 0.5f, false);

                MyRenderProxy.DebugDrawLine3D(Vector3D.Transform(finalPosition, ref DebugTransform), Vector3D.Transform(finalPosition + newTerrainNormal, ref DebugTransform),
                    Color.Black, Color.LightBlue, false);

                MyRenderProxy.DebugDrawArrow3D(Vector3D.Transform(secondBoneOrigin, ref DebugTransform), Vector3D.Transform(newSecondBoneOrigin, ref DebugTransform),
                    Color.Green, Color.White, false);
            }

            ikChain.LastPoleVector = poleVectorNormalized;
            return true;
        }
        

        // ------------------------------------------------------------------------------------
        public static bool SolveIkCcd(MyCharacterBone[] characterBones, int boneIndex, int chainLength, ref Vector3D finalPosition)
        {
            Vector3 desiredEnd = finalPosition;
            Vector3 curEnd;

            int tries = 0;
            int maxTries = 50;
            float stopDistanceSq = 0.005f * 0.005f;

            MyCharacterBone endBone = characterBones[boneIndex];
            MyCharacterBone currentBone = endBone;
            int[] boneIndices = m_boneIndicesPreallocated;
            // ---- preparation ----
            for (int i = 0; i < chainLength; i++)
            {
                if (currentBone == null)
                {
                    chainLength = i;
                    break;
                }
                boneIndices[i] = currentBone.Index;
                currentBone = currentBone.Parent;
            }

            {
                curEnd = endBone.AbsoluteTransform.Translation;
                float initialDistSqInv = 1 / (float)Vector3D.DistanceSquared(curEnd, desiredEnd);

                do
                {
                    for (int i = 0; i < chainLength; i++)
                    {
                        var bone = characterBones[boneIndices[i]];

                        // first recalculate current final transformation
                        endBone.ComputeAbsoluteTransform();

                        // compute the position of the root
                        Matrix currentMatrix = bone.AbsoluteTransform;
                        Vector3 rootPos = currentMatrix.Translation;
                        //Vector3 lastEnd = curEnd;
                        curEnd = endBone.AbsoluteTransform.Translation;
                        // this is our current end of the final bone                  

                        // get the difference from desired and and current final position
                        double distanceSq = Vector3D.DistanceSquared(curEnd, desiredEnd);

                        //{
                        //    Color c = Color.FromNonPremultiplied(new Vector4(4 * (float) (distanceSq),
                        //        1 - 4 * (float) (distanceSq), 0, 1));
                        //    VRageRender.MyRenderProxy.DebugDrawLine3D(
                        //        Vector3D.Transform(lastEnd, worldMatrix),
                        //        Vector3D.Transform(curEnd, worldMatrix), c, c, false);
                        //}

                        // see if i'm already close enough
                        if (distanceSq > stopDistanceSq)
                        {
                            // create the vector to the current effector posm this is the difference vector
                            Vector3 curVector = curEnd - rootPos;
                            // create the desired effector position vector
                            Vector3 targetVector = desiredEnd - rootPos;

                            // normalize the vectors (expensive, requires a sqrt)
                            // MZ: we don't need to do that
                            // curVector.Normalize();
                            // targetVector.Normalize();

                            double curVectorLenSq = curVector.LengthSquared();
                            double targetVectorLenSq = targetVector.LengthSquared();

                            // the dot product gives me the cosine of the desired angle
                            // cosAngle = curVector.Dot(targetVector);

                            double dotCurTarget = curVector.Dot(targetVector);

                            // if the dot product returns 1.0, i don't need to rotate as it is 0 degrees
                            // MZ: yes, but when does this happen to be exactly 1???
                            // if (cosAngle < 1.0)
                            if (dotCurTarget < 0 || dotCurTarget * dotCurTarget < curVectorLenSq * targetVectorLenSq * (1 - MyMathConstants.EPSILON))
                            {
                                // use the cross product to check which way to rotate
                                //var rotationAxis = curVector.Cross(targetVector);
                                //rotationAxis.Normalize();
                                //turnAngle = System.Math.Acos(cosAngle); // get the angle

                                // get the matrix needed to rotate to the desired position
                                //Matrix rotation = Matrix.CreateFromAxisAngle((Vector3) rotationAxis,
                                //    (float) turnAngle * gain);

                                // get the absolute matrix rotation ie - rotation including all the bones before
                                Matrix rotation;
                                float weight = 1 / (initialDistSqInv * (float)distanceSq + 1);
                                Vector3 weightedTarget = Vector3.Lerp(curVector, targetVector, weight);
                                Matrix.CreateRotationFromTwoVectors(ref curVector, ref weightedTarget, out rotation);
                                Matrix absoluteTransform = Matrix.Normalize(currentMatrix).GetOrientation() * rotation;

                                // MZ: faster

                                // compute just the local matrix for the bone - need to multiply with inversion ot its parent matrix and original bind transform      

                                Matrix parentMatrix = Matrix.Identity;
                                if (bone.Parent != null) parentMatrix = bone.Parent.AbsoluteTransform;
                                parentMatrix = Matrix.Normalize(parentMatrix); // may have different scale

                                Matrix localTransform = Matrix.Multiply(absoluteTransform,
                                    Matrix.Invert(bone.BindTransform * parentMatrix));

                                // now change the current matrix rotation                           
                                bone.Rotation = Quaternion.CreateFromRotationMatrix(localTransform);

                                // and recompute the transformation
                                bone.ComputeAbsoluteTransform();
                            }
                        }

                    }

                    // quit if i am close enough or been running long enough
                } while (tries++ < maxTries &&
                         Vector3D.DistanceSquared(curEnd, desiredEnd) > stopDistanceSq);
            }

            return Vector3D.DistanceSquared(curEnd, desiredEnd) <= stopDistanceSq;
        }

        /// <summary>
        /// Register foot IK bone chain.
        /// </summary>
        public void RegisterFootBone(string boneName, int boneChainLength, bool alignBoneWithTerrain)
        {
            m_feet.Add(new MyAnimationIkChainExt()
            {
                BoneIndex = -1,
                BoneName = boneName,
                ChainLength = boneChainLength,
                AlignBoneWithTerrain = alignBoneWithTerrain
            });
        }

        /// <summary>
        /// Register bone ignored by IK. IK will not move it.
        /// </summary>
        public void RegisterIgnoredBone(string boneName)
        {
            m_ignoredBoneNames.Add(boneName);
            m_ignoredBonesTable = null;
        }

        public void Clear()
        {
            m_characterDirDownOffset = 0;
            m_feet.Clear();
            m_ignoredBoneNames.Clear();
            ClearCharacterOffsetFilteringSamples();
        }

        private void RecreateIgnoredBonesTableIfNeeded(MyCharacterBone[] characterBones)
        {
            if (m_ignoredBonesTable != null || characterBones == null) 
                return;
            m_ignoredBonesTable = new bool[characterBones.Length];
            for (int i = 0; i < characterBones.Length; i++)
            {
                m_ignoredBonesTable[i] = m_ignoredBoneNames.Contains(characterBones[i].Name);
            }
        }
    }
}

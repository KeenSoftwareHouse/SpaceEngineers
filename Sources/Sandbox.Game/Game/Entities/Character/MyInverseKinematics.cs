using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.Game.Entities.Character;
using Sandbox.Engine.Utils;
using Havok;
using Sandbox.Engine.Physics;
using VRageRender.Animations;
using VRage.Game.Entity;
using VRage.Utils;


namespace Sandbox.Game.Entities.Character
{
    /// <summary>
    /// OBSOLETE CLASS, DO NOT USE IT, PREFER MyAnimationInverseKinematics from VRage.
    /// </summary>
    [Obsolete]
    public static class MyInverseKinematics
    {
        public struct CastHit
        {
            public Vector3 Position; // position of the hit
            public Vector3 Normal;   // normal at the hit
        }

        /// <summary>
        /// Solve 3D InverseKinematics problem, 3DOF using CCD IK Solver, no constrains
        /// </summary>
        /// <param name="desiredEnd">is the local vector in the model space which should be our end effector position</param>
        /// <param name="bones">this is the list of bones on which we do inverse kinematics</param>
        /// <returns>true if solved - desiredEnd almost reached, otherwise return false</returns>
        public static bool SolveCCDIk(ref Vector3 desiredEnd, List<MyCharacterBone> bones, float stopDistance, int maxTries, float gain, ref Matrix finalTransform, MyCharacterBone finalBone = null, bool allowFinalBoneTranslation = true)
        {
            Vector3D rootPos, curEnd, targetVector, curVector, crossResult;
            double cosAngle, turnAngle;
            MyCharacterBone endBone = bones.Last();
            int tries = 0;

            curEnd = Vector3.Zero;

            do
            {
                foreach (MyCharacterBone bone in bones.Reverse<MyCharacterBone>())
                {
                    // first recalculate current final transformation
                    endBone.ComputeAbsoluteTransform();

                    // compute the position of the root
                    Matrix currentMatrix = bone.AbsoluteTransform;
                    rootPos = (Vector3D)currentMatrix.Translation;  // this is this bone root position
                    curEnd = (Vector3D)endBone.AbsoluteTransform.Translation;   // this is our current end of the final bone                  

                    // get the difference from desired and and current final position
                    double distance = Vector3D.DistanceSquared(curEnd, desiredEnd);

                    // see if i'm already close enough
                    if (distance > stopDistance)
                    {
                        // create the vector to the current effector posm this is the difference vector
                        curVector = curEnd - rootPos;
                        // create the desired effector position vector
                        targetVector = desiredEnd - rootPos;

                        // normalize the vectors (expensive, requires a sqrt)
                        curVector.Normalize();
                        targetVector.Normalize();

                        // the dot product gives me the cosine of the desired angle
                        cosAngle = curVector.Dot(targetVector);

                        // if the dot product returns 1.0, i don't need to rotate as it is 0 degrees
                        if (cosAngle < 1.0)
                        {
                            // use the cross product to check which way to rotate
                            crossResult = curVector.Cross(targetVector);
                            crossResult.Normalize();
                            turnAngle = System.Math.Acos(cosAngle);	// get the angle

                            // get the matrix needed to rotate to the desired position
                            Matrix rotation = Matrix.CreateFromAxisAngle((Vector3)crossResult, (float)turnAngle * gain);

                            // get the absolute matrix rotation ie - rotation including all the bones before
                            Matrix absoluteTransform = Matrix.Normalize(currentMatrix).GetOrientation() * rotation;

                            // compute just the local matrix for the bone - need to multiply with inversion ot its parent matrix and original bind transform      

                            Matrix parentMatrix = Matrix.Identity;
                            if (bone.Parent != null) parentMatrix = bone.Parent.AbsoluteTransform;
                            parentMatrix = Matrix.Normalize(parentMatrix); // may have different scale

                            Matrix localTransform = Matrix.Multiply(absoluteTransform, Matrix.Invert(bone.BindTransform * parentMatrix));

                            // now change the current matrix rotation                           
                            bone.Rotation = Quaternion.CreateFromRotationMatrix(localTransform);

                            // and recompute the transformation
                            bone.ComputeAbsoluteTransform();
                        }
                    }

                }

                // quit if i am close enough or been running long enough
            } while (tries++ < maxTries &&
                Vector3D.DistanceSquared(curEnd, desiredEnd) > stopDistance);

            // solve the last bone
            if (finalBone != null && finalTransform.IsValid())
            {
                //MatrixD absoluteTransformEnd = finalBone.AbsoluteTransform * finalTransform; // this is our local final transform ( rotation)

                // get the related transformation to original binding posefirstBoneAbsoluteTransform
                MatrixD localTransformRelated;

                if (allowFinalBoneTranslation) localTransformRelated = finalTransform * MatrixD.Invert((MatrixD)finalBone.BindTransform * finalBone.Parent.AbsoluteTransform);
                else localTransformRelated = finalTransform.GetOrientation() * MatrixD.Invert((MatrixD)finalBone.BindTransform * finalBone.Parent.AbsoluteTransform);

                //localTransformRelated = Matrix.Normalize(localTransformRelated);
                // from there get the rotation and translation
                finalBone.Rotation = Quaternion.CreateFromRotationMatrix(Matrix.Normalize((Matrix)localTransformRelated.GetOrientation()));
                if (allowFinalBoneTranslation) finalBone.Translation = (Vector3)localTransformRelated.Translation;
                finalBone.ComputeAbsoluteTransform();
            }

            return Vector3D.DistanceSquared(curEnd, desiredEnd) <= stopDistance;
        }

       
        public static bool SolveTwoJointsIk(ref Vector3 desiredEnd, MyCharacterBone firstBone, MyCharacterBone secondBone, MyCharacterBone endBone, ref Matrix finalTransform, Matrix WorldMatrix, Vector3 normal, bool preferPositiveAngle = true, MyCharacterBone finalBone = null, bool allowFinalBoneTranslation = true, bool minimizeRotation = true)
        {
            //TODO: Implement this new method, that will consider signed/unsigned angles of bone configurations for analytic solution and use a passed normal parameter for proper rotations configuration

             throw new NotImplementedException();
            //Matrix firstBoneAbsoluteTransform = firstBone.AbsoluteTransform;
            //Matrix secondBoneAbsoluteTransform = secondBone.AbsoluteTransform;
            //Matrix endBoneAbsoluteTransform = endBone.AbsoluteTransform;

            //Vector3 origin = firstBoneAbsoluteTransform.Translation;
            //Vector3 originToCurrentEnd = endBoneAbsoluteTransform.Translation - origin;
            //Vector3 originToDesiredEnd = desiredEnd - origin;
            //Vector3 firstBoneVector = secondBoneAbsoluteTransform.Translation - origin;
            //Vector3 secondBoneVector = originToCurrentEnd - firstBoneVector;
            //float firstBoneLength = firstBoneVector.Length();
            //float secondBoneLength = secondBoneVector.Length();
            //float originToDesiredEndLength = originToDesiredEnd.Length();
            //float originToCurrentEndLength = originToCurrentEnd.Length();

            //if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_IKSOLVERS)
            //{
            //    VRageRender.MyRenderProxy.DebugDrawSphere(Vector3.Transform(desiredEnd, WorldMatrix), 0.01f, Color.Red, 1, false);
            //    VRageRender.MyRenderProxy.DebugDrawLine3D(Vector3.Transform(origin, WorldMatrix), Vector3.Transform(origin + originToCurrentEnd, WorldMatrix), Color.Yellow, Color.Yellow, false);
            //    VRageRender.MyRenderProxy.DebugDrawLine3D(Vector3.Transform(origin, WorldMatrix), Vector3.Transform(origin + originToDesiredEnd, WorldMatrix), Color.Red, Color.Red, false);
            //    VRageRender.MyRenderProxy.DebugDrawLine3D(Vector3.Transform(origin, WorldMatrix), Vector3.Transform(origin + firstBoneVector, WorldMatrix), Color.Green, Color.Green, false);
            //    VRageRender.MyRenderProxy.DebugDrawLine3D(Vector3.Transform(origin + firstBoneVector, WorldMatrix), Vector3.Transform(origin + firstBoneVector + secondBoneVector, WorldMatrix), Color.Blue, Color.Blue, false);
            //}

            // only two cases, the desired position is reachable or not
            //bool isDesiredEndReachable = firstBoneLength + secondBoneLength > originToDesiredEndLength;

            // alpha = angle between the first bone and originToDesiredEnd vector
            //double finalAlpha = 0;

            // beta = the angle between the first and second bone
            //double finalBeta = 0;

            //if (isDesiredEndReachable)
            //{
            //    CosineLaw(firstBoneLength, secondBoneLength, originToDesiredEndLength, out finalAlpha, out finalBeta);
            //}                   

            // get the current angles between bones
            //Vector3 planeNormal = Vector3.Cross(originToCurrentEnd, firstBoneVector);
            //planeNormal.Normalize();
            
            //double currentAlpha = GetAngleSigned(originToCurrentEnd, firstBoneVector, planeNormal);
            //double delta = GetAngleSigned(originToCurrentEnd, originToDesiredEnd, planeNormal);

            //finalAlpha = 1f;
            //planeNormal.X = 0.94f; planeNormal.Y = 0.27f; planeNormal.Z = -0.18f;
            //RotateBone(firstBone, planeNormal, delta);
            //if (delta < 0)
            //{
            //    finalAlpha = -finalAlpha;
            //}
            //if (finalAlpha.IsValid()) RotateBone(firstBone, planeNormal, finalAlpha);
            


            //currentBeta = Math.PI - currentBeta;

            //// check if the current Alpha is positive or negative oritented
            //Vector3 crossProduct = Vector3.Cross(firstBoneVector, originToCurrentEnd);
            ////crossProduct.Normalize();
            //float sinThetaX = crossProduct.X / (firstBoneVector.Length() * originToCurrentEnd.Length()) * Vector3.Normalize(crossProduct).X;
            //float sinThetaY = crossProduct.Y / (firstBoneVector.Length() * originToCurrentEnd.Length()) * Vector3.Normalize(crossProduct).Y;
            //float sinThetaZ = crossProduct.Z / (firstBoneVector.Length() * originToCurrentEnd.Length()) * Vector3.Normalize(crossProduct).Z;
            //double theta = 0;

            //if (crossProduct.X != 0)
            //{
            //    theta = Math.Asin(sinThetaX);
            //}
            //else if (crossProduct.Y != 0)
            //{
            //    theta = Math.Asin(sinThetaY);
            //}
            //else
            //{
            //    theta = Math.Asin(sinThetaZ);
            //}

            //if (currentAlpha != theta)
            //{
            //    currentAlpha = -currentAlpha;
            //    currentBeta = -currentBeta;
            //}


            // get the angle between original and final position plane normal
            //originToCurrentEnd.Normalize();
            //originToDesiredEnd.Normalize();
            //double dotProd = Vector3.Dot(Vector3.Normalize();

            //dotProd = MathHelper.Clamp(dotProd, -1, 1);
            //double delta = Math.Acos(dotProd);

            // set the alpha to positive/negative so the total rotation is minimized
            //if (currentAlpha + delta + finalAlpha > currentAlpha + delta - finalAlpha)
            //{
            //    finalAlpha = -finalAlpha;
            //    finalBeta = -finalBeta;
            //}
            //else
            //{
            //    //finalBeta = Math.PI-finalBeta;
            //}



            //Vector3 currentPlaneNormal = Vector3.Cross(firstBoneVector, originToCurrentEnd);
            //currentPlaneNormal.Normalize();


            // we can now rotate the bones in current plane as if the desired end was on the currentEnd axis
            //float alphaDif = (float)(finalAlpha - currentAlpha);
            //float betaDif = (float)(finalBeta - currentBeta);
            //Matrix firstBoneRotation = Matrix.CreateFromAxisAngle(currentPlaneNormal, alphaDif);
            //Matrix secondBoneRotation = Matrix.CreateFromAxisAngle(currentPlaneNormal, betaDif);

            //////// Now we compute the final absolute transforms for the bones
            //Matrix firstBoneFinalTransform = firstBoneAbsoluteTransform * firstBoneRotation;
            //Matrix firstBoneParentAbsoluteTransform = firstBone.Parent.AbsoluteTransform;
            //Matrix localFirstBoneTransform = Matrix.Multiply(firstBoneFinalTransform, Matrix.Invert(firstBone.BindTransform * firstBoneParentAbsoluteTransform));
            //firstBone.Rotation = Quaternion.CreateFromRotationMatrix(localFirstBoneTransform);
            //firstBone.ComputeAbsoluteTransform();

            //Matrix secondBoneFinalTransform = secondBoneAbsoluteTransform * secondBoneRotation;
            //Matrix secondBoneParentAbsoluteTransform = secondBone.Parent.AbsoluteTransform;
            //Matrix localSecondBoneTransform = Matrix.Multiply(secondBoneFinalTransform, Matrix.Invert(secondBone.BindTransform * secondBoneParentAbsoluteTransform));

            //secondBone.Rotation = Quaternion.CreateFromRotationMatrix(localSecondBoneTransform);
            //secondBone.ComputeAbsoluteTransform();



            ////Vector3 planeRotationAxis = planeNormal;//Vector3.Cross(originToCurrentEnd, originToDesiredEnd);
            ////planeRotationAxis.Normalize();

            ////// find the rotation matrices for bones in the original plane
            ////Matrix planeRotation = Matrix.CreateFromAxisAngle(planeRotationAxis, (float)delta);

            ////// compute the final rotations
            ////firstBoneRotation = planeRotation * firstBoneRotation;
            ////secondBoneRotation = secondBoneRotation * firstBoneRotation;

            //// draw the final positions if debug enabled
            //if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_IKSOLVERS)
            //{
            //    Vector3 rotatedFirst = Vector3.Transform(firstBoneVector, firstBoneRotation);
            //    Vector3 rotatedSecond = Vector3.Transform(secondBoneVector, secondBoneRotation);
            //    VRageRender.MyRenderProxy.DebugDrawLine3D(Vector3.Transform(origin, WorldMatrix), Vector3.Transform(origin + rotatedFirst, WorldMatrix), Color.Purple, Color.Purple, false);
            //    VRageRender.MyRenderProxy.DebugDrawLine3D(Vector3.Transform(origin + rotatedFirst, WorldMatrix), Vector3.Transform(origin + rotatedFirst + rotatedSecond, WorldMatrix), Color.White, Color.White, false);
            //}

            // Now we compute the final absolute transforms for the bones
            //Matrix firstBoneFinalAbsoluteTransform = firstBoneAbsoluteTransform * firstBoneRotation;
            //Matrix firstBoneParentAbsoluteTransform = firstBone.Parent.AbsoluteTransform;
            //Matrix localFirstBoneTransform = Matrix.Multiply(firstBoneFinalAbsoluteTransform, Matrix.Invert(firstBone.BindTransform * firstBoneParentAbsoluteTransform));
            //firstBone.Rotation = Quaternion.CreateFromRotationMatrix(localFirstBoneTransform);
            //firstBone.ComputeAbsoluteTransform();

            //Matrix secondBoneFinalAbsoluteTransform = secondBoneAbsoluteTransform * secondBoneRotation;
            //Matrix secondBoneParentAbsoluteTransform = secondBone.Parent.AbsoluteTransform;
            //Matrix localSecondBoneTransform = Matrix.Multiply(secondBoneFinalAbsoluteTransform, Matrix.Invert(secondBone.BindTransform * secondBoneParentAbsoluteTransform));

            //secondBone.Rotation = Quaternion.CreateFromRotationMatrix(localSecondBoneTransform);
            //secondBone.ComputeAbsoluteTransform();

            // minimize the rotation of the second joint from it's origin
            // this will mean to rotate around originToDesiredEnd vector
            //if (minimizeRotation && isDesiredEndReachable)
            //{
            //    Vector3 a = originToDesiredEnd;
            //    Vector3 v = secondBone.AbsoluteTransform.Translation - firstBone.AbsoluteTransform.Translation;
            //    Vector3 k = firstBoneVector;
            //    a.Normalize();
            //    v.Normalize();
            //    k.Normalize();
            //    float A = Vector3.Dot(a, v) * Vector3.Dot(a, k) - Vector3.Dot(Vector3.Cross(Vector3.Cross(a, v), a), k);
            //    float B = Vector3.Dot(Vector3.Cross(a, v), k);
            //    float C = Vector3.Dot(v, k);
            //    double theta1 = Math.Atan(2 * B / (C - A));
            //    double theta2 = Math.PI + theta1;
            //    double finalTheta = 0;
            //    double secondDerivate1 = (A - C) / 2 * Math.Cos(theta1) - B * Math.Sin(theta1);
            //    if (secondDerivate1 > 0)
            //    {
            //        finalTheta = theta2;
            //    }
            //    else
            //    {
            //        finalTheta = theta1;
            //    }
                
            //    Matrix minimizingMatrix = Matrix.CreateFromAxisAngle(a, (float)finalTheta);
            //    Matrix firstBoneFinalMinimizedTransform = firstBone.AbsoluteTransform * minimizingMatrix;
            //    firstBoneParentAbsoluteTransform = firstBone.Parent.AbsoluteTransform;
            //    localFirstBoneTransform = Matrix.Multiply(firstBoneFinalMinimizedTransform, Matrix.Invert(firstBone.BindTransform * firstBoneParentAbsoluteTransform));
            //    firstBone.Rotation = Quaternion.CreateFromRotationMatrix(localFirstBoneTransform);
            //    firstBone.ComputeAbsoluteTransform();
            //}

            // solve the last bone 
            //if (finalBone != null && finalTransform.IsValid() && isDesiredEndReachable)
            //{
            //    //MatrixD absoluteTransformEnd = finalBone.AbsoluteTransform * finalTransform; // this is our local final transform ( rotation)

            //    // get the related transformation to original binding pose
            //    MatrixD localTransformRelated;

            //    if (allowFinalBoneTranslation) localTransformRelated = finalTransform * MatrixD.Invert((MatrixD)finalBone.BindTransform * finalBone.Parent.AbsoluteTransform);
            //    else localTransformRelated = finalTransform.GetOrientation() * MatrixD.Invert((MatrixD)finalBone.BindTransform * finalBone.Parent.AbsoluteTransform);

            //    //localTransformRelated = Matrix.Normalize(localTransformRelated);
            //    // from there get the rotation and translation
            //    finalBone.Rotation = Quaternion.CreateFromRotationMatrix(Matrix.Normalize((Matrix)localTransformRelated.GetOrientation()));
            //    if (allowFinalBoneTranslation) finalBone.Translation = (Vector3)localTransformRelated.Translation;
            //    finalBone.ComputeAbsoluteTransform();
            //}

             //    //return isDesiredEndReachable;            
            return true;
        }

        public static bool SolveTwoJointsIkCCD(MyCharacterBone[] characterBones, int firstBoneIndex, int secondBoneIndex, int endBoneIndex, 
            ref Matrix finalTransform, ref MatrixD worldMatrix, MyCharacterBone finalBone = null, bool allowFinalBoneTranslation = true)
        {
            if (finalBone == null)
                return false;

            Vector3 desiredEnd = finalTransform.Translation;
            //VRageRender.MyRenderProxy.DebugDrawSphere(Vector3D.Transform(desiredEnd, worldMatrix), 0.015f, Color.LightGoldenrodYellow, 1, false);
            Vector3 rootPos, curEnd;
            Vector3 curVector;
            double cosAngle, turnAngle;

            int tries = 0;
            int maxTries = 50;
            float stopDistanceSq = 0.005f * 0.005f;
            float gain = 0.65f;

            MyCharacterBone firstBone = characterBones[firstBoneIndex];
            MyCharacterBone secondBone = characterBones[secondBoneIndex];
            MyCharacterBone endBone = characterBones[endBoneIndex];
                
            //unsafe
            {
                //int* boneIndices = stackalloc int[3];
                int[] boneIndices = new int[3];
                boneIndices[2] = firstBoneIndex;
                boneIndices[1] = secondBoneIndex;
                boneIndices[0] = endBoneIndex;
                curEnd = Vector3.Zero;

                for (int i = 0; i < 3; i++)
                {
                    var bone = characterBones[boneIndices[i]];
                    Vector3 tempTranslation = bone.BindTransform.Translation;
                    Quaternion tempRotation = Quaternion.CreateFromRotationMatrix(bone.BindTransform);
                    bone.SetCompleteTransform(ref tempTranslation, ref tempRotation);
                    bone.ComputeAbsoluteTransform();
                }

                endBone.ComputeAbsoluteTransform();
                curEnd = endBone.AbsoluteTransform.Translation;
                float initialDistSqInv = 1 / (float)Vector3D.DistanceSquared(curEnd, desiredEnd);

                do
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var bone = characterBones[boneIndices[i]];

                        // first recalculate current final transformation
                        endBone.ComputeAbsoluteTransform();

                        // compute the position of the root
                        Matrix currentMatrix = bone.AbsoluteTransform;
                        rootPos = currentMatrix.Translation; // this is this bone root position
                        Vector3 lastEnd = curEnd;
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
                            curVector = curEnd - rootPos;
                            // create the desired effector position vector
                            var targetVector = desiredEnd - rootPos;

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

            // solve the last bone
            if (finalTransform.IsValid())
            {
                // get the related transformation to original binding posefirstBoneAbsoluteTransform
                MatrixD localTransformRelated;

                if (allowFinalBoneTranslation) 
                    localTransformRelated = finalTransform * MatrixD.Invert((MatrixD)finalBone.BindTransform * finalBone.Parent.AbsoluteTransform);
                else
                    localTransformRelated = finalTransform.GetOrientation() * MatrixD.Invert((MatrixD)finalBone.BindTransform * finalBone.Parent.AbsoluteTransform);

                // localTransformRelated = Matrix.Normalize(localTransformRelated);
                // from there get the rotation and translation
                finalBone.Rotation = Quaternion.CreateFromRotationMatrix(Matrix.Normalize((Matrix)localTransformRelated.GetOrientation()));
                if (allowFinalBoneTranslation) 
                    finalBone.Translation = (Vector3)localTransformRelated.Translation;

                finalBone.ComputeAbsoluteTransform();
            }

            return true;//Vector3D.DistanceSquared(curEnd, desiredEnd) <= stopDistanceSq;
        }


        public static void RotateBone(MyCharacterBone bone, Vector3 planeNormal, double angle)
        {
            Matrix rotation = Matrix.CreateFromAxisAngle(planeNormal, (float)angle);
            Matrix finalMatrix = bone.AbsoluteTransform * rotation;
            Matrix parentTransform = bone.Parent != null ? bone.Parent.AbsoluteTransform : Matrix.Identity;
            Matrix localTransform = Matrix.Multiply(finalMatrix, Matrix.Invert(bone.BindTransform * parentTransform));
            bone.Rotation = Quaternion.CreateFromRotationMatrix(localTransform);
            bone.ComputeAbsoluteTransform();        
        }

        public static double GetAngle(Vector3 a,Vector3 b)
        {
 	        float dotProduct = Vector3.Dot(Vector3.Normalize(a), Vector3.Normalize(b));
            dotProduct = MathHelper.Clamp(dotProduct, -1, 1);
            return Math.Acos(dotProduct);
        }

        public static double GetAngleSigned(Vector3 a, Vector3 b, Vector3 normal)
        {
            float dotProduct = Vector3.Dot(Vector3.Normalize(a), Vector3.Normalize(b));
            dotProduct = MathHelper.Clamp(dotProduct, -1, 1);
            double angle = Math.Acos(dotProduct);            
            if (Vector3.Dot(normal, Vector3.Cross(a,b)) < 0)
            {
                angle = -angle;
            }
            return angle;
        }

        public static void CosineLaw(float A, float B, float C, out double alpha, out double beta)
        {
 	            // we find proper angles 
                // cosine law c^2 = a^2 + b^2 - 2*a*b*cos(gamma)
                // gamma = acos ( - (c^2 - a^2 - b^2) / (2*a*b) )

                // alpha = angle between the first bone and originToDesiredEnd vector
                double cosAlpha = -(B * B - A * A - C * C) /
                    (2 * A * C);
                cosAlpha = MathHelper.Clamp(cosAlpha, -1, 1);
                alpha = Math.Acos(cosAlpha);

                // beta = the angle between the first and second bone
                double cosBeta = -(C * C - A * A - B * B) /
                    (2 * A * B);
                cosBeta = MathHelper.Clamp(cosBeta, -1, 1);
                beta = Math.Acos(cosBeta);                            
        }

        /// <summary>
        /// Analytic solutions useful fo hands or feet, all in local model space
        /// </summary>
        /// <param name="desiredEnd">in local model space</param>
        /// <param name="firstBone"></param>
        /// <param name="secondBone"></param>
        /// <param name="finalTransform"></param>
        /// <param name="finalBone"></param>
        /// <param name="allowFinalBoneTranslation"></param>
        /// <returns></returns>
        public static bool SolveTwoJointsIk(ref Vector3 desiredEnd, MyCharacterBone firstBone, MyCharacterBone secondBone, MyCharacterBone endBone, ref Matrix finalTransform, Matrix WorldMatrix, MyCharacterBone finalBone = null, bool allowFinalBoneTranslation = true)
        {
            Matrix firstBoneAbsoluteTransform = firstBone.AbsoluteTransform;
            Matrix secondBoneAbsoluteTransform = secondBone.AbsoluteTransform;
            Matrix endBoneAbsoluteTransform = endBone.AbsoluteTransform;

            Vector3 origin = firstBoneAbsoluteTransform.Translation;
            Vector3 originToCurrentEnd = endBoneAbsoluteTransform.Translation - origin;
            Vector3 originToDesiredEnd = desiredEnd - origin;
            Vector3 firstBoneVector = secondBoneAbsoluteTransform.Translation - origin;
            Vector3 secondBoneVector = originToCurrentEnd - firstBoneVector;
            float firstBoneLength = firstBoneVector.Length();
            float secondBoneLength = secondBoneVector.Length();
            float originToDesiredEndLength = originToDesiredEnd.Length();
            float originToCurrentEndLength = originToCurrentEnd.Length();

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_IKSOLVERS)
            {
                VRageRender.MyRenderProxy.DebugDrawSphere(Vector3.Transform(desiredEnd, WorldMatrix), 0.01f, Color.Red, 1, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(Vector3.Transform(origin, WorldMatrix), Vector3.Transform(origin + originToCurrentEnd, WorldMatrix), Color.Yellow, Color.Yellow, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(Vector3.Transform(origin, WorldMatrix), Vector3.Transform(origin + originToDesiredEnd, WorldMatrix), Color.Red, Color.Red, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(Vector3.Transform(origin, WorldMatrix), Vector3.Transform(origin + firstBoneVector, WorldMatrix), Color.Green, Color.Green, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(Vector3.Transform(origin + firstBoneVector, WorldMatrix), Vector3.Transform(origin + firstBoneVector + secondBoneVector, WorldMatrix), Color.Blue, Color.Blue, false);
            }

            // only two cases, the desired position is reachable or not
            bool isDesiredEndReachable = firstBoneLength + secondBoneLength > originToDesiredEndLength;

            // alpha = angle between the first bone and originToDesiredEnd vector
            double finalAlpha = 0;

            // beta = the angle between the first and second bone
            double finalBeta = 0;

            if (isDesiredEndReachable)
            {   // we find proper angles 
                // cosine law c^2 = a^2 + b^2 - 2*a*b*cos(gamma)
                // gamma = acos ( - (c^2 - a^2 - b^2) / (2*a*b) )

                // alpha = angle between the first bone and originToDesiredEnd vector
                double cosAlpha = -(secondBoneLength * secondBoneLength - firstBoneLength * firstBoneLength - originToDesiredEndLength * originToDesiredEndLength) /
                    (2 * firstBoneLength * originToDesiredEndLength);
                cosAlpha = MathHelper.Clamp(cosAlpha, -1, 1);
                finalAlpha = Math.Acos(cosAlpha);

                // beta = the angle between the first and second bone
                double cosBeta = -(originToDesiredEndLength * originToDesiredEndLength - firstBoneLength * firstBoneLength - secondBoneLength * secondBoneLength) /
                    (2 * firstBoneLength * secondBoneLength);
                cosBeta = MathHelper.Clamp(cosBeta, -1, 1);
                finalBeta = Math.Acos(cosBeta);
                // now get it to the root bone axis no
                finalBeta = Math.PI - finalBeta;
            }


            // get the current angles
            double cCosAlpha = -(secondBoneLength * secondBoneLength - firstBoneLength * firstBoneLength - originToCurrentEndLength * originToCurrentEndLength) /
                (2 * firstBoneLength * originToCurrentEndLength);
            cCosAlpha = MathHelper.Clamp(cCosAlpha, -1, 1);
            double currentAlpha = Math.Acos(cCosAlpha);
            double cCosBeta = -(originToCurrentEndLength * originToCurrentEndLength - firstBoneLength * firstBoneLength - secondBoneLength * secondBoneLength) /
                (2 * firstBoneLength * secondBoneLength);
            cCosBeta = MathHelper.Clamp(cCosBeta, -1, 1);
            double currentBeta = Math.Acos(cCosBeta);
            currentBeta = Math.PI - currentBeta;

            Vector3 currentPlaneNormal = Vector3.Cross(firstBoneVector, originToCurrentEnd);
            currentPlaneNormal.Normalize();

            // we can now rotate the bones in current plane as if the desired end was on the currentEnd axis
            float alphaDif = (float)(finalAlpha - currentAlpha);
            float betaDif = (float)(finalBeta - currentBeta);
            Matrix firstBoneRotation = Matrix.CreateFromAxisAngle(-currentPlaneNormal, alphaDif);
            Matrix secondBoneRotation = Matrix.CreateFromAxisAngle(currentPlaneNormal, betaDif);


            // now get the angle between original and final position plane normal
            originToCurrentEnd.Normalize();
            originToDesiredEnd.Normalize();
            double dotProd = originToCurrentEnd.Dot(originToDesiredEnd);

            dotProd = MathHelper.Clamp(dotProd, -1, 1);
            double delta = Math.Acos(dotProd);
            Vector3 planeRotationAxis = Vector3.Cross(originToCurrentEnd, originToDesiredEnd);
            planeRotationAxis.Normalize();

            // find the rotation matrices for bones in the original plane
            Matrix planeRotation = Matrix.CreateFromAxisAngle(planeRotationAxis, (float)delta);

            // compute the final rotations
            firstBoneRotation = planeRotation * firstBoneRotation;
            secondBoneRotation = secondBoneRotation * firstBoneRotation;

            // draw the final positions if debug enabled
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_IKSOLVERS)
            {
                Vector3 rotatedFirst = Vector3.Transform(firstBoneVector, firstBoneRotation);
                Vector3 rotatedSecond = Vector3.Transform(secondBoneVector, secondBoneRotation);
                VRageRender.MyRenderProxy.DebugDrawLine3D(Vector3.Transform(origin, WorldMatrix), Vector3.Transform(origin + rotatedFirst, WorldMatrix), Color.Purple, Color.Purple, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(Vector3.Transform(origin + rotatedFirst, WorldMatrix), Vector3.Transform(origin + rotatedFirst + rotatedSecond, WorldMatrix), Color.White, Color.White, false);
            }

            // Now we compute the final absolute transforms for the bones
            Matrix firstBoneFinalAbsoluteTransform = firstBoneAbsoluteTransform * firstBoneRotation;
            Matrix firstBoneParentAbsoluteTransform = firstBone.Parent.AbsoluteTransform;
            Matrix localFirstBoneTransform = Matrix.Multiply(firstBoneFinalAbsoluteTransform, Matrix.Invert(firstBone.BindTransform * firstBoneParentAbsoluteTransform));
            firstBone.Rotation = Quaternion.CreateFromRotationMatrix(localFirstBoneTransform);
            firstBone.ComputeAbsoluteTransform();

            Matrix secondBoneFinalAbsoluteTransform = secondBoneAbsoluteTransform * secondBoneRotation;
            Matrix secondBoneParentAbsoluteTransform = secondBone.Parent.AbsoluteTransform;
            Matrix localSecondBoneTransform = Matrix.Multiply(secondBoneFinalAbsoluteTransform, Matrix.Invert(secondBone.BindTransform * secondBoneParentAbsoluteTransform));

            secondBone.Rotation = Quaternion.CreateFromRotationMatrix(localSecondBoneTransform);
            secondBone.ComputeAbsoluteTransform();

            // solve the last bone 
            if (finalBone != null && finalTransform.IsValid() && isDesiredEndReachable)
            {
                //MatrixD absoluteTransformEnd = finalBone.AbsoluteTransform * finalTransform; // this is our local final transform ( rotation)

                // get the related transformation to original binding pose
                MatrixD localTransformRelated;

                if (allowFinalBoneTranslation) localTransformRelated = finalTransform * MatrixD.Invert((MatrixD)finalBone.BindTransform * finalBone.Parent.AbsoluteTransform);
                else localTransformRelated = finalTransform.GetOrientation() * MatrixD.Invert((MatrixD)finalBone.BindTransform * finalBone.Parent.AbsoluteTransform);

                //localTransformRelated = Matrix.Normalize(localTransformRelated);
                // from there get the rotation and translation
                finalBone.Rotation = Quaternion.CreateFromRotationMatrix(Matrix.Normalize((Matrix)localTransformRelated.GetOrientation()));
                if (allowFinalBoneTranslation) finalBone.Translation = (Vector3)localTransformRelated.Translation;
                finalBone.ComputeAbsoluteTransform();
            }

            return isDesiredEndReachable;
        }

        /// <summary>
        /// Finds the closest foot support position using raycast - should raycast from start and from end of the foot
        /// </summary>
        /// <param name="from">Vector3 from - the world coordinate from witch to ray cast - usually the foot world position on the ground</param>
        /// <param name="up">Vector3 up - defining the world up vector used to raycast to the ground</param>
        /// <param name="WorldMatrix">is the world matrix of the model</param>
        /// <param name="castUpLimit">is the height from where we start casting</param>
        /// <param name="castDownLimit">is the height to how much deep we cast</param>
        /// <param name="footDimension">this is the foot dimension, used to create shape cast and also ankle's height, X = width, Z = length, Y = height</param>
        /// <returns>returns CashHit if hit or null otherwise</returns>
        public static CastHit? GetClosestFootSupportPosition(MyEntity characterEntity, MyEntity characterTool, Vector3 from, Vector3 up, Vector3 footDimension, Matrix WorldMatrix, float castDownLimit, float castUpLimit, uint raycastFilterLayer = 0)
        {
            bool gotHit = false;
            CastHit hit = new CastHit();
            MatrixD matrix = WorldMatrix;
            Vector3 footTranslationFromAnkle = Vector3.Zero;//new Vector3(0, footDimension.Y, - footDimension.Y + footDimension.X);    // assunming the -Z is facing front
            // set the proper translation
            matrix.Translation = Vector3.Zero; // just keep the matrix's orientation etc.
            footTranslationFromAnkle = Vector3.Transform(footTranslationFromAnkle, matrix); // get it to the world
            matrix.Translation = from + up * castUpLimit + footTranslationFromAnkle; // set the matrix translation to position from where we cast - we need to shift from the ankle

            // our shape cast returned data structure
            //HkContactPointData? cpd = null;
            //// do the foot cast shape, raycast sometimes don't return a value and we need to make sure that we do nut fall into hole
            //HkShape shape = new HkBoxShape(footDimension * 0.5f);
            Vector3 capsA = new Vector3(0, footDimension.Y / 2, 0);
            Vector3 capsB = new Vector3(0, footDimension.Y / 2, -footDimension.Z);
            //capsA = Vector3.Transform(capsA, WorldMatrix.GetOrientation());
            //capsB = Vector3.Transform(capsB, WorldMatrix.GetOrientation());

            //HkShape shape = new HkCapsuleShape(capsA, capsB, footDimension.X / 2);
            Vector3 castFrom = from + up * castUpLimit;
            Vector3 castTo = from - up * castDownLimit;
            //cpd = MyPhysics.CastShapeReturnContactData(castTo + footTranslationFromAnkle, shape, ref matrix, Physics.CharacterProxy.CharacterCollisionFilter, 0.0f);            

           

            //var hit = MyPhysics.CastRay(castFrom, castTo, out position, out normal, Physics.CharacterProxy.CharacterCollisionFilter, true); 

            //LineD castLine = new LineD(castFrom, castTo);
            //var result = MyEntities.GetIntersectionWithLine(ref castLine, characterEntity, characterTool, true, false, true);

            //if (result != null)
            //{
            //    hit.Position = result.Value.IntersectionPointInWorldSpace;
            //    hit.Normal = result.Value.NormalInWorldSpace;
            //    gotHit = true;
            //    if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_RAYCASTHITS)
            //    {
            //        VRageRender.MyRenderProxy.DebugDrawSphere(hit.Position, 0.02f, Color.Gray, 1, false);
            //        VRageRender.MyRenderProxy.DebugDrawText3D(hit.Position, "Entity Intersection hit", Color.Gray, 1, false);
            //    }

            //}

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_RAYCASTLINE)
            {
                VRageRender.MyRenderProxy.DebugDrawText3D(castFrom + footTranslationFromAnkle, "Cast line", Color.White, 1, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(castFrom + footTranslationFromAnkle, castTo + footTranslationFromAnkle, Color.White, Color.White, false);
            }

            if (MyFakes.ENABLE_FOOT_IK_USE_HAVOK_RAYCAST)
            {
                // do the ray cast also, because ground may not be flat and we will use this to correct values just using raycast, this takes in consideration convex shape radius and ignores it
                MyPhysics.HitInfo hitInfo;
                 if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_RAYCASTLINE)
                 {
                     VRageRender.MyRenderProxy.DebugDrawText3D(castFrom, "Raycast line", Color.Green, 1, false);
                     VRageRender.MyRenderProxy.DebugDrawLine3D(castFrom, castTo, Color.Green, Color.Green, false);
                 }
                
                 if (MyPhysics.CastRay(castFrom, castTo, out hitInfo, raycastFilterLayer, true))
                 {
                     gotHit = true;
                     if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_RAYCASTHITS)
                     {
                         VRageRender.MyRenderProxy.DebugDrawSphere(hitInfo.Position, 0.02f, Color.Green, 1, false);
                         VRageRender.MyRenderProxy.DebugDrawText3D(hitInfo.Position, "RayCast hit", Color.Green, 1, false);
                     }
                     // this is hack, if RayCast returns a hit above the graphics cast, take this one
                     if (Vector3.Dot(hitInfo.Position, up) > Vector3.Dot(hit.Position, up))
                     {
                         hit.Position = hitInfo.Position;
                         hit.Normal = hitInfo.HkHitInfo.Normal;                         
                     }
                 }
            }

                
            //    // now we need to recalculate the hit position to center
            //    cp.HitPosition = from - Vector3.Dot(WorldMatrix.Translation - cp.HitPosition, up) * up;

            //    //cp.HitPosition.Interpolate3(castFrom, castTo, cp.DistanceFraction);
            //    //cp.HitPosition -= footTranslationFromAnkle;

            //    if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_RAYCASTHITS)
            //    {
            //        VRageRender.MyRenderProxy.DebugDrawSphere(cp.HitPosition, 0.03f, Color.Violet, 1, false);
            //        VRageRender.MyRenderProxy.DebugDrawText3D(cp.HitPosition, "ShapeCast hit Centered", Color.Violet, 1, false);
            //    }

            //    // shape cast correction using the normal
            //    //float dotProductAbs = WorldMatrix.Forward.Dot(cp.Normal);
            //    //cp.HitPosition -= WorldMatrix.Up * (1 - dotProductAbs) * footDimension.Y;

            //    // draw shape and ray cast hits
            //    if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_RAYCASTHITS)
            //    {
            //        matrix.Translation = cp.HitPosition;
            //        VRageRender.MyRenderProxy.DebugDrawOBB(Matrix.CreateScale(footDimension) * matrix, Color.White, 1, false, false);
            //        VRageRender.MyRenderProxy.DebugDrawCapsule(Vector3.Transform(capsA, WorldMatrix.GetOrientation()) + cp.HitPosition, Vector3.Transform(capsB, WorldMatrix.GetOrientation()) + cp.HitPosition, footDimension.X, Color.Red, false);

            //    }
            //    // use raycast to correct position
            //    //if (hit)
            //    //{

            //    //    // draw shape and ray cast hits
            //    //    if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_RAYCASTHITS)
            //    //    {
            //    //        VRageRender.MyRenderProxy.DebugDrawSphere(position, 0.02f, Color.Green, 1, false, false);
            //    //        VRageRender.MyRenderProxy.DebugDrawText3D(position, "RayCast hit", Color.Green, 1, false);

            //    //    }

            //    //    // shift the foot  up for the ankle height according to the normal orientation
            //    //    //float dotProductAbs = Math.Abs( WorldMatrix.Forward.Dot(normal));                    

            //    //    //{
            //    //    //    // get the difference between shape and ray cast and set the position
            //    //    //    Vector3 difference = (position - cp.HitPosition) * (dotProductAbs);
            //    //    //    cp.HitPosition = position - difference; // prefer shapecast when the ground is on ankle

            //    //    //}
            //    //    cp.HitPosition.Interpolate3(cp.HitPosition, position, Vector3.Dot(WorldMatrix.Up, normal));
            //    //    //cp.Normal = normal;

            //    //}
              

            //    // where will be final foot if not ankle's shifted
            //    if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_RAYCASTHITS)
            //    {
            //        matrix.Translation = cp.HitPosition;
            //        VRageRender.MyRenderProxy.DebugDrawSphere(cp.HitPosition, 0.02f, Color.Red, 1, false, false);
            //        VRageRender.MyRenderProxy.DebugDrawText3D(cp.HitPosition, "Final hit", Color.Red, 1, false);
            //        VRageRender.MyRenderProxy.DebugDrawLine3D(cp.HitPosition, cp.HitPosition + cp.Normal, Color.YellowGreen, Color.YellowGreen, false);
            //        VRageRender.MyRenderProxy.DebugDrawOBB(Matrix.CreateScale(footDimension) * matrix, Color.Cyan, 1, false, false);
            //    }


            //}

            return gotHit ? new CastHit?(hit) : null;
        }
    }
}

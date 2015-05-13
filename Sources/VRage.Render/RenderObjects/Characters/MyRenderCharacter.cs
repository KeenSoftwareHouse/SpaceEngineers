#region Using

using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;
using VRage.Import;
using VRage.Animations;
using VRageRender.Effects;
using VRage.Utils;
using VRage.Library.Utils;

#endregion

namespace VRageRender
{

    class MyRenderCharacter : MyRenderEntity
    {
        const int LEFT_HAND_BONE = 16;
        const int RIGHT_HAND_BONE = 35;

        #region Structs

        #endregion

        #region Fields
        
        public Matrix[] SkinMatrices
        {
            get { return m_skinTransforms; }
        }

        MyInterpolationQueue<Matrix> [] m_boneInterpolators;
        Matrix[] m_interpolatedTransforms;
        Matrix[] m_skinTransforms;
        Matrix[] m_absoluteTransforms;
        MySkeletonBoneDescription[] m_skeletonHierarchy;
        int[] m_skeletonIndices;

        public void SetSkeleton(MySkeletonBoneDescription[] hierarchy, int[] skeletonIndices)
        {
            m_skeletonHierarchy = hierarchy;
            m_skeletonIndices = skeletonIndices;

            m_boneInterpolators = new MyInterpolationQueue<Matrix>[m_skeletonHierarchy.Length];
            m_interpolatedTransforms = new Matrix[m_skeletonHierarchy.Length];
            m_skinTransforms = new Matrix[m_skeletonHierarchy.Length];
            m_absoluteTransforms = new Matrix[m_skeletonHierarchy.Length];

            for (int i = 0; i < m_skeletonHierarchy.Length; i++)
            {
                m_boneInterpolators[i] = new MyInterpolationQueue<Matrix>(5, Matrix.SlerpScale);    
            }
        }

        public void SetAnimationBones(Matrix[] simulatedBones)
        {
            if (m_skeletonHierarchy == null)
                return;

            var boneCount = m_skeletonHierarchy.Length;

            for (int i = 0; i < boneCount; i++)
            {
                m_absoluteTransforms[i] = simulatedBones[i];
            }

            for (int i = 0; i < boneCount; i++)
            {
                if (m_skeletonHierarchy[i].Parent != -1)
                {
                    m_absoluteTransforms[i] = m_absoluteTransforms[i] * m_absoluteTransforms[m_skeletonHierarchy[i].Parent];
                }
            }

            for(int i = 0; i < boneCount; i++)
            {
                if (MyRender.Settings.EnableObjectInterpolation)
                {
                    m_boneInterpolators[i].AddSample(ref m_absoluteTransforms[i], MyRender.CurrentUpdateTime);
                    m_boneInterpolators[i].Interpolate(MyRender.InterpolationTime, out m_absoluteTransforms[i]);
                }
            }

            int bonesCount = m_skeletonIndices.Length;

            for (int i = 0; i < bonesCount; i++)
            {
                m_skinTransforms[i] = m_skeletonHierarchy[m_skeletonIndices[i]].SkinTransform * m_absoluteTransforms[m_skeletonIndices[i]];
            }
        }

        BoundingBoxD m_actualWorldAABB;

        #endregion

        #region Constructor

        public MyRenderCharacter(uint id, string debugName, string model, MatrixD worldMatrix, RenderFlags flags)
            : base(id, debugName, model, worldMatrix, MyMeshDrawTechnique.SKINNED, flags)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(model));

            WorldMatrix = m_worldMatrix;

            base.UpdateWorldAABB();

            m_actualWorldAABB = m_aabb;
        }

        #endregion

        #region Bones Management





        #endregion

        #region Animation Management


        #endregion

        #region Update

        public void Update()
        {

            m_actualWorldAABB = BoundingBoxD.CreateInvalid();

            ContainmentType containmentType;
            m_aabb.Contains(ref m_actualWorldAABB, out containmentType);
            if (containmentType != ContainmentType.Contains)
            {
                SetDirty();
                MyRender.UpdateRenderObject(this, false);
            }
        }

        #endregion

        #region Draw

        public override void BeforeDraw()
        {
            base.BeforeDraw();
        }

        public override bool Draw()
        {
            if (base.Draw())
            {
            }

            return true;
        }

        public override void UpdateWorldAABB()
        {
            m_aabb = m_actualWorldAABB;
            BoundingSphereD.CreateFromBoundingBox(ref m_aabb, out m_volume);
            Flags &= ~MyElementFlag.EF_AABB_DIRTY;
        }

        public override void DebugDraw()
        {
            base.DebugDraw();

			return;

            if (m_skeletonIndices == null)
                return;
            
            int bonesCount = m_skeletonIndices.Length;

            for (int i = 0; i < bonesCount; i++)
            {
                var bone2 = m_skeletonHierarchy[m_skeletonIndices[i]];

                if (bone2.Parent == -1)
                    continue;



                Vector3D p2 = ((MatrixD)m_absoluteTransforms[m_skeletonIndices[i]] * WorldMatrix).Translation;
                Vector3D p1 = ((MatrixD)m_absoluteTransforms[bone2.Parent] * WorldMatrix).Translation;

               MyDebugDraw.DrawLine3D(p1, p2, Color.Green, Color.Green, false);

  //              Vector3 pCenter = (p1 + p2) * 0.5f;
//                VRageRender.MyRenderProxy.DebugDrawText3D(pCenter, bone2 + " (" + s.ToString() + ")", Color.White, 0.5f, false);
    
            }

            //DebugDrawOBB();
            //MyDebugDraw.DrawAxis(WorldMatrix, m_localVolume.Radius, 1, false);
        }

        public BoundingBoxD ActualWorldAABB
        {
            set
            {
                m_actualWorldAABB = value;
                SetDirty();
            }
        }


        #endregion

        #region Animation blending




        #endregion

    }
}


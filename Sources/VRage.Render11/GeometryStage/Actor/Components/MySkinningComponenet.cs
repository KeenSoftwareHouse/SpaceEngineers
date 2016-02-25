using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;

using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Collections;
using System.Collections.Specialized;
using System.Threading;


namespace VRageRender
{
    
    class MySkinningComponent : MyActorComponent
    {
        public Matrix[] SkinMatrices
        {
            get { return m_skinTransforms; }
        }

        internal const int ConstantBufferMatrixNum = 60;

        Matrix[] m_skinTransforms;
        Matrix[] m_absoluteTransforms;
        MySkeletonBoneDescription[] m_skeletonHierarchy;
        int[] m_skeletonIndices;
        
        internal override void Construct()
        {
            base.Construct();
            Type = MyActorComponentEnum.Skinning;
            m_skinTransforms = null;
        }

        internal override void OnRemove(MyActor owner)
        {
            base.OnRemove(owner);

            this.Deallocate();
        }

        internal override void Assign(MyActor owner)
        {
            base.Assign(owner);
        }

        internal void SetSkeleton(MySkeletonBoneDescription[] hierarchy, int[] skeletonIndices)
        {
            m_skeletonHierarchy = hierarchy;
            m_skeletonIndices = skeletonIndices;

            m_skinTransforms = new Matrix[m_skeletonHierarchy.Length];
            m_absoluteTransforms = new Matrix[m_skeletonHierarchy.Length];

            Owner.MarkRenderDirty();
        }

        internal void SetAnimationBones(Matrix[] simulatedBones)
        {
            if (m_skeletonHierarchy == null)
                return;
            var skeletonCount = m_skeletonHierarchy.Length;

            for (int i = 0; i < skeletonCount; i++)
            {
                m_absoluteTransforms[i] = simulatedBones[i];
            }

            for (int i = 0; i < skeletonCount; i++)
            {
                if (m_skeletonHierarchy[i].Parent != -1)
                {
                    m_absoluteTransforms[i] = m_absoluteTransforms[i] * m_absoluteTransforms[m_skeletonHierarchy[i].Parent];
                }
            }

            int bonesCount = m_skeletonIndices.Length;

            for (int i = 0; i < bonesCount; i++)
            {
                m_skinTransforms[i] = Matrix.Transpose(m_skeletonHierarchy[m_skeletonIndices[i]].SkinTransform * m_absoluteTransforms[m_skeletonIndices[i]]);
            }
        }
    }
}

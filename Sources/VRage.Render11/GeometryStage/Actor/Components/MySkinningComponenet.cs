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

using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Collections;
using System.Collections.Specialized;
using System.Threading;
using VRageRender.Messages;


namespace VRageRender
{
    
    class MySkinningComponent : MyActorComponent
    {
        static List<MyDecalPositionUpdate> m_decalUpdateCache = new List<MyDecalPositionUpdate>();
        internal const int ConstantBufferMatrixNum = 60;

        Matrix[] m_skinTransforms;
        Matrix[] m_absoluteTransforms;
        MySkeletonBoneDescription[] m_skeletonHierarchy;
        int[] m_skeletonIndices;

        public Matrix[] SkinMatrices
        {
            get { return m_skinTransforms; }
        }

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

        internal void SetAnimationBones(Matrix[] boneAbsoluteTransforms, IReadOnlyList<MyBoneDecalUpdate> boneDecals)
        {
            if (m_skeletonHierarchy == null)
                return;

            var skeletonCount = m_skeletonHierarchy.Length;
            for (int i = 0; i < skeletonCount; i++)
                m_absoluteTransforms[i] = boneAbsoluteTransforms[i];

            int bonesCount = m_skeletonIndices.Length;
            for (int i = 0; i < bonesCount; i++)
            {
                m_skinTransforms[i] = Matrix.Transpose(m_skeletonHierarchy[m_skeletonIndices[i]].SkinTransform * m_absoluteTransforms[m_skeletonIndices[i]]);
            }

            m_decalUpdateCache.Clear();
            for (int it = 0; it < boneDecals.Count; it++)
            {
                MyBoneDecalUpdate pair = boneDecals[it];

                MyDecalTopoData data;
                bool found = MyScreenDecals.GetDecalTopoData(pair.DecalID, out data);
                if (!found)
                    continue;

                Matrix skinningTrans = ComputeSkinning(data.BoneIndices, ref data.BoneWeights);
                Matrix transform = data.MatrixBinding * skinningTrans;
                m_decalUpdateCache.Add(new MyDecalPositionUpdate() { ID = pair.DecalID, Transform = transform });
            }

            MyScreenDecals.UpdateDecals(m_decalUpdateCache);
        }

        // http://web.stanford.edu/class/cs248/pdf/class_13_skinning.pdf
        private Matrix ComputeSkinning(Vector4UByte indices, ref VRageMath.Vector4 weights)
        {
            // TODO: Optmize
            Matrix ret = new Matrix();
            for (int it = 0; it < 4; it++)
            {
                float weight = weights[it];
                if (weight == 0)
                    break;

                // NOTE: m_skinTransforms are already transposed
                Matrix transform;
                Matrix.Transpose(ref m_skinTransforms[m_skeletonIndices[indices[it]]], out transform);

                transform *= weight;
                ret += transform;
            }

            return ret;
        }
    }
}

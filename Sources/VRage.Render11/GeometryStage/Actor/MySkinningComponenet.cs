using VRageMath;

namespace VRageRender
{
    
    class MySkinningComponent : MyActorComponent
    {
        public Matrix[] SkinMatrices => m_skinTransforms;

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

            m_owner.MarkRenderDirty();
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
                m_skinTransforms[i] = m_skeletonHierarchy[m_skeletonIndices[i]].SkinTransform * m_absoluteTransforms[m_skeletonIndices[i]];
            }
        }
    }
}

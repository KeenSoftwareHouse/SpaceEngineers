using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace VRageRender
{
    //class MySkinning
    //{
    //    public Matrix[] SkinMatrices
    //    {
    //        get { return m_skinTransforms; }
    //    }

    //    Matrix[] m_skinTransforms;
    //    Matrix[] m_absoluteTransforms;
    //    MySkeletonBoneDescription[] m_skeletonHierarchy;
    //    int[] m_skeletonIndices;



    //    public void SetSkeleton(MySkeletonBoneDescription[] hierarchy, int[] skeletonIndices)
    //    {
    //        m_skeletonHierarchy = hierarchy;
    //        m_skeletonIndices = skeletonIndices;

    //        m_skinTransforms = new Matrix[m_skeletonHierarchy.Length];
    //        m_absoluteTransforms = new Matrix[m_skeletonHierarchy.Length];
    //    }

    //    public void SetAnimationBones(Matrix[] simulatedBones)
    //    {
    //        if (m_skeletonHierarchy == null)
    //            return;
    //        var skeletonCount = m_skeletonHierarchy.Length;

    //        for (int i = 0; i < skeletonCount; i++)
    //        {
    //            m_absoluteTransforms[i] = simulatedBones[i];
    //        }

    //        for (int i = 0; i < skeletonCount; i++)
    //        {
    //            if (m_skeletonHierarchy[i].Parent != -1)
    //            {
    //                m_absoluteTransforms[i] = m_absoluteTransforms[i] * m_absoluteTransforms[m_skeletonHierarchy[i].Parent];
    //            }
    //        }

    //        int bonesCount = m_skeletonIndices.Length;

    //        for (int i = 0; i < bonesCount; i++)
    //        {
    //            m_skinTransforms[i] = m_skeletonHierarchy[m_skeletonIndices[i]].SkinTransform * m_absoluteTransforms[m_skeletonIndices[i]];
    //        }
    //    }
    //}
}

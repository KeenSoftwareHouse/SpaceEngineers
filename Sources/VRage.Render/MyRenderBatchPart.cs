using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public struct MyRenderBatchPart
    {
        public string Model;
        public Vector3[] BoneTranslations; // Translations of bones
        public MatrixD ModelMatrix; // Local matrix, to position model in batch
    }
}

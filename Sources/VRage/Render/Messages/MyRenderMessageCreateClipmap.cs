﻿using VRage.Voxels;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageCreateClipmap : IMyRenderMessage
    {
        public uint ClipmapId;
        public MatrixD WorldMatrix;
        public Vector3I SizeLod0;
        public Vector3D Position;
        public MyClipmapScaleEnum ScaleGroup;
		public RenderFlags AdditionalRenderFlags;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.CreateClipmap; } }
    }
}

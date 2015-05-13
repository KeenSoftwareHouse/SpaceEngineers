using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageUpdateRenderQuality : IMyRenderMessage
    {
        public MyRenderQualityEnum RenderQuality;
        public float LodTransitionDistanceNear;
        public float LodTransitionDistanceFar;
        public float LodTransitionDistanceBackgroundStart;
        public float LodTransitionDistanceBackgroundEnd;
        public float EnvironmentLodTransitionDistance;
        public float EnvironmentLodTransitionDistanceBackground;
        public bool EnableCascadeBlending;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateRenderQuality; } }
    }
}

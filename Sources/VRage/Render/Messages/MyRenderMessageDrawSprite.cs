using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender.Graphics;

namespace VRageRender
{
    public class MyRenderMessageDrawSprite : MyRenderMessageBase
    {
        public string Texture;
        public Color Color;
        public Rectangle? SourceRectangle;
        public RectangleF DestinationRectangle;
        public Vector2 Origin;
        public float Rotation;
        public Vector2 RightVector;
        public float Depth;
        public SpriteEffects Effects;
        public bool ScaleDestination;
        public bool WaitTillLoaded;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.Draw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DrawSprite; } }
    }
}

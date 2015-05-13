using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VRageRender.Graphics;
using VRageRender;

using SharpDX.Direct3D9;
using VRageMath;
using VRageRender.Effects;


namespace VRageRender.Profiler
{
    public class MyLineBatch
    {
        int maxSize = 0;
        int numVertices = 0;
        MyVertexFormatPositionColor[] lineData;

        int numOSVertices = 0;
        MyVertexFormatPositionColor[] onScreenLineData;


        public MyLineBatch(Matrix view, Matrix projection, int size)
        {
            maxSize = 2 * size;
            lineData = new MyVertexFormatPositionColor[maxSize];
            onScreenLineData = new MyVertexFormatPositionColor[maxSize];
        }

        public void Begin()
        {
            numVertices = 0;
            numOSVertices = 0;
        }

        public void End()
        {
            var effect = (MyEffectModelsDiffuse)MyRender.GetEffect(MyEffects.ModelDiffuse);
                    
            if (numVertices > 0)
            {
                effect.SetDiffuseColor(Color.White.ToVector3());
                effect.SetProjectionMatrix(MyRenderCamera.ProjectionMatrix);
                var viewMatrix = MyRenderCamera.ViewMatrixAtZero;
                effect.SetViewMatrix(viewMatrix);
                effect.SetWorldMatrix(Matrix.Identity);
                effect.SetTechnique(MyEffectModelsDiffuse.Technique.PositionColor);

                MyRender.GraphicsDevice.VertexDeclaration = MyVertexFormatPositionColor.VertexDeclaration;

                //  Draw the line
                effect.Begin();
                
                MyRender.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, 0, numVertices / 2, lineData);

                effect.End();
            }         

            if (numOSVertices > 0)
            {
                //device.Clear(ClearOptions.DepthBuffer, Color.BlueViolet, 0, 0);

                // you have to set these parameters for the basic effect to be able to draw on the screen
                effect.SetWorldMatrix(Matrix.Identity);
                effect.SetViewMatrix(Matrix.Identity);
                effect.SetProjectionMatrix(Matrix.Identity);
                effect.SetTechnique(MyEffectModelsDiffuse.Technique.PositionColor);
                MyRender.GraphicsDevice.VertexDeclaration = MyVertexFormatPositionColor.VertexDeclaration;

                //  Draw the line
                effect.Begin();
                MyRender.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, 0, numOSVertices / 2, onScreenLineData);
                effect.End();
            }
        }

        public void DrawLine(Vector3D v0, Vector3D v1, Color color)
        {
            if (numVertices + 2 < maxSize)
            {
                lineData[numVertices].Position = v0 - MyRenderCamera.Position;
                lineData[numVertices].Color = color.ToVector4();
                lineData[numVertices + 1].Position = v1 - MyRenderCamera.Position;
                lineData[numVertices + 1].Color = color.ToVector4();
                numVertices += 2;
            }
        }

        public void DrawOnScreenLine(Vector3 v0, Vector3 v1, Color color)
        {
            if (numOSVertices + 2 < maxSize)
            {
                onScreenLineData[numOSVertices].Position = v0;
                onScreenLineData[numOSVertices].Color = color.ToVector4();
                onScreenLineData[numOSVertices + 1].Position = v1;
                onScreenLineData[numOSVertices + 1].Color = color.ToVector4();
                numOSVertices += 2;
            }
        }
    }
}

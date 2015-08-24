using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using Sandbox.Game.World;
using Sandbox.Graphics.TransparentGeometry;
using Sandbox.Game.Entities;
using Sandbox.Common.Components;
using VRage.Components;

namespace Sandbox.Game.Components
{
    class MyRenderComponentReflectorLight : MyRenderComponentLight
    {
        MyReflectorLight m_reflectorLight = null;

        #region overrides
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_reflectorLight = Container.Entity as MyReflectorLight;
        }
        public override void Draw()
        {
            base.Draw();
            if (m_reflectorLight.Light.ReflectorOn)
            {
                DrawReflectorCone();
            }
        }
        #endregion

        private void DrawReflectorCone()
        {
            var world = Container.Entity.PositionComp.WorldMatrix;

            var toLocal = Container.Entity.PositionComp.WorldMatrixNormalizedInv;
            Vector3D position = Container.Entity.PositionComp.GetPosition();
            Vector3D forwardVector = world.Forward;
            Vector3D leftVector = world.Left;
            Vector3D upVector = world.Up;

            if (m_reflectorLight.Light.ReflectorOn)
            {
                m_reflectorLight.Light.ReflectorUp = upVector;
                m_reflectorLight.Light.ReflectorDirection = Vector3D.TransformNormal(world.Forward, toLocal);
                m_reflectorLight.Light.ReflectorColor = m_reflectorLight.Color.ToVector4();
                m_reflectorLight.Light.UpdateReflectorRangeAndAngle(m_reflectorLight.ShortReflectorForwardConeAngleDef, m_reflectorLight.ShortReflectorRangeDef);
            }

            float reflectorLength = 20f;
            float reflectorThickness = m_reflectorLight.IsLargeLight ? 9f : 3f;

            var color = m_reflectorLight.Light.ReflectorColor;
            color.A = 255;

            var glarePosition = position + forwardVector * 0.28f;
            var dot = Vector3.Dot(Vector3D.Normalize(MySector.MainCamera.Position - glarePosition), forwardVector);
            float angle = 1 - Math.Abs(dot);
            float alphaGlareAlphaBlended = (float)Math.Pow(1 - angle, 2);
            float alphaCone = (1 - (float)Math.Pow(1 - angle, 30)) * 0.15f;

            float reflectorRadiusForAlphaBlended = MathHelper.Lerp(0.1f, 0.5f, alphaGlareAlphaBlended);

            //  Multiply alpha by reflector level (and not the color), because if we multiply the color and let alpha unchanged, reflector cune will be drawn as very dark cone, but still visible
            var reflectorLevel = CurrentLightPower;

            alphaCone *= reflectorLevel;
            alphaGlareAlphaBlended *= reflectorLevel * 0.3f;

            bool drivenFromFPS = MySession.Static.CameraController is MyCockpit ? ((MyCockpit)MySession.Static.CameraController).IsInFirstPersonView : false;

            if (!drivenFromFPS)
            {
                MyTransparentGeometry.AddLineBillboard(
                    "ReflectorCone",
                    color * alphaCone,
                    position - forwardVector * 0.8f,
                    forwardVector,
                    reflectorLength,
                    reflectorThickness);
            }

            MyTransparentGeometry.AddPointBillboard(
                "ReflectorGlareAlphaBlended",
                color * alphaGlareAlphaBlended,
                glarePosition,
                reflectorRadiusForAlphaBlended,
                0);

            //            VRageRender.MyRenderProxy.DebugDrawAABB(WorldAABB, Vector3.One, 1, 1, false);
        }
    }
}

using System;
using VRageMath;
using Sandbox.Game.World;
using Sandbox.Game.Entities;
using VRage.Game;

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
            Vector3D position = Container.Entity.PositionComp.GetPosition();
            Vector3D forwardVector = world.Forward;

            if (m_reflectorLight.Light.ReflectorOn)
            {
                m_reflectorLight.Light.ReflectorColor = m_reflectorLight.Color.ToVector4();
                m_reflectorLight.Light.UpdateReflectorRangeAndAngle(m_reflectorLight.ShortReflectorForwardConeAngleDef, m_reflectorLight.ReflectorRadius);
            }

            float reflectorLength = 20f;
            float reflectorThickness = m_reflectorLight.IsLargeLight ? 9f : 3f;

            var color = m_reflectorLight.Light.ReflectorColor;

            var glarePosition = position + forwardVector * 0.112f * m_reflectorLight.CubeGrid.GridSize;
            var dot = Vector3.Dot(Vector3D.Normalize(MySector.MainCamera.Position - glarePosition), forwardVector);
            float angle = 1 - Math.Abs(dot);
            float alphaCone = (1 - (float)Math.Pow(1 - angle, 30)) * 0.15f;

            //  Multiply alpha by reflector level (and not the color), because if we multiply the color and let alpha unchanged, reflector cune will be drawn as very dark cone, but still visible
            var reflectorLevel = CurrentLightPower;
            alphaCone *= reflectorLevel;

            bool drivenFromFPS = MySession.Static.CameraController is MyCockpit ? ((MyCockpit)MySession.Static.CameraController).IsInFirstPersonView : false;
            if (!drivenFromFPS)
            {
                MyTransparentGeometry.AddLineBillboard(
                    "ReflectorCone",
                    color * alphaCone,
                    position - forwardVector * m_reflectorLight.CubeGrid.GridSize * 0.32f,
                    forwardVector,
                    reflectorLength,
                    reflectorThickness, VRageRender.MyBillboard.BlenType.AdditiveBottom);
            }
        }
    }
}

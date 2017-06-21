using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.World;
using VRageMath;
using VRageRender;
using Sandbox.Graphics;
using Sandbox.Game.Entities;

using VRage.Game.Components;
using VRage.Game;

namespace Sandbox.Game.Components
{
    class MyRenderComponentSensor : MyRenderComponent
    {
        MySensorBase m_sensor = null;

        float m_lastHighlight = 0;
        protected Vector4 m_color;
        bool DrawSensor = true;

        #region overrides
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_sensor = Container.Entity as MySensorBase;
        }
        public override void Draw()
        {
            if (DrawSensor)
            {
                SetHighlight();

                var matrix = Container.Entity.PositionComp.WorldMatrix;
                if (MySession.Static.ControlledEntity == this)
                {
                    Vector4 color = Color.Red.ToVector4();
                    MySimpleObjectDraw.DrawLine(matrix.Translation, matrix.Translation + matrix.Forward * Container.Entity.PositionComp.LocalVolume.Radius * 1.2f, null, ref color, 0.05f);
                }

                //if (this.Physics.RigidBody.CollisionShape.ShapeType == BroadphaseNativeType.BoxShape)
                //{
                //    MySimpleObjectDraw.DrawTransparentBox(ref matrix, ref m_localAABB, ref m_color, MySimpleObjectRasterizer.Solid, 4);
                //}
                //else if (this.Physics.RigidBody.CollisionShape.ShapeType == BroadphaseNativeType.SphereShape)
                //{
                //    MySimpleObjectDraw.DrawTransparentSphere(ref matrix, LocalVolume.Radius, ref m_color, MySimpleObjectRasterizer.Solid, 16);
                //}
                //else
                //{
                //    Debug.Fail("Draw method not specified...no draw");
                //}
            }
        }
        #endregion

        protected void SetHighlight()
        {
            SetHighlight(new Vector4(0.0f, 0.0f, 0.0f, 0.3f));

            if (m_sensor.AnyEntityWithState(MySensorBase.EventType.Add))
            {
                SetHighlight(new Vector4(1.0f, 0.0f, 0.0f, 0.3f), true);
            }
            else if (m_sensor.AnyEntityWithState(MySensorBase.EventType.Delete))
            {
                SetHighlight(new Vector4(1.0f, 0.0f, 1.0f, 0.3f), true);
            }
            else if (m_sensor.HasAnyMoved())
            {
                SetHighlight(new Vector4(0.0f, 0.0f, 1.0f, 0.3f));
            }
            else if (m_sensor.AnyEntityWithState(MySensorBase.EventType.None))
            {
                SetHighlight(new Vector4(0.4f, 0.4f, 0.4f, 0.3f));
            }
        }
        void SetHighlight(Vector4 color, bool keepForMinimalTime = false)
        {
            const int highlightLength = 300; //ms
            if (MySandboxGame.TotalGamePlayTimeInMilliseconds > m_lastHighlight + highlightLength)
            {
                m_color = color;
                if (keepForMinimalTime)
                    m_lastHighlight = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            }
        }
    }
}

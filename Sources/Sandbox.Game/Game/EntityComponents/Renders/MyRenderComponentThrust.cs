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
using Sandbox.Common.ObjectBuilders;
using VRage.Utils;
using Sandbox.Engine.Utils;
using VRage;
using VRage;
using VRage.Components;
using Sandbox.Engine.Physics;
using Sandbox.Graphics.TransparentGeometry.Particles;
using VRage.Library.Utils;

namespace Sandbox.Game.Components
{
    class MyRenderComponentThrust : MyRenderComponentCubeBlock
    {
        MyThrust m_thrust = null;
		const int m_landingEffectUpdateInterval = 5;
		int m_landingEffectUpdateCounter = 0;
		MyParticleEffect m_landingEffect = null;

		static int m_maxNumberLandingEffects = 10;
		static int m_landingEffectCount = 0;

		int m_updatesSinceLastHit = 0;
		MyPhysics.HitInfo? m_lastHitInfo;

        #region overrides
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_thrust = Container.Entity as MyThrust;
        }

		public override void OnBeforeRemovedFromContainer()
		{
			base.OnBeforeRemovedFromContainer();

			if(m_landingEffect != null)
			{
				m_landingEffect.Stop();
				m_landingEffect = null;
				--m_landingEffectCount;
			}
		}

        public override void Draw()
        {
            base.Draw();            

            var worldToLocal = MatrixD.Invert(Container.Entity.PositionComp.WorldMatrix);

			if (m_thrust.CanDraw())
			{
				m_thrust.UpdateThrustFlame();
				m_thrust.UpdateThrustColor();

				foreach (var flame in m_thrust.Flames)
				{
					if (m_thrust.CubeGrid.Physics == null)
						continue;

					var flameDirection = Vector3D.TransformNormal(flame.Direction, Container.Entity.PositionComp.WorldMatrix);
					var flamePosition = Vector3D.Transform(flame.Position, Container.Entity.PositionComp.WorldMatrix);

					float radius = m_thrust.ThrustRadiusRand * flame.Radius;
					float length = m_thrust.ThrustLengthRand * flame.Radius;
					float thickness = m_thrust.ThrustThicknessRand * flame.Radius;

					Vector3D velocityAtNewCOM = Vector3D.Cross(m_thrust.CubeGrid.Physics.AngularVelocity, flamePosition - m_thrust.CubeGrid.Physics.CenterOfMassWorld);
					var velocity = m_thrust.CubeGrid.Physics.LinearVelocity + velocityAtNewCOM;

					if (m_thrust.CurrentStrength > 0 && length > 0)
					{
						float angle = 1 - Math.Abs(Vector3.Dot(MyUtils.Normalize(MySector.MainCamera.Position - flamePosition), flameDirection));
						float alphaCone = (1 - (float)Math.Pow(1 - angle, 30)) * 0.5f;
						//  We move polyline particle backward, because we are stretching ball texture and it doesn't look good if stretched. This will hide it.
						MyTransparentGeometry.AddLineBillboard(m_thrust.FlameLengthMaterial, m_thrust.ThrustColor * alphaCone, flamePosition - flameDirection * length * 0.25f,
							GetRenderObjectID(), ref worldToLocal, flameDirection, length, thickness);

					}

					if (radius > 0)
						MyTransparentGeometry.AddPointBillboard(m_thrust.FlamePointMaterial, m_thrust.ThrustColor, flamePosition, GetRenderObjectID(), ref worldToLocal, radius, 0);

					if (m_landingEffectUpdateCounter-- <= 0)
					{
						m_landingEffectUpdateCounter = (int)Math.Round(m_landingEffectUpdateInterval * (0.8f + MyRandom.Instance.NextFloat() * 0.4f));

						m_lastHitInfo = MyPhysics.CastRay(flamePosition,
							flamePosition + flameDirection * m_thrust.ThrustLengthRand * (m_thrust.CubeGrid.GridSizeEnum == MyCubeSize.Large ? 5.0f : 3.0f) * flame.Radius,
							MyPhysics.ObjectDetectionCollisionLayer);
					}

					if (m_landingEffect != null)
					{
						m_landingEffect.Stop(true);
						m_landingEffect = null;
						--m_landingEffectCount;
					}
					continue;

					if (m_landingEffect == null && m_landingEffectCount < m_maxNumberLandingEffects && MyParticlesManager.TryCreateParticleEffect(54, out m_landingEffect))
					{
						++m_landingEffectCount;
					}

					if (m_landingEffect == null)
						continue;

					m_landingEffect.UserScale = m_thrust.CubeGrid.GridSize;
					m_landingEffect.WorldMatrix = MatrixD.CreateFromTransformScale(Quaternion.CreateFromForwardUp(-m_lastHitInfo.Value.HkHitInfo.Normal, Vector3.CalculatePerpendicularVector(m_lastHitInfo.Value.HkHitInfo.Normal)), m_lastHitInfo.Value.Position, Vector3D.One);
				}
			}
			else if(m_landingEffect != null)
			{
				m_landingEffect.Stop(true);
				m_landingEffect = null;
				--m_landingEffectCount;
			}

            if (m_thrust.Light != null)
            {
                m_thrust.UpdateLight();
            }

            
        }            
        #endregion
    }
}

using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Engine.Utils;

using VRage.Utils;
using Sandbox.Engine.Physics;
using Sandbox.Common;

namespace Sandbox.Game.Entities
{
    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Camera spring like binding with rigid body
    /// </summary>
    public class MyCameraSpring
    {
        private MyPhysicsBody m_physics;

        //m_MaxVelocity - maximalni rychlost jakou se hlava pohybuje vuci lodi
        //m_MaxAccel - max zrychleni hlavy vuci lodi
        //m_MaxDistanceSpeed - maximalni rychlost do ktere se zvetsuje kam se muze hlava hybat

        //Utlum rychlosti z duvody zmeny pohybu. Tohle tam vicemene drzi setrvacnost, ale musi se ta rychlost redukovat jinak by se nedala ta rychlost zmenit dostatecne rychle. Proto se ta rychlost redukuje vyrazne a v dalsim snimku znova nastavuje.
        // m_LinearVelocity *= 0.8f;

        //Kdyz je:
        //  m_LinearVelocity = 0.0f; znamena to, ze je vyrazna zmena pohybu lode - podminka, ze zrychleni je jinym smerem nez rychlost, v tom pripade zacnu tu rychlost kumulovat znova

        //Navrat hlavy je reseni utlumem toho vectoru:
        // m_LocalTranslation *= 0.98f; 

        public float MaxVelocity = 0.2f;        //maximum head speed according to ship
        public float MaxAccel = 38.0f;           //maximum acceleration according to ship
        public float MaxDistanceSpeed = 250f;   //maximum speed limit of head
        public float LinearVelocityDumping = 0.20f;//speed reduction 
        public float LocalTranslationDumping = 0.98f;//returing head into initial position, local dif position reduction

        private Vector3 m_linearVelocity;
        private Vector3 m_limitedVelocity;
        private Vector3 m_localTranslation;

        public MyCameraSpring(MyPhysicsBody attachedTo)
        {
            m_limitedVelocity = Vector3.Zero;
            m_physics = attachedTo;
            m_linearVelocity = Vector3.Zero;
            m_localTranslation = Vector3.Zero;
        }

        public static readonly float PLAYER_HEAD_MAX_DISTANCE = 0.05f;

        public void Update(float timeStep, MatrixD inverseRboMatrix, ref Vector3 headLocal)
        {
            if (m_physics == null)
                return;

            Vector3 localVelocity = Vector3.TransformNormal(m_physics.LinearVelocity, inverseRboMatrix);

            Vector3 localAccel = Vector3.TransformNormal(m_physics.LinearAcceleration, inverseRboMatrix);

            //VRage.Trace.MyTrace.Watch("localAccel", localAccel.ToString());

            m_linearVelocity = localVelocity;

            Vector3 oldVel = m_limitedVelocity;
            m_limitedVelocity = m_linearVelocity;

            //if (m_limitedVelocity.LengthSquared() > MaxVelocity)
            //{
            //    m_limitedVelocity.Normalize();
            //    m_limitedVelocity *= MaxVelocity;
            //}

            Vector3 accel = (m_limitedVelocity - oldVel) / timeStep;

            //if (accel.Length() > MaxAccel)
            //{
            //    accel.Normalize();
            //    m_limitedVelocity = oldVel + accel * (MaxAccel * timeStep);
            //}

            Vector3 oldTranslation = m_localTranslation;
            m_localTranslation += accel * -0.0001f;

            float speedFactor = MathHelper.Clamp(localVelocity.Length() / MaxDistanceSpeed, 0.3f, 1.0f);

            //  Limiting head movement
            if (m_localTranslation.Length() > PLAYER_HEAD_MAX_DISTANCE)
            {
                m_localTranslation = Vector3.Normalize(m_localTranslation) * PLAYER_HEAD_MAX_DISTANCE;
            }

            m_localTranslation *= LocalTranslationDumping;
     

            float velocityLength = localVelocity.Length();

            if (velocityLength > MyMathConstants.EPSILON)
            {
                float velocityFactor = MathHelper.Clamp(velocityLength * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 0.5f, 0, 3);
         //       m_Ship.IncreaseHeadShake(velocityFactor);
            }

            headLocal = m_localTranslation;
        }
    }
}


#region Using

using System.Diagnostics;
using Sandbox.Engine.Physics;
using VRageMath;

using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;
using VRage.Utils;
using System.Linq;
using System.Collections.Generic;

using VRageRender;
using Sandbox.AppCode.Game;
using Sandbox.Game.Utils;
using Sandbox.Engine.Models;
using Havok;
using Sandbox.Game.Entities.Cube;
using Sandbox.Common;
using Sandbox.Game;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Entities.Character;

#endregion

namespace Sandbox.Engine.Physics
{
    public class MyCharacterProxy
    {
        public event ContactPointEventHandler ContactPointCallback;

        #region Fields

        bool m_isDynamic;

        Vector3 m_gravity;
        bool m_jump = false;
        float m_posX;
        float m_posY;
        Vector3 m_angularVelocity;

        Vector3 m_forward;
        float m_speed = 0;
        float m_maxSpeedRelativeToShip = 7.0f; //Defualt MaxSprintSpeed

        int m_airFrameCounter = 0;
        float m_mass = 0;

        float m_maxImpulse;

        //static
        HkCharacterProxy CharacterProxy;
        HkSimpleShapePhantom CharacterPhantom;

        //dynamic
        public HkCharacterRigidBody CharacterRigidBody { get; private set; }

        #endregion

        #region Constructor, Destruction

        public static HkShape CreateCharacterShape(float height, float width, float headHeight, float headSize, float headForwardOffset, float downOffset = 0, bool capsuleForHead = false)
        {
            HkCapsuleShape capsule = new HkCapsuleShape(Vector3.Up * (height - downOffset) / 2.0f, Vector3.Down * (height) / 2.0f, width / 2.0f);

            if (headSize > 0)
            {
                HkConvexShape headShape;

                if (capsuleForHead)
                {
                    headShape = new HkCapsuleShape(new Vector3(0, 0, -0.3f), new Vector3(0, 0, 0.3f), headSize);
                }
                else
                {
                    headShape = new HkSphereShape(headSize);
                }
                //headShape = new HkCapsuleShape(new Vector3(0, 0, -0.05f), new Vector3(0, 0, 0.05f), headSize);

                HkShape[] shapes = new HkShape[]
                {
                    capsule,
                    new HkConvexTranslateShape(headShape, Vector3.Up * (headHeight - downOffset) / 2.0f + Vector3.Forward * headForwardOffset, HkReferencePolicy.TakeOwnership),
                };
                
                return new HkListShape(shapes, shapes.Length, HkReferencePolicy.TakeOwnership);
            }
            else
            {
                return capsule;
            }
        }

        HkShape m_characterShape = HkShape.Empty;
        HkShape m_crouchShape = HkShape.Empty;
        HkShape m_characterCollisionShape = HkShape.Empty;
        MyPhysicsBody m_physicsBody;

        public MyCharacterProxy(bool isDynamic, bool isCapsule, float characterWidth, float characterHeight,
            float crouchHeight, float ladderHeight, float headSize, float headHeight,
            Vector3 position, Vector3 up, Vector3 forward,
            float mass, MyPhysicsBody body, bool isOnlyVertical, float maxSlope, float maxImpulse, float maxSpeedRelativeToShip, float? maxForce = null, HkRagdoll ragDoll = null)
        {
            m_isDynamic = isDynamic;
            m_physicsBody = body;
            m_mass =  mass;
            m_maxImpulse = maxImpulse;
            m_maxSpeedRelativeToShip = maxSpeedRelativeToShip;

            if (isCapsule)
            {
                m_characterShape = CreateCharacterShape(characterHeight, characterWidth, characterHeight + headHeight, headSize, 0, 0);
                m_characterCollisionShape = CreateCharacterShape(characterHeight * 0.9f, characterWidth * 0.9f, characterHeight * 0.9f + headHeight, headSize * 0.9f, 0, 0);
                m_crouchShape = CreateCharacterShape(characterHeight, characterWidth, characterHeight + headHeight, headSize, 0.0f, 1.0f);

                if (!m_isDynamic)
                    CharacterPhantom = new HkSimpleShapePhantom(m_characterShape, MyPhysics.CollisionLayers.CharacterCollisionLayer);
            }
            else
            {
                HkBoxShape box = new HkBoxShape(new Vector3(characterWidth / 2.0f, characterHeight / 2.0f, characterWidth / 2.0f));
                if (!m_isDynamic)
                    CharacterPhantom = new HkSimpleShapePhantom((HkShape)box, MyPhysics.CollisionLayers.CharacterCollisionLayer);
                m_characterShape = box;
            }

            if (!m_isDynamic)
            {
                HkCharacterProxyCinfo characterProxyInfo = new HkCharacterProxyCinfo();
                characterProxyInfo.StaticFriction = 1;
                characterProxyInfo.DynamicFriction = 1;
                characterProxyInfo.ExtraDownStaticFriction = 1000;
                //characterProxyInfo.ContactAngleSensitivity = 20;
                //characterProxyInfo.KeepContactTolerance = 1f;
                characterProxyInfo.MaxCharacterSpeedForSolver = 10000;
                characterProxyInfo.RefreshManifoldInCheckSupport = true;
                characterProxyInfo.Up = up;
                characterProxyInfo.Forward = forward;
                characterProxyInfo.UserPlanes = 4;
                characterProxyInfo.MaxSlope = MathHelper.ToRadians(maxSlope);
                characterProxyInfo.Position = position;
                characterProxyInfo.CharacterMass = mass;
                characterProxyInfo.CharacterStrength = 100;
                characterProxyInfo.ShapePhantom = CharacterPhantom;

                CharacterProxy = new HkCharacterProxy(characterProxyInfo);

                characterProxyInfo.Dispose();          
            }
            else
            {
                HkCharacterRigidBodyCinfo characterRBCInfo = new HkCharacterRigidBodyCinfo();
                characterRBCInfo.Shape = m_characterShape;
                characterRBCInfo.CrouchShape = m_crouchShape;
                characterRBCInfo.Friction = 0;
                characterRBCInfo.MaxSlope = MathHelper.ToRadians(maxSlope);
                characterRBCInfo.Up = up;
                characterRBCInfo.Mass = mass;
                characterRBCInfo.CollisionFilterInfo = MyPhysics.CollisionLayers.CharacterCollisionLayer;
                //characterRBCInfo.UnweldingHeightOffsetFactor = 100;
                characterRBCInfo.MaxLinearVelocity = 1000000;
                characterRBCInfo.MaxForce = maxForce.HasValue ? maxForce.Value : 100000;
                characterRBCInfo.AllowedPenetrationDepth = MyFakes.ENABLE_LIMITED_CHARACTER_BODY ? 0.3f : 0.1f;
                characterRBCInfo.JumpHeight = 0.8f;

                float maxCharacterSpeed = MyGridPhysics.ShipMaxLinearVelocity() + m_maxSpeedRelativeToShip;
                CharacterRigidBody = new HkCharacterRigidBody(characterRBCInfo, maxCharacterSpeed, body, isOnlyVertical);

                CharacterRigidBody.GetRigidBody().ContactPointCallbackEnabled = true;
                CharacterRigidBody.GetRigidBody().ContactPointCallback -= RigidBody_ContactPointCallback;
                CharacterRigidBody.GetRigidBody().ContactPointCallback += RigidBody_ContactPointCallback;
                CharacterRigidBody.GetRigidBody().ContactPointCallbackDelay = 0;

                //CharacterRigidBody.GetHitRigidBody().ContactPointCallbackEnabled = true;
                //CharacterRigidBody.GetHitRigidBody().ContactPointCallback += RigidBody_ContactPointCallback;
                //CharacterRigidBody.GetHitRigidBody().ContactPointCallbackDelay = 0;

                //CharacterRigidBody.SetSupportDistance(10);
                //CharacterRigidBody.SetHardSupportDistance(10);

                characterRBCInfo.Dispose();
            }            
        }


        void RigidBody_ContactPointCallback(ref HkContactPointEvent value)
        {
            if (ContactPointCallback != null)
                ContactPointCallback(ref value);
        }

        public void Dispose()
        {
            if (CharacterProxy != null)
            {
                CharacterProxy.Dispose();
                CharacterProxy = null;
            }

            if (CharacterPhantom != null)
            {
                CharacterPhantom.Dispose();
                CharacterPhantom = null;
            }

            if (CharacterRigidBody != null)
            {
                if (CharacterRigidBody.GetRigidBody() != null)
                {
                    CharacterRigidBody.GetRigidBody().ContactPointCallback -= RigidBody_ContactPointCallback;
                }
                CharacterRigidBody.Dispose();
                CharacterRigidBody = null;
            }

            m_characterShape.RemoveReference();
            m_characterCollisionShape.RemoveReference();
            m_crouchShape.RemoveReference();
        }

        #endregion

        public void SetCollisionFilterInfo(uint info)
        {
            if (m_isDynamic)
                CharacterRigidBody.SetCollisionFilterInfo(info);
        }
        

        #region Activation

        public void Activate(HkWorld world)
        {
            if (CharacterPhantom != null)
            {
                world.AddPhantom(CharacterPhantom);
            }
            if (CharacterRigidBody != null)
            {
                world.AddCharacterRigidBody(CharacterRigidBody);
                if (!float.IsInfinity(m_maxImpulse))
                {
                    world.BreakOffPartsUtil.MarkEntityBreakable(CharacterRigidBody.GetRigidBody(), m_maxImpulse);
                }
            }
        }

        public void Deactivate(HkWorld world)
        {
            if (CharacterPhantom != null)
            {
                world.RemovePhantom(CharacterPhantom);
            }
            if (CharacterRigidBody != null)
            {
                world.RemoveCharacterRigidBody(CharacterRigidBody);
            }
        }

        #endregion

        #region Properties

        public Vector3  LinearVelocity
        {
            get
            {
                if (m_isDynamic)
                    return CharacterRigidBody.LinearVelocity;
                else
                    return CharacterProxy.LinearVelocity;
            }
            set
            {
                value.AssertIsValid();
                if (m_isDynamic)
                    CharacterRigidBody.LinearVelocity = value;
                else
                    CharacterProxy.LinearVelocity = value;
            }
        }

        public Vector3 Forward
        {
            get
            {
                if (m_isDynamic)
                    return CharacterRigidBody.Forward;
                else
                    return CharacterProxy.Forward;
            }
            set
            {
                value.AssertIsValid();

                m_forward = value;

                if (m_isDynamic)
                    CharacterRigidBody.Forward = value;
                else
                    CharacterProxy.Forward = value;
            }
        }

        public Vector3 Up
        {
            get
            {
                if (m_isDynamic)
                {
                    return CharacterRigidBody.Up;
                }
                else
                    return CharacterProxy.Up;
            }
            set
            {
                value.AssertIsValid();
                
                if (m_isDynamic)
                    CharacterRigidBody.Up = value;
                else
                    CharacterProxy.Up = value;
            }
        }

        public HkCharacterStateType GetState()
        {
            if (m_isDynamic)
            {
                // TODO: It would be much safer to test convex cast in gravity direction and consider character on ground when ground distance is small (e.g. under 0.1m)
                var state = CharacterRigidBody.GetState();
                if (state != HkCharacterStateType.HK_CHARACTER_ON_GROUND)
                    m_airFrameCounter++;
                if (state == HkCharacterStateType.HK_CHARACTER_ON_GROUND)
                    m_airFrameCounter = 0;
                if (state == HkCharacterStateType.HK_CHARACTER_IN_AIR && m_airFrameCounter < 8)
                    state = HkCharacterStateType.HK_CHARACTER_ON_GROUND;

                return state;
            }
            else
                return CharacterProxy.GetState();
        }

        public void SetState(HkCharacterStateType state)
        {
            if (m_isDynamic)
            {
                CharacterRigidBody.SetState(state);
            }
            else
            {
                CharacterProxy.SetState(state);
            }
        }

       
        public Vector3 Gravity
        {
            get
            {
                return m_gravity;
            }
            set
            {
                m_gravity = value;
            }
        }

        public float Elevate
        {
            get
            {
                if (m_isDynamic)
                {
                    return CharacterRigidBody.Elevate;
                }

                return 0;
            }
            set
            {
                if (m_isDynamic)
                {
                    CharacterRigidBody.Elevate = value;
                }
            }
        }

        public bool AtLadder
        {
            get
            {
                if (m_isDynamic)
                {
                    return CharacterRigidBody.AtLadder;
                }

                return false;
            }
            set
            {
                if (m_isDynamic)
                {
                    CharacterRigidBody.AtLadder = value;
                }
            }
        }

        public Vector3 ElevateVector
        {
            get
            {
                if (m_isDynamic)
                {
                    return CharacterRigidBody.ElevateVector;
                }

                return Vector3.Zero;
            }
            set
            {
                if (m_isDynamic)
                {
                    CharacterRigidBody.ElevateVector = value;
                }
            }
        }

        public Vector3 ElevateUpVector
        {
            get
            {
                if (m_isDynamic)
                {
                    return CharacterRigidBody.ElevateUpVector;
                }

                return Vector3.Zero;
            }
            set
            {
                if (m_isDynamic)
                {
                    CharacterRigidBody.ElevateUpVector = value;
                }
            }
        }
        
        public bool Jump
        {
            set
            {
                m_jump = value;
            }
        }

        public Vector3 Position
        {
            get
            {
                if (m_isDynamic)
                {
                   
                    return CharacterRigidBody.Position;
                }
                else
                    return CharacterProxy.Position;
            }
            set
            {
                if (m_isDynamic)
                {
                    CharacterRigidBody.Position = value;
                }
                else
                    CharacterProxy.Position = value;
            }
        }

        public float PosX
        {
            set
            {
                m_posX = MathHelper.Clamp(value, -1, 1);
            }
        }

        public float PosY
        {
            set
            {
                m_posY = MathHelper.Clamp(value, -1, 1);
            }
        }

        public Vector3 AngularVelocity
        {
            set
            {
                m_angularVelocity = value;
                if (CharacterRigidBody != null)
                {
                    CharacterRigidBody.AngularVelocity = m_angularVelocity;
                    CharacterRigidBody.SetAngularVelocity(m_angularVelocity);
                }
            }
            get
            {
                return CharacterRigidBody != null ? CharacterRigidBody.GetAngularVelocity() : m_angularVelocity;
            }
        }

        public float Speed
        {
            set
            {
                m_speed = value;
            }
            get
            {
                return m_speed;
            }
        }

        public bool Supported { get; private set; }

        public Vector3 SupportNormal { get; private set; }
        public Vector3 GroundVelocity { get; private set; }

        #endregion

        public void StepSimulation(float stepSizeInSeconds)
        {
            if (CharacterProxy != null)
            {
                CharacterProxy.PosX = m_posX;
                CharacterProxy.PosY = m_posY;
                CharacterProxy.Jump = m_jump; m_jump = false;
                CharacterProxy.Gravity = m_gravity;
                CharacterProxy.StepSimulation(stepSizeInSeconds);
            }
            if (CharacterRigidBody != null)
            {
                CharacterRigidBody.PosX = m_posX;
                CharacterRigidBody.PosY = m_posY;
                CharacterRigidBody.Jump = m_jump; m_jump = false;
                CharacterRigidBody.Gravity = m_gravity;
                CharacterRigidBody.Speed = Speed;
                CharacterRigidBody.StepSimulation(stepSizeInSeconds);
                CharacterRigidBody.Elevate = Elevate;
                Supported = CharacterRigidBody.Supported;
                SupportNormal = CharacterRigidBody.SupportNormal;
                GroundVelocity = CharacterRigidBody.GroundVelocity;

                if(false)
                {
                    // Coordinate system
                    Matrix worldMatrix = GetPhysicsBody().GetWorldMatrix();
                    Vector3 Side = CharacterRigidBody.Up.Cross(CharacterRigidBody.Forward);
                    //MyPhysicsDebugDraw.DebugDrawCoordinateSystem(worldMatrix.Translation, CharacterRigidBody.Forward, Side, CharacterRigidBody.Up);

                    //BoundingBoxD 
                    //MyPhysicsDebugDraw.DebugDrawAabb(worldMatrix.Translation,Color.LightBlue);

                    //Velocity and Acceleration
                    MyPhysicsDebugDraw.DebugDrawVector3(worldMatrix.Translation, CharacterRigidBody.LinearVelocity, Color.Red, 1.0f);
                    //MyPhysicsDebugDraw.DebugDrawVector3(worldMatrix.Translation, CharacterRigidBody.LinearAcceleration, Color.Blue, 1.0f);

                    // Gravity
                    MyPhysicsDebugDraw.DebugDrawVector3(worldMatrix.Translation, CharacterRigidBody.Gravity, Color.Yellow, 0.1f);

                    // Controller Up
                    //MyPhysicsDebugDraw.DebugDrawVector3(worldMatrix.Translation, CharacterRigidBody.GetControllerUp(), Color.Pink, 1.0f);
                    MyPhysicsDebugDraw.DebugDrawVector3(worldMatrix.Translation, CharacterRigidBody.Up, Color.Pink, 1.0f);

                    Vector3 TestUp = Vector3.Up;
                    MyPhysicsDebugDraw.DebugDrawVector3(worldMatrix.Translation, TestUp, Color.Green, 1.0f);

                    /*
                    // Capsule Shape draw 
                    float radius = 0.4f;
                    Vector3 vertexA = Vector3.Zero;
                    Vector3 vertexB = Vector3.Zero;
                    CharacterRigidBody.GetPxCapsuleShapeDrawData(ref radius,ref vertexA,ref vertexB);
                    if ((vertexA != vertexB) && (radius > 0.0f))
                    {
                        HkShape capsuleShape = new HkCapsuleShape(vertexA, vertexB, radius); // only for debug display

                        int index = 0;
                        const float alpha = 0.3f;
                        MyPhysicsDebugDraw.DrawCollisionShape(capsuleShape, worldMatrix, alpha, ref index);
                    }
                    */
                }
            }           
        }

        public void UpdateSupport(float stepSizeInSeconds)
        {
            if (CharacterRigidBody != null)
            {
                CharacterRigidBody.UpdateSupport(stepSizeInSeconds);
                Supported = CharacterRigidBody.Supported;
                SupportNormal = CharacterRigidBody.SupportNormal;
                GroundVelocity = CharacterRigidBody.GroundVelocity;
            }
        }

        public void SkipSimulation(MatrixD mat)
        {
            if (CharacterRigidBody != null)
            {
                CharacterRigidBody.Position = mat.Translation;
                CharacterRigidBody.Forward = mat.Forward;
                CharacterRigidBody.Up = mat.Up;
                Supported = CharacterRigidBody.Supported;
                SupportNormal = CharacterRigidBody.SupportNormal;
                GroundVelocity = CharacterRigidBody.GroundVelocity;
            }
        }

        bool m_flyingStateEnabled = false;
        private HkRigidBody m_oldRigidBody;

        public void EnableFlyingState(bool enable)
        {
            //multiply by constant because walking on max moving ship
            float maxCharacterWalkingSpeed = MyGridPhysics.ShipMaxLinearVelocity() + m_maxSpeedRelativeToShip;
            float maxCharacterFlyingSpeed = MyGridPhysics.ShipMaxLinearVelocity() + m_maxSpeedRelativeToShip;
            float maxAcceleration = 9; // why
            EnableFlyingState(enable, maxCharacterWalkingSpeed, maxCharacterFlyingSpeed, maxAcceleration);           
        }

        public void EnableFlyingState(bool enable, float maxCharacterSpeed, float maxFlyingSpeed, float maxAcceleration)
        {
            if (m_flyingStateEnabled != enable)
            {
                if (CharacterRigidBody != null)
                {
                    CharacterRigidBody.EnableFlyingState(enable, maxCharacterSpeed, maxFlyingSpeed, maxAcceleration);
                }

                // To allow astronaut fly freely in deep space (otherwise he stops in up direction)
                StepSimulation(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);

                m_flyingStateEnabled = enable;
            }
        }

        public void EnableLadderState(bool enable)
        {
            EnableLadderState(enable, MyGridPhysics.ShipMaxLinearVelocity(), 1);
        }

        public void EnableLadderState(bool enable, float maxCharacterSpeed, float maxAcceleration)
        {
            if (CharacterRigidBody != null)
            {
                CharacterRigidBody.EnableLadderState(enable, maxCharacterSpeed, maxAcceleration);
            }
        }

        public void SetShapeForCrouch(HkWorld world, bool enable)
        {
            if (CharacterRigidBody != null && world != null)
            {
                world.Lock();

                if (enable)
                    CharacterRigidBody.SetShapeForCrouch();
                else
                    CharacterRigidBody.SetDefaultShape();

                if (m_physicsBody.IsInWorld)
                    world.ReintegrateCharacter(CharacterRigidBody);

                world.Unlock();
            }
        }



        //
        // Summary:
        //     Applies linear impulse to center of mass of rigid body.
        public void ApplyLinearImpulse(Vector3 impulse)
        {
            if (CharacterRigidBody != null)
            {
                CharacterRigidBody.ApplyLinearImpulse(impulse);
            }
        }

        //
        // Summary:
        //     Applies angular impulse to rigid body.
        public void ApplyAngularImpulse(Vector3 impulse)
        {
            if (CharacterRigidBody != null)
            {
                CharacterRigidBody.ApplyAngularImpulse(impulse);
            }
        }

        // Apply gravity as linear velocity to character proxy only
        public void ApplyGravity(Vector3 gravity)
        {
            CharacterRigidBody.LinearVelocity += (gravity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS); 
        }
      
        public bool ImmediateSetWorldTransform
        {
            get;
            set;
        }

        public void SetRigidBodyTransform(Matrix m)
        {
            if (CharacterRigidBody != null)
            {
                CharacterRigidBody.SetRigidBodyTransform(m);
            }
        }

        public HkShape GetShape()
        {
            return m_characterShape;
        }

        public HkShape GetCollisionShape()
        {
            return m_characterCollisionShape;
        }

        public void SetSupportDistance(float distance)
        {
            if (CharacterRigidBody != null)
            {
                CharacterRigidBody.SetSupportDistance(distance);
            }
        }
        
        public void SetHardSupportDistance(float distance)
        {
            if (CharacterRigidBody != null)
            {
                CharacterRigidBody.SetHardSupportDistance(distance);
            }
        }

        public bool ContactPointCallbackEnabled
        {
            set 
            {
                if (CharacterRigidBody != null)
                {
                    CharacterRigidBody.ContactPointCallbackEnabled = value;
                }
            }
            get
            {
                if (CharacterRigidBody != null)
                {
                    return CharacterRigidBody.ContactPointCallbackEnabled;
                }
                return false;
            }
        }

        public MyPhysicsBody GetPhysicsBody()
        {
            if (m_physicsBody != null)
            {
                return m_physicsBody;
            }
            return null;
        }

        public HkEntity GetRigidBody()
        {
            if (CharacterRigidBody != null)
            {
                return CharacterRigidBody.GetRigidBody();
            }
            return null;
        }

        public HkRigidBody GetHitRigidBody()
        {
            if (CharacterRigidBody != null)
            {
                return CharacterRigidBody.GetHitRigidBody();
            }
            return null;
        }

        public Matrix GetRigidBodyTransform()
        {
            if (CharacterRigidBody != null)
            {
                return CharacterRigidBody.GetRigidBodyTransform();
            }
            return Matrix.Identity;
        }

        public float Mass
        {
            get { return m_mass; }
        }

        public float MaxSpeedRelativeToShip
        {
            get { return m_maxSpeedRelativeToShip; }
        }

        public float CharacterFlyingMaxLinearVelocity()
        {
            return m_maxSpeedRelativeToShip + MyGridPhysics.ShipMaxLinearVelocity(); 
        }

        public float CharacterWalkingMaxLinearVelocity()
        {
            return m_maxSpeedRelativeToShip + MyGridPhysics.ShipMaxLinearVelocity(); 
        }

    }
}

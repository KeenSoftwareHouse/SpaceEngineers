using Sandbox.Common;
using System;
using VRage.Animations;
using VRage.Utils;
using VRageMath;
using VRageRender;


//  This is unified particle class. It can be used for point/billboard particles, or for line/polyline particles.
//  Reason I put it into one class is that their are similar but most important is, that I need to have only
//  one preallocated list of particles. Ofcourse I will waste a lot of value per particle by storing parameters I
//  don't need for that particular particle, but at the end, having two or three buffers can be bigger wasting.

namespace Sandbox.Graphics.TransparentGeometry.Particles
{
    public enum MyParticleTypeEnum : byte
    {
        Point = 0,
        Line = 1, 
        Trail = 2,
    }


    public class MyAnimatedParticle
    {
        [System.Flags]
        public enum ParticleFlags : byte
        {
            BlendTextures = 1 << 0,
            IsInFrustum =   1 << 1
        }

        public object Tag;

        float m_elapsedTime;  //secs
        

        MyParticleGeneration m_generation;

        public MyParticleTypeEnum Type;

        public MyQuadD Quad = new MyQuadD();

        //Start values
        public Vector3D StartPosition;
        Vector3 m_velocity;
        public Vector3 Velocity
        {
            get { return m_velocity; }
            set
            {
                m_velocity = value;
            }
        }

        public float Life;  //secs
        public float Angle;
        public float RotationSpeed;
        public float Thickness;
        public ParticleFlags Flags;

        public MyAnimatedPropertyFloat PivotDistance = null;
        public MyAnimatedPropertyVector3 PivotRotation = null;
        private Vector3 m_actualPivot = Vector3.Zero;

        public MyAnimatedPropertyVector3 Acceleration = null;
        private Vector3 m_actualAcceleration = Vector3.Zero;


        //Per life values
        public MyAnimatedPropertyFloat Radius = new MyAnimatedPropertyFloat();
        public MyAnimatedPropertyVector4 Color = new MyAnimatedPropertyVector4();
        public MyAnimatedPropertyTransparentMaterial Material = new MyAnimatedPropertyTransparentMaterial();

        Vector3D m_actualPosition;
        float m_actualAngle;

        float m_elapsedTimeDivider;
        float m_normalizedTime;

        //  Parameter-less constructor - because particles are stored in object pool
        public MyAnimatedParticle()
        {
            //  IMPORTANT: This class isn't realy inicialized by constructor, but by Start()
            //  So don't initialize members here, do it in Start()
        }

        public void Start(MyParticleGeneration generation)
        {
            System.Diagnostics.Debug.Assert(Life > 0);

            m_elapsedTime = 0;
            m_normalizedTime = 0.0f;
            m_elapsedTimeDivider = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS / Life;
            m_generation = generation;

            MyUtils.AssertIsValid(StartPosition);
            MyUtils.AssertIsValid(Angle);
            MyUtils.AssertIsValid(Velocity);
            MyUtils.AssertIsValid(RotationSpeed);            

            m_actualPosition = StartPosition;
            m_actualAngle = Angle;

            if (PivotRotation != null && PivotDistance != null)
            {
                Vector3 rotation;
                float distance;
                PivotRotation.GetInterpolatedValue<Vector3>(0, out rotation);
                PivotDistance.GetInterpolatedValue<float>(0, out distance);
                Quaternion rotationQ = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(rotation.Y), MathHelper.ToRadians(rotation.X), MathHelper.ToRadians(rotation.Z));
                m_actualPivot = Vector3.Transform(Vector3.Forward, rotationQ) * distance;
            }            
        }

        public bool Update()
        {
            m_elapsedTime += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            if (m_elapsedTime >= Life)
                return false;

            m_normalizedTime += m_elapsedTimeDivider;

            m_velocity.Y += m_generation.Gravity * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            m_actualPosition.X += Velocity.X * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            m_actualPosition.Y += Velocity.Y * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            m_actualPosition.Z += Velocity.Z * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            if (PivotRotation != null && PivotDistance != null)
            {
                MyTransparentGeometry.StartParticleProfilingBlock("Pivot calculation");
                
                Vector3 rotation;
                float distance;
                PivotRotation.GetInterpolatedValue<Vector3>(m_normalizedTime, out rotation);
                PivotDistance.GetInterpolatedValue<float>(m_normalizedTime, out distance);
                Quaternion rotationQ = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(rotation.Y), MathHelper.ToRadians(rotation.X), MathHelper.ToRadians(rotation.Z));
                m_actualPivot = Vector3.Transform(Vector3.Forward, rotationQ) * distance;

                MyTransparentGeometry.EndParticleProfilingBlock();
            }

            if (Acceleration != null)
            {
                MyTransparentGeometry.StartParticleProfilingBlock("Acceleration calculation");

                Acceleration.GetInterpolatedValue<Vector3>(m_normalizedTime, out m_actualAcceleration);
                Velocity += m_actualAcceleration * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                MyTransparentGeometry.EndParticleProfilingBlock();
            }

            m_actualAngle += RotationSpeed * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            MyUtils.AssertIsValid(m_actualPosition);
            MyUtils.AssertIsValid(m_actualAngle);

            return true;
        }



        //  Update position, check collisions, etc. and draw if particle still lives.
        //  Return false if particle dies/timeouts in this tick.
        public bool Draw(VRageRender.MyBillboard billboard)
        {

            var actualPosition = m_actualPosition + m_actualPivot;
            
            MyTransparentGeometry.StartParticleProfilingBlock("Distance calculation");
            //  This time is scaled according to planned lifespan of the particle

            // Distance for sorting
            billboard.DistanceSquared = (float)Vector3D.DistanceSquared(MyTransparentGeometry.Camera.Translation, actualPosition);

            MyTransparentGeometry.EndParticleProfilingBlock();

            // If distance to camera is really small don't draw it.
            if (billboard.DistanceSquared <= 0.1f)
            {
                return false;
            }

            MyTransparentGeometry.StartParticleProfilingBlock("Quad calculation");

            MyTransparentGeometry.StartParticleProfilingBlock("actualRadius");
            float actualRadius = 1;
            Radius.GetInterpolatedValue<float>(m_normalizedTime, out actualRadius);
            MyTransparentGeometry.EndParticleProfilingBlock();

            billboard.ContainedBillboards.Clear();

            billboard.Near = m_generation.GetEffect().Near;
            billboard.Lowres = m_generation.GetEffect().LowRes || VRageRender.MyRenderConstants.RenderQualityProfile.LowResParticles;
            billboard.CustomViewProjection = -1;
            billboard.ParentID = -1;

            float alpha = 1;

            if (Type == MyParticleTypeEnum.Point)
            {
                MyTransparentGeometry.StartParticleProfilingBlock("GetBillboardQuadRotated");
                GetBillboardQuadRotated(billboard, ref actualPosition, actualRadius, m_actualAngle);
                MyTransparentGeometry.EndParticleProfilingBlock();
            }
            else if (Type == MyParticleTypeEnum.Line)
            {
                if (MyUtils.IsZero(Velocity.LengthSquared()))
                    Velocity = MyUtils.GetRandomVector3Normalized();

                MyQuadD quad = new MyQuadD();

                MyPolyLineD polyLine = new MyPolyLineD();
                polyLine.LineDirectionNormalized = MyUtils.Normalize(Velocity);

                if (m_actualAngle > 0)
                {
                    polyLine.LineDirectionNormalized = Vector3.TransformNormal(polyLine.LineDirectionNormalized, Matrix.CreateRotationY(MathHelper.ToRadians(m_actualAngle)));
                }

                polyLine.Point0 = actualPosition;
                polyLine.Point1.X = actualPosition.X + polyLine.LineDirectionNormalized.X * actualRadius;
                polyLine.Point1.Y = actualPosition.Y + polyLine.LineDirectionNormalized.Y * actualRadius;
                polyLine.Point1.Z = actualPosition.Z + polyLine.LineDirectionNormalized.Z * actualRadius;

                if (m_actualAngle > 0)
                { //centerize
                    polyLine.Point0.X = polyLine.Point0.X - polyLine.LineDirectionNormalized.X * actualRadius * 0.5f;
                    polyLine.Point0.Y = polyLine.Point0.Y - polyLine.LineDirectionNormalized.Y * actualRadius * 0.5f;
                    polyLine.Point0.Z = polyLine.Point0.Z - polyLine.LineDirectionNormalized.Z * actualRadius * 0.5f;
                    polyLine.Point1.X = polyLine.Point1.X - polyLine.LineDirectionNormalized.X * actualRadius * 0.5f;
                    polyLine.Point1.Y = polyLine.Point1.Y - polyLine.LineDirectionNormalized.Y * actualRadius * 0.5f;
                    polyLine.Point1.Z = polyLine.Point1.Z - polyLine.LineDirectionNormalized.Z * actualRadius * 0.5f;
                }

                polyLine.Thickness = Thickness;
                var camPos = MyTransparentGeometry.Camera.Translation;
                MyUtils.GetPolyLineQuad(out quad, ref polyLine, camPos);

                if (this.m_generation.AlphaAnisotropic)
                {
                    float angle = 1 - Math.Abs(Vector3.Dot(MyUtils.Normalize(MyTransparentGeometry.Camera.Forward), polyLine.LineDirectionNormalized));
                    float alphaCone = (float)Math.Pow(angle, 0.5f);
                    alpha = alphaCone;
                }

                billboard.Position0 = quad.Point0;
                billboard.Position1 = quad.Point1;
                billboard.Position2 = quad.Point2;
                billboard.Position3 = quad.Point3;
            }
            else if (Type == MyParticleTypeEnum.Trail)
            {
                if (Quad.Point0 == Quad.Point2) //not moving particle
                    return false;
                if (Quad.Point1 == Quad.Point3) //not moving particle was previous one
                    return false;
                if (Quad.Point0 == Quad.Point3) //not moving particle was previous one
                    return false;

                billboard.Position0 = Quad.Point0;
                billboard.Position1 = Quad.Point1;
                billboard.Position2 = Quad.Point2;
                billboard.Position3 = Quad.Point3;

                //if (this.m_generation.AlphaAnisotropic)
             /*   { //Trails are anisotropic by default (nobody wants them to see ugly)
                    Vector3 lineDir = Vector3.Normalize(Quad.Point1 - Quad.Point0);
                    float angle = 1 - Math.Abs(Vector3.Dot(MyMwcUtils.Normalize(MyCamera.ForwardVector), lineDir));
                    float alphaCone = (float)Math.Pow(angle, 0.3f);
                    alpha = alphaCone;
                }*/
            }
            else
            {
                throw new NotSupportedException(Type + " is not supported particle type");
            }

            MyTransparentGeometry.EndParticleProfilingBlock();

            MyTransparentGeometry.StartParticleProfilingBlock("Material calculation");

            Vector4 color;
            Color.GetInterpolatedValue<Vector4>(m_normalizedTime, out color);

            var material1 = MyTransparentMaterials.GetMaterial("ErrorMaterial");
            var material2 = MyTransparentMaterials.GetMaterial("ErrorMaterial");
            float textureBlendRatio = 0;
            if ((Flags & ParticleFlags.BlendTextures) != 0)
            {
                float prevTime, nextTime, difference;
                Material.GetPreviousValue(m_normalizedTime, out material1, out prevTime);
                Material.GetNextValue(m_normalizedTime, out material2, out nextTime, out difference);

                if (prevTime != nextTime)
                    textureBlendRatio = (m_normalizedTime - prevTime) * difference;
            }
            else
            {
                Material.GetInterpolatedValue(m_normalizedTime, out material1);
            }

            MyTransparentGeometry.EndParticleProfilingBlock();
                     
            //This gets 0.44ms for 2000 particles
            MyTransparentGeometry.StartParticleProfilingBlock("billboard.Start");


            billboard.Material = material1.Name;
            billboard.BlendMaterial = material2.Name;
            billboard.BlendTextureRatio = textureBlendRatio;
            billboard.EnableColorize = false;

            billboard.Color = color * alpha * m_generation.GetEffect().UserColorMultiplier;

            MyTransparentGeometry.EndParticleProfilingBlock();

            return true;
        }

        public void AddMotionInheritance(ref float motionInheritance, ref MatrixD deltaMatrix)
        {
            Vector3D newPosition = Vector3D.Transform(m_actualPosition, deltaMatrix);

            m_actualPosition = m_actualPosition + (newPosition - m_actualPosition) * motionInheritance;

            Velocity = Vector3.TransformNormal(Velocity, deltaMatrix);
        }

        public Vector3D ActualPosition
        {
            get { return m_actualPosition; }
        }

        /// <summary>
        /// Return quad whos face is always looking to the camera. 
        /// IMPORTANT: This bilboard looks same as point vertexes (point sprites) - horizontal and vertical axes of billboard are always parallel to screen
        /// That means, if billboard is in the left-up corner of screen, it won't be distorted by perspective. If will look as 2D quad on screen. As I said, it's same as GPU points.
        /// </summary>
        private static void GetBillboardQuadRotated(VRageRender.MyBillboard billboard, ref Vector3D position, float radius, float angle)
        {
            float angleCos = radius * (float)Math.Cos(angle);
            float angleSin = radius * (float)Math.Sin(angle);

            //	Two main vectors of a billboard rotated around the view axis/vector
            Vector3D billboardAxisX = new Vector3D();
            billboardAxisX.X = angleCos * MyGuiManager.Camera.Left.X + angleSin * MyGuiManager.Camera.Up.X;
            billboardAxisX.Y = angleCos * MyGuiManager.Camera.Left.Y + angleSin * MyGuiManager.Camera.Up.Y;
            billboardAxisX.Z = angleCos * MyGuiManager.Camera.Left.Z + angleSin * MyGuiManager.Camera.Up.Z;

            Vector3D billboardAxisY = new Vector3D();
            billboardAxisY.X = -angleSin * MyGuiManager.Camera.Left.X + angleCos * MyGuiManager.Camera.Up.X;
            billboardAxisY.Y = -angleSin * MyGuiManager.Camera.Left.Y + angleCos * MyGuiManager.Camera.Up.Y;
            billboardAxisY.Z = -angleSin * MyGuiManager.Camera.Left.Z + angleCos * MyGuiManager.Camera.Up.Z;

            //	Coordinates of four points of a billboard's quad
            billboard.Position0.X = position.X + billboardAxisX.X + billboardAxisY.X;
            billboard.Position0.Y = position.Y + billboardAxisX.Y + billboardAxisY.Y;
            billboard.Position0.Z = position.Z + billboardAxisX.Z + billboardAxisY.Z;

            billboard.Position1.X = position.X - billboardAxisX.X + billboardAxisY.X;
            billboard.Position1.Y = position.Y - billboardAxisX.Y + billboardAxisY.Y;
            billboard.Position1.Z = position.Z - billboardAxisX.Z + billboardAxisY.Z;

            billboard.Position2.X = position.X - billboardAxisX.X - billboardAxisY.X;
            billboard.Position2.Y = position.Y - billboardAxisX.Y - billboardAxisY.Y;
            billboard.Position2.Z = position.Z - billboardAxisX.Z - billboardAxisY.Z;

            billboard.Position3.X = position.X + billboardAxisX.X - billboardAxisY.X;
            billboard.Position3.Y = position.Y + billboardAxisX.Y - billboardAxisY.Y;
            billboard.Position3.Z = position.Z + billboardAxisX.Z - billboardAxisY.Z;
        }


        public bool IsValid()
        {
            if (Life <= 0)
            {
                return false;
            }
            return MyUtils.IsValid(StartPosition) &&
            MyUtils.IsValid(Angle) &&
            MyUtils.IsValid(Velocity) &&
            MyUtils.IsValid(RotationSpeed) &&
            MyUtils.IsValid(m_actualPosition) &&
            MyUtils.IsValid(m_actualAngle);
        }
    }
}

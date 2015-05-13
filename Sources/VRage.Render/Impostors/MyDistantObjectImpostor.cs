using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRage.Utils;
using VRageRender.Utils;


namespace VRageRender
{
    public enum MyDistantObjectImpostorTypeEnum
    {
        Blinker,
        Explosion,
        Mover
        
    }

    public class MyDistantObjectImpostor
    {


        public Vector3 Position
        {
            get
            {
                return MyUtils.GetCartesianCoordinatesFromSpherical(m_angleHorizontal, m_angleVertical, MyDistantObjectsImpostorsConstants.DISTANT_OBJECTS_SPHERE_RADIUS);
            }
        }

        public float Radius;
        public float Angle;
        float m_angleHorizontal;
        float m_angleVertical;
        float m_targetHorizontal;
        float m_targetVertical;
        public MyDistantObjectImpostorTypeEnum Type;
        public Vector4 Color;
        bool m_fading;
        bool m_exploding;
        float m_blinkMultiplierUp;
        float m_blinkMultiplierDown;
        float m_glowTime;
        bool m_IsWaiting;
        int m_explosionDelay;

        // for explosions their starting radius
        float m_startingRadius;

        float m_moverSpeed;
        
        public MyDistantObjectImpostor()
        {
        }

        public void Start(MyDistantObjectImpostorTypeEnum type)
        {
            float angleHorizontal = MathHelper.ToRadians(MyUtils.GetRandomFloat(0, 360));
            float angleVertical = MathHelper.ToRadians(MyUtils.GetRandomFloat(0, 360));

            float LOW_PRIORITY_ANGLE = MathHelper.ToRadians(45);
            if (((angleVertical >= (MathHelper.PiOver2 - LOW_PRIORITY_ANGLE)) && (angleVertical <= (MathHelper.PiOver2 + LOW_PRIORITY_ANGLE))) ||
                ((angleVertical >= (3 * MathHelper.PiOver2 - LOW_PRIORITY_ANGLE)) && (angleVertical <= (3 * MathHelper.PiOver2 + LOW_PRIORITY_ANGLE))))
            {
                angleVertical = MathHelper.ToRadians(MyUtils.GetRandomFloat(0, 360));
            }
            
            int typeEnum = MyUtils.GetRandomInt(0, 3);
            m_angleHorizontal = angleHorizontal;
            m_angleVertical = angleVertical;           
            Angle = 0;
            Color = new Vector4(1f, 1f, 1f, 1f);
            Type = type;
            m_fading = true;
            m_glowTime = 0;

            //  We make a random starting alpha and a random blink multiplier
            //  so they don't all blink at the same time
            if (Type == MyDistantObjectImpostorTypeEnum.Blinker)
            {
                Radius = MyUtils.GetRandomFloat(3000, 5000f);
                m_blinkMultiplierUp = MyUtils.GetRandomFloat(70f, 90.0f);
                m_blinkMultiplierDown = MyUtils.GetRandomFloat(70f, 90.0f);
                m_startingRadius = Radius;
            }

            //  Moving objects are initialized with a random destination
            if (Type == MyDistantObjectImpostorTypeEnum.Mover)
            {
                
                Radius = MyUtils.GetRandomFloat(4000, 6000f);
                m_targetHorizontal = MathHelper.ToRadians(MyUtils.GetRandomFloat(0, 360));
                m_moverSpeed = MyUtils.GetRandomFloat(MyDistantObjectsImpostorsConstants.MAX_MOVE_DISTANCE*0.3f, MyDistantObjectsImpostorsConstants.MAX_MOVE_DISTANCE);
            }

            //  Explosions also have a blink multiplier to make flashes more random.
            //  Explosions will appear around their targer angles, but
            //  will stay in the same general area
            if (Type == MyDistantObjectImpostorTypeEnum.Explosion)
            {
                Radius = MyUtils.GetRandomFloat(4000f, 6000f);
                Color.X = MyUtils.GetRandomFloat(170.0f / 255f, 1.0f);
                Color.Y = MyUtils.GetRandomFloat(100.0f / 255f, Color.X);
                Color.Z = MyUtils.GetRandomFloat(0.0f, 40.0f / 255f);
                Color.W = MyUtils.GetRandomFloat(0.0f, 1.0f);
                m_blinkMultiplierUp = MyUtils.GetRandomFloat(11f, 16f);
                m_blinkMultiplierDown = MyUtils.GetRandomFloat(0.5f, 2f);
                m_targetHorizontal = m_angleHorizontal;
                m_targetVertical = m_angleVertical;
                m_startingRadius = Radius;
                m_IsWaiting = false;
                m_exploding = Convert.ToBoolean(MyUtils.GetRandomInt(0,1));
                m_explosionDelay = MyUtils.GetRandomInt((int)(MyDistantObjectsImpostorsConstants.EXPLOSION_WAIT_MILLISECONDS / 2.0f), (int)(MyDistantObjectsImpostorsConstants.EXPLOSION_WAIT_MILLISECONDS * 3f));
            }
        }

        public void Update()
        {
            if (Type == MyDistantObjectImpostorTypeEnum.Blinker)
            {
                updateBlinker();
            }
            else if (Type == MyDistantObjectImpostorTypeEnum.Explosion)
            {
                updateExplosion();
            }
            else if (Type == MyDistantObjectImpostorTypeEnum.Mover)
            {
                updateMover();
            }
        }

        //  Moving objects move towards a random destination
        //  When they get there, the destination changes
        private void updateMover()
        {
            if (m_targetHorizontal > m_angleHorizontal)
            {
                m_angleHorizontal += m_moverSpeed;
                m_targetHorizontal += m_moverSpeed;
            }
            else
            {
                m_angleHorizontal -= m_moverSpeed;
                m_targetHorizontal -= m_moverSpeed;
            }

        }

        private int GlowTimePerUpdate
        {
            // TODO: This is wrong, length of impostor explosion depends on frame rate
            get { return (int)(1000.0f / 60); }
        }

        //  Update an explosion objects
        //  Explosions blinks and then wait some time
        //  After the explosion dissapears, it will move to a nearby location and
        //  repeat
        private void updateExplosion()
        {
            if (m_fading && !m_IsWaiting)
            {
                Color.W -= MyDistantObjectsImpostorsConstants.EXPLOSION_FADE * m_blinkMultiplierDown;
                
            }
            else if (m_exploding)
            {
                Color.W += MyDistantObjectsImpostorsConstants.EXPLOSION_FADE * m_blinkMultiplierUp;
            }

            Radius = MathHelper.Lerp(m_startingRadius/4f, m_startingRadius, Color.W);

            if (Color.W <= 0)
            {
                m_IsWaiting = true;
                Color.W = MathHelper.Clamp(Color.W, 0, 1);
                Color.X = 0;
                Color.Y = 0;
                Color.Z = 0;

                if (m_glowTime > m_explosionDelay)
                {
                    float minNewHorizontal = m_targetHorizontal - (MyDistantObjectsImpostorsConstants.EXPLOSION_MOVE_DISTANCE * MyUtils.GetRandomInt(4, 7));
                    float maxNewHorizontal = m_targetHorizontal + (MyDistantObjectsImpostorsConstants.EXPLOSION_MOVE_DISTANCE * MyUtils.GetRandomInt(4, 7));
                    float minNewVertical = m_targetVertical - (MyDistantObjectsImpostorsConstants.EXPLOSION_MOVE_DISTANCE * MyUtils.GetRandomInt(4, 7));
                    float maxNewVertical = m_targetVertical + (MyDistantObjectsImpostorsConstants.EXPLOSION_MOVE_DISTANCE * MyUtils.GetRandomInt(4, 7));

                    Color.X = MyUtils.GetRandomFloat(170.0f / 255f, 1.0f);
                    Color.Y = MyUtils.GetRandomFloat(100.0f / 255f, 1.0f);
                    Color.Z = MyUtils.GetRandomFloat(0.0f, 40.0f / 255f);

                    m_angleHorizontal = MyUtils.GetRandomFloat(minNewHorizontal, maxNewHorizontal);
                    m_angleVertical = MyUtils.GetRandomFloat(minNewVertical, maxNewVertical);
                    m_IsWaiting = false;
                    m_exploding = true;
                    m_fading = false;
                }

            }

            if (m_IsWaiting)
            {
                m_glowTime += GlowTimePerUpdate;
            }
            else m_glowTime = 0;


            if (Color.W >= 1f)
            {
                m_fading = true;
                m_exploding = false;
                Color.W = MathHelper.Clamp(Color.W, 0, 1);
                //  We set delay time here to avoid unnecessarry logic for it
                m_explosionDelay = MyUtils.GetRandomInt((int)(MyDistantObjectsImpostorsConstants.EXPLOSION_WAIT_MILLISECONDS / 3f),(int)(MyDistantObjectsImpostorsConstants.EXPLOSION_WAIT_MILLISECONDS * 3f));
            }
        }

        //  Update a blinking object
        //  This object will blink and then wait some time
        private void updateBlinker()
        {
            if (m_fading && !m_IsWaiting)
            {
                Color.W -= MyDistantObjectsImpostorsConstants.BLINKER_FADE * m_blinkMultiplierDown;
            }
            if (m_exploding)
            {
                Color.W += MyDistantObjectsImpostorsConstants.BLINKER_FADE *  m_blinkMultiplierUp;
            }

            Radius = MathHelper.Lerp(m_startingRadius / 4f, m_startingRadius, Color.W);


            if (Color.W <= 0.3f)
            {
                m_IsWaiting = true;
                Color.W = MathHelper.Clamp(Color.W, 0, 1);

                if (m_glowTime > MyDistantObjectsImpostorsConstants.BLINKER_WAIT_MILLISECONDS)
                {
                    m_fading = false;
                    m_IsWaiting = false;
                    m_exploding = true;
                    m_glowTime = 0;
                }

            }

            if (m_IsWaiting)
            {
                m_glowTime += GlowTimePerUpdate;
            }

            if (Color.W >= 1f)
            {
                m_exploding = false;
                m_fading = true;
                Color.W = MathHelper.Clamp(Color.W, 0, 1);
            }
        }
    }
}

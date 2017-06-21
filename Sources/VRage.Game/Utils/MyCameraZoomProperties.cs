using System;
using VRageMath;
using VRage.Utils;

namespace VRage.Game.Utils
{
    public enum MyCameraZoomOperationType
    {
        NoZoom,
        ZoomingIn,
        ZoomingOut,
        Zoomed
    }

    //  Zooming 
    public class MyCameraZoomProperties
    {
        static readonly float FIELD_OF_VIEW_MIN = MathHelper.ToRadians(40);

        float ZoomTime = 0.075f; // s

        float m_currentZoomTime; //    Zoom time
        MyCameraZoomOperationType m_zoomType = MyCameraZoomOperationType.NoZoom;
        
        float m_FOV; // Current fov
        //float m_FOVForNearObjects;  // not used anymore
        float m_zoomLevel; //    Fov ratio

        MyCamera m_camera;

        //  Some basic options
        public MyCameraZoomProperties(MyCamera camera)
        {
            m_camera = camera;
            Update(0.0f);
        }

        //  Update!
        public void Update(float updateStepSize)
        {
            switch (m_zoomType)
            {
                case MyCameraZoomOperationType.NoZoom:
                    break;

                case MyCameraZoomOperationType.ZoomingIn:
                    {
                        if (m_currentZoomTime <= ZoomTime)
                        {
                            m_currentZoomTime += updateStepSize; // VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                            if (m_currentZoomTime >= ZoomTime)
                            {
                                m_currentZoomTime = ZoomTime;
                                m_zoomType = MyCameraZoomOperationType.Zoomed;
                            }
                        }
                    }
                    break;

                case MyCameraZoomOperationType.ZoomingOut:
                    {
                        if (m_currentZoomTime >= 0)
                        {
                            m_currentZoomTime -= updateStepSize; // VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                            if (m_currentZoomTime <= 0)
                            {
                                m_currentZoomTime = 0;
                                m_zoomType = MyCameraZoomOperationType.NoZoom;
                            }
                        }
                    }
                    break;
            }

            m_zoomLevel = 1 - m_currentZoomTime / ZoomTime;

            m_FOV = ApplyToFov ? MathHelper.Lerp(FIELD_OF_VIEW_MIN, m_camera.FieldOfView, m_zoomLevel) : m_camera.FieldOfView;
            // not used anymore
            //m_FOVForNearObjects = ApplyToFov ? MathHelper.Lerp(FIELD_OF_VIEW_MIN, m_camera.FieldOfViewForNearObjects, m_zoomLevel) : m_camera.FieldOfViewForNearObjects;
        }

        //reset zoom
        public void ResetZoom()
        {
            //StopZoomingALoopSound();
            m_zoomType = MyCameraZoomOperationType.NoZoom;
            m_currentZoomTime = 0;
        }

        //  Set zoom : 1 - zoom in, 0 - no zoom, -1 - zoom out
        public void SetZoom(MyCameraZoomOperationType inZoomType)
        {
            //if (m_zoomType == MyCameraZoomOperationType.NoZoom || m_zoomType == MyCameraZoomOperationType.Zoomed)
            {
                m_zoomType = inZoomType;
                //PlaySound(inZoomType);
            }
        }

        //  Return zoom level. 0 = 100% zoom in, 1 = no zoom;
        public float GetZoomLevel()
        {
            return m_zoomLevel;
        }

        public float GetFOV()
        {
            return MyMath.Clamp(m_FOV, MyMathConstants.EPSILON, (float)Math.PI - MyMathConstants.EPSILON);
        }

        // not used anymore
        //public float GetFOVForNearObjects()
        //{
        //    return MyMath.Clamp(m_FOVForNearObjects, MyMathConstants.EPSILON, (float)Math.PI - MyMathConstants.EPSILON);
        //}

        public bool IsZooming()
        {
            return m_zoomType != MyCameraZoomOperationType.NoZoom;
        }

        public bool ApplyToFov { get; set; }
    }
}

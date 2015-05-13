using System;
using VRageMath;

using Sandbox.Common;
using Sandbox.Game.Entities;
using VRage.Audio;

namespace Sandbox.Engine.Utils
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
        static MySoundPair ZOOM_SOUND = new MySoundPair("WepSniperScopeZoomA");

        float ZoomTime = 0.075f; // s

        float m_currentZoomTime; //    Zoom time
        MyCameraZoomOperationType m_zoomType = MyCameraZoomOperationType.NoZoom;
        
        float m_FOV; // Current fov
        float m_FOVForNearObjects;
        float m_zoomLevel; //    Fov ratio
        IMySourceVoice m_zoomRelCue; //  Zoom release sound
        IMySourceVoice m_zoomALoopCue;    //  Attack and Loop zoom sound

        MyCamera m_camera;

        //  Some basic options
        public MyCameraZoomProperties(MyCamera camera)
        {
            m_camera = camera;
            Update();
        }

        //  Update!
        public void Update()
        {
            switch (m_zoomType)
            {
                case MyCameraZoomOperationType.NoZoom:
                    break;

                case MyCameraZoomOperationType.ZoomingIn:
                    {
                        if (m_currentZoomTime <= ZoomTime)
                        {
                            ResumeCue(m_zoomALoopCue);
                            ResumeCue(m_zoomRelCue);

                            m_currentZoomTime += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                            if (m_currentZoomTime >= ZoomTime)
                            {
                                m_currentZoomTime = ZoomTime;
                                StopZoomingALoopSound();
                                m_zoomType = MyCameraZoomOperationType.Zoomed;
                            }
                        }
                    }
                    break;

                case MyCameraZoomOperationType.ZoomingOut:
                    {
                        if (m_currentZoomTime >= 0)
                        {
                            ResumeCue(m_zoomALoopCue);
                            ResumeCue(m_zoomRelCue);

                            m_currentZoomTime -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                            if (m_currentZoomTime <= 0)
                            {
                                m_currentZoomTime = 0;
                                StopZoomingALoopSound();
                                m_zoomType = MyCameraZoomOperationType.NoZoom;
                            }
                        }
                    }
                    break;
            }

            m_zoomLevel = 1 - m_currentZoomTime / ZoomTime;

            m_FOV = ApplyToFov ? MathHelper.Lerp(MyConstants.FIELD_OF_VIEW_MIN, m_camera.FieldOfView, m_zoomLevel) : m_camera.FieldOfView;
            m_FOVForNearObjects = ApplyToFov ? MathHelper.Lerp(MyConstants.FIELD_OF_VIEW_MIN, m_camera.FieldOfViewForNearObjects, m_zoomLevel) : m_camera.FieldOfViewForNearObjects;
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
            return m_FOV;
        }

        public float GetFOVForNearObjects()
        {
            return m_FOVForNearObjects;
        }

        public bool IsZooming()
        {
            return m_zoomType != MyCameraZoomOperationType.NoZoom;
        }

        public void PauseZoomCue()
        {
            PauseCue(m_zoomALoopCue);
            PauseCue(m_zoomRelCue);
        }

        void PauseCue(IMySourceVoice cue)
        {
            if ((cue != null) && cue.IsPlaying)
            {
                cue.Pause();
            }
        }

        void ResumeCue(IMySourceVoice cue)
        {
            if ((cue != null) && cue.IsPaused)
            {
                cue.Resume();
            }
        }

        //  Will stop zooming loop sound and play zooming rel sound
        void StopZoomingALoopSound()
        {
            if ((m_zoomALoopCue != null) && m_zoomALoopCue.IsPlaying)
            {
                m_zoomALoopCue.Stop();
                PlayZoomingRelSound();
            }
        }

        void PlayZoomingALoopSound()
        {
            if ((m_zoomALoopCue == null) || !m_zoomALoopCue.IsPlaying)
            {
                m_zoomALoopCue = MyAudio.Static.PlaySound(ZOOM_SOUND.SoundId);
            }
        }

        void PlayZoomingRelSound()
        {
            if ((m_zoomRelCue == null) || !m_zoomRelCue.IsPlaying)
            {
                m_zoomRelCue = MyAudio.Static.PlaySound(ZOOM_SOUND.SoundId);
            }
        }

        //  Plays correct sound
        void PlaySound(MyCameraZoomOperationType inZoomType)
        {
            switch (inZoomType)
            {
                case MyCameraZoomOperationType.ZoomingIn:
                        PlayZoomingALoopSound();
                    break;
                case MyCameraZoomOperationType.ZoomingOut:
                        PlayZoomingALoopSound();
                    break;
                case MyCameraZoomOperationType.NoZoom:
                        StopZoomingALoopSound();
                    break;
            }
        }

        public bool ApplyToFov { get; set; }
    }
}

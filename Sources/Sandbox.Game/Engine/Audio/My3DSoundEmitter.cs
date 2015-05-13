using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Platform;

namespace Sandbox.Engine.Audio
{
    interface IMy3DSoundEmitter
    {
        MySoundCuesEnum CueEnum { get; }
        MySourceVoice Sound { get; set; }
        Vector3 SourcePosition { get; }
        Vector3 DirForward { get; }
        Vector3 DirUp { get; }
        Vector3 Velocity { get; }
        MyEntity Entity { get; }
        MyEntity OwnedBy { get; }
    }

    class MyEntity3DSoundEmitter : IMy3DSoundEmitter
    {
        #region fields
        private MySoundCuesEnum m_cueEnum;
        private MySourceVoice m_sound;
        private MyEntity m_entity;
        private MyEntity m_owner;
        private Vector3? m_position;
        private Vector3? m_velocity;
        #endregion

        #region properties
        public MySoundCuesEnum CueEnum
        {
            get { return m_cueEnum; }
            set { m_cueEnum = value; }
        }

        public MySourceVoice Sound
        {
            get { return m_sound; }
            set { m_sound = value; }
        }

        public void SetPosition(Vector3 position)
        {
            m_position = position;
        }

        public void SetVelocity(Vector3 velocity)
        {
            m_velocity = velocity;
        }
        #endregion

        public MyEntity3DSoundEmitter(MyEntity entity, MyEntity owner = null)
        {
            m_entity = entity;
            m_owner = owner;
        }

        public void PlaySingleSound(MySoundCuesEnum cueEnum, bool update)
        {
            CueEnum = cueEnum;
            Sound = MyAudio.PlayCue3D(this, CueEnum, update);
        }

        public void PlayIntroLoopPair(MySoundCuesEnum introCueEnum, MySoundCuesEnum loopCueEnum)
        {
            CueEnum = loopCueEnum;
            Sound = MyAudio.PlayIntroLoopPair3D(this, introCueEnum, loopCueEnum);
        }

        public void StopSound(bool forced)
        {
            if ((m_sound != null) && m_sound.IsPlaying)
            {
                m_sound.Stop(forced);
                m_sound = null;
                CueEnum = MySoundCuesEnum.None;
            }
        }

        #region IMy3DSoundEmitter
        public Vector3 SourcePosition
        {
            get
            {
                if (m_position.HasValue)
                    return m_position.Value;
                else if (m_entity != null)
                    return m_entity.WorldMatrix.Translation;
                else
                    return Vector3.Zero;
            }
        }
        public Vector3 DirForward
        {
            get { return Vector3.Normalize(SharpDXHelper.ToXNA(MyAudio.Listener.Position) - SourcePosition); }
            //get
            //{
            //    if ((MySession.Player != null) && (MySession.Player.PlayerEntity != null) && (MySession.Player.PlayerEntity.Entity != null))
            //        return Vector3.Normalize(MySession.Player.PlayerEntity.Entity.GetPosition() - SourcePosition);
            //    else if (MySector.MainCamera != null)
            //        return Vector3.Normalize(MySector.MainCamera.Position - SourcePosition);
            //    else
            //        return m_entity.WorldMatrix.Forward;
            //}
        }
        public Vector3 DirUp
        {
            get
            {
                if (m_entity != null)
                    return m_entity.WorldMatrix.Up;
                else
                    return Vector3.Up;
            }
        }
        public Vector3 Velocity
        { 
            get
            {
                if (m_velocity.HasValue)
                    return m_velocity.Value;
                else if ((m_entity != null) && (m_entity.Physics != null))
                    return m_entity.Physics.LinearVelocity;
                else
                    return Vector3.Zero;
            }
        }
        public MyEntity Entity
        {
            get { return m_entity; }
            set { m_entity = value; }
        }
        public MyEntity OwnedBy
        {
            get { return m_owner; }
            set { m_owner = value; }
        }
        #endregion
    }
}

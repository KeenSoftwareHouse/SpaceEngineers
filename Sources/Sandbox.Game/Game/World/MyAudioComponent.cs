using Sandbox.Common;
using Sandbox.Game.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Audio;
using VRage.Collections;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Game.World
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MyAudioComponent : MySessionComponentBase
    {
        public static ConcurrentDictionary<long, byte> ContactSoundsPool = new ConcurrentDictionary<long,byte>();
        private static int m_updateCounter = 0;
        private const int POOL_CAPACITY = 30;
        private static MyConcurrentQueue<MyEntity3DSoundEmitter> m_singleUseEmitterPool = new MyConcurrentQueue<MyEntity3DSoundEmitter>(POOL_CAPACITY);
        private static int m_currentEmitters;

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            m_updateCounter++;
            //if(m_updateCounter % 100 == 0)
            //    for (int i = 0; i < ContactSoundsPool.Count; i++)
            //    {
            //        var key = ContactSoundsPool.Keys.ElementAt(i);
            //        if (ContactSoundsPool[key].Sound == null || !ContactSoundsPool[key].Sound.IsPlaying)
            //        {
            //            ContactSoundsPool.Remove(key);
            //            i--;
            //        }
            //    }
        }

        /// <summary>
        /// Use this only for 3d one-time nonloop sounds, emitter returns to pool after the sound is played
        /// Dont forget to set your entity
        /// </summary>
        /// <returns>Emitter or null if none is avaliable in pool</returns>
        public static MyEntity3DSoundEmitter TryGetSoundEmitter()
        {
            MyEntity3DSoundEmitter emitter = null;
            if(!m_singleUseEmitterPool.TryDequeue(out emitter))
                if (m_currentEmitters < POOL_CAPACITY)
                {
                    emitter = new MyEntity3DSoundEmitter(null);
                    emitter.StoppedPlaying += emitter_StoppedPlaying;
                    m_currentEmitters++;
                }
            return emitter;
        }

        static void emitter_StoppedPlaying(MyEntity3DSoundEmitter emitter)
        {
            emitter.Entity = null;
            emitter.SoundId = new MyCueId(MyStringHash.NullOrEmpty);
            m_singleUseEmitterPool.Enqueue(emitter);
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            m_singleUseEmitterPool.Clear();
            m_currentEmitters = 0;
        }
    }
}

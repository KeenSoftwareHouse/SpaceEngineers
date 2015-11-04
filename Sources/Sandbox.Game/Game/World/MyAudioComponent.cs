using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Collections;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

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

        static MyStringId m_startCue = MyStringId.GetOrCompute("Start");

        public static void PlayContactSound(long entityId, Vector3D position, MyStringHash materialA, MyStringHash materialB, float volume = 1, Func<bool> canHear = null, Func<bool> shouldPlay2D = null)
        {
            ProfilerShort.Begin("GetCue");
            MySoundPair cue = MyMaterialPropertiesHelper.Static.GetCollisionCue(m_startCue, materialA, materialB);
            if (!cue.SoundId.IsNull && MyAudio.Static.SourceIsCloseEnoughToPlaySound(position, cue.SoundId))
            {

                MyEntity3DSoundEmitter emitter = MyAudioComponent.TryGetSoundEmitter();
                if (emitter == null)
                {
                    ProfilerShort.End();
                    return;
                }
                ProfilerShort.BeginNextBlock("Emitter lambdas");
                MyAudioComponent.ContactSoundsPool.TryAdd(entityId, 0);
                emitter.StoppedPlaying += (e) =>
                {
                    byte val;
                    MyAudioComponent.ContactSoundsPool.TryRemove(entityId, out val);
                };
                if (MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS)
                {
                    Action<MyEntity3DSoundEmitter> remove = null;
                    remove = (e) =>
                    {
                        emitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.CanHear].Remove(canHear);
                        emitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.ShouldPlay2D].Remove(shouldPlay2D);
                        emitter.StoppedPlaying -= remove;
                    };
                    emitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.CanHear].Add(canHear);
                    emitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.ShouldPlay2D].Add(shouldPlay2D);
                    emitter.StoppedPlaying += remove;
                }
                ProfilerShort.BeginNextBlock("PlaySound");
                emitter.SetPosition(position);
                emitter.PlaySound(cue, true);

                if (emitter.Sound != null)
                {
                    if (volume != 0)
                    {
                        emitter.Sound.SetVolume(volume);
                    }
                }
            }
            ProfilerShort.End();
        }

        private static MyStringId m_destructionSound = MyStringId.GetOrCompute("Destruction");
        public static void PlayDestructionSound(MyFracturedPiece fp)
        {
            var bDef = MyDefinitionManager.Static.GetCubeBlockDefinition(fp.OriginalBlocks[0]);

            if (bDef == null)
                return;
            MyPhysicalMaterialDefinition def = bDef.PhysicalMaterial;

            MySoundPair destructionCue;
            if (def.GeneralSounds.TryGetValue(m_destructionSound, out destructionCue) && !destructionCue.SoundId.IsNull)
            {
                var emmiter = MyAudioComponent.TryGetSoundEmitter();
                if (emmiter == null)
                    return;
                Vector3D pos = fp.PositionComp.GetPosition();
                emmiter.SetPosition(pos);
                emmiter.PlaySound(destructionCue);
            }
        }

        public static void PlayDestructionSound(MySlimBlock b)
        {
            MyPhysicalMaterialDefinition def = null;
            if (b.FatBlock is MyCompoundCubeBlock)
            {
                def = (b.FatBlock as MyCompoundCubeBlock).GetBlocks()[0].BlockDefinition.PhysicalMaterial;
            }
            else if (b.FatBlock is MyFracturedBlock)
            {
                MyCubeBlockDefinition bDef;
                if (MyDefinitionManager.Static.TryGetDefinition<MyCubeBlockDefinition>((b.FatBlock as MyFracturedBlock).OriginalBlocks[0], out bDef))
                    def = bDef.PhysicalMaterial;
            }
            else
                def = b.BlockDefinition.PhysicalMaterial;

            if (def == null)
                return;

            MySoundPair destructionCue;
            if (def.GeneralSounds.TryGetValue(m_destructionSound, out destructionCue) && !destructionCue.SoundId.IsNull)
            {
                var emmiter = MyAudioComponent.TryGetSoundEmitter();
                if (emmiter == null)
                    return;
                Vector3D pos;
                b.ComputeWorldCenter(out pos);
                emmiter.SetPosition(pos);
                emmiter.PlaySound(destructionCue);
            }
        }
    }
}

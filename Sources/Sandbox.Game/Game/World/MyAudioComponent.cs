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
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.World
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MyAudioComponent : MySessionComponentBase
    {
        public static ConcurrentDictionary<long, byte> ContactSoundsPool = new ConcurrentDictionary<long, byte>();
        private static int m_updateCounter = 0;
        private const int POOL_CAPACITY = 50;
        private static MyConcurrentQueue<MyEntity3DSoundEmitter> m_singleUseEmitterPool = new MyConcurrentQueue<MyEntity3DSoundEmitter>(POOL_CAPACITY);
        private static List<MyEntity3DSoundEmitter> m_borrowedEmitters = new List<MyEntity3DSoundEmitter>();
        private static List<MyEntity3DSoundEmitter> m_emittersToRemove = new List<MyEntity3DSoundEmitter>();
        private static Dictionary<string, MyEntity3DSoundEmitter> m_emitterLibrary = new Dictionary<string, MyEntity3DSoundEmitter>();
        private static List<string> m_emitterLibraryToRemove = new List<string>();
        private static int m_currentEmitters;
        private static List<MyEntity> m_detectedGrids = new List<MyEntity>();
        private static MyCueId m_nullCueId = new MyCueId(MyStringHash.NullOrEmpty);

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            m_updateCounter++;
            if (m_updateCounter % 100 == 0 && MySession.Static.LocalCharacter != null)
            {
                m_detectedGrids.Clear();
                BoundingSphereD playerSphere = new BoundingSphereD(MySession.Static.LocalCharacter.PositionComp.GetPosition(), 500f);
                MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref playerSphere, m_detectedGrids);
                for (int i = 0; i < m_detectedGrids.Count; i++)
                {
                    MyCubeGrid grid = m_detectedGrids[i] as MyCubeGrid;
                    if (grid != null)
                    {
                        foreach (var block in grid.CubeBlocks)
                        {
                            if (block.FatBlock is MyFunctionalBlock)
                                (block.FatBlock as MyFunctionalBlock).UpdateSoundEmitters();
                        }
                    }
                }
                foreach (var key in m_emitterLibraryToRemove)
                {
                    if (m_emitterLibrary.ContainsKey(key))
                    {
                        m_emitterLibrary[key].StopSound(true);
                        m_emitterLibrary.Remove(key);
                    }
                }
                m_emitterLibraryToRemove.Clear();
                foreach (var emitter in m_emitterLibrary.Values)
                    emitter.Update();
            }
        }

        /// <summary>
        /// Use this only for 3d one-time nonloop sounds, emitter returns to pool after the sound is played
        /// Dont forget to set your entity
        /// </summary>
        /// <returns>Emitter or null if none is avaliable in pool</returns>
        public static MyEntity3DSoundEmitter TryGetSoundEmitter()
        {
            if (m_currentEmitters >= POOL_CAPACITY)
                CheckEmitters();
            if (m_emittersToRemove.Count > 0)
                CleanUpEmitters();

            MyEntity3DSoundEmitter emitter = null;
            if (!m_singleUseEmitterPool.TryDequeue(out emitter))
            {
                if (m_currentEmitters < POOL_CAPACITY)
                {
                    emitter = new MyEntity3DSoundEmitter(null);
                    emitter.StoppedPlaying += emitter_StoppedPlaying;
                    emitter.CanPlayLoopSounds = false;
                    m_currentEmitters++;
                }
            }
            if (emitter != null)
                m_borrowedEmitters.Add(emitter);
            return emitter;
        }

        static void emitter_StoppedPlaying(MyEntity3DSoundEmitter emitter)
        {
            if (emitter == null)
                return;
            m_emittersToRemove.Add(emitter);
        }

        private static void CheckEmitters()
        {
            for (int i = 0; i < m_borrowedEmitters.Count; i++)
            {
                var emitter = m_borrowedEmitters[i];
                if (emitter != null && !emitter.IsPlaying)
                    m_emittersToRemove.Add(emitter);
            }
        }

        private static void CleanUpEmitters()
        {
            for (int i = 0; i < m_emittersToRemove.Count; i++)
            {
                var emitter = m_emittersToRemove[i];
                if (emitter != null)
                {
                    emitter.Entity = null;
                    emitter.SoundId = m_nullCueId;
                    m_singleUseEmitterPool.Enqueue(emitter);
                    m_borrowedEmitters.Remove(emitter);
                }
            }
            m_emittersToRemove.Clear();
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            m_singleUseEmitterPool.Clear();
            foreach (var emitter in m_emitterLibrary.Values)
                emitter.StopSound(true, true);
            m_emitterLibrary.Clear();
            m_emitterLibraryToRemove.Clear();
            m_currentEmitters = 0;
        }

        public static MyEntity3DSoundEmitter CreateNewLibraryEmitter(string id, MyEntity entity = null)
        {
            if (!m_emitterLibrary.ContainsKey(id))
            {
                m_emitterLibrary.Add(id, new MyEntity3DSoundEmitter(entity, (entity != null && entity is MyCubeBlock)));
                return m_emitterLibrary[id];
            }
            else
            {
                return null;
            }
        }

        public static MyEntity3DSoundEmitter GetLibraryEmitter(string id)
        {
            if (m_emitterLibrary.ContainsKey(id))
                return m_emitterLibrary[id];
            else
                return null;
        }

        public static void RemoveLibraryEmitter(string id)
        {
            if (m_emitterLibrary.ContainsKey(id))
            {
                m_emitterLibrary[id].StopSound(true, true);
                m_emitterLibraryToRemove.Add(id);
            }
        }

        public static bool PlayContactSound(long entityId, MyStringId strID, Vector3D position, MyStringHash materialA, MyStringHash materialB, float volume = 1, Func<bool> canHear = null, Func<bool> shouldPlay2D = null, MyEntity surfaceEntity = null, float separatingVelocity = 0f)
        {
            ProfilerShort.Begin("GetCue");

            MyEntity firstEntity = null;
            if (!MyEntities.TryGetEntityById(entityId, out firstEntity) || MyMaterialPropertiesHelper.Static == null || MySession.Static == null)
            {
                ProfilerShort.End();
                return false;
            }

            MySoundPair cue = (firstEntity.Physics != null && firstEntity.Physics.IsStatic == false) ?
                MyMaterialPropertiesHelper.Static.GetCollisionCueWithMass(strID, materialA, materialB, ref volume, firstEntity.Physics.Mass, separatingVelocity) :
                MyMaterialPropertiesHelper.Static.GetCollisionCue(strID, materialA, materialB);

            if (cue == null || cue.SoundId == null || MyAudio.Static == null)
                return false;

            if (separatingVelocity > 0f && separatingVelocity < 0.5f)
                return false;

            if (!cue.SoundId.IsNull && MyAudio.Static.SourceIsCloseEnoughToPlaySound(position, cue.SoundId))
            {
                MyEntity3DSoundEmitter emitter = MyAudioComponent.TryGetSoundEmitter();
                if (emitter == null)
                {
                    ProfilerShort.End();
                    return false;
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
                bool inSpace = MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS && MySession.Static.LocalCharacter != null && MySession.Static.LocalCharacter.AtmosphereDetectorComp != null && MySession.Static.LocalCharacter.AtmosphereDetectorComp.InVoid;
                if (surfaceEntity != null && !inSpace)
                    emitter.Entity = surfaceEntity;
                else
                    emitter.Entity = firstEntity;
                emitter.SetPosition(position);

                //GR: Changed stopPrevious argument to false due to bugs with explosion sound. May revision to the future
                emitter.PlaySound(cue, false);

                if (emitter.Sound != null)
                    emitter.Sound.SetVolume(emitter.Sound.Volume * volume);

                if (inSpace && surfaceEntity != null)
                {
                    MyEntity3DSoundEmitter emitter2 = MyAudioComponent.TryGetSoundEmitter();
                    if (emitter2 == null)
                    {
                        ProfilerShort.End();
                        return false;
                    }
                    ProfilerShort.BeginNextBlock("Emitter 2 lambdas");
                    Action<MyEntity3DSoundEmitter> remove = null;
                    remove = (e) =>
                    {
                        emitter2.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.CanHear].Remove(canHear);
                        emitter2.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.ShouldPlay2D].Remove(shouldPlay2D);
                        emitter2.StoppedPlaying -= remove;
                    };
                    emitter2.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.CanHear].Add(canHear);
                    emitter2.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.ShouldPlay2D].Add(shouldPlay2D);
                    emitter2.StoppedPlaying += remove;

                    ProfilerShort.BeginNextBlock("PlaySound");
                    emitter2.Entity = surfaceEntity;
                    emitter2.SetPosition(position);

                    emitter2.PlaySound(cue, false);

                    if (emitter2.Sound != null)
                        emitter2.Sound.SetVolume(emitter2.Sound.Volume * volume);
                }

                ProfilerShort.End();
                return true;
            }

            ProfilerShort.End();
            return false;
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
                var compound = b.FatBlock as MyCompoundCubeBlock;
                if (compound.GetBlocksCount() > 0)
                    def = compound.GetBlocks()[0].BlockDefinition.PhysicalMaterial;
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

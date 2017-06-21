#region Using

using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage.Utils;
using VRageMath;
using ConstraintKey = VRage.MyTuple<Sandbox.Game.Entities.Cube.MySlimBlock, Sandbox.Game.Entities.Cube.MySlimBlock>;
using Sandbox.Game.World;

#endregion

namespace Sandbox.Game.GameSystems.StructuralIntegrity
{
    public class MyStructuralIntegrity
    {
        public static bool Enabled { get { return MyFakes.ENABLE_STRUCTURAL_INTEGRITY && MySession.Static != null && MySession.Static.Settings.EnableStructuralSimulation; } }
        public bool EnabledOnlyForDraw = false;
        public static float MAX_SI_TENSION = 10;

        private MyCubeGrid m_cubeGrid;
        private IMyIntegritySimulator m_simulator;

        public MyStructuralIntegrity(MyCubeGrid cubeGrid)
        {
            m_cubeGrid = cubeGrid;
            m_cubeGrid.OnBlockAdded += cubeGrid_OnBlockAdded;
            m_cubeGrid.OnBlockRemoved += cubeGrid_OnBlockRemoved;
            m_cubeGrid.OnBlockIntegrityChanged += cubeGrid_OnBlockIntegrityChanged;

            switch (1)
            {
                case 0: m_simulator = new MyJacobianConstraintSimulator(m_cubeGrid.GetBlocks().Count); break;
                case 1: m_simulator = new MyAdvancedStaticSimulator(m_cubeGrid); break;
				case 2: m_simulator = new MyOndraSimulator(m_cubeGrid); break;
                case 3: m_simulator = new MyOndraSimulator2(m_cubeGrid); break;
                case 4: m_simulator = new MyOndraSimulator3(m_cubeGrid); break;
            }

            foreach (var block in m_cubeGrid.GetBlocks())
            {
                cubeGrid_OnBlockAdded(block);
            }
        }

        public void Close()
        {
            m_cubeGrid.OnBlockAdded -= cubeGrid_OnBlockAdded;
            m_cubeGrid.OnBlockRemoved -= cubeGrid_OnBlockRemoved;
            m_cubeGrid.OnBlockIntegrityChanged -= cubeGrid_OnBlockIntegrityChanged;

            m_simulator.Close();
        }

        void cubeGrid_OnBlockAdded(MySlimBlock block)
        {
            m_simulator.Add(block);
        }

        void cubeGrid_OnBlockRemoved(MySlimBlock block)
        {
            m_simulator.Remove(block);
        }

        void cubeGrid_OnBlockIntegrityChanged(MySlimBlock obj)
        {
            m_simulator.ForceRecalc();
        }

        public void ForceRecalculation()
        {
            m_simulator.ForceRecalc();
        }

        private int DestructionDelay = 10;
        private int m_destructionDelayCounter = 0;
        private bool m_SISimulated = false;

        public void Update(float deltaTime)
        {
            // Solve constraints.
            if (m_simulator.Simulate(deltaTime))
                m_SISimulated = true;
            
            if (m_destructionDelayCounter > 0)
                m_destructionDelayCounter--;

            if (m_SISimulated && m_destructionDelayCounter == 0 && MyPetaInputComponent.ENABLE_SI_DESTRUCTIONS && !EnabledOnlyForDraw)
            { //supported weights changed            
                if (Sync.IsServer)
                {
                    m_destructionDelayCounter = DestructionDelay;
                    m_SISimulated = false;

                    MySlimBlock worstBlock = null;
                    float maxTension = float.MinValue;

                    foreach (var block in m_cubeGrid.GetBlocks())
                    {
                        float tension = m_simulator.GetTension(block.Position);

                        if (tension > maxTension)
                        {
                            maxTension = tension;
                            worstBlock = block;
                        }
                    }

                    Vector3D worldCenter = Vector3D.Zero;
                    if (worstBlock != null)
                        worstBlock.ComputeWorldCenter(out worldCenter);

                    if (maxTension > MAX_SI_TENSION)
                    {
                        m_SISimulated = true;

                        CreateSIDestruction(worldCenter);
                    }
                }

                m_cubeGrid.TestDynamic = MyCubeGrid.MyTestDynamicReason.GridSplit;
            }
        }

        public void CreateSIDestruction(Vector3D worldCenter)
        {
            HkdFractureImpactDetails details = HkdFractureImpactDetails.Create();
            details.SetBreakingBody(m_cubeGrid.Physics.RigidBody);
            details.SetContactPoint(m_cubeGrid.Physics.WorldToCluster(worldCenter));
            details.SetDestructionRadius(1.5f);
            details.SetBreakingImpulse(Sandbox.MyDestructionConstants.STRENGTH * 10);
            details.SetParticleVelocity(Vector3.Zero);
            details.SetParticlePosition(m_cubeGrid.Physics.WorldToCluster(worldCenter));
            details.SetParticleMass(10000);
            //details.ZeroColidingParticleVelocity();
            details.Flag = details.Flag | HkdFractureImpactDetails.Flags.FLAG_DONT_RECURSE;
            if (m_cubeGrid.GetPhysicsBody().HavokWorld.DestructionWorld != null)
            {
                MyPhysics.FractureImpactDetails destruction = new MyPhysics.FractureImpactDetails();
                destruction.Details = details;
                destruction.World = m_cubeGrid.GetPhysicsBody().HavokWorld;
                destruction.Entity = m_cubeGrid;
                destruction.ContactInWorld = worldCenter;
                MyPhysics.EnqueueDestruction(destruction);
            }
        }

        public void Draw()
        {
            m_simulator.Draw();
        }

        public void DebugDraw()
        {
            m_simulator.DebugDraw();
        }

        public bool IsConnectionFine(MySlimBlock blockA, MySlimBlock blockB)
        {
            return m_simulator.IsConnectionFine(blockA, blockB);
        }
    }

}

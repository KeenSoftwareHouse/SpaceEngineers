
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using ConstraintKey = VRage.MyTuple<Sandbox.Game.Entities.Cube.MySlimBlock, Sandbox.Game.Entities.Cube.MySlimBlock>;

namespace Sandbox.Game.GameSystems.StructuralIntegrity
{
    internal class MyJacobianConstraintSimulator : IMyIntegritySimulator
    {
        private static readonly ConstraintComparer m_constraintComparer = new ConstraintComparer();

        private Dictionary<ConstraintKey, ConstraintBase> m_constraints = new Dictionary<ConstraintKey, ConstraintBase>(m_constraintComparer);
        private Dictionary<MySlimBlock, BlockState> m_statesByBlock = new Dictionary<MySlimBlock, BlockState>();
        private SolverFast m_solver = new SolverFast();
        private bool m_structureChanged;
        private int m_constraintAtomCount;

        public MyJacobianConstraintSimulator(int capacity)
        {
            m_structureChanged = true;
        }

        public bool EnabledMovement
        {
            get { return false; }
        }

        private bool HasConstraintBetween(MySlimBlock a, MySlimBlock b)
        {
            return m_constraints.ContainsKey(new ConstraintKey(a, b));
        }

        public void Add(MySlimBlock block)
        {
            BlockState blockState, neighbourState;
            blockState = GetOrCreateState(block);

            foreach (var neighbour in block.Neighbours)
            {
                if (HasConstraintBetween(block, neighbour))
                    continue;
                neighbourState = GetOrCreateState(neighbour);
                Add(blockState, neighbourState);
            }
        }

        private BlockState GetOrCreateState(MySlimBlock block)
        {
            BlockState res;
            if (!m_statesByBlock.TryGetValue(block, out res))
            {
                res = new BlockState(block);
                m_statesByBlock.Add(block, res);
            }
            return res;
        }

        private void Add(BlockState a, BlockState b)
        {
            // Correct order of blocks in the constraint.
            Vector3D centerA, centerB;
            a.Block.ComputeScaledCenter(out centerA);
            b.Block.ComputeScaledCenter(out centerB);
            var relativeDir = centerA - centerB;
            // Checks whether there is only one coordinate set (currently this is assumption).
            Debug.Assert(relativeDir.LengthSquared() == relativeDir.Sum * relativeDir.Sum);
            if (relativeDir.Sum < 0)
                MyUtils.Swap(ref a, ref b);

            var key = new ConstraintKey(a.Block, b.Block);
            Debug.Assert(!m_constraints.ContainsKey(key));

            ConstraintBase constraint = new ConstraintFixed1DoF();
            constraint.Bind(a, b);
            m_constraints[key] = constraint;
            m_structureChanged = true;
        }

        public void Remove(MySlimBlock block)
        {
            Debug.Assert(m_statesByBlock.ContainsKey(block));
            m_statesByBlock.Remove(block);
            foreach (var neighbor in block.Neighbours)
            {
                m_constraints.Remove(new ConstraintKey(block, neighbor));
                m_structureChanged = true;
            }
        }

        public bool Simulate(float deltaTime)
        {
            if (m_constraints.Count == 0)
                return false;

            ProfilerShort.Begin("Rebuild structure");
            RebuildStructure();
            ProfilerShort.End();

            GenerateForces();

            var blocksEtor = m_statesByBlock.Values.GetEnumerator();
            var constraintsEtor = m_constraints.Values.GetEnumerator();

            m_solver.Setup(deltaTime,
                m_statesByBlock.Count, blocksEtor,
                m_constraintAtomCount, constraintsEtor);

            m_solver.Solve(10);

            m_solver.ApplySolution(blocksEtor, constraintsEtor);

            // Integrate
            foreach (var block in m_statesByBlock.Values)
            {
                if (!block.IsFixed)
                    Integrate(block, deltaTime);

                block.LinearAcceleration = Vector3.Zero;
                block.AngularAcceleration = Vector3.Zero;
            }

            return true;
        }

        private static void Integrate(BlockState state, float dt)
        {
            state.LinearVelocity += state.LinearAcceleration * dt - 0.005f * state.LinearVelocity;
            state.AngularVelocity += state.AngularAcceleration * dt - 0.005f * state.AngularVelocity;
        }

        private void RebuildStructure()
        {
            if (!m_structureChanged)
                return;

            MyCubeGrid grid = GetGrid();

            int blockCount = 0;
            foreach (var block in m_statesByBlock.Values)
            {
                block.Index = blockCount;
                ++blockCount;
            }

            m_constraintAtomCount = 0;
            foreach (var entry in m_constraints)
            {
                entry.Value.Index = m_constraintAtomCount;
                m_constraintAtomCount += entry.Value.AtomCount;
            }

            m_structureChanged = false;
        }

        private void GenerateForces()
        {
            // Generate some dummy forces.
            var gravity = 9.8f * Vector3.Down;
            foreach (var block in m_statesByBlock.Values)
            {
                if (!block.IsFixed)
                {
                    block.LinearAcceleration = gravity;
                }
            }
        }

        private MyCubeGrid GetGrid()
        {
            MyCubeGrid grid = null;
            var tmp = m_statesByBlock.Keys.GetEnumerator();
            if (tmp.MoveNext())
                grid = tmp.Current.CubeGrid;
            return grid;
        }

        public void Draw()
        {
        }

        public void DebugDraw()
        {
            foreach (var constraint in m_constraints.Values)
            {
                constraint.DebugDraw();
            }
        }

        public bool IsConnectionFine(MySlimBlock blockA, MySlimBlock blockB)
        {
            throw new NotImplementedException();
        }

        struct SolverBody
        {
            public float InvMass;
            public Matrix InvInertiaWorld;

            public Vector3 DeltaLinearAcceleration;
            public Vector3 DeltaAngularAcceleration;

            public void ApplyImpulse(ref Vector3 linearComponent, ref Vector3 angularComponent, float impulseMagnitude)
            {
                DeltaLinearAcceleration += impulseMagnitude * linearComponent;
                DeltaAngularAcceleration += impulseMagnitude * angularComponent;
            }

            public void ApplyImpulse(ref Vector3 linearComponent, float impulseMagnitude)
            {
                DeltaLinearAcceleration += impulseMagnitude * linearComponent;
            }

            public void Reset()
            {
                InvMass = 0f;
                InvInertiaWorld = Matrix.Zero;
                DeltaLinearAcceleration = Vector3.Zero;
                DeltaAngularAcceleration = Vector3.Zero;
            }

            public override string ToString()
            {
#if enableAngular
                return string.Format("dAl {0}; dAa {1}", DeltaLinearAcceleration, DeltaAngularAcceleration);
#else
                return string.Format("dAl {0}",  DeltaLinearAcceleration);
#endif
            }
        }

        class SolverFast
        {
            private SolverConstraint[] m_constraints;
            private SolverBody[] m_bodies;
            private int m_constraintAtomCount;

            private float m_deltaTime;
            private float m_invDeltaTime;

            public void Setup<TBlocks, TConstraints>(float deltaTime, int blocksCount, TBlocks blocks, int constraintAtomCount, TConstraints constraints)
                where TBlocks : struct, IEnumerator<BlockState>
                where TConstraints : struct, IEnumerator<ConstraintBase>
            {
                ProfilerShort.Begin("SolverFast.Setup");

                m_deltaTime = deltaTime;
                m_invDeltaTime = 1f / deltaTime;

                SetSize(blocksCount, constraintAtomCount);

                while (blocks.MoveNext())
                    AddBlock(blocks.Current);

                while (constraints.MoveNext())
                    AddConstraint(constraints.Current);

                ProfilerShort.End();
            }

            private void SetSize(int bodyCount, int constraintAtomCount)
            {
                if (m_bodies == null || m_bodies.Length < bodyCount)
                    m_bodies = new SolverBody[bodyCount];
                else
                {
                    for (int i = 0; i < bodyCount; ++i)
                        m_bodies[i].Reset();
                }

                if (m_constraints == null || m_constraints.Length < constraintAtomCount)
                    m_constraints = new SolverConstraint[constraintAtomCount];
                else
                {
                    for (int i = 0; i < constraintAtomCount; ++i)
                        m_constraints[i].Reset();
                }

                m_constraintAtomCount = constraintAtomCount;
            }

            private void AddBlock(BlockState block)
            {
                int i = block.Index;
                m_bodies[i].InvMass = 1f / block.Mass;
                m_bodies[i].DeltaLinearAcceleration = Vector3.Zero;

                if (m_bodies[i].InvMass != 0f)
                {
                    block.ComputeTransformedInvInertia(out m_bodies[i].InvInertiaWorld);
                }
                else
                {
                    m_bodies[i].InvInertiaWorld = Matrix.Zero;
                }
                m_bodies[i].DeltaAngularAcceleration = Vector3.Zero;
            }

            private void AddConstraint(ConstraintBase constraint)
            {
                constraint.SetAtoms(m_constraints, m_invDeltaTime);
                int start = constraint.Index;
                int end = constraint.Index + constraint.AtomCount;
                var rbA = constraint.BlockA;
                var rbB = constraint.BlockB;
                for (int i = start; i < end; ++i)
                {
                    int idA = rbA.Index;
                    int idB = rbB.Index;
                    m_constraints[i].m_solverBodyIdA = idA;
                    m_constraints[i].m_solverBodyIdB = idB;

                    Vector3.TransformNormal(ref m_constraints[i].m_JaAngularAxis, ref m_bodies[idA].InvInertiaWorld, out m_constraints[i].m_angularComponentA);
                    Vector3.TransformNormal(ref m_constraints[i].m_JbAngularAxis, ref m_bodies[idB].InvInertiaWorld, out m_constraints[i].m_angularComponentB);

                    {
                        Vector3 iMJlA, iMJlB;
                        iMJlA = m_constraints[i].m_JaLinearAxis * m_bodies[idA].InvMass;
                        iMJlB = m_constraints[i].m_JbLinearAxis * m_bodies[idB].InvMass;
                        Vector3 iMJaA, iMJaB;
                        Vector3.TransformNormal(ref m_constraints[i].m_JaAngularAxis, ref m_bodies[idA].InvInertiaWorld, out iMJaA);
                        Vector3.TransformNormal(ref m_constraints[i].m_JbAngularAxis, ref m_bodies[idB].InvInertiaWorld, out iMJaB);
                        float sum = iMJlA.Dot(ref m_constraints[i].m_JaLinearAxis);
                        sum += iMJlB.Dot(ref m_constraints[i].m_JbLinearAxis);
                        sum += iMJaA.Dot(ref m_constraints[i].m_JaAngularAxis);
                        sum += iMJaB.Dot(ref m_constraints[i].m_JbAngularAxis);
                        sum = Math.Abs(sum);
                        m_constraints[i].m_jacDiagABInv = (sum > MyMathConstants.EPSILON) ? (1f / sum) : 0f;
                    }

                    {
                        float acc1Dotn = m_constraints[i].m_JaLinearAxis.Dot(m_invDeltaTime * rbA.LinearVelocity + rbA.LinearAcceleration);
                        float acc2Dotn = m_constraints[i].m_JbLinearAxis.Dot(m_invDeltaTime * rbB.LinearVelocity + rbB.LinearAcceleration);

                        acc1Dotn += m_constraints[i].m_JaAngularAxis.Dot(m_invDeltaTime * rbA.AngularVelocity + rbA.AngularAcceleration);
                        acc2Dotn += m_constraints[i].m_JbAngularAxis.Dot(m_invDeltaTime * rbB.AngularVelocity + rbB.AngularAcceleration);

                        float posError   = m_constraints[i].m_rhs; // filled by constraint
                        float accError   = acc1Dotn + acc2Dotn;
                        float posImpulse = posError * m_constraints[i].m_jacDiagABInv;
                        float accImpulse = accError * m_constraints[i].m_jacDiagABInv;
                        m_constraints[i].m_rhs = posImpulse - accImpulse;
                    }

                    const bool WARMSTARTING = true;
                    if (WARMSTARTING)
                    {
                        m_constraints[i].m_appliedImpulse *= 0.25f;
                        var linearComponentA = m_constraints[i].m_JaLinearAxis * m_bodies[idA].InvMass;
                        var linearComponentB = m_constraints[i].m_JbLinearAxis * m_bodies[idB].InvMass;
                        m_bodies[idA].ApplyImpulse(ref linearComponentA, ref m_constraints[i].m_angularComponentA, m_constraints[i].m_appliedImpulse);
                        m_bodies[idB].ApplyImpulse(ref linearComponentB, ref m_constraints[i].m_angularComponentB, m_constraints[i].m_appliedImpulse);
                    }
                    else
                    {
                        m_constraints[i].m_appliedImpulse = 0f;
                    }
                }
            }

            public void Solve(int maxIterations)
            {
                ProfilerShort.Begin("SolverFast.Solve");
                float impulseSum = 0f;
                float impulseSumLast = float.PositiveInfinity;
                for (int iteration = 0; iteration < maxIterations; ++iteration)
                {
                    impulseSum = 0f;
                    for (int i = 0; i < m_constraintAtomCount; ++i)
                    {
                        float impulseDelta = resolveSingleConstraint(
                            ref m_bodies[m_constraints[i].m_solverBodyIdA],
                            ref m_bodies[m_constraints[i].m_solverBodyIdB],
                            ref m_constraints[i]);
                        impulseSum += Math.Abs(impulseDelta);
                    }

                    if (impulseSumLast > impulseSum && impulseSumLast - impulseSum < 1f)
                    { // Barely any change in impulses.
                        break;
                    }
                    impulseSumLast = impulseSum;
                }

                ProfilerShort.End();
            }

            private float resolveSingleConstraint(ref SolverBody bodyA, ref SolverBody bodyB, ref SolverConstraint c)
            {
                float deltaImpulse = c.m_rhs;
                float deltaAcc1Dotn = c.m_JaLinearAxis.Dot(ref bodyA.DeltaLinearAcceleration);
                float deltaAcc2Dotn = c.m_JbLinearAxis.Dot(ref bodyB.DeltaLinearAcceleration);

                deltaAcc1Dotn += c.m_JaAngularAxis.Dot(ref bodyA.DeltaAngularAcceleration);
                deltaAcc2Dotn += c.m_JbAngularAxis.Dot(ref bodyB.DeltaAngularAcceleration);

                deltaImpulse -= deltaAcc1Dotn * c.m_jacDiagABInv;
                deltaImpulse -= deltaAcc2Dotn * c.m_jacDiagABInv;

                c.m_appliedImpulse += deltaImpulse;

                var linearImpulseA = bodyA.InvMass * c.m_JaLinearAxis;
                var linearImpulseB = bodyB.InvMass * c.m_JbLinearAxis;
                bodyA.ApplyImpulse(ref linearImpulseA, ref c.m_angularComponentA, deltaImpulse);
                bodyB.ApplyImpulse(ref linearImpulseB, ref c.m_angularComponentB, deltaImpulse);

                return deltaImpulse;
            }

            public void ApplySolution<TBlocks, TConstraints>(TBlocks blocks, TConstraints constraints)
                where TBlocks : struct, IEnumerator<BlockState>
                where TConstraints : struct, IEnumerator<ConstraintBase>
            {
                ProfilerShort.Begin("SolverFast.ApplySolution");
                while (blocks.MoveNext())
                {
                    var current = blocks.Current;
                    int idx = current.Index;
                    current.LinearAcceleration += m_bodies[idx].DeltaLinearAcceleration;
                    current.AngularAcceleration += m_bodies[idx].DeltaAngularAcceleration;
                }

                ConstraintBase.ResetMaxStrain();
                while (constraints.MoveNext())
                {
                    constraints.Current.ReadStrain(m_constraints);
                }
                ProfilerShort.End();
            }
        }

        class ConstraintComparer : IEqualityComparer<ConstraintKey>
        {
            public bool Equals(ConstraintKey x, ConstraintKey y)
            {
                // There can be only one constraint between any pair of blocks.
                return (x.Item1 == y.Item1 && x.Item2 == y.Item2) ||
                       (x.Item1 == y.Item2 && x.Item2 == y.Item1);
            }

            public int GetHashCode(ConstraintKey obj)
            {
                return obj.Item1.GetHashCode() ^ obj.Item2.GetHashCode();
            }
        }

        abstract class ConstraintBase
        {
            public static float MaxStrain
            {
                get;
                protected set;
            }

            public static void ResetMaxStrain()
            {
                MaxStrain = 0f;
            }

            protected BlockState m_blockA;
            protected BlockState m_blockB;
            protected MyTransform m_pivotA;
            protected MyTransform m_pivotB;

            public BlockState BlockA { get { return m_blockA; } }
            public BlockState BlockB { get { return m_blockB; } }
            public int Index;

            protected Color m_debugLineColor;

            public abstract int AtomCount { get; }

            public abstract void SetAtoms(SolverConstraint[] constraints, float invTimeStep);

            public abstract void ReadStrain(SolverConstraint[] constraints);

            public virtual void Bind(BlockState blockA, BlockState blockB)
            {
                m_blockA = blockA;
                m_blockB = blockB;
                Vector3D centerA, centerB;
                blockA.Block.ComputeScaledCenter(out centerA);
                blockB.Block.ComputeScaledCenter(out centerB);
                Vector3 pivot = (centerA + centerB) * 0.5f;
                m_pivotA = new MyTransform(pivot - centerA);
                m_pivotB = new MyTransform(pivot - centerB);
            }

            public virtual void DebugDraw()
            {
                MyTransform gridTransform = new MyTransform(m_blockA.Block.CubeGrid.WorldMatrix);
                var worldCenterA = MyTransform.Transform(ref m_blockA.Transform.Position, ref gridTransform);
                var worldCenterB = MyTransform.Transform(ref m_blockB.Transform.Position, ref gridTransform);
                VRageRender.MyRenderProxy.DebugDrawLine3D(worldCenterA, worldCenterB, m_debugLineColor, m_debugLineColor, false);
            }
        }

        struct SolverConstraint
        {
            public Vector3 m_JaAngularAxis; // m_relpos1CrossNormal, m_J1angularAxis
            public Vector3 m_JbAngularAxis; // m_relpos2CrossNormal, m_J2angularAxis
            public Vector3 m_JaLinearAxis;  // m_contactNormal1,     m_J1linearAxis
            public Vector3 m_JbLinearAxis;  // m_contactNormal2,     m_J2linearAxis  ; usually m_JbLinearAxis == -m_JaLinearAxis, but not always

            public float m_rhs; // m_rhs, m_constraintError; filled to have positional error

            public Vector3 m_angularComponentA; // RBaInvInertiaTensorWorld * m_JaAngularAxis
            public Vector3 m_angularComponentB; // RBbInvInertiaTensorWorld * m_JbAngularAxis

            public float m_jacDiagABInv; // 1/(J M^-1 J^t), single abs(value)

            public float m_appliedImpulse;

            public int m_solverBodyIdA;
            public int m_solverBodyIdB;

            public void Reset()
            {
                m_JaAngularAxis = Vector3.Zero;
                m_JaLinearAxis = Vector3.Zero;
                m_JbAngularAxis = Vector3.Zero;
                m_JbLinearAxis = Vector3.Zero;
                m_angularComponentA = Vector3.Zero;
                m_angularComponentB = Vector3.Zero;

                m_rhs = 0f;
                m_jacDiagABInv = 0f;
                m_appliedImpulse = 0f;

                m_solverBodyIdA = 0;
                m_solverBodyIdB = 0;
            }

            public override string ToString()
            {
                return string.Format("J = [ {0} | {1} | {2} | {3} ], Impulse = {4}, RHS = {5}",
                    m_JaLinearAxis, m_JaAngularAxis, m_JbLinearAxis, m_JbAngularAxis,
                    m_appliedImpulse, m_rhs);
            }
        }

        class ConstraintFixed1DoF : ConstraintBase
        {
            private float m_strain;
            private float m_appliedImpuls;

            public override int AtomCount
            {
                get { return 1; }
            }

            public override void SetAtoms(SolverConstraint[] constraints, float invTimeStep)
            {
                var rA = Vector3.Transform(m_pivotA.Position, m_blockA.Transform.Rotation);
                var rB = Vector3.Transform(m_pivotB.Position, m_blockB.Transform.Rotation);

                // setting linear jacobian entries
                constraints[Index].m_JaLinearAxis.Y = 1f;
                constraints[Index].m_JbLinearAxis.Y = -1f;

                // set linear and angular error
                var linearError = 0.85f * invTimeStep * invTimeStep * (rB.Y + m_blockB.Transform.Position.Y - rA.Y - m_blockA.Transform.Position.Y);
                constraints[Index].m_rhs = linearError;

                constraints[Index].m_appliedImpulse = m_appliedImpuls;
            }

            public override void ReadStrain(SolverConstraint[] constraints)
            {
                m_appliedImpuls = constraints[Index].m_appliedImpulse;
                m_strain = Math.Abs(m_appliedImpuls);
                if (MaxStrain < m_strain)
                    MaxStrain = m_strain;
            }

            public override void DebugDraw()
            {
                var t = MathHelper.Clamp(m_strain / MaxStrain, 0f, 1f);
                if (t < 0.5f)
                    m_debugLineColor = Color.Lerp(Color.Green, Color.Yellow, t * 2f);
                else
                    m_debugLineColor = Color.Lerp(Color.Yellow, Color.Red, (t - 0.5f) * 2f);

                base.DebugDraw();
            }
        }

        class ConstraintFixed3DoF : ConstraintBase
        {
            private float m_strain;
            private float[] m_appliedImpulses;

            public override int AtomCount
            {
                get { return 3; }
            }

            public ConstraintFixed3DoF()
            {
                m_appliedImpulses = new float[AtomCount];
            }

            public override void SetAtoms(SolverConstraint[] constraints, float invTimeStep)
            {
                var rA = Vector3.Transform(m_pivotA.Position, m_blockA.Transform.Rotation);
                var rB = Vector3.Transform(m_pivotB.Position, m_blockB.Transform.Rotation);

                // setting linear jacobian entries
                constraints[Index + 0].m_JaLinearAxis.X = 1f;
                constraints[Index + 1].m_JaLinearAxis.Y = 1f;
                constraints[Index + 2].m_JaLinearAxis.Z = 1f;

                constraints[Index + 0].m_JbLinearAxis.X = -1f;
                constraints[Index + 1].m_JbLinearAxis.Y = -1f;
                constraints[Index + 2].m_JbLinearAxis.Z = -1f;

                // set linear and angular error
                var linearError = 0.85f * invTimeStep * invTimeStep * (rB + m_blockB.Transform.Position - rA - m_blockA.Transform.Position);
                constraints[Index + 0].m_rhs = linearError.X;
                constraints[Index + 1].m_rhs = linearError.Y;
                constraints[Index + 2].m_rhs = linearError.Z;

                for (int i = 0; i < AtomCount; ++i)
                {
                    constraints[Index + i].m_appliedImpulse = m_appliedImpulses[i];
                }
            }

            public override void ReadStrain(SolverConstraint[] constraints)
            {
                m_strain = 0f;
                for (int i = 0; i < AtomCount; ++i)
                {
                    m_appliedImpulses[i] = constraints[Index + i].m_appliedImpulse;
                    m_strain += Math.Abs(m_appliedImpulses[i]);
                }
                if (MaxStrain < m_strain)
                    MaxStrain = m_strain;
            }

            public override void DebugDraw()
            {
                var t = MathHelper.Clamp(m_strain / MaxStrain, 0f, 1f);
                if (t < 0.5f)
                    m_debugLineColor = Color.Lerp(Color.Green, Color.Yellow, t * 2f);
                else
                    m_debugLineColor = Color.Lerp(Color.Yellow, Color.Red, (t - 0.5f) * 2f);

                base.DebugDraw();
            }
        }

        class ConstraintFixed6DoF : ConstraintBase
        {
            private float m_strain;
            private float[] m_appliedImpulses;

            public override int AtomCount
            {
                get { return 6; }
            }

            public ConstraintFixed6DoF()
            {
                m_appliedImpulses = new float[AtomCount];
            }

            public override void SetAtoms(SolverConstraint[] constraints, float invTimeStep)
            {
                var rA = Vector3.Transform(m_pivotA.Position, m_blockA.Transform.Rotation);
                var rB = Vector3.Transform(m_pivotB.Position, m_blockB.Transform.Rotation);

                // setting linear jacobian entries
                constraints[Index + 0].m_JaLinearAxis.X = 1f;
                constraints[Index + 1].m_JaLinearAxis.Y = 1f;
                constraints[Index + 2].m_JaLinearAxis.Z = 1f;

                constraints[Index + 0].m_JbLinearAxis.X = -1f;
                constraints[Index + 1].m_JbLinearAxis.Y = -1f;
                constraints[Index + 2].m_JbLinearAxis.Z = -1f;

                constraints[Index + 0].m_JaAngularAxis = new Vector3(0f, rA.Z, -rA.Y);
                constraints[Index + 1].m_JaAngularAxis = new Vector3(-rA.Z, 0f, rA.X);
                constraints[Index + 2].m_JaAngularAxis = new Vector3(rA.Y, -rA.X, 0f);

                constraints[Index + 0].m_JbAngularAxis = new Vector3(0f, -rB.Z, rB.Y);
                constraints[Index + 1].m_JbAngularAxis = new Vector3(rB.Z, 0f, -rB.X);
                constraints[Index + 2].m_JbAngularAxis = new Vector3(-rB.Y, rB.X, 0f);

                // setting angular jacobian entries
                constraints[Index + 3].m_JaAngularAxis.X = 1f;
                constraints[Index + 4].m_JaAngularAxis.Y = 1f;
                constraints[Index + 5].m_JaAngularAxis.Z = 1f;

                constraints[Index + 3].m_JbAngularAxis.X = -1f;
                constraints[Index + 4].m_JbAngularAxis.Y = -1f;
                constraints[Index + 5].m_JbAngularAxis.Z = -1f;

                // set linear and angular error
                var linearError = 0.85f * invTimeStep * invTimeStep * (rB + m_blockB.Transform.Position - rA - m_blockA.Transform.Position);
                constraints[Index + 0].m_rhs = linearError.X;
                constraints[Index + 1].m_rhs = linearError.Y;
                constraints[Index + 2].m_rhs = linearError.Z;

                var relativeRotation =
                    Matrix.CreateFromQuaternion(
                        Quaternion.Multiply(m_blockA.Transform.Rotation,
                                            Quaternion.Inverse(m_blockB.Transform.Rotation)));
                Vector3 angularError;
                Matrix.GetEulerAnglesXYZ(ref relativeRotation, out angularError);
                angularError *= 0.85f * invTimeStep * invTimeStep;
                constraints[Index + 3].m_rhs = angularError.X;
                constraints[Index + 4].m_rhs = angularError.Y;
                constraints[Index + 5].m_rhs = angularError.Z;

                for (int i = 0; i < AtomCount; ++i)
                {
                    constraints[Index + i].m_appliedImpulse = m_appliedImpulses[i];
                }
            }

            public override void ReadStrain(SolverConstraint[] constraints)
            {
                m_strain = 0f;
                for (int i = 0; i < AtomCount; ++i)
                {
                    m_appliedImpulses[i] = constraints[Index + i].m_appliedImpulse;
                    m_strain += Math.Abs(m_appliedImpulses[i]);
                }
                if (MaxStrain < m_strain)
                    MaxStrain = m_strain;
            }

            public override void DebugDraw()
            {
                var t = MathHelper.Clamp(m_strain / MaxStrain, 0f, 1f);
                if (t < 0.5f)
                    m_debugLineColor = Color.Lerp(Color.Green, Color.Yellow, t * 2f);
                else
                    m_debugLineColor = Color.Lerp(Color.Yellow, Color.Red, (t - 0.5f) * 2f);

                base.DebugDraw();
            }
        }

        class BlockState
        {
            public MySlimBlock Block;
            public bool IsFixed;
            public int Index;

            public Vector3 LocalInertiaInv;

            public Vector3 LinearAcceleration;
            public Vector3 LinearVelocity;

            public Vector3 AngularAcceleration;
            public Vector3 AngularVelocity;

            public MyTransform Transform;

            public BlockState(MySlimBlock block)
            {
                Block = block;

                IsFixed = MyCubeGrid.IsInVoxels(Block);

                Vector3D basePosition;
                Vector3 extents;
                block.ComputeScaledCenter(out basePosition);
                block.ComputeScaledHalfExtents(out extents);
                extents *= 2;
                Transform = new MyTransform(basePosition);
                var massInTensor = (1f / 12f) * block.BlockDefinition.Mass;
                LocalInertiaInv = 1f / new Vector3(
                    massInTensor * (extents.Y * extents.Y + extents.Z * extents.Z),
                    massInTensor * (extents.X * extents.X + extents.Z * extents.Z),
                    massInTensor * (extents.X * extents.X + extents.Y * extents.Y));
            }

            public void ComputeTransformedInvInertia(out Matrix outInvInertia)
            {
                Matrix invIMatrix, rotation;
                Matrix.CreateScale(ref LocalInertiaInv, out invIMatrix);
                Matrix.CreateFromQuaternion(ref Transform.Rotation, out rotation);
                outInvInertia = Matrix.Multiply(Matrix.Multiply(rotation, invIMatrix), Matrix.Transpose(rotation));
            }

            public float Mass
            {
                get { return IsFixed ? float.PositiveInfinity : Block.BlockDefinition.Mass; }
            }
        }

        public float GetSupportedWeight(Vector3I pos)
        {
            return 0;
        }

        public float GetTension(Vector3I pos)
        {
            return 0;
        }

        public void Close()
        {
        }

        public void ForceRecalc()
        {
        }
    }
}

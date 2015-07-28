using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.GameSystems.StructuralIntegrity
{
    interface IMyIntegritySimulator
    {
        void Add(MySlimBlock block);
        void Remove(MySlimBlock block);

        bool Simulate(float deltaTime);

        void Draw();
        void DebugDraw();

        void Close();

        bool IsConnectionFine(MySlimBlock blockA, MySlimBlock blockB);

        float GetSupportedWeight(Vector3I pos);

        float GetTension(Vector3I pos);

        void ForceRecalc();
    }

}

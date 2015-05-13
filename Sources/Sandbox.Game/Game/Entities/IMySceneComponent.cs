using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities
{
    // Data only component for scene, part of MyEntities
    // Maybe merged somehow with scene or something
    public interface IMySceneComponent
    {
        void Load();
        void Unload();
    }
}

using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.AI
{
    public abstract class MyAutopilotBase
    {
        protected MyShipController ShipController { private set; get; }

        // Override this to "false" for persistent AI pilots
        public virtual bool RemoveOnPlayerControl
        {
            get
            {
                return true;
            }
        }

        public MyAutopilotBase()
        {
            ShipController = null;
        }

        public abstract MyObjectBuilder_AutopilotBase GetObjectBuilder();
        public abstract void Init(MyObjectBuilder_AutopilotBase objectBuilder);

        public void AttachedToShipController(MyShipController newShipController)
        {
            ShipController = newShipController;
            OnShipControllerChanged();
        }

        public void RemovedFromCockpit()
        {
            ShipController = null;
            OnShipControllerChanged();
        }

        protected abstract void OnShipControllerChanged();
        public abstract void Update();
        public virtual void DebugDraw() { }
    }
}

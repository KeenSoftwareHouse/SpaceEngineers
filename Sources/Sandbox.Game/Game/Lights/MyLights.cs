using System.Collections.Generic;
using Sandbox.Engine.Utils;
using VRage.Generics;


using VRageMath;
using Sandbox;
using Sandbox.Game.World;
using Sandbox.Common;
using VRage.Game;
using VRage.Utils;
using VRage.Game.Components;


//  This class is responsible for holding list of dynamic lights, adding, removing and finally drawing on voxels or other models.

namespace Sandbox.Game.Lights
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, Priority = 600)]
    public class MyLights : MySessionComponentBase
    {
        static MyObjectsPool<MyLight> m_preallocatedLights = null;

        static int m_lightsCount;
        static BoundingSphere m_lastBoundingSphere;

        static MyLights()
        {
        }

        public override void LoadData()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyLights.LoadData");
            MySandboxGame.Log.WriteLine("MyLights.LoadData() - START");
            MySandboxGame.Log.IncreaseIndent();

            if (m_preallocatedLights == null)
            {
                m_preallocatedLights = new MyObjectsPool<MyLight>(MyLightsConstants.MAX_LIGHTS_COUNT);
            }

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyLights.LoadData() - END");
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        protected override void UnloadData()
        {
            if (m_preallocatedLights != null)
            {
                // all lights should be deallocated at this point
                MyDebug.AssertDebug(m_preallocatedLights.ActiveCount == 0, "MyLights.UnloadData: preallocated lights not emptied!");
                m_preallocatedLights.DeallocateAll();
                m_preallocatedLights = null;
            }
        }

        //  Add new light to the list, but caller needs to start it using Start() method
        public static MyLight AddLight()
        {
            MyLight result;
            m_preallocatedLights.AllocateOrCreate(out result);
            //result.ProxyId = MyDynamicAABBTree.NullNode;
            return result;
        }

        public static void RemoveLight(MyLight light)
        {
            if (light != null)
            {
                light.Clear();

                //by Gregory: added null check happened once when unloading session
                if (m_preallocatedLights != null)
                {
                    m_preallocatedLights.Deallocate(light);
                }
            }
        }        
    }
}

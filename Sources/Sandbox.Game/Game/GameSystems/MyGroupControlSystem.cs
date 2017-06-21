using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.Electricity;

using VRage.Utils;
using VRage.Trace;
using VRageMath;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using VRageRender;

namespace Sandbox.Game.GameSystems
{
    public class MyGroupControlSystem
    {
        private MyShipController m_currentShipController = null;

        private readonly HashSet<MyShipController> m_groupControllers = new HashSet<MyShipController>();
        private readonly HashSet<MyCubeGrid> m_cubeGrids = new HashSet<MyCubeGrid>();

        private bool m_controlDirty;
        private bool m_firstControlRecalculation;

        public MyGroupControlSystem()
        {
            m_currentShipController = null;
            m_controlDirty = false;
            m_firstControlRecalculation = true;
        }

        public void UpdateBeforeSimulation()
        {
            if (m_controlDirty)
            {
                UpdateControl();

                m_controlDirty = false;
                m_firstControlRecalculation = false;
            }

            UpdateControls();
        }

        private void UpdateControl()
        {
            MyShipController preferredController = null;

            foreach (var controller in m_groupControllers)
            {
                if (preferredController == null)
                {
                    preferredController = controller;
                }
                else
                {
                    if (MyShipController.HasPriorityOver(controller, preferredController))
                    {
                        preferredController = controller;
                    }
                }
            }

            m_currentShipController = preferredController;

            // The server synchronizes control to all clients, but current ship controller is determined by each client separately
            if (Sync.IsServer && m_currentShipController != null)
            {
                var newController = m_currentShipController.ControllerInfo.Controller;
                foreach (var grid in m_cubeGrids)
                {
                    Debug.Assert(m_firstControlRecalculation || Sync.Players.GetControllingPlayer(grid) == null);
                    Debug.Assert(m_currentShipController.ControllerInfo.Controller != null || m_currentShipController is MyRemoteControl, "Trying to extend control from uncontrolled cockpit!");

                    Sync.Players.TryExtendControl(m_currentShipController, grid);
                }
            }
        }

        public void RemoveControllerBlock(MyShipController controllerBlock)
        {
            bool result = m_groupControllers.Remove(controllerBlock);
            Debug.Assert(result, "Controller block was not present in the control group's controller list!");

            if (controllerBlock == m_currentShipController)
                m_controlDirty = true;

            if (Sync.IsServer)
            {
                if (controllerBlock == m_currentShipController)
                {
                    Sync.Players.ReduceAllControl(m_currentShipController);
                    m_currentShipController = null;
                }
            }
        }

        public void AddControllerBlock(MyShipController controllerBlock)
        {
            bool result = m_groupControllers.Add(controllerBlock);
            bool found = false;
            if (m_currentShipController != null && m_currentShipController.CubeGrid != controllerBlock.CubeGrid)
            {
               
                var group = MyCubeGridGroups.Static.Logical.GetGroup(controllerBlock.CubeGrid);

                if (group != null)
                {
                   foreach(var node in group.Nodes)
                   {
                       if(node.NodeData == m_currentShipController.CubeGrid )
                       {
                           found = true;
                           break;
                       }
                   }
                }
            }

            if (found == false && m_currentShipController != null && m_currentShipController.CubeGrid != controllerBlock.CubeGrid)
            {
                RemoveControllerBlock(m_currentShipController);
                m_currentShipController = null;
            }

            bool newControllerHasPriority = m_currentShipController == null || MyShipController.HasPriorityOver(controllerBlock, m_currentShipController);

            if (newControllerHasPriority)
                m_controlDirty = true;

            if (Sync.IsServer)
            {
                if (m_currentShipController != null && newControllerHasPriority)
                {
                    Sync.Players.ReduceAllControl(m_currentShipController);
                }
            }
        }

        public void RemoveGrid(MyCubeGrid CubeGrid)
        {
            if (Sync.IsServer)
            {
                if (m_currentShipController != null)
                {
                    Sync.Players.ReduceControl(m_currentShipController, CubeGrid);
                }
            }

            m_cubeGrids.Remove(CubeGrid);
        }

        public void AddGrid(MyCubeGrid CubeGrid)
        {
            m_cubeGrids.Add(CubeGrid);

            if (Sync.IsServer)
            {
                Debug.Assert(MySession.Static.ElapsedPlayTime == TimeSpan.Zero || Sync.Players.GetControllingPlayer(CubeGrid) == null, "Grid added to the physical group should have erased control!");
                if (!m_controlDirty && m_currentShipController != null)
                {
                    Sync.Players.ExtendControl(m_currentShipController, CubeGrid);
                }
            }
        }

        public bool IsLocallyControlled
        {
            get
            {
                var controller = GetController();
                if (controller == null)
                {
                    return false;
                }

                return controller.Player.IsLocalPlayer;
            }
        }

        public MyEntityController GetController()
        {
            return m_currentShipController == null ? null : m_currentShipController.ControllerInfo.Controller;
        }

        public MyShipController GetShipController()
        {
            return m_currentShipController;
        }

        public bool IsControlled
        {
            get
            {
                var controller = GetController();
                return (controller != null);
            }
        }

        public void DebugDraw(float startYCoord)
        {
            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, startYCoord), "Controlled group controllers:", Color.GreenYellow, 0.5f); startYCoord += 13.0f;
            foreach (var controller in m_groupControllers)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, startYCoord), "  " + controller.ToString(), Color.LightYellow, 0.5f); startYCoord += 13.0f;
            }

            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, startYCoord), "Controlled group grids:", Color.GreenYellow, 0.5f); startYCoord += 13.0f;
            foreach (var grid in m_cubeGrids)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, startYCoord), "  " + grid.ToString(), Color.LightYellow, 0.5f); startYCoord += 13.0f;
            }
        }

        public void UpdateControls()
        {
            foreach (var controller in m_groupControllers)
            {
                controller.UpdateControls();
            }
        }
    }
}
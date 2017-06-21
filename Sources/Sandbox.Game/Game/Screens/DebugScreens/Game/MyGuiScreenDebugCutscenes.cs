using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRage;
using Sandbox.Engine.Utils;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using VRage.Game;
using Sandbox.Game.World;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.GUI.DebugInputComponents;
using VRage.Game.Entity;

namespace Sandbox.Game.Screens.DebugScreens
{
#if !XB1

    [MyDebugScreen("Game", "Cutscenes")]
    class MyGuiScreenDebugCutscenes : MyGuiScreenDebugBase
    {
        MyGuiControlCombobox m_comboCutscenes;
        MyGuiControlCombobox m_comboNodes;
        MyGuiControlCombobox m_comboWaypoints;


        MyGuiControlButton m_playButton;
        MyGuiControlSlider m_nodeTimeSlider;

        MyGuiControlButton m_spawnButton;
        MyGuiControlButton m_removeAllButton;

        MyGuiControlButton m_addNodeButton;
        MyGuiControlButton m_deleteNodeButton;

        MyGuiControlButton m_addCutsceneButton;
        MyGuiControlButton m_deleteCutsceneButton;

        Cutscene m_selectedCutscene;
        CutsceneSequenceNode m_selectedCutsceneNode;

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugCubeBlocks";
        }
        public MyGuiScreenDebugCutscenes()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Cutscenes", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);


            m_comboCutscenes = AddCombo();
            m_playButton = AddButton(new StringBuilder("Play"), onClick_PlayButton);
            m_addCutsceneButton = AddButton(new StringBuilder("Add cutscene"), onClick_AddCutsceneButton);
            m_deleteCutsceneButton = AddButton(new StringBuilder("Delete cutscene"), onClick_DeleteCutsceneButton);


            m_currentPosition.Y += 0.01f;

            AddLabel("Nodes", Color.Yellow.ToVector4(), 1);
            m_comboNodes = AddCombo();
            m_comboNodes.ItemSelected += m_comboNodes_ItemSelected;


            m_addNodeButton = AddButton(new StringBuilder("Add node"), onClick_AddNodeButton);
            m_deleteNodeButton = AddButton(new StringBuilder("Delete node"), onClick_DeleteNodeButton);

            m_nodeTimeSlider = AddSlider("Node time", 0, 0, 100, OnNodeTimeChanged);

            var cutscenes = MySession.Static.GetComponent<MySessionComponentCutscenes>();

            m_comboCutscenes.ClearItems();
            foreach (var key in cutscenes.GetCutscenes().Keys) 
            {
                m_comboCutscenes.AddItem(key.GetHashCode(), key);
            }

            m_comboCutscenes.SortItemsByValueText();
            m_comboCutscenes.ItemSelected += m_comboCutscenes_ItemSelected;

            AddLabel("Waypoints", Color.Yellow.ToVector4(), 1);
            m_comboWaypoints = AddCombo();
            m_comboWaypoints.ItemSelected += m_comboWaypoints_ItemSelected;

            



            m_currentPosition.Y += 0.01f;

            m_spawnButton = AddButton(new StringBuilder("Spawn entity"), onSpawnButton);
            m_removeAllButton = AddButton(new StringBuilder("Remove all"), onRemoveAllButton);


            if (m_comboCutscenes.GetItemsCount() > 0)
                m_comboCutscenes.SelectItemByIndex(0);

         
         
      
            
      
        }

        void m_comboCutscenes_ItemSelected()
        {
            m_selectedCutscene = MySession.Static.GetComponent<MySessionComponentCutscenes>().GetCutscene(m_comboCutscenes.GetSelectedValue().ToString());

            m_comboNodes.ClearItems();

            if (m_selectedCutscene.SequenceNodes != null)
            {
                int i = 0;
                foreach (var node in m_selectedCutscene.SequenceNodes)
                {
                    m_comboNodes.AddItem(i, node.Time.ToString());
                    i++;
                }
            }


            if (m_comboNodes.GetItemsCount() > 0)
                m_comboNodes.SelectItemByIndex(0);

         
        }

        void m_comboNodes_ItemSelected()
        {
            m_selectedCutsceneNode = m_selectedCutscene.SequenceNodes[m_comboNodes.GetSelectedKey()];
            m_nodeTimeSlider.Value = m_selectedCutsceneNode.Time;

            m_comboWaypoints.ClearItems();

            if (m_selectedCutsceneNode.Waypoints != null)
            {
                foreach (var waypoint in m_selectedCutsceneNode.Waypoints)
                {
                    m_comboWaypoints.AddItem(waypoint.Name.GetHashCode(), waypoint.Name);
                }

                if (m_comboWaypoints.GetItemsCount() > 0)
                    m_comboWaypoints.SelectItemByIndex(0);
            }
        }



        void onClick_PlayButton(MyGuiControlButton sender)
        {
            if (m_comboCutscenes.GetItemsCount() > 0)
            {
                var cutscenes = MySession.Static.GetComponent<MySessionComponentCutscenes>();

                cutscenes.PlayCutscene(m_comboCutscenes.GetSelectedValue().ToString());
            }
        }

        void OnNodeTimeChanged(MyGuiControlSlider slider)
        {
            if (m_selectedCutsceneNode != null)
            {
                m_selectedCutsceneNode.Time = slider.Value;
            }
        }

        void onSpawnButton(MyGuiControlButton sender)
        {
            MyVisualScriptingDebugInputComponent.SpawnEntity(onEntitySpawned);
        }

        void onEntitySpawned(MyEntity entity)
        {
            if (m_selectedCutsceneNode != null)
            {
                m_selectedCutsceneNode.MoveTo = entity.Name;
                m_selectedCutsceneNode.RotateTowards = entity.Name;
            }
        }
        
        
        void onClick_AddNodeButton(MyGuiControlButton sender)
        {
            CutsceneSequenceNode[] newNodes = new CutsceneSequenceNode[] { new CutsceneSequenceNode() };
            if (m_selectedCutscene.SequenceNodes != null)
            {
                m_selectedCutscene.SequenceNodes = m_selectedCutscene.SequenceNodes.Union(newNodes).ToArray();
            }
            else
                m_selectedCutscene.SequenceNodes = newNodes;
        }

        void onClick_DeleteNodeButton(MyGuiControlButton sender)
        {
            if (m_selectedCutscene.SequenceNodes != null)
            {
                m_selectedCutscene.SequenceNodes = m_selectedCutscene.SequenceNodes.Where(x => x != m_selectedCutsceneNode).ToArray();
            }
        }

        void onRemoveAllButton(MyGuiControlButton sender)
        {
            var cutscenes = MySession.Static.GetComponent<MySessionComponentCutscenes>();
            cutscenes.GetCutscenes().Clear();
        }

        void m_comboWaypoints_ItemSelected()
        {
        }

        void onClick_AddCutsceneButton(MyGuiControlButton sender)
        {
            var cutscenes = MySession.Static.GetComponent<MySessionComponentCutscenes>();
            string name = "Cutscene" + cutscenes.GetCutscenes().Count;
            cutscenes.GetCutscenes().Add(name, new Cutscene());

            m_comboCutscenes.ClearItems();
            foreach (var key in cutscenes.GetCutscenes().Keys)
            {
                m_comboCutscenes.AddItem(key.GetHashCode(), key);
            }

            m_comboCutscenes.SelectItemByKey(name.GetHashCode());
        }

        void onClick_DeleteCutsceneButton(MyGuiControlButton sender)
        {
            var cutscenes = MySession.Static.GetComponent<MySessionComponentCutscenes>();

            if (m_selectedCutscene != null)
            {
                cutscenes.GetCutscenes().Remove(m_selectedCutscene.Name);
                m_comboCutscenes.RemoveItem(m_selectedCutscene.Name.GetHashCode());

                if (cutscenes.GetCutscenes().Count == 0)
                    m_selectedCutscene = null;
                else
                    m_comboCutscenes.SelectItemByIndex(cutscenes.GetCutscenes().Count - 1);
            }            
        }


    }

#endif
}

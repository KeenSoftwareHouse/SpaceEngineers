﻿#region Using

using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;


#endregion

namespace Sandbox.Game.Gui
{
    #region Block Info
    public class MyHudBlockInfo
    {
        public struct ComponentInfo
        {
            public MyDefinitionId DefinitionId;
            public string Icon;
            public String ComponentName;
            public int MountedCount;
            public int StockpileCount;
            public int TotalCount;
			public int AvailableAmount;

            public int InstalledCount
            {
                get 
                {
                    Debug.Assert(MountedCount + StockpileCount <= TotalCount, "Wrong count of components");
                    return MountedCount + StockpileCount; 
                }
            }

            public override string ToString()
            {
                return String.Format("{0}/{1}/{2} {3}", MountedCount, StockpileCount, TotalCount, ComponentName);
            }
        }

        public struct BlockInfo
        {
            //public List<ComponentInfo> Components = new List<ComponentInfo>(12);

            public String BlockName;
            //public string BlockIcon;
            //public float BlockIntegrity;
            //public float CriticalIntegrity;
            //public float OwnershipIntegrity;

            //public int CriticalComponentIndex = -1;
            //public int MissingComponentIndex = -1;
        }

        public bool ShowDetails = false; //show components, icons... for compound block subblocks
        public List<BlockInfo> BlockInfos = new List<BlockInfo>();
        /// <summary>
        /// First component in block component stack is also first in this list
        /// </summary>
        public List<ComponentInfo> Components = new List<ComponentInfo>(12);

        public String BlockName;
        public string BlockIcon;
        public float BlockIntegrity;
        public float CriticalIntegrity;
        public float OwnershipIntegrity;

        public int CriticalComponentIndex = -1;
        public int MissingComponentIndex = -1;

        public bool Visible;
    }
    #endregion
}

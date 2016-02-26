using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Game.Entities.EnvironmentItems
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, 500)]
    public class MyEnvironmentItemsCoordinator : MySessionComponentBase
    {
        private static MyEnvironmentItemsCoordinator Static;

        private HashSet<MyEnvironmentItems> m_tmpItems;
        private List<TransferData> m_transferList;
        private float? m_transferTime;

        private struct TransferData
        {
            public MyEnvironmentItems From;
            public MyEnvironmentItems To;
            public int LocalId;
            public MyStringHash SubtypeId;
        }

        public override bool IsRequiredByGame
        {
            get
            {
                return MyPerGameSettings.Game == GameEnum.ME_GAME;
            }
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            // MW: finish batch before save happens. The flora component will get updated correctly due to session component priority
            //     entities are being saved after the checkpoint, so it should work properly
            if (m_transferTime.HasValue)
                FinalizeTransfers();

            return base.GetObjectBuilder();
        }

        public override void LoadData()
        {
            base.LoadData();

            m_transferList = new List<TransferData>();
            m_tmpItems = new HashSet<MyEnvironmentItems>();
            Static = this;
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            Static = null;
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            
            if (m_transferTime.HasValue)
            {
                m_transferTime = m_transferTime.Value - VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if (m_transferTime < 0)
                {
                    FinalizeTransfers();
                }
            }
        }

        private void FinalizeTransfers()
        {
            foreach (var transferData in m_transferList)
            {
                if (MakeTransfer(transferData))
                    m_tmpItems.Add(transferData.To);
            }

            m_transferList.Clear();
            m_transferTime = null;

            foreach (var envItems in m_tmpItems)
            {
                envItems.EndBatch(true);
            }

            m_tmpItems.Clear();
        }

        private bool MakeTransfer(TransferData data)
        {
            MyEnvironmentItems.ItemInfo itemInfo;
            if (!data.From.TryGetItemInfoById(data.LocalId, out itemInfo))
            {
                // the item was removed or never existed
                return false;
            }

            data.From.RemoveItem(data.LocalId, true);
            if (!data.To.IsBatching)
                data.To.BeginBatch(true);
            data.To.BatchAddItem(itemInfo.Transform.Position, data.SubtypeId, true);
            return true;
        }

        private void StartTimer(int updateTimeS)
        {
            if (!m_transferTime.HasValue)
                m_transferTime = updateTimeS;
        }

        public static void TransferItems(MyEnvironmentItems from, MyEnvironmentItems to, int localId, MyStringHash subtypeId, int timeS = 10)
        {
            Static.AddTransferData(from, to, localId, subtypeId);
            Static.StartTimer(timeS);
        }

        private void AddTransferData(MyEnvironmentItems from, MyEnvironmentItems to, int localId, MyStringHash subtypeId)
        {
            m_transferList.Add(new TransferData()
                {
                    From = from,
                    To = to,
                    LocalId = localId,
                    SubtypeId = subtypeId,
                });
        }
    }
}

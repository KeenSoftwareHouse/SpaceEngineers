using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Definitions.GUI;
using VRage.Utils;

namespace Sandbox.Game.GUI
{
    public class MyGuiSkinManager
    {
        #region Static
        private static MyGuiSkinManager m_instance;

        public static MyGuiSkinManager Static
        {
            get
            {
                if (m_instance == null)
                    m_instance = new MyGuiSkinManager();
                return m_instance;
            }
        }
        #endregion

        #region Private
        private MyGuiSkinDefinition m_currentSkin;
        private Dictionary<int, MyGuiSkinDefinition> m_availableSkins;
        #endregion

        #region Properties
        public MyGuiSkinDefinition CurrentSkin
        {
            get { return m_currentSkin; }
        }

        public Dictionary<int, MyGuiSkinDefinition> AvailableSkins
        {
            get { return m_availableSkins; }
        }

        public int CurrentSkinId
        {
            get
            {
                if (CurrentSkin == null)
                    return 0;
                else
                    return MyStringId.GetOrCompute(CurrentSkin.Id.SubtypeName).Id;
            }
        }

        public int SkinCount
        {
            get { return m_availableSkins.Count; }
        }
        #endregion

        public void Init()
        {
            m_availableSkins = new Dictionary<int, MyGuiSkinDefinition>();
            var skins = MyDefinitionManager.Static.GetDefinitions<MyGuiSkinDefinition>();
            if (skins == null)
                return;

            foreach (var skin in skins)
                m_availableSkins[MyStringId.GetOrCompute(skin.Id.SubtypeName).Id] = skin;

            m_availableSkins.TryGetValue(MyStringId.GetOrCompute(MySandboxGame.Config.Skin).Id, out m_currentSkin);
        }

        public void SelectSkin(int skinId)
        {
            if (m_availableSkins.TryGetValue(skinId, out m_currentSkin))
                MySandboxGame.Config.Skin = m_currentSkin.Id.SubtypeName;
        }
    }
}

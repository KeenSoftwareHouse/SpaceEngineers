using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncTextPanel : MySyncCubeBlock
    {
        [ProtoContract]
        [MessageId(430, P2PMessageEnum.Reliable)]
        struct ChangeDescriptionMsg : IEntityMessage
        {
            [ProtoMember]
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }

            [ProtoMember]
            public string Description;
            [ProtoMember]
            public bool IsPublic;
        }

        [ProtoContract]
        [MessageId(431, P2PMessageEnum.Reliable)]
        struct ChangeTitleMsg : IEntityMessage
        {
            [ProtoMember]
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }

            [ProtoMember]
            public string Title;
            [ProtoMember]
            public bool IsPublic;

        }

        [MessageId(432, P2PMessageEnum.Reliable)]
        struct ChangeAccessFlagMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }

            public byte AccessFlag;
        }

        [MessageId(433, P2PMessageEnum.Reliable)]
        struct ChangeOpenMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }

            public BoolBlit IsOpen;
            public BoolBlit Editable;
            public ulong User;
            public BoolBlit IsPublic;
        }

        [MessageId(434, P2PMessageEnum.Reliable)]
        struct ChangeIntervalMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }
            public float Interval;
        }

        [MessageId(435, P2PMessageEnum.Reliable)]
        struct ChangeFontSizeMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }
            public float FontSize;
        }

        [ProtoContract]
        [MessageId(436, P2PMessageEnum.Reliable)]
        struct SelectImagesMsg : IEntityMessage
        {
           [ProtoMember]
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }
            [ProtoMember]
            public int[] Selection;
        }

        [ProtoContract]
        [MessageId(437, P2PMessageEnum.Reliable)]
        struct RemoveSelectedImagesMsg : IEntityMessage
        {
            [ProtoMember]
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }
            [ProtoMember]
            public int[] Selection;
        }

        [ProtoContract]
        [MessageId(438, P2PMessageEnum.Reliable)]
        struct ChangeFontColorMsg : IEntityMessage
        {
            [ProtoMember]
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }
            [ProtoMember]
            public Color FontColor;
        }

        [ProtoContract]
        [MessageId(439, P2PMessageEnum.Reliable)]
        struct ChangeBackgroundColorMsg : IEntityMessage
        {
            [ProtoMember]
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }
            [ProtoMember]
            public Color BackgroundColor;
        }

        [ProtoContract]
        [MessageId(440, P2PMessageEnum.Reliable)]
        struct ChangeShowOnScreenMsg : IEntityMessage
        {
            [ProtoMember]
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }
            [ProtoMember]
            public byte Show;
        }

        MyTextPanel m_block = null;

        static void OnChangeFontSizeRequest(ref ChangeFontSizeMsg msg, MyNetworkClient sender)
        { 
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyTextPanel block = entity as MyTextPanel;
            if (block != null)
            {
                block.FontSize = msg.FontSize;
                Sync.Layer.SendMessageToAll(msg, MyTransportMessageEnum.Success);
            }
        }
        static void OnChangeFontSizeSucess(ref ChangeFontSizeMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyTextPanel block = entity as MyTextPanel;
            if (block != null)
            {
                block.FontSize = msg.FontSize;
            }
        }
        public void SendFontSizeChangeRequest(float FontSize)
        {
            ChangeFontSizeMsg msg = new ChangeFontSizeMsg();

            msg.EntityId = m_block.EntityId;
            msg.FontSize = FontSize;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void OnChangeIntervalRequest(ref ChangeIntervalMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyTextPanel block = entity as MyTextPanel;
            if (block != null)
            {
                block.ChangeInterval = msg.Interval;
                Sync.Layer.SendMessageToAll(msg, MyTransportMessageEnum.Success);
            }
        }
        static void OnChangeIntervalSucess(ref ChangeIntervalMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyTextPanel block = entity as MyTextPanel;
            if (block != null)
            {
                block.ChangeInterval = msg.Interval;
            }
        }

        public void SendIntervalChangeRequest(float interval)
        {
            ChangeIntervalMsg msg = new ChangeIntervalMsg();

            msg.EntityId = m_block.EntityId;
            msg.Interval = interval;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        public void SendRemoveSelectedImageRequest(int[] selection)
        {
            RemoveSelectedImagesMsg msg = new RemoveSelectedImagesMsg();

            msg.EntityId = m_block.EntityId;
            msg.Selection = selection;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void OnRemoveSelectedImageRequest(ref RemoveSelectedImagesMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyTextPanel block = entity as MyTextPanel;
            if (block != null)
            {
                block.RemoveItems(msg.Selection);
                Sync.Layer.SendMessageToAll(msg, MyTransportMessageEnum.Success);
            }
        }
        static void OnRemoveSelectedImagesSucess(ref RemoveSelectedImagesMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyTextPanel block = entity as MyTextPanel;
            if (block != null)
            {
                block.RemoveItems(msg.Selection);
            }
        }

        public void SendAddImagesToSelectionRequest(int[] selection)
        {
            SelectImagesMsg msg = new SelectImagesMsg();

            msg.EntityId = m_block.EntityId;
            msg.Selection = selection;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void OnSelectImageRequest(ref SelectImagesMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyTextPanel block = entity as MyTextPanel;
            if (block != null)
            {
                block.SelectItems(msg.Selection);
                Sync.Layer.SendMessageToAll(msg, MyTransportMessageEnum.Success);
            }
        }
        static void OnSelectImageSucess(ref SelectImagesMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyTextPanel block = entity as MyTextPanel;
            if (block != null)
            {
                block.SelectItems(msg.Selection);
            }
        }
        private static StringBuilder m_helperSB = new StringBuilder();

        static MySyncTextPanel()
        {
            MySyncLayer.RegisterEntityMessage<MySyncTextPanel, ChangeDescriptionMsg>(OnChangeDescription, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncTextPanel, ChangeTitleMsg>(OnChangeTitle, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncTextPanel, ChangeAccessFlagMsg>(OnChangAccessFlag, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncTextPanel, ChangeOpenMsg>(OnChangeOpen, MyMessagePermissions.Any);

            MySyncLayer.RegisterMessage<ChangeIntervalMsg>(OnChangeIntervalRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeIntervalMsg>(OnChangeIntervalSucess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeFontSizeMsg>(OnChangeFontSizeRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeFontSizeMsg>(OnChangeFontSizeSucess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<SelectImagesMsg>(OnSelectImageRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<SelectImagesMsg>(OnSelectImageSucess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<RemoveSelectedImagesMsg>(OnRemoveSelectedImageRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<RemoveSelectedImagesMsg>(OnRemoveSelectedImagesSucess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeFontColorMsg>(ChangeFontColorRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeFontColorMsg>(ChangeFontColorSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeBackgroundColorMsg>(ChangeBackgroundColorRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeBackgroundColorMsg>(ChangeBackgroundColorSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);


            MySyncLayer.RegisterMessage<ChangeShowOnScreenMsg>(OnShowOnScreenRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeShowOnScreenMsg>(OnShowOnScreenSucess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

        }

        public MySyncTextPanel(MyTextPanel block) : base(block)
        {
            m_block = block;
        }

        public new MyTextPanel Entity
        {
            get { return (MyTextPanel)base.Entity; }
        }

        static void OnChangeDescription(MySyncTextPanel sync, ref ChangeDescriptionMsg msg, MyNetworkClient sender)
        {
            m_helperSB.Clear().Append(msg.Description);
            if (msg.IsPublic)
            {
                sync.Entity.PublicDescription = m_helperSB;
            }
            else
            {
                sync.Entity.PrivateDescription = m_helperSB;
            }

            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(ref msg);
        }

        static void OnChangeTitle(MySyncTextPanel sync, ref ChangeTitleMsg msg, MyNetworkClient sender)
        {
            m_helperSB.Clear().Append(msg.Title);
            if (msg.IsPublic)
            {
                sync.Entity.PublicTitle = m_helperSB;
            }
            else
            {
                sync.Entity.PrivateTitle = m_helperSB;
            }

            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(ref msg);
        }

        static void OnChangAccessFlag(MySyncTextPanel sync, ref ChangeAccessFlagMsg msg, MyNetworkClient sender)
        {
            sync.Entity.AccessFlag = (TextPanelAccessFlag)msg.AccessFlag;

            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(ref msg);
        }

        static void OnChangeOpen(MySyncTextPanel sync, ref ChangeOpenMsg msg, MyNetworkClient sender)
        {
            if (Sync.IsServer && sync.Entity.IsOpen && msg.IsOpen)
                return;

            sync.Entity.IsOpen = msg.IsOpen;
            sync.Entity.UserId = msg.User;

            if (!MySandboxGame.IsDedicated && msg.User == Sync.MyId && msg.IsOpen)
            {            
                sync.Entity.OpenWindow(msg.Editable, false,msg.IsPublic);
            }

            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(ref msg);
        }

        public void SendChangeDescriptionMessage(StringBuilder description,bool isPublic)
        {
            if (description.CompareTo(Entity.PublicDescription) == 0 && isPublic)
            {
                return;
            }

            if (description.CompareTo(Entity.PrivateDescription) == 0 && isPublic == false)
            {
                return;
            }

            if(isPublic)
            {
                Entity.PublicDescription = description;
            }
            else
            {
                Entity.PrivateDescription = description;
            }

            var msg = new ChangeDescriptionMsg()
            {
                EntityId = Entity.EntityId,
                Description = description.ToString(),
                IsPublic = isPublic,
            };

            Sync.Layer.SendMessageToServer(ref msg);
        }

        public void SendChangeTitleMessage(StringBuilder title, bool isPublic)
        {
            if ( title.CompareTo(Entity.PublicTitle) == 0 && isPublic)
            {
                return;
            }

            if (title.CompareTo(Entity.PrivateTitle) == 0 && isPublic == false)
            {
                return;
            }

            if (isPublic)
            {
                Entity.PublicTitle = title;
            }
            else
            {
                Entity.PrivateTitle = title;
            }

            var msg = new ChangeTitleMsg()
            {
                EntityId = Entity.EntityId,
                Title = title.ToString(),
                IsPublic = isPublic,
            };

            Sync.Layer.SendMessageToServer(ref msg);
        }

        public void SendChangeAccessFlagMessage(byte accessFlag)
        {
            var msg = new ChangeAccessFlagMsg()
            {
                EntityId = Entity.EntityId,
                AccessFlag = accessFlag,
            };

            Sync.Layer.SendMessageToServer(ref msg);
        }

        public void SendChangeOpenMessage(bool isOpen, bool editable = false, ulong user = 0,bool isPublic = false)
        {
            var msg = new ChangeOpenMsg()
            {
                EntityId = Entity.EntityId,
                IsOpen = isOpen,
                Editable = editable,
                User = user,
                IsPublic = isPublic,
            };

            Sync.Layer.SendMessageToServer(ref msg);
        }

        public void SendChangeFontColorRequest(Color color)
        {
            var msg = new ChangeFontColorMsg();

            msg.EntityId = m_block.EntityId;
            msg.FontColor = color; 

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }
        static void ChangeFontColorRequest(ref ChangeFontColorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyTextPanel)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }
        static void ChangeFontColorSuccess(ref ChangeFontColorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var textPanel = entity as MyTextPanel;
            if (textPanel != null)
            {
                textPanel.FontColor = msg.FontColor;
            }
        }

        public void SendChangeBackgroundColorRequest(Color backgroundColor)
        {
            var msg = new ChangeBackgroundColorMsg();

            msg.EntityId = m_block.EntityId;
            msg.BackgroundColor = backgroundColor;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }
        static void ChangeBackgroundColorRequest(ref ChangeBackgroundColorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyTextPanel)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }
        static void ChangeBackgroundColorSuccess(ref ChangeBackgroundColorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var textPanel = entity as MyTextPanel;
            if (textPanel != null)
            {
                textPanel.BackgroundColor = msg.BackgroundColor;
            }
        }

        static void OnShowOnScreenRequest(ref ChangeShowOnScreenMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyTextPanel block = entity as MyTextPanel;
            if (block != null)
            {
                block.ShowTextFlag = (ShowTextOnScreenFlag)msg.Show;
                Sync.Layer.SendMessageToAll(msg, MyTransportMessageEnum.Success);
            }
        }
        static void OnShowOnScreenSucess(ref ChangeShowOnScreenMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyTextPanel block = entity as MyTextPanel;
            if (block != null)
            {
                block.ShowTextFlag = (ShowTextOnScreenFlag)msg.Show;
            }
        }

        public void SendShowOnScreenChangeRequest(byte show)
        {
            ChangeShowOnScreenMsg msg = new ChangeShowOnScreenMsg();

            msg.EntityId = m_block.EntityId;
            msg.Show = show;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }
    }
}

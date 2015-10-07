using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageDrawScene : IMyRenderMessage
    {
        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DrawScene; } }
    }

    public class MyRenderMessageUnloadData : IMyRenderMessage
    {
        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UnloadData; } }
    }

    public class MyRenderMessageRebuildCullingStructure : IMyRenderMessage
    {
        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.RebuildCullingStructure; } }
    }

    public class MyRenderMessageReloadEffects : IMyRenderMessage
    {
        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.ReloadEffects; } }
    }

    public class MyRenderMessageReloadModels : IMyRenderMessage
    {
        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.ReloadModels; } }
    }

    public class MyRenderMessageReloadTextures : IMyRenderMessage
    {
        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.ReloadTextures; } }
    }

    public class MyRenderMessageReloadGrass : IMyRenderMessage
    {
        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.ReloadGrass; } }
    }

    public class MyRenderMessageUpdateEnvironmentMap : IMyRenderMessage
    {
        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateEnvironmentMap; } }
    }
}

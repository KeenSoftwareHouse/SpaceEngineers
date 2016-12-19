using VRageRender;

namespace VRage.Render11.GeometryStage2.Rendering
{
    static class MyPassIdResolver
    {
        public const int MaxGBufferPassesCount = 1;
        public const int MaxCascadeDepthPassesCount = 8;
        public const int MaxSingleDepthPassesCount = 8;
        public const int MaxForwardPassesCount = 6;
        public const int AllPassesCount = MaxGBufferPassesCount + MaxCascadeDepthPassesCount + MaxSingleDepthPassesCount + MaxForwardPassesCount;

        public const int DefaultGBufferPassId = 0;

        public static int GetGBufferPassId(int i)
        {
            MyRenderProxy.Assert(i < MaxGBufferPassesCount);
            return i;
        }

        public static int GetGBufferPassIdx(int id)
        {
            MyRenderProxy.Assert(id < MaxGBufferPassesCount);
            return id;
        }

        public static bool IsGBufferPassId(int passId)
        {
            return (passId == 0);
        }

        public static bool IsDepthPassId(int passId)
        {
            return passId >= MaxGBufferPassesCount && passId < GetForwardPassId(0);
        }

        public static int GetCascadeDepthPassId(int i)
        {
            MyRenderProxy.Assert(i < MaxCascadeDepthPassesCount);
            return i + MaxGBufferPassesCount;
        }
        public static int GetCascadeDepthPassIdx(int id)
        {
            var i = id - MaxGBufferPassesCount;
            MyRenderProxy.Assert(i < MaxCascadeDepthPassesCount);
            return i;
        }
        public static int GetSingleDepthPassId(int i)
        {
            MyRenderProxy.Assert(i < MaxSingleDepthPassesCount);
            return i + MaxGBufferPassesCount + MaxCascadeDepthPassesCount;
        }

        public static int GetSingleDepthPassIdx(int id)
        {
            var i = id - MaxGBufferPassesCount - MaxCascadeDepthPassesCount;
            MyRenderProxy.Assert(i < MaxSingleDepthPassesCount);
            return i;
        }

        public static int GetForwardPassId(int i)
        {
            MyRenderProxy.Assert(i < MaxForwardPassesCount);
            return i + MaxGBufferPassesCount + MaxCascadeDepthPassesCount + MaxSingleDepthPassesCount;
        }
    }
}

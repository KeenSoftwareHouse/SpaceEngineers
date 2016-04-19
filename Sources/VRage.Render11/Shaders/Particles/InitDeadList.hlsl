// A simple compute shader that adds each particle to the dead list UAV

AppendStructuredBuffer<uint>    g_DeadListToAddTo        : register( u0 );

[numthreads(256,1,1)]
void __compute_shader(uint3 id : SV_DispatchThreadID)
{
    g_DeadListToAddTo.Append( id.x );
}
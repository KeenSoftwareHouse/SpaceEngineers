float4 unpack_position_and_scale(float4 position)
{
	return float4(position.xyz * position.w, 1);
}

float3 unpack_normal(float4 p)
{
	float zsign = p.y > 0.5f ? 1 : -1;
	if(zsign > 0) p.y -= 0.5f;
	float2 xy = 256 * (p.xz + 256 * p.yw);		
	xy /= 32767;
	xy = 2 * xy - 1;
	return float3(xy.xy, zsign * sqrt(saturate(1-dot(xy, xy))));
}

float4 unpack_tangent_sign(float4 p)
{
	float sign = p.w > 0.5f ? 1 : -1;
	float zsign = p.y > 0.5f ? 1 : -1;
	if(zsign > 0) p.y -= 0.5f;
	if(sign > 0) p.w -= 0.5f;
	float2 xy = 256 * (p.xz + 256 * p.yw);		
	xy /= 32767; 
	xy = 2 * xy - 1;
	return float4(xy.xy, zsign * sqrt(saturate(1-dot(xy, xy))), sign);
}

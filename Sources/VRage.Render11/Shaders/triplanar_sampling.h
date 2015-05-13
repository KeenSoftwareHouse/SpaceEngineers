#define INVERT_NM_Y
#define ENABLE_L3

float4 sample_color_triplanar_grad(Texture2DArray<float4> texture_array, float3 texcoords, float3 weights, float3 N,
	float2 texcoords_ddx[3], float2 texcoords_ddy[3], float f)
{
	float2 texcoords_x = texcoords.zy * f;
	float2 texcoords_y = texcoords.xz * f;
	float2 texcoords_z = texcoords.xy * f;

	return
		texture_array.SampleGrad(TextureSampler, float3(texcoords_x, 0), texcoords_ddx[0] * f, texcoords_ddy[0] * f) * weights.x + 
		texture_array.SampleGrad(TextureSampler, float3(texcoords_y, (N.y > 0)), texcoords_ddx[1] * f, texcoords_ddy[1] * f) * weights.y +
		texture_array.SampleGrad(TextureSampler, float3(texcoords_z, 0), texcoords_ddx[2] * f, texcoords_ddy[2] * f) * weights.z;	
}

float4 sample_normal_gloss_triplanar_grad(Texture2DArray<float4> texture_array, float3 texcoords, float3 weights, float3 N,
	float2 texcoords_ddx[3], float2 texcoords_ddy[3], float f)
{
	float2 texcoords_x = texcoords.zy * f;
	float2 texcoords_y = texcoords.xz * f;
	float2 texcoords_z = texcoords.xy * f;

	float4 nm_gloss_x = texture_array.SampleGrad(TextureSampler, float3(texcoords_x, 0), texcoords_ddx[0] * f, texcoords_ddy[0] * f);
	float4 nm_gloss_y = texture_array.SampleGrad(TextureSampler, float3(texcoords_y, (N.y > 0)), texcoords_ddx[1] * f, texcoords_ddy[1] * f);
	float4 nm_gloss_z = texture_array.SampleGrad(TextureSampler, float3(texcoords_z, 0), texcoords_ddx[2] * f, texcoords_ddy[2] * f);

#ifdef INVERT_NM_Y
	nm_gloss_x.y = 1 - nm_gloss_x.y;
	nm_gloss_y.y = 1 - nm_gloss_y.y;
	nm_gloss_z.y = 1 - nm_gloss_z.y;
#endif

	float gloss = dot(float3(nm_gloss_x.w, nm_gloss_y.w, nm_gloss_z.w), weights);

	float3 nx = nm_gloss_x.zyx * 2 - 1;
	float3 ny = nm_gloss_y.xzy * 2 - 1;
	float3 nz = nm_gloss_z.yxz * 2 - 1;
	nx.x *= sign(N.x);
	ny.y *= sign(N.y);
	nz.z *= sign(N.z);
	float3 Nt = nx * weights.x + ny * weights.y + nz * weights.z;
	return float4(Nt, gloss);
}


void calc_derivatives(float3 texcoords, out float2 t_dx[3], out float2 t_dy[3])
{
	float2 texcoords_x = texcoords.zy;
	float2 texcoords_y = texcoords.xz;
	float2 texcoords_z = texcoords.xy;

	t_dx[0] = ddx(texcoords_x);
	t_dy[0] = ddy(texcoords_x);
	t_dx[1] = ddx(texcoords_y);
	t_dy[1] = ddy(texcoords_y);
	t_dx[2] = ddx(texcoords_z);
	t_dy[2] = ddy(texcoords_z);
}
float compute_voxel_ambient_occlusion(float perVertexAmbient, float distance)
{
#if 0
	// perVertexAmbient = 0.349896222;
	perVertexAmbient = 0.0;
#endif
#if 0
	const float highAmbientStart = 2000;
	const float highAmbientFull = 2500;
	float ambientMultiplier = lerp(1.0f, 1.5f, (distance - highAmbientStart) / (highAmbientFull - highAmbientStart));
	ambientMultiplier = clamp(ambientMultiplier, 1, 1.5f);
#else
	const float ambientMultiplier = 1;
#endif
	float ambient = frame_.EnableVoxelAo * (perVertexAmbient - frame_.VoxelAoMin) * rcp(frame_.VoxelAoMax - frame_.VoxelAoMin) + frame_.VoxelAoOffset;
	return saturate(1 - ambient * ambientMultiplier);
}

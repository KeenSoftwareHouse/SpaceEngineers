
struct VertexShaderInputLow_DNS
{
	float4 Position : POSITION0;
	float4 Normal : NORMAL;
	float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutputLow_DNS
{
	float4 Position : POSITION0;
	float4 TexCoordAndViewDistance : TEXCOORD0; //z is linear depth, w is radial depth
	float4 ScreenPosition : TEXCOORD1;
	float3 Normal : TEXCOORD2;
	float3 WorldPos : TEXCOORD3;
	float4 Color : BLENDWEIGHT0; // Color including dithering value
};


// DNS normal (5 input registers)
struct VertexShaderInput_DNS
{
	VertexShaderInputLow_DNS BaseInput;
	float4 Tangent : TANGENT;
};

// Instance data (10 input registers)
// We need to fit into 16 registers
// 6 registers are models data with tangents, bone indices and weights
// We have only 10 registers left for instance data (therefore packing is necessary)
struct VertexShaderInput_InstanceData
{
	float4 bones1 : TEXCOORD1; // w contains bone9.x
	float4 bones2 : TEXCOORD2; // w contains bone9.y
	float4 bones3 : TEXCOORD3; // w contains bone9.z
	float4 bones4 : TEXCOORD4; // w contains deformation flag 0 = no deformation (skip skinning stage), other value = do skinning
	float4 bones5 : TEXCOORD5; // w contains bone range * 10 (5 or 25)
	float4 bones6 : TEXCOORD6; // w contains x texture offset
	float4 bones7 : TEXCOORD7; // w contains y texture offset
	float4 bones8 : TEXCOORD8; // w contains the sign of dithering
	float4 posAndRot : TEXCOORD9;
	float4 colorMaskHSV : TEXCOORD10; //w is free to use
};

struct VertexShaderInput_GenericInstanceData
{
	float4 matrix_row0 : TEXCOORD1;
	float4 matrix_row1 : TEXCOORD2;
	float4 matrix_row2 : TEXCOORD3;
	float4 colorMaskHSV : TEXCOORD4; 
};

float3 unpackBone(float4 bonePos, float boneRange)
{
	float epsilon = 0.5 / 255;
	return (bonePos.xyz + epsilon - float3(0.5f, 0.5f, 0.5f)) * boneRange * 2;
}

float4x4 GetInstanceMatrix(float3 inputPosition, int4 indices, float4 weights, VertexShaderInput_InstanceData instanceData, out float4 outColorMaskHSV)
{
	float4x4 instanceMatrix = CreateWorldFromTranslationAndBasicRotation(instanceData.posAndRot);

	// Branching to do skinning only when necessary
	if(instanceData.bones4.w)
	{
		float4 bones[9] = { instanceData.bones1, instanceData.bones2, instanceData.bones3, instanceData.bones4, 
			instanceData.bones5, instanceData.bones6, instanceData.bones7, instanceData.bones8, float4(instanceData.bones1.w, instanceData.bones2.w, instanceData.bones3.w, 0) }; 
	
		float4x4 b = { bones[indices[0]], bones[indices[1]], bones[indices[2]], bones[indices[3]] };
		float4 translation = mul(weights, b);
		instanceMatrix._41_42_43 += unpackBone(translation, instanceData.bones5.w / 10.0f * 255.0f);
	}
	outColorMaskHSV = instanceData.colorMaskHSV;
	if (instanceData.bones8.w)
	{
		outColorMaskHSV.w = -outColorMaskHSV.w;
	}
	//outColorMaskHSV = float3(0.3,0,0);
	return instanceMatrix;
}

float4x4 GetInstanceMatrixOnlyPosition(float3 inputPosition, VertexShaderInput_InstanceData instanceData, out float4 outColorMaskHSV)
{
	float4x4 instanceMatrix = CreateWorldFromTranslationAndBasicRotation(instanceData.posAndRot);

	outColorMaskHSV = instanceData.colorMaskHSV;
	//outColorMaskHSV = float3(0.3,0,0);
	return instanceMatrix;
}
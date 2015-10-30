// Macros for targetting shader model 2.0 (DX9)

#define BEGIN_CONSTANTS
#define MATRIX_CONSTANTS
#define END_CONSTANTS


#define DECLARE_TEXTURE(Name, index) \
    texture2D Name; \
    sampler Name##Sampler : register(s##index) = sampler_state { Texture = (Name); };

#define DECLARE_CUBEMAP(Name, index) \
    textureCUBE Name; \
    sampler Name##Sampler : register(s##index) = sampler_state { Texture = (Name); };

#define SAMPLE_TEXTURE(Name, texCoord)  tex2D(Name##Sampler, texCoord)
#define SAMPLE_CUBEMAP(Name, texCoord)  texCUBE(Name##Sampler, texCoord)


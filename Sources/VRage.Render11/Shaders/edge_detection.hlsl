#include <postprocess_base.h>
#include <gbuffer.h>
#include <Math/math.h>

void __pixel_shader(PostprocessVertex vertex)
{
	if(!gbuffer_edgedetect(vertex.position.xy))
		discard;
}

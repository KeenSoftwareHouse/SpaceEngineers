#include <postprocess_base.h>
#include <gbuffer.h>
#include <math.h>

void edge_marking(PostprocessVertex vertex)
{
	if(!gbuffer_edgedetect(vertex.position.xy))
		discard;
}

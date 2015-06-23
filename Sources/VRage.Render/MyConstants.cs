using System;
using VRageMath;
using VRage.Utils;

namespace VRageRender
{
    static class MyLightsConstants
    {
        //  This number tells us how many light can be enabled during drawing using one effect. 
        //  IMPORTANT: This number is also hardcoded inside of hlsl effect file.
        //  IMPORTANT: So if you change it here, change it too in MyCommonEffects.fxh
        //  It means, how many lights can player see (meaning light as lighted triangleVertexes, not light flare, etc).
        public const int MAX_LIGHTS_FOR_EFFECT = 8;

        // Maximum radius for all types of point lights. Any bigger value will assert
        public const int MAX_POINTLIGHT_RADIUS = 120;
    }

    static class MyHudConstants
    {
        public const float BACK_CAMERA_HEIGHT = 0.18f;
        public const float BACK_CAMERA_ASPECT_RATIO = 1.6f;
    }

    static class MyTransparentGeometryConstants
    {
        public const int MAX_TRANSPARENT_GEOMETRY_COUNT = 50000;

        public const int MAX_PARTICLES_COUNT = (int)(MAX_TRANSPARENT_GEOMETRY_COUNT * 0.05f);
        public const int MAX_NEW_PARTICLES_COUNT = (int)(MAX_TRANSPARENT_GEOMETRY_COUNT * 0.7f);
        public const int MAX_COCKPIT_PARTICLES_COUNT = 30;      //  We don't need much cockpit particles

        public const int TRIANGLES_PER_TRANSPARENT_GEOMETRY = 2;
        public const int VERTICES_PER_TRIANGLE = 3;
        public const int INDICES_PER_TRANSPARENT_GEOMETRY = TRIANGLES_PER_TRANSPARENT_GEOMETRY * VERTICES_PER_TRIANGLE;
        //public const int VERTICES_PER_TRANSPARENT_GEOMETRY = INDICES_PER_TRANSPARENT_GEOMETRY;
        public const int VERTICES_PER_TRANSPARENT_GEOMETRY = 4;
        public const int MAX_TRANSPARENT_GEOMETRY_VERTICES = MAX_TRANSPARENT_GEOMETRY_COUNT * VERTICES_PER_TRANSPARENT_GEOMETRY;
        public const int MAX_TRANSPARENT_GEOMETRY_INDICES = MAX_TRANSPARENT_GEOMETRY_COUNT * TRIANGLES_PER_TRANSPARENT_GEOMETRY * VERTICES_PER_TRIANGLE;

        //  Use this for all SOFT particles: dust, explosions, smoke, etc. Value was hand-picked.
        public const float SOFT_PARTICLE_DISTANCE_SCALE_DEFAULT_VALUE = 0.5f;

        //  Use this for all particles that will be near an object and you practically don't want soft-particle effect on them. 
        //  It will make them HARD particles. Value was hand-picked.
        public const float SOFT_PARTICLE_DISTANCE_SCALE_FOR_HARD_PARTICLES = 1000;

        //Use this only for decal particles, which reside always close to depth, but not cut into it
        public const float SOFT_PARTICLE_DISTANCE_DECAL_PARTICLES = 10000;
    }

    static class MySecondaryCameraConstants
    {
        public const float NEAR_PLANE_DISTANCE = 1.0f;
        public const int FIELD_OF_VIEW = 50;
    }

    static class MyDecalsConstants
    {
        public const int DECAL_BUFFERS_COUNT = 10;
        public const int DECALS_FADE_OUT_INTERVAL_MILISECONDS = 1000;

        public const int MAX_DECAL_TRIANGLES_IN_BUFFER = 128;
        public const int MAX_DECAL_TRIANGLES_IN_BUFFER_SMALL = 128;
        public const int MAX_DECAL_TRIANGLES_IN_BUFFER_LARGE = 32;

        public const int TEXTURE_LARGE_MAX_NEIGHBOUR_TRIANGLES = 36;
        public const float TEXTURE_LARGE_FADING_OUT_START_LIMIT_PERCENT = 0.7f;     //  Number of decal triangles for large texture (explosion smut). It's used for voxels and phys objects too.
        public const float TEXTURE_LARGE_FADING_OUT_MINIMAL_TRIANGLE_COUNT_PERCENT = 1 - TEXTURE_LARGE_FADING_OUT_START_LIMIT_PERCENT;

        public const int TEXTURE_SMALL_MAX_NEIGHBOUR_TRIANGLES = 32;
        public const float TEXTURE_SMALL_FADING_OUT_START_LIMIT_PERCENT = 0.7f;      //  Number of decal triangles for small texture (bullet hole). It's used for voxels and phys objects too.
        public const float TEXTURE_SMALL_FADING_OUT_MINIMAL_TRIANGLE_COUNT_PERCENT = 1 - TEXTURE_SMALL_FADING_OUT_START_LIMIT_PERCENT;

        public const int VERTEXES_PER_DECAL = 3;
        public static readonly float MAX_NEIGHBOUR_ANGLE = MathHelper.ToRadians(80);

        //  This will how far or distance decals we have to draw. Every decal that is two times farest than reflector spot won't be drawn.
        public const float MAX_DISTANCE_FOR_DRAWING_DECALS_MULTIPLIER_FOR_REFLECTOR = 2.0f;

        //  Don't create decals if it is farther than this distance
        public const float MAX_DISTANCE_FOR_ADDING_DECALS = 500;

        //  Don't draw decals if it is farther than this distance
        public const float MAX_DISTANCE_FOR_DRAWING_DECALS = 200;

        //  We will draw large decals in larger distance
        public const float DISTANCE_MULTIPLIER_FOR_LARGE_DECALS = 3.5f;

        //  This value isn't really needed, because models doesn't have sun defined in triangles, but in shade per object
        //  It's only because some parts of decals are same for voxels and I want this information not lost.
        //public const byte SUN_FOR_MODEL_DECALS = 255;

        public static readonly Color PROJECTILE_DECAL_COLOR = new Color(1.0f, 0.6f, 0.1f);

        // These values give the percentage of how much we move decals in the direction of the dominant normal.
        public const float DECAL_OFFSET_BY_NORMAL = 0.10f;
        public const float DECAL_OFFSET_BY_NORMAL_FOR_SMUT_DECALS = 0.25f;
    }

    static class MyVoxelMapImpostorsConstants
    {
        public const float RANDOM_COLOR_MULTIPLIER_MIN = 0.9f;
        public const float RANDOM_COLOR_MULTIPLIER_MAX = 1.0f;

        public const int TRIANGLES_PER_IMPOSTOR = 2;
        public const int VERTEXES_PER_IMPOSTOR = TRIANGLES_PER_IMPOSTOR * 3;
    }

    static class MyDistantObjectsImpostorsConstants
    {
        public const int MAX_NUMBER_DISTANT_OBJECTS = 50;
        public const float MAX_MOVE_DISTANCE = .00045f;
        public static readonly float DISTANT_OBJECTS_SPHERE_RADIUS = MySectorConstants.SECTOR_DIAMETER * 2.5f;
        public const float RANDOM_COLOR_MULTIPLIER_MIN = 0.9f;
        public const float RANDOM_COLOR_MULTIPLIER_MAX = 1.0f;
        public const float BLINKER_FADE = .005f;
        public const float EXPLOSION_FADE = .02f;
        public const float EXPLOSION_WAIT_MILLISECONDS = 1000f;
        public const float BLINKER_WAIT_MILLISECONDS = 1500f;
        public const float EXPLOSION_MOVE_DISTANCE = .01f;
        public const int TRIANGLES_PER_IMPOSTOR = 2;
        public const int VERTEXES_PER_IMPOSTOR = TRIANGLES_PER_IMPOSTOR * 3;
    }

    static class MySunConstants
    {
        // for zoom values lower than this, there are no glare effects, because occlusion doesn't work well for some reason
        public const float ZOOM_LEVEL_GLARE_END = 0.6f;

        // for screen edge falloff of glare - when center of sun is this many pixels away from the border of the screen, glare starts to fall off
        public const float SCREEN_BORDER_DISTANCE_THRESHOLD = 25;

        // for changing the maximum intensity of the sun (when it is in the centre of the screen). Acceptable values are [0, 10]
        public const float MAX_GLARE_MULTIPLIER = 2.5f;

        // distance in which render sun
        public static float RENDER_SUN_DISTANCE
        {
            get
            {
                return MyRenderCamera.FAR_PLANE_DISTANCE;
            }
        }

        // sun glow and glare size multiplier
        public static float SUN_SIZE_MULTIPLIER = 1.0f;
       

        // minimum and maximum sun glow and glare sizes (in no specific unit)
        public const float MIN_SUN_SIZE = 250;  // will be used in sectors distant from sun (e.g. post-uranus)
        public const float MAX_SUN_SIZE = 4000; // will be used in sectors close to sun

        // for sun occlusion query
        public static readonly int MIN_QUERY_SIZE = 1000;

        // maximum number of occlusion query pixels - higher numbers are discarded as faulty query results
        public const int MAX_SUNGLARE_PIXELS = 100000;
    }
}
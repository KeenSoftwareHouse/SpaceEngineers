#region Using
using SharpDX.Direct3D9;
#endregion

namespace VRageRender
{
    public enum PostProcessStage
    {
        /// <summary>
        /// Stage right after LOD blend. Render target is unknown. You can modify content of GBuffer here
        /// (Diffuse, Normal and Depth targets), these target are then used in lighting process
        /// Do not read or write source (it's null), use availableRT (is parameter of Render()) for anything you need. 
        /// RT returned as result from post proccess is not taken into account. You could modify diffuse or anything else.
        /// </summary>
        LODBlend,

        /// <summary>
        /// This stage is right after lighting, volumetric fog is done here.
        /// Render target is HdrBlendable. DO NOT CHANGE RT! JUST BLEND IN ANYTHING YOU NEED!
        /// Do not read or write source (it's null), use availableRT (is parameter of Render()) for anything you need. 
        /// RT returned as result from post proccess is not taken into account. You could modify diffuse or anything else.
        /// </summary>
        PostLighting,

        /// <summary>
        /// HDR stage. HDR is done as first in this stage (or should be). It is called also if Lighting is disabled.
        /// Use availableRT (is parameter of Render()) to anything you need. Return RT with result from Render() (could be source or availableRT)
        /// </summary>
        HDR,

        /// <summary>
        /// Stage right after alpha blended objects (particles, glass). Do not change render target here.
        /// Use availableRT (is parameter of Render()) to anything you need. Return RT with result from Render() (could be source or availableRT)
        /// </summary>
        AlphaBlended
    }

    abstract class MyPostProcessBase
    {
        public MyPostProcessBase(bool enabled = true)
        {
            Enabled = enabled;
        }

        /// <summary>
        /// Enables this post process
        /// </summary>
        public virtual bool Enabled { set; get; }

        /// <summary>
        /// Name of the post process
        /// </summary>
        public abstract MyPostProcessEnum Name { get; }

        public abstract string DisplayName { get; }

        /// <summary>
        /// Render method is called directly by renderer. Depending on stage, post process can do various things 
        /// </summary>
        /// <param name="postProcessStage">Stage indicating in which part renderer currently is.</param>
        /// <param name="source">Render target where is current scene.</param>
        /// <param name="availableRenderTarget">Render target for use in shader, can be used as output or not.</param>
        /// <returns>Returns render target with output, must be source or availableRenderTarget</returns>
        public abstract Texture Render(PostProcessStage postProcessStage, Texture source, Texture availableRenderTarget);
    }
}

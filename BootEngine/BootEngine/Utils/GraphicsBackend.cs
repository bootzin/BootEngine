namespace BootEngine.Utils
{
	public enum GraphicsBackend : byte
    {
        /// <summary>
        /// Direct3D 11.
        /// </summary>
        Direct3D11,
        /// <summary>
        /// Vulkan.
        /// </summary>
        Vulkan,
        /// <summary>
        /// OpenGL.
        /// </summary>
        OpenGL,
        /// <summary>
        /// Metal.
        /// </summary>
        Metal,
        /// <summary>
        /// OpenGL ES.
        /// </summary>
        OpenGLES,
    }
}

void pixel_program(PixelInterface pixel, out MaterialOutputInterface output)
{
	output.base_color = 0.5f;
	output.normal = pixel.custom.normal;
	output.smoothness = 0.5f;
	output.metalness = 0;
	output.transparency = 0;
	output.id = 0;
}
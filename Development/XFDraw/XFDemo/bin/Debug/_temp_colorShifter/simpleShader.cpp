//vignette shader v1.0
in float opacity;
out byte4 color;

void main()
{
	

	color = byte4(color.R * opacity, color.G * opacity, color.B * opacity);
}

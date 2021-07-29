//vignette shader v1.0
out byte4 color;
uniform vec2 viewportMod;

void main()
{
	float X = (gl_FragCoord.x * viewportMod.x) - 1.0f;
	float Y = (gl_FragCoord.y * viewportMod.y) - 1.0f;

	X = 1.0f - 0.5f * X * X;
	Y = X * (1.0f - 0.5f * Y * Y);

	unsigned char R = 255 * Y;
	unsigned char G = 255 * Y;
	unsigned char B = 0;

	color = byte4(R, G, B);
}

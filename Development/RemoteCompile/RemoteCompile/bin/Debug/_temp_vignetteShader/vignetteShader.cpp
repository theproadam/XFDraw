/*
Vignette Shader v1.1.1.1 :)))))
*/

out float outMultiplier;
uniform vec2 viewportMod;

void main()
{
	float X = (gl_FragCoord.x * viewportMod.x) - 1.0f;
	float Y = (gl_FragCoord.y * viewportMod.y) - 1.0f;

	X = 1.0f - 0.5f * X * X;
	Y = X * (1.0f - 0.5f * Y * Y);


	outMultiplier = Y;
}



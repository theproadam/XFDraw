//vignetteShader

out float outMultiplier;
uniform vec2 viewportMod;

/*
	some comment here
	Vignette Shader v1.1.1.1 :)))))
*/

struct Material {
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
	float shininess;
};

uniform Material mat;

void TestShader()
{
	float abc = 2;
	float bdc = 10;
}

void main()
{
	float X = (gl_FragCoord.x * viewportMod.x) - 1.0f;
	float Y = (gl_FragCoord.y * viewportMod.y) - 1.0f;

	X = 1.0f - 0.5f * X * X;
	Y = X * (1.0f - 0.5f * Y * Y);

	outMultiplier = Y;
}



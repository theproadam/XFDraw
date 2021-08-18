out float ssao_out;

uniform sampler2D ssaoInput;
uniform int pass_dir;


void main() {
	float result = 0.0;
	
	vec2 coord = vec2(gl_FragCoord);

	if (pass_dir == 0)
	{
		for (int x = -2; x < 2; ++x)
		{
			result += texture<float>(ssaoInput, coord + vec2(x, 0));
		}		
	}
	else
	{
		for (int y = -2; y < 2; ++y)
		{
			result += texture<float>(ssaoInput, coord + vec2(0, y));
		}
	}

	ssao_out = result * 0.25f; //(1/16)
}
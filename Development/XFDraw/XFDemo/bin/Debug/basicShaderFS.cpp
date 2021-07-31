//version 330 Core
out byte4 FragColor;

in vec3 some_data;
in vec2 uv_data;

uniform sampler2D myTexture;
uniform vec2 textureSize;

void main()
{
	//vec3 result = vec3(0.5f, 0.5f, 0.5);
//	FragColor = byte4(result.x * 255, result.y * 255, result.z * 255);

//	byte4 color = textureBILINEAR(myTexture, uv_data * textureSize);

	//if (color.A == 0) return;

	FragColor = texture(myTexture, vec2(uv_data.x * textureSize.x, uv_data.y * textureSize.y));

	//FragColor = byte4(uv_data.x * 255, uv_data.y * 255, 0);
}

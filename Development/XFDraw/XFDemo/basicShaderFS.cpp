//version 330 Core
out byte4 FragColor;

in vec2 uv_data;
in vec3 frag_pos;

uniform sampler2D myTexture;
uniform vec2 textureSize;
uniform vec3 camera_Pos;
uniform float heightScale;
uniform sampler2D depthMap;


void main()
{

	vec3 viewDir = normalize(camera_Pos - frag_pos);
	//vec3 viewDir = normalize(frag_pos - camera_Pos);
	byte4 reslt = texture(depthMap, vec2(uv_data.x * textureSize.x, uv_data.y * textureSize.y));

	float height = 1.0f - (float)reslt.R * 0.00392156862745f;

	vec2 texCoords = uv_data - vec2(viewDir) * (height * heightScale);
	//vec2 texCoords = vec2(0, 0);

	if (texCoords.x > 1.0 || texCoords.y > 1.0 || texCoords.x < 0.0 || texCoords.y < 0.0)
	{
		//FragColor = byte4(255, 0, 255);
		return;
	}

	FragColor = texture(myTexture, vec2(texCoords.x * textureSize.x, texCoords.y * textureSize.y));

	//FragColor = byte4(uv_data.x * 255, uv_data.y * 255, 0);
}

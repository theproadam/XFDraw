// version 330 Core
out byte4 diffuse;
out vec3 normal;
out vec3 world_pos;
out float specular;

in vec2 uv_data;
in vec3 frag_pos;

uniform sampler2D albedoTexture;
uniform sampler2D normalTexture;
uniform sampler2D heightTexture;
uniform sampler2D speculTexture;

uniform vec2 textureSize;
uniform vec3 camera_Pos;
uniform float heightScale;

void main()
{
	vec3 viewDir = normalize(frag_pos - camera_Pos);


	//compensate for lack of bitangent tangent mapping in this shader
	float yVal = 1.0f - viewDir.y;
	viewDir.y = viewDir.z;
	viewDir.z = yVal;

	float height = 1.0f - texture(heightTexture, vec2(uv_data.x * textureSize.x, uv_data.y * textureSize.y)).R * 0.00392156862745f;

	vec2 texCoords = uv_data - vec2(viewDir) * (height * heightScale);
	vec2 texResult = texCoords * textureSize;

	diffuse = texture(albedoTexture, texResult);
	specular = texture(speculTexture, texResult).R * 0.00392156862745f;
	byte4 tR = texture(normalTexture, texResult);

	//compensate for lack of bitangent tangent mapping in this shader
	normal = vec3(tR.R * 0.0078431372549f - 1.0f, tR.B * 0.0078431372549f - 1.0f, (255.0f - tR.G) * 0.0078431372549f - 1.0f);


	
	world_pos = frag_pos;
}

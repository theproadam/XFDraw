// version 330 Core
out byte4 FragColor;

in vec2 uv_data;
in vec3 frag_pos;

uniform sampler2D albedoTexture;
uniform sampler2D normalTexture;

uniform vec2 textureSize;
uniform vec3 lightDir;

inline float max(float a, float b)
{
	if (a < b) return b;
	else return a;
}


void main()
{
	const float ambientStrength = 0.1f;
	byte4 tR = texture(normalTexture, uv_data * textureSize);

	vec3 norm_data = vec3(tR.R * 0.0078431372549f - 1.0f, tR.B * 0.0078431372549f - 1.0f, (255.0f - tR.G) * 0.0078431372549f - 1.0f);
	
	byte4 diffuse1 = texture(albedoTexture, uv_data * textureSize);
	vec3 objectColor = diffuse1.xyz();


	vec3 lightColor = vec3(0.8f, 0.8f, 0.8f);
	vec3 ambient = lightColor * ambientStrength;

	float diff = max(dot(norm_data, lightDir), 0.0);
	vec3 diffuse = lightColor * diff;

	vec3 result = objectColor * (ambient + diffuse) * 255.0f;//

	if (result.x > 255) result.x = 255;
	if (result.y > 255) result.y = 255;
	if (result.z > 255) result.z = 255;

	FragColor = byte4(result.x, result.y, result.z);
}

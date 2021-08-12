// version 330 Core
out byte4 diffuse;
out vec3 normal;
out vec3 world_pos;

in vec2 uv_data;
in vec3 frag_pos;

uniform sampler2D myTexture;
uniform vec2 textureSize;

void main()
{
	diffuse = texture(myTexture, uv_data * textureSize);
	//diffuse = byte4(255, 255, 255);


	normal = vec3(0, 1.0f, 0);
	world_pos = frag_pos;
}

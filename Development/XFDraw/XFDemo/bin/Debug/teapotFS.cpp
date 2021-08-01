//version 330 Core
out byte4 FragColor;

in vec3 norm_data;
in vec3 frag_pos;

uniform samplerCube skybox;
uniform vec3 camera_Pos;

void main()
{
	vec3 I = (frag_pos - camera_Pos);
	vec3 R = reflect(I, norm_data);
	
	FragColor = textureNEAREST(skybox, R);
}

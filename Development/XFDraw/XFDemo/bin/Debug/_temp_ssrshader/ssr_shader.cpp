//version 330 Core
out byte4 FragColor;
out vec3 nor_data;
out vec3 pos_data;

in vec3 norm_data;
in vec3 frag_pos;



void main()
{
	nor_data = norm_data;
	pos_data = frag_pos;


}

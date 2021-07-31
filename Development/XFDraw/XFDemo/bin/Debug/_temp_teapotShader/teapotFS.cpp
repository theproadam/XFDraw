//version 330 Core
out byte4 FragColor;

in vec3 norm_data;

void main()
{
	FragColor = byte4(norm_data.x * 127.5f + 127.5f, norm_data.y * 127.5f + 127.5f, norm_data.z * 127.5f + 127.5f);
}

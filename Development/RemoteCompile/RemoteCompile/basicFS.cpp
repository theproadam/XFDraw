out byte4 FragColor;

in vec3 FragPos;
in vec3 Normal;


void main()
{
	FragColor = byte4(Normal.x * 127.5f + 127.5f, Normal.y * 127.5f + 127.5f, Normal.z * 127.5f + 127.5f);
}
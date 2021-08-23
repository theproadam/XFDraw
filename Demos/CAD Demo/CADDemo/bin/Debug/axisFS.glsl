//This shader is sampled only one per triangle
out byte4 FragColor;
in vec3 normals;

uniform mat3 camera_rotation;
uniform int colorMode;

uniform mat3 object_r;

void main()
{
	float opacity = -(((camera_rotation * object_r) * normals).z) * 127.5f + 127.5f;
	
	if (colorMode == 0)
	{
		FragColor = byte4(opacity * 0.8f, 50, 50);
	}
	else if (colorMode == 1)
	{
		FragColor = byte4(50, opacity * 0.8f, 50);
	}
	else if (colorMode == 2)
	{
		FragColor = byte4(50, 50, opacity * 0.8f);
	}
}
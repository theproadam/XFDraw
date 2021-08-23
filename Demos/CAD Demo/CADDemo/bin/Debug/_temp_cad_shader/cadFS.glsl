//This shader is sampled only one per triangle
out byte4 FragColor;

uniform sampler1D normal_buffer;
uniform mat3 camera_rotation;
uniform int isOrange;

void main()
{
	if (isOrange == 1)
	{
		FragColor = byte4(255, 127, 80);
	}
	else
	{
		vec3 normal = texture<vec3>(normal_buffer, gl_InstanceID);
		normal = (camera_rotation * normal);
		float color = -normal.z * 127.5f + 127.5f;

		FragColor = byte4(color, color, color);
	}
}
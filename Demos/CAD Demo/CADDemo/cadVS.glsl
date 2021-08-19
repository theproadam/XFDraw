layout (location = 0) in vec3 vertex_data;

uniform vec3 cameraPos;
uniform mat3 cameraRot;
uniform float normalOffset;
uniform sampler1D normal_buffer_vs;

void main()
{
	if (normalOffset == 0)
	{
		gl_Position = cameraRot * (vertex_data - cameraPos);

	}
	else
	{
		vec3 normal = texture<vec3>(normal_buffer_vs, gl_InstanceID);
		gl_Position = (cameraRot * (vertex_data - cameraPos)) + normal * normalOffset;
	}
	
}
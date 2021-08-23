layout (location = 0) in vec3 vertex_data;

uniform vec3 cameraPos;
uniform mat3 cameraRot;
uniform float normalOffset;
uniform sampler1D normal_buffer_vs;

uniform float object_scale;
uniform vec3 object_center;

inline vec3 scaleVec(vec3 input, vec3 scale)
{
	return vec3(input.x * scale.x, input.y * scale.y, input.z * scale.z);
}


void main()
{
	vec3 fPos = cameraRot * (((vertex_data - object_center) * object_scale + object_center) - cameraPos);

	if (normalOffset == 0)
	{
		gl_Position = fPos;
	}
	else
	{
		vec3 normal = texture<vec3>(normal_buffer_vs, gl_InstanceID);
		gl_Position = fPos + normal * normalOffset;
	}
}
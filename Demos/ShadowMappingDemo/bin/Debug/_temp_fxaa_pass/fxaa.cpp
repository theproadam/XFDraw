out byte4 FragColor;
in byte4 currentPixel;
uniform sampler2D src;

vec2 min(vec2 a, vec2 b)
{
	vec2 result = vec2();
	result.x = a.x < b.x ? a.x : b.x;
	result.y = a.y < b.y ? a.y : b.y;
	return result;
}

vec2 max(vec2 a, vec2 b)
{
	vec2 result = vec2();
	result.x = a.x > b.x ? a.x : b.x;
	result.y = a.y > b.y ? a.y : b.y;
	return result;
}

inline float max(float a, float b)
{
	return a > b ? a : b;
}

inline float min(float a, float b)
{
	return a < b ? a : b;
}

inline float absf(float val)
{
	return val > 0 ? val : -val;
}

void main()
{
	const float FXAA_SPAN_MAX = 8.0f;
	const float FXAA_REDUCE_MUL = 1.0f / 8.0f;
	const float FXAA_REDUCE_MIN = 1.0f / 128.0f;

	vec2 screenXY = vec2(gl_FragCoord);
	byte4 cPixel = currentPixel;

	vec3 rgbM = vec3(cPixel.R * 0.00392156862745f, cPixel.G * 0.00392156862745f, cPixel.B * 0.00392156862745f);
	vec3 rgbNW = texture(src, screenXY + vec2(-1.0f, 0.0f)).xyz();
	vec3 rgbNE = texture(src, screenXY + vec2(1.0f, 0.0f)).xyz();
	vec3 rgbSW = texture(src, screenXY + vec2(0.0f, 1.0f)).xyz();
	vec3 rgbSE = texture(src, screenXY + vec2(0.f, -1.0f)).xyz();
	

	vec3 luma = vec3(0.299f, 0.587f, 0.114f);
	float lumaNW = dot(rgbNW, luma);
	float lumaNE = dot(rgbNE, luma);
	float lumaSW = dot(rgbSW, luma);
	float lumaSE = dot(rgbSE, luma);
	float lumaM = dot(rgbM, luma);

	float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
	float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));

	float range = lumaMax - lumaMin;

	if (range <= 0.5f && range >= 0.06f)
	{
		float delta1 = absf(lumaNW - lumaNE); //horizontal smear
		float delta2 = absf(lumaSW - lumaSE); //vertical smear

		float norm = 1.0f / (delta1 + delta2);
		delta1 *= norm;
		delta2 *= norm;

		float norm1 = 1.0f / (lumaNW + lumaNE);
		lumaNW *= norm1;
		lumaNE *= norm1;

	//	FragColor = byte4(255, 255, 255);
	//	return;

		vec3 val = (rgbNW * 0.5f + rgbNE * 0.5f) * delta2 + (rgbSW * 0.5f + rgbSE * 0.5f) * delta1;// +rgbM * 0.6f;
		FragColor = byte4(val.x * 255.0f, val.y * 255.0f, val.z * 255.0f);
		return;
	}

	FragColor = byte4(rgbM.x * 255.0f, rgbM.y * 255.0f, rgbM.z * 255.0f);
}
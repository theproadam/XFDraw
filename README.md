# XFDraw
XFDraw is a realtime, high performance, software renderer written in C++ controlled via a wrapper written in C#. XFDraw is still in development, however with its WIP "Simple" mode XFDraw can handle insane amounts of polygons. It is the last and fastest version of the "renderX" series.

### Currently XFDraw is still WIP. Expect semi-regular updates.

### Demo Screenshots
#### Depth Fill. 350k Triangles ~3.3ms
![Depth Fill Demo](https://i.imgur.com/OlIJDbv.png)

#### Vignette Shader. 1080p [not pictured] ~0.6ms
![Screenspace Shaders Example](https://i.imgur.com/gBNrAQr.png)

### Example Shader Code (Used for vignette buffer building)
```c++
struct CreateVingetteBuffer
{
	out float* outMultiplier;
	variable int2 XY_Coords;
	uniform vec2 viewportMod;

	void main()
	{
		float X = (XY_Coords.X * viewportMod.x) - 1.0f;
		float Y = (XY_Coords.Y * viewportMod.y) - 1.0f;

		X = 1.0f - 0.5f * X * X;
		Y = X * (1.0f - 0.5f * Y * Y);

		*outMultiplier = Y;
	}
};
DeclareShader(BuildVignette, CreateVingetteBuffer, main)
```



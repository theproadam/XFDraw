# XFDraw
XFDraw is a realtime, high performance, software renderer written in C++ controlled via a wrapper written in C#. XFDraw is still in development, however with its WIP "Simple" mode XFDraw can handle insane amounts of polygons. It is the last and fastest version of the "renderX" series.


Just like renderXF, XFDraw has been also designed to be as simple and user friendly as possible. Because of this most of the code is composed of super simple commands, and buffer, shader and framebuffer initialization is also very simple. There is also a Wiki available to help with a quick start.

### Currently XFDraw is still WIP. Expect semi-regular updates.

### Demo Screenshots
#### Phong Example. 926 Triangles ~1.2ms (>20ms if the viewport is filled due to forward rendering)
![Phong Shader Demo](https://raw.githubusercontent.com/theproadam/XFDraw/main/Screenshots/TeapotPhong.png)

#### Normal Example. 926 Triangles ~0.4ms
![Depth Fill Demo](https://raw.githubusercontent.com/theproadam/XFDraw/main/Screenshots/TeapotNormals.png)

#### Depth Fill. 350k Triangles ~3.3ms
![Depth Fill Demo](https://i.imgur.com/OlIJDbv.png)

#### Shadow mapping support. (Not Optimized Yet)
![Shadow Mapping](https://cdn.discordapp.com/attachments/545669301164703754/862901922033565696/unknown.png)

#### Shadow mapping example. (Not Optimized Yet)
![Shadow Mapping](https://cdn.discordapp.com/attachments/545669301164703754/863470567185055784/unknown.png)




#### Vignette Shader. ~0.6ms (in 1080p, not shown)
![Screenspace Shaders Example](https://i.imgur.com/gBNrAQr.png)

### Example Shader Code (Used for vignette buffer building)
C++ Side:
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
C# Side:
```c#
Shader buildVignette = Shader.Load(shaderModules[2], typeof(CreateVingetteBuffer));
buildVignette.AssignVariable("XY_Coords", VariableType.XYScreenCoordinates);
buildVignette.AssignBuffer("outMultiplier", vignetteBuffer);
buildVignette.SetValue("viewportMod", new Vector2(2f / ViewportWidth, 2f / ViewportHeight));

//When done, perform the pass:
GL.Pass(buildVignette);
```


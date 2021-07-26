# XFDraw
XFDraw is a real-time, high-performance, software renderer written in C++ controlled via a wrapper written in C#. XFDraw is still in development, however, with its WIP "Simple" mode, XFDraw can handle insane amounts of polygons. It is the last and fastest version of the "renderX" series.


Just like renderXF, XFDraw has been also designed to be as simple and user-friendly as possible. Because of this most of the code is composed of super simple commands, and buffer, shader and, framebuffer initialization is also very simple. There is also a Wiki available to help with a quick start.

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

### Example Screen Space Shader Code (Used for vignette buffer building)
C++ side `myVignetteShader.cpp`:
```c++
//Vignette Shader Buffer Builder v1.0
out float outMultiplier;
uniform vec2 viewportMod;

void main(){
	float X = (gl_FragCoord.x * viewportMod.x) - 1.0f;
	float Y = (gl_FragCoord.y * viewportMod.y) - 1.0f;
	X = 1.0f - 0.5f * X * X;
	Y = X * (1.0f - 0.5f * Y * Y);
	outMultiplier = Y;
}
```
C# Side:
```c#
//Parse and compile the shader ->
ShaderCompile sModule;
ShaderParser.Parse("myVignetteShader.cpp", out sModule);
Shader vignetteShader;
sModule.Compile(out vignetteShader)

//Assign uniforms and link buffers ->
vignetteShader.SetValue("viewportMod", new Vector2(2f / viewportWidth, 2f / viewportHeight));
vignetteShader.AssignBuffer("outMultiplier", vignetteBuffer);

//When done, perform the pass:
vignetteShader.Pass();
```


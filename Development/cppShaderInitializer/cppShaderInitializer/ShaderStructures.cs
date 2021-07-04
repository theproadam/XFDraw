using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xfcore.Shaders.Structs;
using System.Runtime.InteropServices;

namespace cppShaderInitializer
{
#pragma warning disable 0169
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct FS3D
    {
        //out
        [xout] byte4* FragColor;

        //in
        [xinp] vec2* UV;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct VS3D
    {
        //[Layout.Location(0), xinp] vec3* coords_in;
        [xinp] vec3* coords_in;
        [xinp] vec2* uv_in;

	    [VS_out] vec3* coords_out;
        [xout] vec2* uv_out;

	    [uniform] vec3 camPos;
	    [uniform] vec3 rotCos;
	    [uniform] vec3 rotSin;

    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct CShift
    {
        [xout] byte4* outColor; 
	
	    [variable] int2 XY_Coords;
	    [uniform] vec2 viewportMod;
    }

    unsafe struct CreateVingetteBuffer
    {
	    [xout] float* outMultiplier;
	    [variable] int2 XY_Coords;
	    [uniform] vec2 viewportMod;
    }

    unsafe struct MultiplyBy
    {
        [xout] byte4* outColor;
	    [xinp] float* inMultiplier;
    }

    unsafe struct ColorShift
    {
	    [xout] byte4* outColor;
	    [uniform] byte4 tcolor;
    }

    unsafe struct DisplayTexture
    {
	    [xout] byte4* outColor;

	    [variable] int2 XY_Coords;
	    [uniform] vec2 viewportMod;
	    [uniform] sampler2D sourceTexture;
    }


    #pragma warning restore 0169
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using xfcore.Buffers;

namespace xfcore.Performance
{
    public unsafe static class GLFast
    {
        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void VignettePass(int* TargetBuffer, float* SourceBuffer, int Width, int Height);

        public static void VignetteMultiply(GLTexture Target, GLTexture Multiplier)
        {
            Target.RequestLock();
            Multiplier.RequestLock();
            
            if (Target.Stride != 4 | Multiplier.Stride != 4)
                throw new Exception("Target and Multiplier both need to be 4 byte stride textures");

            if (Target.Width != Multiplier.Width || Multiplier.Height != Target.Height)
                throw new Exception("Target and Multiplier Bufers are not of the same size!");

            VignettePass((int*)Target.GetAddress(), (float*)Multiplier.GetAddress(), Target.Width, Target.Height);

            Target.ReleaseLock();
            Multiplier.ReleaseLock();
        }



    }
}

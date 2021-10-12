using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using xfcore.Buffers;
using xfcore.Extras;
using xfcore.Shaders;

namespace xfcore.Performance
{
    public unsafe static class GLFast
    {
        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void VignettePass(int* TargetBuffer, float* SourceBuffer, int Width, int Height);

        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void DrawSkybox(float* tris, int* iptr, int skyBoxWidth, GLData projData, Matrix3x3 rotMatrix, float** sptr, int* bsptr, int** txptr, float* sdptr);

        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void FXAA_PASS(int* TargetBuffer, int* SourceBuffer, int width, int height);

        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void BOX_BLUR(int* TargetBuffer, int* SourceBuffer, int* tempBuffer, int width, int height);

        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void BOX_BLUR_FLOAT(float* TargetBuffer, float* SourceBuffer, float* tempBuffer, int width, int height);

        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void BOX_BLUR5(int* TargetBuffer, int* SourceBuffer, int* tempBuffer, int width, int height);

        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void BOX_BLUR5_FLOAT(float* TargetBuffer, float* SourceBuffer, float* tempBuffer, int width, int height);


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

        public static void BoxBlur3x3(GLTexture outputBuffer, GLTexture inputBuffer, GLTexture tempBuffer)
        {
            outputBuffer.RequestLock();
            inputBuffer.RequestLock();

            if (outputBuffer.Stride != 4)
                throw new Exception("outputBuffer stride must be 32bpp!");

            if (inputBuffer.Stride != 4)
                throw new Exception("outputBuffer stride must be 32bpp!");

            if (inputBuffer.Width != outputBuffer.Width)
                throw new Exception("Input and output buffer widths are not the same!");

            if (inputBuffer.Height != outputBuffer.Height)
                throw new Exception("Input and output buffer heights are not the same!");

            int* addr1 = (int*)outputBuffer.GetAddress();
            int* addr2 = (int*)inputBuffer.GetAddress();
            int* addr3 = (int*)tempBuffer.GetAddress();

            BOX_BLUR(addr1, addr2, addr3, inputBuffer.Width, inputBuffer.Height);

            outputBuffer.ReleaseLock();
            inputBuffer.ReleaseLock();
        }

        public static void BoxBlur5x5(GLTexture outputBuffer, GLTexture inputBuffer, GLTexture tempBuffer)
        {
            outputBuffer.RequestLock();
            inputBuffer.RequestLock();

            if (outputBuffer.Stride != 4)
                throw new Exception("outputBuffer stride must be 32bpp!");

            if (inputBuffer.Stride != 4)
                throw new Exception("outputBuffer stride must be 32bpp!");

            if (inputBuffer.Width != outputBuffer.Width)
                throw new Exception("Input and output buffer widths are not the same!");

            if (inputBuffer.Height != outputBuffer.Height)
                throw new Exception("Input and output buffer heights are not the same!");

            int* addr1 = (int*)outputBuffer.GetAddress();
            int* addr2 = (int*)inputBuffer.GetAddress();
            int* addr3 = (int*)tempBuffer.GetAddress();

            BOX_BLUR5(addr1, addr2, addr3, inputBuffer.Width, inputBuffer.Height);

            outputBuffer.ReleaseLock();
            inputBuffer.ReleaseLock();
        }

        public static void BoxBlur3x3Float(GLTexture outputBuffer, GLTexture inputBuffer, GLTexture tempBuffer)
        {
            outputBuffer.RequestLock();
            inputBuffer.RequestLock();

            if (outputBuffer.Stride != 4)
                throw new Exception("outputBuffer stride must be 32bpp!");

            if (inputBuffer.Stride != 4)
                throw new Exception("outputBuffer stride must be 32bpp!");

            if (inputBuffer.Width != outputBuffer.Width)
                throw new Exception("Input and output buffer widths are not the same!");

            if (inputBuffer.Height != outputBuffer.Height)
                throw new Exception("Input and output buffer heights are not the same!");

            float* addr1 = (float*)outputBuffer.GetAddress();
            float* addr2 = (float*)inputBuffer.GetAddress();
            float* addr3 = (float*)tempBuffer.GetAddress();

            BOX_BLUR_FLOAT(addr1, addr2, addr3, inputBuffer.Width, inputBuffer.Height);

            outputBuffer.ReleaseLock();
            inputBuffer.ReleaseLock();
        }

        public static void BoxBlur5x5Float(GLTexture outputBuffer, GLTexture inputBuffer, GLTexture tempBuffer)
        {
            outputBuffer.RequestLock();
            inputBuffer.RequestLock();

            if (outputBuffer.Stride != 4)
                throw new Exception("outputBuffer stride must be 32bpp!");

            if (inputBuffer.Stride != 4)
                throw new Exception("outputBuffer stride must be 32bpp!");

            if (inputBuffer.Width != outputBuffer.Width)
                throw new Exception("Input and output buffer widths are not the same!");

            if (inputBuffer.Height != outputBuffer.Height)
                throw new Exception("Input and output buffer heights are not the same!");

            float* addr1 = (float*)outputBuffer.GetAddress();
            float* addr2 = (float*)inputBuffer.GetAddress();
            float* addr3 = (float*)tempBuffer.GetAddress();

            BOX_BLUR5_FLOAT(addr1, addr2, addr3, inputBuffer.Width, inputBuffer.Height);

            outputBuffer.ReleaseLock();
            inputBuffer.ReleaseLock();
        }


        public static void DrawSkybox(GLTexture colorBuffer, GLCubemap skybox, Matrix3x3 cameraRotation)
        {
            colorBuffer.RequestLock();

            if (!skybox.isValid())
                throw new Exception("Invalid Cubemap!");

            if (skybox.cubemap[0].Width != skybox.cubemap[0].Height)
                throw new Exception("Invalid Cubemap!");

            for (int i = 0; i < skybox.cubemap.Length; i++)
                skybox.cubemap[i].RequestLock();

            GLBuffer cubeBuffer = GLPrimitives.Cube;

            //no idea what is going on here!
            IntPtr SkyboxPointerBuffer = Marshal.AllocHGlobal(colorBuffer.Height * 12 * 4);
            IntPtr SkyboxData = Marshal.AllocHGlobal(4 * 77 * 12);
            IntPtr SkyboxFaceCountData = Marshal.AllocHGlobal(colorBuffer.Height * 4);
            IntPtr SkyboxTexturePointers = Marshal.AllocHGlobal(12 * 4);

            GL.RtlZeroMemory(SkyboxFaceCountData, colorBuffer.Height * 4);
            GL.RtlZeroMemory(SkyboxData, 4 * 76 * 12);
            GL.RtlZeroMemory(SkyboxPointerBuffer, colorBuffer.Height * 12 * 4);

            float** sptr = (float**)SkyboxPointerBuffer;
            float* sdptr = (float*)SkyboxData;
            int* bsptr = (int*)SkyboxFaceCountData;
            int** txptr = (int**)SkyboxTexturePointers;

            txptr[2] = (int*)skybox.FRONT.GetAddress();
            txptr[3] = (int*)skybox.FRONT.GetAddress();
            txptr[0] = (int*)skybox.BACK.GetAddress();
            txptr[1] = (int*)skybox.BACK.GetAddress();
            txptr[4] = (int*)skybox.LEFT.GetAddress();
            txptr[5] = (int*)skybox.LEFT.GetAddress();
            txptr[6] = (int*)skybox.RIGHT.GetAddress();
            txptr[7] = (int*)skybox.RIGHT.GetAddress();
            txptr[10] = (int*)skybox.TOP.GetAddress();
            txptr[11] = (int*)skybox.TOP.GetAddress();
            txptr[8] = (int*)skybox.BOTTOM.GetAddress();
            txptr[9] = (int*)skybox.BOTTOM.GetAddress();

            GLMatrix projMat = GLMatrix.Perspective(90f, colorBuffer.Width, colorBuffer.Height);
            GLData gData = new GLData(colorBuffer.Width, colorBuffer.Height, projMat);

            float* bufAddr = (float*)cubeBuffer.GetAddress();
            int* colAddr = (int*)colorBuffer.GetAddress();

            DrawSkybox(bufAddr, colAddr, skybox.cubemap[0].Width, gData, cameraRotation, sptr, bsptr, txptr, sdptr);

            Marshal.FreeHGlobal(SkyboxFaceCountData);
            Marshal.FreeHGlobal(SkyboxData);
            Marshal.FreeHGlobal(SkyboxPointerBuffer);
            Marshal.FreeHGlobal(SkyboxTexturePointers);

            for (int i = 0; i < skybox.cubemap.Length; i++)
                skybox.cubemap[i].ReleaseLock();

            colorBuffer.ReleaseLock();
        }

        public static void FastFXAA(GLTexture outputBuffer, GLTexture inputBuffer)
        {
            outputBuffer.RequestLock();
            inputBuffer.RequestLock();

            if (outputBuffer.Stride != 4)
                throw new Exception("outputBuffer stride must be 32bpp!");

            if (inputBuffer.Stride != 4)
                throw new Exception("outputBuffer stride must be 32bpp!");

            if (inputBuffer.Width != outputBuffer.Width)
                throw new Exception("Input and output buffer widths are not the same!");

            if (inputBuffer.Height != outputBuffer.Height)
                throw new Exception("Input and output buffer heights are not the same!");

            //easier for debugging, -> breakpoint step in doesnt go to GetAddress();
            int* addr1 = (int*)outputBuffer.GetAddress();
            int* addr2 = (int*)inputBuffer.GetAddress();

            FXAA_PASS(addr1, addr2, inputBuffer.Width, inputBuffer.Height);

            outputBuffer.ReleaseLock();
            inputBuffer.ReleaseLock();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using xfcore.Shaders;
using xfcore.Buffers;
using xfcore.Blit;
using xfcore.Extras;

namespace xfcore
{
    public unsafe static partial class GL
    {
        #region PINVOKE

        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

        [DllImport("gdi32.dll")]
        static extern int SetDIBitsToDevice(IntPtr hdc, int XDest, int YDest, uint
           dwWidth, uint dwHeight, int XSrc, int YSrc, uint uStartScan, uint cScanLines,
           IntPtr lpvBits, [In] ref BITMAPINFO lpbmi, uint fuColorUse);

        [DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        static extern IntPtr MemSet(IntPtr dest, int c, int byteCount);

        [DllImport("kernel32.dll")]
        static extern void RtlZeroMemory(IntPtr dst, int length);

        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void ClearColor(int* iptr, int Width, int Height, int Color);
        #endregion

        #region PInvokeXFCore
        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int SizeCheck();

        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void Pass(void* Shader, int Width, int Height, int* sMem, int sSize, int* iInstr, int iSize, long xyPOS);

        [DllImport("AdvShaderEnvTest.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void ShaderCallFunction(int start, int stop, float* tris, float* dptr, byte* uDataVS, byte* uDataFS, byte** ptrPtrs, GLData pData, long FACE, long mode);


        #endregion

        static GL()
        {
            //Check pointer sizes are the same!
            if (!File.Exists("XFCore.dll"))
                throw new Exception("XFCore.dll was not found!");

            if (sizeof(int*) != 4) throw new Exception("Error: renderXF2 only supports 32bit applications!");
            if (SizeCheck() != 4) throw new Exception("Error: XFCore.dll is not 32bit!");
        }

        /// <summary>
        /// Ready the Graphics Library. Note that you will need to check the inner exception if a exception occurs
        /// </summary>
        public static void Initialize()
        {

        }

        static void memcpy(byte* dest, byte* src, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                dest[i] = src[i];
            }
        }
     
        public static void Blit(GLTexture Data, BlitData Destination)
        {
            Data.RequestLock();

            if (Data.Stride != 4)
                throw new Exception("Blitting anything but 32bpp buffers will result in terrible performance!");

            Destination.BINFO.bmiHeader.biWidth = Data.Width;
            Destination.BINFO.bmiHeader.biHeight = Data.Height;
            SetDIBitsToDevice(Destination.TargetDC, 0, 0,
                (uint)Data.Width, (uint)Data.Height, 0, 0, 0, (uint)Data.Height, Data.GetAddress(), ref Destination.BINFO, 0);

            Data.ReleaseLock();
        }

        public static void Clear(GLTexture TargetBuffer)
        {
            TargetBuffer.RequestLock();

            RtlZeroMemory(TargetBuffer.GetAddress(), TargetBuffer.Width * TargetBuffer.Height * TargetBuffer.Stride);
            TargetBuffer.ReleaseLock();
        }

        public static void Clear(GLTexture TargetBuffer, byte R, byte G, byte B)
        {
            int color = ((((((byte)255 << 8) | (byte)R) << 8) | (byte)G) << 8) | (byte)B;

            TargetBuffer.RequestLock();

            if (TargetBuffer.Stride != 4)
                throw new Exception("This function only clears 32bpp buffers!");

            ClearColor((int*)TargetBuffer.GetAddress(), TargetBuffer.Width, TargetBuffer.Height, color);
            TargetBuffer.ReleaseLock();   
        }

        public static void Clear(GLTexture TargetBuffer, Color4 color)
        {
            int col = ((((((byte)color.A << 8) | (byte)color.R) << 8) | (byte)color.G) << 8) | (byte)color.B;

            if (TargetBuffer.Stride != 4)
                throw new Exception("This function only clears 32bpp buffers!");

            TargetBuffer.RequestLock();
            
            ClearColor((int*)TargetBuffer.GetAddress(), TargetBuffer.Width, TargetBuffer.Height, col);

            TargetBuffer.ReleaseLock();
        }

        static void fRtlZeroMem(byte* ptr, int size)
        {
            for (int i = 0; i < size; i++)
                ptr[i] = 0;
        }


        public static void Draw(GLBuffer buffer, Shader shader, GLTexture depth, GLMatrix projectionMatrix, GLMode drawMode, int startIndex = 0, int stopIndex = int.MaxValue)
        {
            lock (shader.ThreadLock)
            {
                if (shader.isScreenSpace)
                        throw new Exception("Draw() cannot be used with a screenspace shader!");

                if (shader.readStride != buffer.stride)
                    throw new Exception("The buffer is not the same stride as the shader read stride!");

                //ADD BUFFER SIZE CHECKING
                int trisCount = ((buffer.Size / 4) / buffer.stride) / 3;

                if (startIndex < 0) throw new Exception("Start Index Cannot be less than Zero!");
                if (startIndex >= trisCount || stopIndex == 0) return;
                if (stopIndex > trisCount) stopIndex = trisCount;
                if (buffer.Size < 3 * buffer.stride)
                    throw new Exception("This function requires an entire triangle to draw!");

                GLTexture[] textureSlots = shader.GetTextureSlots();

                for (int i = 0; i < textureSlots.Length; i++)
                    if (textureSlots[i] == null)
                        throw new Exception("One of the assigned textures is null!");

                if (textureSlots.Length == 0)
                    throw new Exception("Atleast one Buffer must be assigned!");

                for (int i = 0; i < textureSlots.Length; i++)
                    textureSlots[i].RequestLock();

                int width = textureSlots[0].Width, height = textureSlots[0].Height;

                GLData drawConfig = new GLData(width, height, projectionMatrix);

                for (int i = 1; i < textureSlots.Length; i++)
                {
                    if (textureSlots[i].Height != height) throw new Exception("Height must be the same on all buffers!");
                    if (textureSlots[i].Width != width) throw new Exception("Width must be the same on all buffers!");
                }

                IntPtr ptrPtrs = Marshal.AllocHGlobal(textureSlots.Length * 4);
                GCHandle uniformDataVS = GCHandle.Alloc(shader.GetUniformVS(), GCHandleType.Pinned);
                GCHandle uniformDataFS = GCHandle.Alloc(shader.GetUniformFS(), GCHandleType.Pinned);

                byte* bptr = (byte*)ptrPtrs;

                fRtlZeroMem(bptr, textureSlots.Length * 4); //Zero it just in case something is wrong, so it crashes instantly.

                byte** PTRS = (byte**)bptr;
                for (int i = 0; i < textureSlots.Length; i++)
                    PTRS[i] = (byte*)textureSlots[i].GetAddress();

                //Call the shader
                //ShaderCallScreen(width, height, PTRS, (void*)uniformData.AddrOfPinnedObject());

                byte* uVS = (byte*)uniformDataVS.AddrOfPinnedObject();
                byte* uFS = (byte*)uniformDataVS.AddrOfPinnedObject();


                shader.ShaderCall(startIndex, stopIndex, (float*)buffer.HEAP_ptr, (float*)depth.GetAddress(), uVS, uFS, PTRS, drawConfig, 1, 0);

              //  ShaderCallFunction(startIndex, stopIndex, (float*)buffer.HEAP_ptr, (float*)depth.GetAddress(), uVS, uFS, PTRS, drawConfig, 0, 0);


                Marshal.FreeHGlobal(ptrPtrs);
                uniformDataVS.Free();
                uniformDataFS.Free();

                for (int i = 0; i < textureSlots.Length; i++)
                    textureSlots[i].ReleaseLock();
            }
        }

    }

    public enum GLMode
    {
        Triangle,
        Wireframe,
        Line
    }

    public class BlitData : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        internal BITMAPINFO BINFO;
        internal IntPtr TargetDC;
        internal IntPtr LinkedHandle;

        bool disposed = false;

        ~BlitData()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                ReleaseDC(LinkedHandle, TargetDC);
                disposed = true;
            }
        }

        public BlitData(Form TargetForm)
        {
            BINFO = new BITMAPINFO();
            BINFO.bmiHeader.biBitCount = 32; //BITS PER PIXEL
            BINFO.bmiHeader.biWidth = 1024; //filler width and height
            BINFO.bmiHeader.biHeight = 768;
            BINFO.bmiHeader.biPlanes = 1;
            unsafe
            {
                BINFO.bmiHeader.biSize = (uint)sizeof(BITMAPINFOHEADER);
            }

            IntPtr OutputHandle = TargetForm.Handle;

            LinkedHandle = OutputHandle;
            TargetDC = GetDC(OutputHandle);
        }

        public BlitData(Control TargetControl)
        {
            BINFO = new BITMAPINFO();
            BINFO.bmiHeader.biBitCount = 32; //BITS PER PIXEL
            BINFO.bmiHeader.biWidth = 1024; //filler width and height
            BINFO.bmiHeader.biHeight = 768;
            BINFO.bmiHeader.biPlanes = 1;
            unsafe
            {
                BINFO.bmiHeader.biSize = (uint)sizeof(BITMAPINFOHEADER);
            }

            IntPtr OutputHandle = TargetControl.Handle;

            LinkedHandle = OutputHandle;
            TargetDC = GetDC(OutputHandle);
        }

        public BlitData(IntPtr TargetHandle)
        {
            BINFO = new BITMAPINFO();
            BINFO.bmiHeader.biBitCount = 32; //BITS PER PIXEL
            BINFO.bmiHeader.biWidth = 1024; //filler width and height
            BINFO.bmiHeader.biHeight = 768;
            BINFO.bmiHeader.biPlanes = 1;
            unsafe
            {
                BINFO.bmiHeader.biSize = (uint)sizeof(BITMAPINFOHEADER);
            }

            IntPtr OutputHandle = TargetHandle;

            LinkedHandle = OutputHandle;
            TargetDC = GetDC(OutputHandle);
        }
    }

    public struct GLMatrix
    {
        internal float hFOV;
        internal float vFOV;

        internal float hSize;
        internal float vSize;

        internal float iValue;

        public float ZNear;
        public float ZFar;

        public static float FOVMod(float FOV, float aspectRatio)
        {
            const float deg2rads = (float)(Math.PI / 180d);
            return 2.0f * (float)Math.Atan(Math.Tan(FOV * 0.5f * deg2rads) / aspectRatio) / deg2rads;
        }

        GLMatrix(float vfov, float hfov, float vsize, float hsize, float iValue = 0)
        {
            vFOV = vfov;
            hFOV = hfov;

            vSize = vsize;
            hSize = hsize;

            this.iValue = iValue;

            ZNear = 0.03f;
            ZFar = 1000f;
        }

        public static GLMatrix Perspective(float vFOV, float hFOV)
        {
            if (vFOV <= 0 || vFOV >= 180) throw new Exception("Invalid vFOV");
            if (hFOV <= 0 || hFOV >= 180) throw new Exception("Invalid hFOV");

            return new GLMatrix(vFOV, hFOV, 0, 0, 0f);
        }

        public static GLMatrix Perspective(float vFOV, int viewportWidth, int viewportHeight)
        {
            if (vFOV <= 0 || vFOV >= 180) throw new Exception("Invalid FOV");
            if (viewportWidth <= 0 || viewportHeight <= 0) throw new Exception("Invalid width or height!");

            float aspectRatio = (float)viewportWidth / (float)viewportHeight;
            return new GLMatrix(vFOV, FOVMod(vFOV, aspectRatio), 0, 0, 0f);
        }

        public static GLMatrix Orthographic(float vSize, float hSize)
        {
            if (vSize <= 0) throw new Exception("Invalid vSize");
            if (hSize <= 0) throw new Exception("Invalid hSize");

            return new GLMatrix(0, 0, vSize, hSize, 1f);
        }

        public static GLMatrix Orthographic(float Size, int viewportWidth, int viewportHeight)
        {
            if (Size <= 0) throw new Exception("Invalid Size");
            if (viewportWidth <= 0 || viewportHeight <= 0) throw new Exception("Invalid width or height!");

            float aspectRatio = (float)viewportWidth / (float)viewportHeight;
            return new GLMatrix(0, 0, Size, Size / aspectRatio, 1f);
        }

        public static GLMatrix Mix(GLMatrix perspectiveMat, GLMatrix orthographicMat, float factorOfB)
        {
            if (perspectiveMat.iValue != 0)
                throw new Exception("perspectiveMat must be a perspective matrix!");

            if (orthographicMat.iValue != 1)
                throw new Exception("orthographicMat must be a orthographic matrix!");

            throw new Exception("Not Yet Supported!");

           return new GLMatrix(0, 0, 0, 0, 0);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RenderSettings
    {
        internal float farZ;
        internal float nearZ;
        internal int renderWidth;
        internal int renderHeight;
        internal float degFOV;
    }

    internal static class Serializer
    {
        internal static byte[] Serialize(object input)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, input);
                return ms.ToArray();
            }
        }

        internal static object DeSerialize(byte[] data)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(data, 0, data.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
        }
    }
}

namespace xfcore.Blit
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public BitmapCompressionMode biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RGBQUAD
    {
        public byte rgbBlue;
        public byte rgbGreen;
        public byte rgbRed;
        public byte rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public RGBQUAD bmiColors;
    }

    public enum BitmapCompressionMode : uint
    {
        BI_RGB = 0,
        BI_RLE8 = 1,
        BI_RLE4 = 2,
        BI_BITFIELDS = 3,
        BI_JPEG = 4,
        BI_PNG = 5
    }
}

namespace xfcore.Info
{
    public static class GLInfo
    {
        /// <summary>
        /// Gets the current RAM usage of all GLTextures and GLBuffers in bytes
        /// </summary>
        public static int RAMUsage
        {
            get { return GLTexture.TotalRAMUsage + GLBuffer.TotalRAMUsage; }
        }

        /// <summary>
        /// Gets the current RAM usage of all GLTextures and GLBuffers in Megabytes
        /// </summary>
        public static float RAMUsageMB
        {
            get { return (GLTexture.TotalRAMUsage + GLBuffer.TotalRAMUsage) / 1024f / 1024f; }
        }
    }
}
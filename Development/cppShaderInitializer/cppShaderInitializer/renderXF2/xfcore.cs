using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using xfcore.Shaders;
using xfcore.Buffers;
using xfcore.Blit;

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

        #endregion

        delegate void ShaderExecute();

        static GL()
        {
            //Check pointer sizes are the same!
            if (sizeof(int*) != 4) throw new Exception("Error: renderXF2 only supports 32bit applications!");
            if (SizeCheck() != 4) throw new Exception("Error: XFCore.dll is not 32bit!");
        }

        public static void Initialize()
        {
            
        }
        
        public static void Pass(Shader shader)
        {
            if (!shader.isScreenSpace)
                throw new Exception("You cannot use the Pass() function with screenspace shaders!");

            for (int i = 0; i < shader.FSD.Length; i++)
            {
                if (shader.FSD[i].Size == 0)
                    throw new Exception("Field Member \'" + shader.FSData[i].Name + "\' was not initialized!");
            }

            IntPtr StructData = Marshal.AllocHGlobal(shader.FSS);
            byte* sptr = (byte*)StructData;

            RtlZeroMemory(StructData, shader.FSS);

            List<GLTexture> lockable = new List<GLTexture>();
            List<GCHandle> PinnedItems = new List<GCHandle>();

            List<int> WidthCount = new List<int>(), HeightCount = new List<int>();
            List<int> AddMem = new List<int>(), AddPos = new List<int>(), AddSize = new List<int>();
            
            ShaderExecute del1 = delegate
            {
                bool RequestXY = false;
                int XYPos = -1;

                for (int i = 0; i < shader.FSD.Length; i++){
                    if (shader.FSD[i].CustomInOutUniform == (int)DataItemType.Uniform)
                    {
                        GCHandle Item = GCHandle.Alloc(shader.FSD[i].AssignedValue, GCHandleType.Pinned);
                        byte* bptr = (byte*)Item.AddrOfPinnedObject();

                        int size = Marshal.SizeOf(shader.FSD[i].AssignedValue);

                        for (int n = 0; n < size; n++)
                        {
                            sptr[shader.FSD[i].bytePosition + n] = bptr[n];
                        }

                        PinnedItems.Add(Item);
                    }
                    else if (shader.FSD[i].CustomInOutUniform == (int)DataItemType.In)
                    {
                        AddMem.Add((int)shader.FSD[i].AssignedTexture.HEAP_ptr);
                        AddPos.Add(shader.FSD[i].bytePosition);
                        AddSize.Add(shader.FSD[i].Size);

                        lockable.Add(shader.FSD[i].AssignedTexture);

                        WidthCount.Add(shader.FSD[i].AssignedTexture.Width);
                        HeightCount.Add(shader.FSD[i].AssignedTexture.Height);
                    }
                    else if (shader.FSD[i].CustomInOutUniform == (int)DataItemType.Out)
                    {
                        AddMem.Add((int)shader.FSD[i].AssignedTexture.HEAP_ptr);
                        AddPos.Add(shader.FSD[i].bytePosition);
                        AddSize.Add(shader.FSD[i].Size);

                        lockable.Add(shader.FSD[i].AssignedTexture);

                        WidthCount.Add(shader.FSD[i].AssignedTexture.Width);
                        HeightCount.Add(shader.FSD[i].AssignedTexture.Height);
                    }
                    else if (shader.FSD[i].CustomInOutUniform == (int)DataItemType.Variable)
                    {
                        if (!RequestXY)
                        {
                            RequestXY = true;
                            XYPos = shader.FSD[i].bytePosition;
                        }
                        else throw new Exception("Multiple Variable Types of the Same Assignment are NOT allowed");
                    }
                    else throw new Exception("Shader outline code has missing attributes!");
                }

                if (WidthCount.Any(o => o != WidthCount[0])) throw new Exception("All of the GLTexture outputs need to have identical sizes!");
                if (HeightCount.Any(o => o != HeightCount[0])) throw new Exception("All of the GLTexture outputs need to have identical sizes!");


                //these were split to ensure protected r/w memory error would occur
                IntPtr AddInstruction = Marshal.AllocHGlobal(AddMem.Count * 4 * 3);
                IntPtr VarInstruction = Marshal.AllocHGlobal(AddMem.Count * 4 * 3);

                RtlZeroMemory(AddInstruction, AddMem.Count * 4 * 3);
                RtlZeroMemory(VarInstruction, AddMem.Count * 4 * 2);

                int* iInstr = (int*)AddInstruction;
                int* vInstr = (int*)VarInstruction;

                for (int i = 0; i < AddMem.Count; i++)
                {
                    iInstr[i * 3 + 0] = AddPos[i];
                    iInstr[i * 3 + 1] = AddSize[i];
                    iInstr[i * 3 + 2] = AddMem[i];
                }
 
                Pass(shader.FSptr, WidthCount[0], HeightCount[0], (int*)StructData, shader.FSS, iInstr, AddMem.Count, XYPos);

                Marshal.FreeHGlobal(AddInstruction);
            };

            MultiLock(shader.FSD, del1);

          //  Pass(shader.FSptr, WidthCount[0], HeightCount[0], (int*)StructData, shader.FSS, iInstr, AddMem.Count, XYPos);

            for (int i = 0; i < PinnedItems.Count; i++)
                PinnedItems[i].Free();

            Marshal.FreeHGlobal(StructData);
            
        }

        static void Pass2(void* Shader, int Width, int Height, int* sMem, int sSize, int* iInstr, int iSize, int xyPOS)
        {
            int color = (((((byte)0) << 8) | (byte)0) << 8) | (byte)255;

            for (int h = 0; h < Height; ++h)
		    {
			    int hWidth = h * Width;
			    byte* bptr = stackalloc byte[sSize];
                byte** bbptr = (byte**)bptr;
                memcpy(bptr, (byte*)sMem, sSize);

			    for (int i = 0; i < iSize; ++i)
			    {
                    int* abc = (int*)(bptr + iInstr[i * 3]);
                    *abc = (int)((byte*)iInstr[i * 3 + 2] + iInstr[i * 3 + 1] * hWidth);

                   // byte** abc = (byte**)(bptr + iInstr[i * 3]);
                   // byte* hwd = (byte*)iInstr[i * 3 + 2] + iInstr[i * 3 + 1] * hWidth;
                  //  *abc = hwd;
			    }

			    for (int w = 0; w < Width; ++w)
			    {
                     int* p = ((int**)bptr)[0];

                    int c = (byte)(w / 1600f * 255f) + 256 * (byte)(0) + 65536 * (byte)(0);
                    *p = c;

                    for (int i = 0; i < iSize; ++i)
                    {
                        byte** abc = (byte**)(bptr + iInstr[i * 3]);
                        (*abc) += iInstr[i * 3 + 1];
                    }
			    }
		    }
        }

        static void Pass1(void* Shader, int Width, int Height, int* sMem, int sSize, int* iInstr, int iSize, int* oInstr, int oSize)
        {
            int iS = iSize * 3, oS = oSize * 3;

            Console.WriteLine(oInstr[2]);

            for (int h = 0; h < Height; ++h)
		    {
                int hWidth = h * Width;
			    byte* bptr = stackalloc byte[sSize];
			    memcpy(bptr, (byte*)sMem, sSize);

			    for (int w = 0; w < Width; ++w)
			    {


                    //Process Out's
                    for (int i = 0; i < oS; i += 3)
                    {
                        //	fmemcpy((unsigned char*)oInstr[i + 2] + hWidth * oInstr[i + 1] + oInstr[i + 1] * w, 
                        //		bptr + oInstr[i + 0], 
                        //	oInstr[i + 1]);

                        int* t = (int*)((byte*)oInstr[i + 2] + hWidth * oInstr[1] + oInstr[i + 1] * w);
                        *((int**)(bptr + oInstr[i])) = t;



                        throw new Exception();
                        t[0] = 0;
                    }            
			    }
		    }
        }

        static void memcpy(byte* dest, byte* src, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                dest[i] = src[i];
            }
        }

        public static void Draw(Shader shader, GLTexture depthBuffer)//, int StartTriangle, int StopTriangle, GLMode Mode)
        {
            for (int i = 0; i < shader.VSD.Length; i++)
            {
                if (shader.VSD[i].Size == 0 && !Attribute.IsDefined(shader.VSData[i], typeof(Shaders.Structs.VS_out)) && 
                    !Attribute.IsDefined(shader.VSData[i], typeof(Shaders.Structs.xout)))
                    throw new Exception("Field Member \'" + shader.VSData[i].Name + "\' was not initialized!");
            }
            
            for (int i = 0; i < shader.FSD.Length; i++)
            {
                if (shader.FSD[i].Size == 0 && !Attribute.IsDefined(shader.VSData[i], typeof(Shaders.Structs.xinp)))
                    throw new Exception("Field Member \'" + shader.FSData[i].Name + "\' was not initialized!");
            }

            IntPtr StructData = Marshal.AllocHGlobal(shader.FSS);
            byte* sptr = (byte*)StructData;

            RtlZeroMemory(StructData, shader.FSS);

            List<GLTexture> lockable = new List<GLTexture>();

            List<int> WidthCount = new List<int>(), HeightCount = new List<int>();
            List<int> AddMem = new List<int>(), AddPos = new List<int>(), AddSize = new List<int>();

            ShaderExecute del1 = delegate ()
            {
                bool RequestXY = false, RequestXYZ = false, RequestDepth = false;
                int XYPos = -1, XYZPos = -1, ZPos = -1;

                for (int i = 0; i < shader.VSD.Length; i++)
                {
                    if (shader.FSD[i].CustomInOutUniform == (int)DataItemType.Uniform)
                    {
                        GCHandle Item = GCHandle.Alloc(shader.FSD[i].AssignedValue, GCHandleType.Pinned);
                        byte* bptr = (byte*)Item.AddrOfPinnedObject();

                        int size = Marshal.SizeOf(shader.FSD[i].AssignedValue);

                        for (int n = 0; n < size; n++)
                            sptr[shader.FSD[i].bytePosition + n] = bptr[n];

                        Item.Free();
                    }
                    else if (shader.FSD[i].CustomInOutUniform == (int)DataItemType.In || shader.FSD[i].CustomInOutUniform == (int)DataItemType.Out)
                    {
                        AddMem.Add((int)shader.FSD[i].AssignedTexture.HEAP_ptr);
                        AddPos.Add(shader.FSD[i].bytePosition);
                        AddSize.Add(shader.FSD[i].Size);

                        lockable.Add(shader.FSD[i].AssignedTexture);

                        WidthCount.Add(shader.FSD[i].AssignedTexture.Width);
                        HeightCount.Add(shader.FSD[i].AssignedTexture.Height);
                    }
                    else if (shader.FSD[i].CustomInOutUniform == (int)DataItemType.Variable)
                    {
                        if (!RequestDepth)
                        {
                            RequestDepth = true;
                            ZPos = shader.FSD[i].bytePosition;
                        }
                        else throw new Exception("Multiple Variable Types of the Same Assignment are NOT allowed");

                    }
                    else throw new Exception("Shader outline code has missing attributes!");
                }
            };

        }

        public static void Blit(GLTexture Data, BlitData Destination)
        {
            lock (Data.ThreadLock)
            {
                if (Data.Stride != 4)
                    throw new Exception("Blitting anything but 32bpp buffers will result in terrible performance!");

                Destination.BINFO.bmiHeader.biWidth = Data.Width;
                Destination.BINFO.bmiHeader.biHeight = Data.Height;
                SetDIBitsToDevice(Destination.TargetDC, 0, 0,
                    (uint)Data.Width, (uint)Data.Height, 0, 0, 0, (uint)Data.Height, Data.HEAP_ptr, ref Destination.BINFO, 0);
            }
        }

        public static void Clear(GLTexture TargetBuffer)
        {
            lock (TargetBuffer.ThreadLock)
            {
                RtlZeroMemory(TargetBuffer.HEAP_ptr, TargetBuffer.Size * TargetBuffer.Stride);
            }
        }

        public static void Clear(GLTexture TargetBuffer, byte R, byte G, byte B)
        {
            int color = ((((((byte)255 << 8) | (byte)R) << 8) | (byte)G) << 8) | (byte)B;

            if (TargetBuffer.Stride != 4)
                throw new Exception("This function only clears 32bpp buffers!");

            lock (TargetBuffer.ThreadLock)
                ClearColor((int*)TargetBuffer.HEAP_ptr, TargetBuffer.Width, TargetBuffer.Height, color);
        }

        static void MultiLock(DataItem[] DItems, ShaderExecute pFunc, int index = 0)
        {
            if (index < DItems.Count())
            {
                if (DItems[index].AssignedTexture != null)
                {
                    lock (DItems[index].AssignedTexture.ThreadLock)
                    {
                        MultiLock(DItems, pFunc, index + 1);
                    }
                }
                else MultiLock(DItems, pFunc, index + 1);      
            }
            else
            {
                pFunc();
            }
        }

    }

    public enum GLMode
    { 
        Triangle,
        TriangleFlat,
        Wireframe
    }

    struct VSFSPass
    {
        int VSPosition;
        int FSPosition;
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
            unsafe{
                BINFO.bmiHeader.biSize = (uint)sizeof(BITMAPINFOHEADER);
            }

            IntPtr OutputHandle = TargetForm.Handle;

            LinkedHandle = OutputHandle;
            TargetDC = GetDC(OutputHandle);
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
        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void SetGLInfoMode(bool TC, bool PC);

        internal static int triangleCount;
        internal static int pixelCount;

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

        /// <summary>
        /// Gets the amount of triangles that have been rendered
        /// </summary>
        public static int TriangleCount
        {
            get { return Interlocked.CompareExchange(ref triangleCount, 0, 0); }
        }

        /// <summary>
        /// Gets the amount of pixels that have been rendered
        /// </summary>
        public static int PixelCount
        {
            get { return Interlocked.CompareExchange(ref pixelCount, 0, 0); }
        }

        /// <summary>
        /// Sets the Pixel/Triangle count logging mode
        /// </summary>
        /// <param name="enableTriangleCount">Value for logging triangle counts</param>
        /// <param name="enablePixelCount">Value for logging pixel fill rate</param>
        public static void SetDrawableLogging(bool enableTriangleCount, bool enablePixelCount)
        {
            SetGLInfoMode(enableTriangleCount, enableTriangleCount);
        }

        /// <summary>
        /// Resets The Triangle And Pixel Counters
        /// </summary>
        public static void ResetCount()
        {
            Interlocked.Exchange(ref pixelCount, 0);
            Interlocked.Exchange(ref triangleCount, 0);
        }
    }
}
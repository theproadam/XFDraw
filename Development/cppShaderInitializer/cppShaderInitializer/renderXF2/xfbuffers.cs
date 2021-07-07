using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;

namespace xfcore.Buffers
{
    public class GLTexture
    {
        internal IntPtr HEAP_ptr;
        internal int Width;
        internal int Height;
        internal int Size;
        internal int Stride;

        bool disposed = false;

        static int Texture_RAM_Usage = 0;

        public static int TotalRAMUsage
        {
            get { return Interlocked.CompareExchange(ref Texture_RAM_Usage, 0, 0); }
        }

        public static float TotalRAMUsageMB
        {
            get { return Interlocked.CompareExchange(ref Texture_RAM_Usage, 0, 0) / 1024f / 1024f; }
        }

        internal object ThreadLock = new object();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GLTexture()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (ThreadLock)
                if (!disposed)
                {
                    Marshal.FreeHGlobal(HEAP_ptr);
                    disposed = true;
                }
        }

        public GLTexture(int width, int height, int ByteStride = 4)
        {
            if (width <= 2 || height <= 2)
                throw new Exception("GLTexture must be bigger than 2x2!");

            Width = width;
            Height = height;
            Size = width * height;
            Stride = ByteStride;
            Interlocked.Add(ref Texture_RAM_Usage, Size * Stride);
            HEAP_ptr = Marshal.AllocHGlobal(Width * Height * ByteStride);         
        }

        public GLTexture(int width, int height, Type ObjectStride)
        {
            if (width <= 2 || height <= 2)
                throw new Exception("GLTexture must be bigger than 2x2!");

            Width = width;
            Height = height;
            Size = width * height;
            Stride = Marshal.SizeOf(ObjectStride);
            Interlocked.Add(ref Texture_RAM_Usage, Size * Stride);
            HEAP_ptr = Marshal.AllocHGlobal(Width * Height * Stride);            
        }

        public void Resize(int newWidth, int newHeight)
        {
            if (newWidth <= 2 || newHeight <= 2)
                throw new Exception("GLTexture must be bigger than 2x2!");

            lock (ThreadLock)
            {
                Interlocked.Add(ref Texture_RAM_Usage, -Size * Stride);
                Width = newWidth;
                Height = newHeight;
                Size = newWidth * newHeight;
                
                HEAP_ptr = Marshal.ReAllocHGlobal(HEAP_ptr, (IntPtr)(newWidth * newHeight * Stride));
                Interlocked.Add(ref Texture_RAM_Usage, Size * Stride);
            }
        }

    }

    public unsafe class GLBuffer
    {
        internal IntPtr HEAP_ptr;
        internal int Size;
        float* fptr;

        static int Buffer_RAM_Usage = 0;

        bool disposed = false;
        internal object ThreadLock = new object();
        internal int stride;
        internal int elementSize;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GLBuffer()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (ThreadLock)
                if (!disposed)
                {
                    Marshal.FreeHGlobal(HEAP_ptr);
                    disposed = true;
                }
        }

        public static int TotalRAMUsage
        {
            get { return Interlocked.CompareExchange(ref Buffer_RAM_Usage, 0, 0); }
        }

        public static float TotalRAMUsageMB
        {
            get { return Interlocked.CompareExchange(ref Buffer_RAM_Usage, 0, 0) / 1024f / 1024f; }
        }

        public float this[int i]
        {
            get {
                if (i >= 0 || i < Size)
                    return fptr[i];
                else throw new IndexOutOfRangeException(); 
            }
            set
            {
                if (i >= 0 || i < Size)
                    fptr[i] = value;
                else throw new IndexOutOfRangeException();
            }
        }

        public GLBuffer(int size, int Stride = 3)
        {
            if (size <= 0) throw new Exception("Size must be bigger than zero!");
            if (size % Stride != 0) throw new Exception("Invalid Stride OR Size!");
            if (size % 4 != 0) throw new Exception("GLBuffer only supports FP32 numbers!");


          //  if (elementSize != 4) throw new Exception("Only FP32 Buffers are allowed!");

            stride = Stride;
            Size = size;
            Interlocked.Add(ref Buffer_RAM_Usage, Size);
            HEAP_ptr = Marshal.AllocHGlobal(size);
            fptr = (float*)HEAP_ptr;
        }

        public GLBuffer(float[] Source, int Stride = 3)
        {
            if (Source == null) throw new ArgumentNullException();

            if (Source.Length <= 0) throw new Exception("Size must be bigger than zero!");
            if (Source.Length % Stride != 0) throw new Exception("Invalid Stride OR Size!");

            stride = Stride;
            Size = Source.Length * 4;
            Interlocked.Add(ref Buffer_RAM_Usage, Size);
            HEAP_ptr = Marshal.AllocHGlobal(Size);
            fptr = (float*)HEAP_ptr;

            for (int i = 0; i < Source.Length; i++)
            {
                fptr[i] = Source[i];
            }
        }

        public void Resize(int size, int newStride = 3)
        {
            lock (ThreadLock)
            {
                Interlocked.Add(ref Buffer_RAM_Usage, -size);
                Size = size;

                HEAP_ptr = Marshal.ReAllocHGlobal(HEAP_ptr, (IntPtr)(Size));
                fptr = (float*)HEAP_ptr;
                stride = newStride;
                Interlocked.Add(ref Buffer_RAM_Usage, Size);
            }
        }
    }
}

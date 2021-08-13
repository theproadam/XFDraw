using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using xfcore.Buffers;
using xfcore.Extras;
using System.Diagnostics;
using xfcore.Info;

namespace xfcore.Debug
{
    public unsafe static class GLDebug
    {
        #region PINVOKE
        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void WireframeDebug(int* iptr, float* p, int count, int stride, int iColor, Vector3 co, Vector3 si, Vector3 ca, RenderSettings rconfig, int* P, int* T);

        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void FillFlatDebug(int* iptr, float* dptr, float* p, int count, int stride, int iColor, Vector3 co, Vector3 si, Vector3 ca, RenderSettings rconfig, int FC, int* P, int* T);

        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void PhongBase(int* iptr, float* dptr, float* p, int count, int stride, Vector3 co, Vector3 si, Vector3 ca, RenderSettings rconfig, PhongConfig pc, int FC);

        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void DepthFill(float* dptr, float* p, int count, int stride, Vector3 co, Vector3 si, Vector3 ca, RenderSettings rconfig, int FC);


        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void SetParallelizationMode(bool useOpenMP, int LongThreadCount);

        [DllImport("kernel32.dll")]
        static extern void RtlZeroMemory(IntPtr dst, int length);
        #endregion


        public static void DrawWireframe(GLBuffer Buffer, GLTexture target, Vector3 camPos, Vector3 camRot)
        {
            // Vector3 camRot = new Vector3(0, 0, 0);
            int color = ((((((byte)255 << 8) | (byte)255) << 8) | (byte)255) << 8) | (byte)255;

            target.RequestLock();

            lock (Buffer.ThreadLock)
            {
                int stride = Buffer.stride;
                int divV = (4 * 3 * Buffer.stride);

                if (target.Stride != 4) throw new Exception("32bpp target required!");
                if (Buffer._size % divV != 0) throw new Exception("Buffer is of invalid size!");

                RenderSettings RS = new RenderSettings();
                RS.degFOV = 90f;
                RS.farZ = 1000f;
                RS.nearZ = 1f;
                RS.renderWidth = target.Width;
                RS.renderHeight = target.Height;

                int PC = 0;
                int TC = 0;

                WireframeDebug((int*)target.GetAddress(), (float*)Buffer.HEAP_ptr, Buffer._size / divV, stride, color, GetCos(camRot), GetSin(camRot), camPos, RS, &PC, &TC);

              //  Interlocked.Add(ref GLInfo.pixelCount, PC);
              //  Interlocked.Add(ref GLInfo.triangleCount, TC);

            }

            target.ReleaseLock();
        }

        public static void DrawFlatFill(GLBuffer Buffer, GLTexture target, GLTexture depth, Vector3 camPos, Vector3 camRot, bool mode)
        {
            // Vector3 camRot = new Vector3(0, 0, 0);
            int color = ((((((byte)255 << 8) | (byte)0) << 8) | (byte)0) << 8) | (byte)255;

            target.RequestLock();
            depth.RequestLock();

            lock (Buffer.ThreadLock)
            {
                int stride = Buffer.stride;
                int divV = (4 * 3 * Buffer.stride);

                if (target.Stride != 4) throw new Exception("32bpp target required!");
                if (depth.Stride != 4) throw new Exception("32bpp target required!");

                if (depth.Height != target.Height || depth.Width != target.Width)
                    throw new Exception("Target and Depth must be of the same dimensions!");

                int bFCull = 2;

                if (Buffer._size % divV != 0) throw new Exception("Buffer is of invalid size!");

                RenderSettings RS = new RenderSettings();
                RS.degFOV = 90f;
                RS.farZ = 1000f;
                RS.nearZ = 1f;
                RS.renderWidth = target.Width;
                RS.renderHeight = target.Height;

                int PC = 0;
                int TC = 0;

                //  if (mode)
                FillFlatDebug((int*)target.GetAddress(), (float*)depth.GetAddress(), (float*)Buffer.HEAP_ptr, Buffer._size / divV, stride, color, GetCos(camRot), GetSin(camRot), camPos, RS, bFCull, &PC, &TC);
                //    else
                //         FillFlatDebug2((int*)target.HEAP_ptr, (float*)depth.HEAP_ptr, (float*)Buffer.HEAP_ptr, Buffer.Size / divV, stride, color, GetCos(camRot), GetSin(camRot), camPos, RS);


              //  Interlocked.Add(ref GLInfo.pixelCount, PC);
              //  Interlocked.Add(ref GLInfo.triangleCount, TC);

            }

            target.ReleaseLock();
            depth.RequestLock();
        }

        public static void DrawDepth(GLBuffer Buffer, GLTexture depth, Vector3 camPos, Vector3 camRot)
        {

        }

        public static void DrawPhong(GLBuffer Buffer, GLTexture target, GLTexture depth, Vector3 camPos, Vector3 camRot, PhongConfig pc)
        {
            target.RequestLock();
            depth.RequestLock();

            int stride = Buffer.stride;
            if (stride != 6) throw new Exception("DrawPhong only support stride as 6");
            int divV = (4 * 3 * Buffer.stride);

            if (target.Stride != 4) throw new Exception("32bpp target required!");
            if (depth.Stride != 4) throw new Exception("32bpp target required!");

            if (depth.Height != target.Height || depth.Width != target.Width)
                throw new Exception("Target and Depth must be of the same dimensions!");

            int bFCull = 1;

            if (Buffer._size % divV != 0) throw new Exception("Buffer is of invalid size!");

            RenderSettings RS = new RenderSettings();
            RS.degFOV = 90f;
            RS.farZ = 1000f;
            RS.nearZ = 1f;
            RS.renderWidth = target.Width;
            RS.renderHeight = target.Height;

            //throw new Exception();
            PhongBase((int*)target.GetAddress(), (float*)depth.GetAddress(), (float*)Buffer.HEAP_ptr, Buffer._size / divV, stride, GetCos(camRot), GetSin(camRot), camPos, RS, pc, bFCull);
            //Console.WriteLine("waiting");

            target.ReleaseLock();
            depth.ReleaseLock();
        }

        public static void FillDepth(GLBuffer Buffer, GLTexture depth, Vector3 camPos, Vector3 camRot)
        {
            depth.RequestLock();

            int stride = Buffer.stride;

            int divV = (4 * 3 * Buffer.stride);

            if (depth.Stride != 4) throw new Exception("32bpp depth required!");
            int bFCull = 0;

            if (Buffer._size % divV != 0) throw new Exception("Buffer is of invalid size!");

            RenderSettings RS = new RenderSettings();
            RS.degFOV = 90f;
            RS.farZ = 1000f;
            RS.nearZ = 1f;
            RS.renderWidth = depth.Width;
            RS.renderHeight = depth.Height;

            DepthFill((float*)depth.GetAddress(), (float*)Buffer.HEAP_ptr, Buffer._size / divV, stride, GetCos(camRot), GetSin(camRot), camPos, RS, bFCull);

            depth.ReleaseLock();
        }

        public static void DepthToColor(GLTexture colorBuffer, GLTexture depthBuffer, float VisualScale)
        {
            colorBuffer.RequestLock();
            depthBuffer.RequestLock();

            if (colorBuffer.Stride != 4) throw new Exception("32bpp color required!");
            if (depthBuffer.Stride != 4) throw new Exception("32bpp depth required!");

            if (colorBuffer.Height != depthBuffer.Height || colorBuffer.Width != depthBuffer.Width)
                throw new Exception("Target and Depth must be of the same dimensions!");

            int* iptr = (int*)colorBuffer.GetAddress();
            float* dptr = (float*)depthBuffer.GetAddress();



            int wsd = depthBuffer.Width;
            for (int i = 0; i < depthBuffer.Height * depthBuffer.Width; i++)
            {
               // iptr[i] = (byte)(dptr[i] * VisualScale) + 256 * (byte)(dptr[i] * VisualScale) + (byte)(dptr[i] * VisualScale) * 65536;
                iptr[i] = (((((255 << 8) | (byte)(dptr[i] * VisualScale)) << 8) | (byte)(dptr[i] * VisualScale)) << 8) | (byte)(byte)(dptr[i] * VisualScale);
            }



            colorBuffer.ReleaseLock();
            depthBuffer.ReleaseLock();
        }

        public static void DepthToColorMod(GLTexture colorBuffer, GLTexture depthBuffer, float VisualScale)
        {
            colorBuffer.RequestLock();
            depthBuffer.RequestLock();

            if (colorBuffer.Stride != 4) throw new Exception("32bpp color required!");
            if (depthBuffer.Stride != 4) throw new Exception("32bpp depth required!");

            if (colorBuffer.Height != depthBuffer.Height || colorBuffer.Width != depthBuffer.Width)
                throw new Exception("Target and Depth must be of the same dimensions!");

            int* iptr = (int*)colorBuffer.GetAddress();
            float* dptr = (float*)depthBuffer.GetAddress();

            float x_mod = 1.0f / colorBuffer.Width * 255.0f;
            float y_mod = 1.0f / colorBuffer.Height * 255.0f;

            Parallel.For(0, colorBuffer.Height, h => {
                float* depth = dptr + colorBuffer.Width * h;
                int* color = iptr + colorBuffer.Width * h;

                for (int i = 0; i < colorBuffer.Width; i++)
                {
                  //  color[i] = (((((255 << 8) | (byte)(depth[i] * VisualScale)) << 8) | (byte)(depth[i] * VisualScale) << 8) | (byte)(depth[i] * VisualScale));
                  //  color[i] = 1203493209 + h * 100;
                    color[i] = (((((255 << 8) | (byte)(i * x_mod)) << 8) | (byte)(h * y_mod)) << 8) | (byte)(depth[i] * VisualScale);
                }
            });



            colorBuffer.ReleaseLock();
            depthBuffer.ReleaseLock();
        }


        public static void NormalsToColor(GLTexture colorBuffer, GLTexture normalBuffer)
        {
            colorBuffer.RequestLock();
            normalBuffer.RequestLock();

            if (colorBuffer.Stride != 4) throw new Exception("32bpp color required!");
            if (normalBuffer.Stride != 12) throw new Exception("vec3 depth required!");

            if (colorBuffer.Height != normalBuffer.Height || colorBuffer.Width != normalBuffer.Width)
                throw new Exception("Target and Depth must be of the same dimensions!");

            int* iptr = (int*)colorBuffer.GetAddress();
            Vector3* dptr = (Vector3*)normalBuffer.GetAddress();

            int wsd = normalBuffer.Width;
            for (int i = 0; i < normalBuffer.Height * normalBuffer.Width; i++)
            {
                // iptr[i] = (byte)(dptr[i] * VisualScale) + 256 * (byte)(dptr[i] * VisualScale) + (byte)(dptr[i] * VisualScale) * 65536;
               // iptr[i] = (((((255 << 8) | (byte)(dptr[i] * VisualScale)) << 8) | (byte)(dptr[i] * VisualScale)) << 8) | (byte)(byte)(dptr[i] * VisualScale);

                iptr[i] = (((((255 << 8) | (byte)(dptr[i].x * 127.5f + 127.5f)) << 8) | (byte)(dptr[i].y * 127.5f + 127.5f)) << 8) | (byte)(dptr[i].z * 127.5f + 127.5f);
            }



            colorBuffer.ReleaseLock();
            normalBuffer.ReleaseLock();
        }

        public static void PositionToColor(GLTexture colorBuffer, GLTexture normalBuffer, float scale)
        {
            colorBuffer.RequestLock();
            normalBuffer.RequestLock();

            if (colorBuffer.Stride != 4) throw new Exception("32bpp color required!");
            if (normalBuffer.Stride != 12) throw new Exception("vec3 depth required!");

            if (colorBuffer.Height != normalBuffer.Height || colorBuffer.Width != normalBuffer.Width)
                throw new Exception("Target and Depth must be of the same dimensions!");

            int* iptr = (int*)colorBuffer.GetAddress();
            Vector3* dptr = (Vector3*)normalBuffer.GetAddress();

            for (int i = 0; i < normalBuffer.Height * normalBuffer.Width; i++)
            {
                iptr[i] = (((((255 << 8) | (byte)(dptr[i].x * scale)) << 8) | (byte)(dptr[i].y * scale)) << 8) | (byte)(dptr[i].z * scale);
            }



            colorBuffer.ReleaseLock();
            normalBuffer.ReleaseLock();
        }





        #region Debuggable
        static void FillFlatDebug2(int* iptr, float* dptr, float* p, int count, int stride, int iColor, Vector3 co, Vector3 si, Vector3 ca, RenderSettings rconfig)
        {
            const float M_PI = 3.14159265358f;

            float radsFOV = rconfig.degFOV * M_PI / 180.0f;

            float nearZ = rconfig.nearZ, farZ = rconfig.farZ;
            float fovCoefficient = (float)Math.Tan((M_PI / 2.0f) - (radsFOV / 2.0f));
            float hFovCoefficient = ((float)rconfig.renderWidth / (float)rconfig.renderHeight) * (float)Math.Tan((M_PI / 2.0f) - (radsFOV / 2.0f));

            float tanVert = (float)Math.Tan((radsFOV / 2.0f)) * (1.0f - 0.0f);
            float tanHorz = (float)Math.Tan((radsFOV) / 2.0f) * ((float)rconfig.renderHeight / (float)rconfig.renderWidth) * (1.0f - 0.0f);

            float rw = (rconfig.renderWidth - 1.0f) / 2.0f, rh = (rconfig.renderHeight - 1.0f) / 2.0f;

            float fw = rw * fovCoefficient;
            float fh = rh * hFovCoefficient;

            float oh = 0;
            float ow = 0;

            int RW = rconfig.renderWidth;
            int RH = rconfig.renderHeight;

            bool FACE_CULL = true;
            bool CULL_FRONT = false;

            int s1 = stride * 3;

            int T_COUNT = 0;

            float oValue = 0;

            //  for (int index = 0; index < count; index++)
            Parallel.For(0, count, index =>
            {
                ExecMethod(index, iptr, dptr, p, ca, co, si, stride, tanVert, tanHorz, rw, rh, fw, fh, s1, RW, RH, oValue, FACE_CULL, CULL_FRONT, nearZ, farZ, oh, ow);
            });
        }

        static void ExecMethod(int index, int* iptr, float* dptr, float* p, Vector3 ca, Vector3 co, Vector3 si,
            int stride, float tanVert, float tanHorz, float rw, float rh, float fw, float fh, int s1, int RW, int RH, float oValue, bool FACE_CULL, bool CULL_FRONT, float nearZ, float farZ, float oh, float ow)
        {
            float* VERTEX_DATA = stackalloc float[12];

            int BUFFER_SIZE = 3;

            for (int b = 0; b < 3; b++)
            {
                float X = *(p + (index * s1 + b * stride)) - ca.x;
                float Y = *(p + (index * s1 + b * stride + 1)) - ca.y;
                float Z = *(p + (index * s1 + b * stride + 2)) - ca.z;

                float fiX = X * co.z - Z * si.z;
                float fiZ = Z * co.z + X * si.z;
                float ndY = Y * co.y + fiZ * si.y;

                //Returns the newly rotated Vector
                *(VERTEX_DATA + b * 3 + 0) = fiX * co.x - ndY * si.x;
                *(VERTEX_DATA + b * 3 + 1) = ndY * co.x + fiX * si.x;
                *(VERTEX_DATA + b * 3 + 2) = fiZ * co.y - Y * si.y;
            }
            //TODO: Replace RTL_ZERO_MEMORY with a simple loop, it should be much faster

            // bool* AP = (bool*)alloca(BUFFER_SIZE + 12);
            bool* AP = stackalloc bool[BUFFER_SIZE + 12];


            #region NearPlaneCFG
            int v = 0;


            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] < nearZ)
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region NearPlane
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];

                int API = 0;

                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }

                BUFFER_SIZE = API / 3;
                VERTEX_DATA = strFLT;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }

            #endregion

            #region RightFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * tanVert + ow < VERTEX_DATA[i * 3])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;
            #endregion

            #region RightFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert, ow);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, tanVert, ow);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert, ow);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, tanVert, ow);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region LeftFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * -tanVert - ow > VERTEX_DATA[i * 3])
                {
                    AP[i] = true;
                    v++;
                }

            }

            if (v == BUFFER_SIZE)
                return;
            #endregion

            #region LeftFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, -ow);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, -ow);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, -ow);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, -ow);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region TopFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * tanHorz + oh < VERTEX_DATA[i * 3 + 1])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region TopFOV

            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz, oh);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz, oh);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz, oh);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz, oh);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);


            }

            #endregion

            #region BottomFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * -tanHorz - oh > VERTEX_DATA[i * 3 + 1])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region BottomFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];

                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, -oh);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, -oh);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, -oh);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, -oh);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
            }
            #endregion


            int yMax = 0;
            int yMin = RH;

            #region CameraSpaceToScreenSpace
            if (true)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * 3 + 0] = rw + (VERTEX_DATA[im * 3 + 0] / VERTEX_DATA[im * 3 + 2]) * fw;
                    VERTEX_DATA[im * 3 + 1] = rh + (VERTEX_DATA[im * 3 + 1] / VERTEX_DATA[im * 3 + 2]) * fh;
                    VERTEX_DATA[im * 3 + 2] = 1f / VERTEX_DATA[im * 3 + 2];

                    if (VERTEX_DATA[im * 3 + 1] > yMax) yMax = (int)VERTEX_DATA[im * 3 + 1];
                    if (VERTEX_DATA[im * 3 + 1] < yMin) yMin = (int)VERTEX_DATA[im * 3 + 1];
                }

            #endregion

            #region FaceCulling
            if (FACE_CULL)
            {
                float A = BACKFACECULL3(VERTEX_DATA);
                if (CULL_FRONT && A > 0) return;
                else if (!CULL_FRONT && A < 0) return;
            }
            #endregion

            int BGR = 194844;
            //  byte* bBGR = (byte*)&BGR;


            float slopeZ;
            float bZ;
            float s;

            if (yMax >= RH) yMax = RH - 1;
            if (yMin < 0) yMin = 0;


            float* Intersects = stackalloc float[4];

            float* FROM;
            float* TO;

            int FromX;
            int ToX;

            int* RGB_iptr;
            float* Z_fptr;

            float zBegin;

            for (int i = yMin; i <= yMax; ++i)
            {
                if (ScanLine(i, VERTEX_DATA, BUFFER_SIZE, Intersects))
                {
                    if (Intersects[0] > Intersects[2])
                    {
                        TO = Intersects;
                        FROM = Intersects + 2;
                    }
                    else
                    {
                        FROM = Intersects;
                        TO = Intersects + 2;
                    }

                    FromX = (int)FROM[0];
                    ToX = (int)TO[0];

                    #region Z_Interpolation
                    slopeZ = (FROM[1] - TO[1]) / (FROM[0] - TO[0]);
                    bZ = -slopeZ * FROM[0] + FROM[1];
                    #endregion

                    #region BufferOverflowProtection
                    if (ToX >= RW) ToX = RW - 1;
                    if (FromX < 0) FromX = 0;
                    #endregion

                    RGB_iptr = iptr + i * RW;
                    Z_fptr = dptr + i * RW;

                    zBegin = slopeZ * (float)(FromX + 1) + bZ;

                    for (int o = FromX + 1; o <= ToX; ++o)
                    {
                        if (true) s = farZ - (1f / zBegin - oValue);
                        else s = farZ - zBegin;

                        zBegin += slopeZ;

                        if (Z_fptr[o] > s) continue;
                        Z_fptr[o] = s;
                        RGB_iptr[o] = (int)s * 8 + 205640;

                        //  RGB_iptr++;// = BGR;

                    }


                }
            }
        }

        static float BACKFACECULL3(float* VERTEX_DATA)
        {
            return ((VERTEX_DATA[3]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[7]) - (VERTEX_DATA[1])) - ((VERTEX_DATA[6]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[4]) - (VERTEX_DATA[1]));
        }

        static bool ScanLine(int Line, float* TRIS_DATA, int TRIS_SIZE, float* Intersects)
        {
            int IC = 0;
            for (int i = 0; i < TRIS_SIZE; i++)
            {
                if (TRIS_DATA[i * 3 + 1] <= Line)
                {
                    if (i == 0 && TRIS_DATA[(TRIS_SIZE - 1) * 3 + 1] >= Line)
                    {
                        LIP(Intersects, IC, TRIS_DATA, TRIS_SIZE - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }
                    else if (i > 0 && TRIS_DATA[(i - 1) * 3 + 1] >= Line)
                    {
                        LIP(Intersects, IC, TRIS_DATA, i - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }
                }
                else if (TRIS_DATA[i * 3 + 1] > Line)
                {
                    if (i == 0 && TRIS_DATA[(TRIS_SIZE - 1) * 3 + 1] <= Line)
                    {
                        LIP(Intersects, IC, TRIS_DATA, TRIS_SIZE - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }
                    else if (i > 0 && TRIS_DATA[(i - 1) * 3 + 1] <= Line)
                    {
                        LIP(Intersects, IC, TRIS_DATA, i - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }
                }
            }


            return IC == 2;
        }

        static void LIP(float* XR, int I, float* V_DATA, int A, int B, int LinePos)
        {
            float X = 0;
            float Z = 0;

            A *= 3;
            B *= 3;

            if (V_DATA[A + 1] == LinePos)
            {
                XR[I * 2] = V_DATA[A];
                XR[I * 2 + 1] = V_DATA[A + 2];
                return;
            }

            if (V_DATA[B + 1] == LinePos)
            {
                XR[I * 2] = V_DATA[B];
                XR[I * 2 + 1] = V_DATA[B + 2];
                return;
            }

            if (V_DATA[A + 1] - V_DATA[B + 1] != 0)
            {
                float slope = (V_DATA[A] - V_DATA[B]) / (V_DATA[A + 1] - V_DATA[B + 1]);
                float b = -slope * V_DATA[A + 1] + V_DATA[A];
                X = slope * LinePos + b;

                float slopeZ = (V_DATA[A + 2] - V_DATA[B + 2]) / (V_DATA[A + 1] - V_DATA[B + 1]);
                float bZ = -slopeZ * V_DATA[A + 1] + V_DATA[A + 2];
                Z = slopeZ * LinePos + bZ;
            }
            else
            {
                //throw new Exception("il fix this later");
                //this shoud not occur!
            }

            XR[I * 2] = X;
            XR[I * 2 + 1] = Z;
        }

        static void SIP(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, float oW)
        {
            float X;
            float Y;
            float Z;

            A *= 3;
            B *= 3;

            float s1 = VD[A + 0] - VD[B + 0];
            float s2 = VD[A + 2] - VD[B + 2];
            s1 *= s1;
            s2 *= s2;
            //TODO clean this code up!

            if (s2 > s1)
            {
                float slope = (VD[A] - VD[B]) / (VD[A + 2] - VD[B + 2]);
                float b = -slope * VD[A + 2] + VD[A];

                float V = (b - oW) / (TanSlope - slope);

                X = V * slope + b;
                Z = V;
            }
            else
            {
                float slope = (VD[A + 2] - VD[B + 2]) / (VD[A] - VD[B]);
                float b = -slope * VD[A] + VD[A + 2];

                Z = (slope * oW + b) / (1.0f - slope * TanSlope);
                X = TanSlope * Z + oW;
            }


            //FLOATING POINT PRECESION ISSUES WITH X - Y != 0 BUT RATHER A VERY VERY SMALL NUMBER
            //SOLUTION INTERPOLATE BASED OF LARGEST NUMBER
            if (s1 > s2)
            {
                float slope = (VD[A + 1] - VD[B + 1]) / (VD[A] - VD[B]);
                float b = -slope * VD[A] + VD[A + 1];

                Y = slope * X + b;
            }
            else
            {
                float slope = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
                float b = -slope * VD[A + 2] + VD[A + 1];

                Y = slope * Z + b;
            }

            TA[INDEX] = X;
            TA[INDEX + 1] = Y;
            TA[INDEX + 2] = Z;
        }

        static void SIPH(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, float oH)
        {
            float X;
            float Y;
            float Z;

            A *= 3;
            B *= 3;

            float s1 = VD[A + 1] - VD[B + 1];
            float s2 = VD[A + 2] - VD[B + 2];
            s1 *= s1;
            s2 *= s2;

            if (s2 > s1)
            {
                float slope = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
                float b = -slope * VD[A + 2] + VD[A + 1];

                float V = (b - oH) / (TanSlope - slope);

                Y = V * slope + b;
                Z = V;
            }
            else
            {
                float slope = (VD[A + 2] - VD[B + 2]) / (VD[A + 1] - VD[B + 1]);
                float b = -slope * VD[A + 1] + VD[A + 2];

                Z = (slope * oH + b) / (1.0f - slope * TanSlope);
                Y = TanSlope * Z + oH;
            }

            //Floating point precision errors require this code:
            if (s1 > s2)
            {
                float slope = (VD[A] - VD[B]) / (VD[A + 1] - VD[B + 1]);
                float b = -slope * VD[A + 1] + VD[A];

                X = slope * Y + b;
            }
            else
            {
                float slope = (VD[A] - VD[B]) / (VD[A + 2] - VD[B + 2]);
                float b = -slope * VD[A + 2] + VD[A];

                X = slope * Z + b;
            }

            TA[INDEX] = X;
            TA[INDEX + 1] = Y;
            TA[INDEX + 2] = Z;
        }

        static void FIP(float* TA, int INDEX, float* VD, int A, int B, float LinePos)
        {
            float X = 0;
            float Y = 0;

            A *= 3;
            B *= 3;

            if (VD[A + 2] - VD[B + 2] != 0)
            {
                float slopeY = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
                float bY = -slopeY * VD[A + 2] + VD[A + 1];
                Y = slopeY * LinePos + bY;

                float slopeX = (VD[A + 0] - VD[B + 0]) / (VD[A + 2] - VD[B + 2]);
                float bX = -slopeX * VD[A + 2] + VD[A + 0];
                X = slopeX * LinePos + bX;
            }
            else
            {
                throw new Exception("Please give a stack trace, if this exception ever occur!");
            }

            TA[INDEX] = X;
            TA[INDEX + 1] = Y;
            TA[INDEX + 2] = LinePos;
        }

        static void DrawLine(int* iptr, int rw, int rh, int diValue, float fromX, float fromY, float toX, float toY)
        {
            if (fromX == toX & fromY == toY)
                return;

            // Buffer OverFlow Protection will still be needed regardless how polished the code is...
            float aa = (fromX - toX);
            float ba = (fromY - toY);

            if (aa * aa > ba * ba)
            {
                float slope = (fromY - toY) / (fromX - toX);
                float b = -slope * fromX + fromY;

                if (fromX < toX)
                    for (int i = (int)fromX; i <= toX; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (i < 0 || tY < 0 || tY >= rh || i >= rw) continue;

                        *(iptr + rw * tY + i) = diValue;
                    }
                else
                    for (int i = (int)toX; i <= fromX; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (i < 0 || tY < 0 || tY >= rh || i >= rw) continue;

                        *(iptr + rw * tY + i) = diValue;
                    }
            }
            else
            {
                float slope = (fromX - toX) / (fromY - toY);
                float b = -slope * fromY + fromX;

                if (fromY < toY)
                    for (int i = (int)fromY; i <= toY; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (i < 0 || tY < 0 || tY >= rw || i >= rh) continue;

                        *(iptr + rw * i + tY) = diValue;
                    }
                else


                    for (int i = (int)toY; i <= fromY; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (i < 0 || tY < 0 || tY >= rw || i >= rh) continue;

                        *(iptr + rw * i + tY) = diValue;
                    }

            }
        }

        #endregion

        static Vector3 GetCos(Vector3 EulerAnglesDEG)
        {
            return new Vector3((float)Math.Cos(EulerAnglesDEG.x / 57.2958f), (float)Math.Cos(EulerAnglesDEG.y / 57.2958f), (float)Math.Cos(EulerAnglesDEG.z / 57.2958f));
        }

        static Vector3 GetSin(Vector3 EulerAnglesDEG)
        {
            return new Vector3((float)Math.Sin(EulerAnglesDEG.x / 57.2958f), (float)Math.Sin(EulerAnglesDEG.y / 57.2958f), (float)Math.Sin(EulerAnglesDEG.z / 57.2958f));
        }

        /// <summary>
        /// Sets the method of parallelization.
        /// </summary>
        /// <param name="useOpenMP">True uses OpenMP, False uses the Parallel Patterns Library</param>
        public static void SetParallelizationTechnique(bool useOpenMP)
        {
            SetParallelizationMode(useOpenMP, 8);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct PhongConfig
    {
        public Vector3 lightPosition;
        public Vector3 lightColor;
        public Vector3 objectColor;

        public float ambientStrength;
        public float specularStrength;
        public int specularPower;

        public int shadowMapPresent; //0 No 1 Yes
        public float* shadowMapAddress;
        public int shadowMapWidth;
        public int shadowMapHeight;

        public float srw;
        public float srh;

        public float sfw;
        public float sfh;

        public Vector3 lightPosReal;
        public Vector3 lightRotCos;
        public Vector3 lightRotSin;

        public float ShadowBias;
        public float ShadowNormalBias;

        public void SetShadowMap(GLTexture shadowmap)
        {
            float M_PI = (float)Math.PI;
            float radsFOV = 90f * (float)Math.PI / 180.0f;

            float fovCoefficient = (float)Math.Tan((M_PI / 2.0f) - (radsFOV / 2.0f));
            float hFovCoefficient = ((float)shadowmap.Width / (float)shadowmap.Height) * (float)Math.Tan((M_PI / 2.0f) - (radsFOV / 2.0f));

            float tanVert = (float)Math.Tan(radsFOV / 2.0f) * (1.0f - 0.0f);
            float tanHorz = (float)Math.Tan(radsFOV / 2.0f) * ((float)shadowmap.Height / (float)shadowmap.Width) * (1.0f - 0.0f);

            float rw = (shadowmap.Width - 1.0f) / 2.0f, rh = (shadowmap.Height - 1.0f) / 2.0f;

            float fw = rw * fovCoefficient;
            float fh = rh * hFovCoefficient;

            srw = rw;
            srh = rh;

            sfw = fw;
            sfh = fh;

            shadowMapWidth = shadowmap.Width;
            shadowMapHeight = shadowmap.Height;
            shadowMapPresent = 1;
            shadowMapAddress = (float*)shadowmap.GetAddress();

            ShadowBias = 5;
            ShadowNormalBias = 5;
        }

        public void LightPosCameraSpace(Vector3 cameraPosition, Vector3 cameraRotation)
        {
            lightPosition = Rot(lightPosition, cameraPosition, GetCos(cameraRotation), GetSin(cameraRotation));
        }

        public void SetLightRotation(Vector3 Rotation)
        {
            lightRotCos = GetCos(Rotation);
            lightRotSin = GetSin(Rotation);
        }

        Vector3 Rot(Vector3 I, Vector3 c, Vector3 co, Vector3 s)
        {
            float X = I.x - c.x;
            float Y = I.y - c.y;
            float Z = I.z - c.z;

            float fiX = (X) * co.z - (Z) * s.z;
            float fiZ = (Z) * co.z + (X) * s.z;
            float ndY = (Y) * co.y + (fiZ) * s.y;

            float Fx = (fiX) * co.x - (ndY) * s.x;
            float Fy = (ndY) * co.x + (fiX) * s.x;
            float Fz = (fiZ) * co.y - (Y) * s.y;

            return new Vector3(Fx, Fy, Fz);
        }

        static Vector3 GetCos(Vector3 EulerAnglesDEG)
        {
            return new Vector3((float)Math.Cos(EulerAnglesDEG.x / 57.2958f), (float)Math.Cos(EulerAnglesDEG.y / 57.2958f), (float)Math.Cos(EulerAnglesDEG.z / 57.2958f));
        }

        static Vector3 GetSin(Vector3 EulerAnglesDEG)
        {
            return new Vector3((float)Math.Sin(EulerAnglesDEG.x / 57.2958f), (float)Math.Sin(EulerAnglesDEG.y / 57.2958f), (float)Math.Sin(EulerAnglesDEG.z / 57.2958f));
        }

    }

    public enum Culling
    {
        None = 0,
        Backface = 1,
        Frontface = 2
    }
}


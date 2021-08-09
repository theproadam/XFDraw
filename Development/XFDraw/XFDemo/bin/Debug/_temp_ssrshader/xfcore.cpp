void FIPA(float* TA, int INDEX, float* VD, int A, int B, float LinePos, int Stride)
{
	float X;
	float Y;
	int s = 3;

	A *= Stride;
	B *= Stride;

	if (VD[A + 2] - VD[B + 2] != 0)
	{
		float slopeY = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
		float bY = -slopeY * VD[A + 2] + VD[A + 1];
		Y = slopeY * LinePos + bY;

		float slopeX = (VD[A + 0] - VD[B + 0]) / (VD[A + 2] - VD[B + 2]);
		float bX = -slopeX * VD[A + 2] + VD[A + 0];
		X = slopeX * LinePos + bX;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
			float bA = -slopeA * VD[A + 2] + VD[A + i];
			TA[INDEX + i] = slopeA * LinePos + bA;
		}

	}


	TA[INDEX] = X;
	TA[INDEX + 1] = Y;
	TA[INDEX + 2] = LinePos;
}

void SIPA(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, float oW, int Stride)
{
	float X;
	float Y;
	float Z;

	int s = 3;

	A *= Stride;
	B *= Stride;

	float s1 = VD[A + 2] - VD[B + 2];
	float s2 = (VD[A] - VD[B]);
	s1 *= s1;
	s2 *= s2;

	if (s1 > s2)
	{
		float slope = (VD[A] - VD[B]) / (VD[A + 2] - VD[B + 2]);
		float b = -slope * VD[A + 2] + VD[A];

		Z = (b - oW) / (TanSlope - slope);
		X = Z * slope + b;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
			float bA = -slopeA * VD[A + 2] + VD[A + i];
			TA[INDEX + i] = slopeA * Z + bA;
		}
		s = Stride;
	}
	else
	{
		float slope = (VD[A + 2] - VD[B + 2]) / (VD[A] - VD[B]);
		float b = -slope * VD[A] + VD[A + 2];

		Z = (slope * oW + b) / (1.0f - slope * TanSlope);
		X = TanSlope * Z + oW;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A] - VD[B]);
			float bA = -slopeA * VD[A] + VD[A + i];
			TA[INDEX + i] = slopeA * X + bA;
		}
		s = Stride;
	}

	//Floating point error solution:
	if (s1 > s2)
	{
		float slope = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
		float b = -slope * VD[A + 2] + VD[A + 1];

		Y = slope * Z + b;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
			float bA = -slopeA * VD[A + 2] + VD[A + i];
			TA[INDEX + i] = slopeA * Z + bA;
		}
		s = Stride;
	}
	else
	{
		float slope = (VD[A + 1] - VD[B + 1]) / (VD[A] - VD[B]);
		float b = -slope * VD[A] + VD[A + 1];

		Y = slope * X + b;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A] - VD[B]);
			float bA = -slopeA * VD[A] + VD[A + i];
			TA[INDEX + i] = slopeA * X + bA;
		}
		s = Stride;
	}

	TA[INDEX] = X;
	TA[INDEX + 1] = Y;
	TA[INDEX + 2] = Z;
}

void SIPHA(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, float oH, int Stride)
{
	float X;
	float Y;
	float Z;

	int s = 3;

	A *= Stride;
	B *= Stride;

	//compared to non stride siph, the s1 s2 are flipped; not sure why

	float s1 = VD[A + 2] - VD[B + 2];
	float s2 = VD[A + 1] - VD[B + 1];
	s1 *= s1;
	s2 *= s2;

	if (s2 > s1)
	{
		float slope = (VD[A + 2] - VD[B + 2]) / (VD[A + 1] - VD[B + 1]);
		float b = -slope * VD[A + 1] + VD[A + 2];

		Z = (slope * oH + b) / (1.0f - slope * TanSlope);
		Y = TanSlope * Z + oH;


		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 1] - VD[B + 1]);
			float bA = -slopeA * VD[A + 1] + VD[A + i];
			TA[INDEX + i] = slopeA * Y + bA;
		}
		s = Stride;
	}
	else
	{
		float slope = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
		float b = -slope * VD[A + 2] + VD[A + 1];

		float V = (b - oH) / (TanSlope - slope);

		Y = V * slope + b;
		Z = V;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
			float bA = -slopeA * VD[A + 2] + VD[A + i];
			TA[INDEX + i] = slopeA * Z + bA;
		}
		s = Stride;
	}

	if (s1 > s2)
	{
		float slope = (VD[A] - VD[B]) / (VD[A + 2] - VD[B + 2]);
		float b = -slope * VD[A + 2] + VD[A];

		X = slope * Z + b;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
			float bA = -slopeA * VD[A + 2] + VD[A + i];
			TA[INDEX + i] = slopeA * Z + bA;
		}
		s = Stride;
	}
	else
	{
		float slope = (VD[A] - VD[B]) / (VD[A + 1] - VD[B + 1]);
		float b = -slope * VD[A + 1] + VD[A];

		X = slope * Y + b;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 1] - VD[B + 1]);
			float bA = -slopeA * VD[A + 1] + VD[A + i];
			TA[INDEX + i] = slopeA * Y + bA;
		}
		s = Stride;
	}

	TA[INDEX] = X;
	TA[INDEX + 1] = Y;
	TA[INDEX + 2] = Z;
}

void LIPA_PLUS(float* XR, int I, float* V_DATA, int A, int B, int LinePos, int Stride)
{
	float X;
	float Z;

	A *= Stride;
	B *= Stride;

	if (V_DATA[A + 1] == LinePos)
	{
		XR[I * (Stride - 1)] = V_DATA[A];
		XR[I * (Stride - 1) + 1] = V_DATA[A + 2];

		for (int a = 3; a < Stride; a++)
		{
			XR[I * (Stride - 1) + (a - 1)] = V_DATA[A + a];
		}
		return;
	}

	if (V_DATA[B + 1] == LinePos)
	{
		XR[I * (Stride - 1)] = V_DATA[B];
		XR[I * (Stride - 1) + 1] = V_DATA[B + 2];

		for (int a = 3; a < Stride; a++)
		{
			XR[I * (Stride - 1) + (a - 1)] = V_DATA[B + a];
		}
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


	float ZDIFF = (1.0f / V_DATA[A + 2] - 1.0f / V_DATA[B + 2]);
	bool usingZ = ZDIFF != 0;

	if (ZDIFF != 0)
		usingZ = ZDIFF * ZDIFF >= 0.00001f;

	if (usingZ)
	for (int a = 3; a < Stride; a++)
	{
		float slopeA = (V_DATA[A + a] - V_DATA[B + a]) / ZDIFF;
		float bA = -slopeA / V_DATA[A + 2] + V_DATA[A + a];
		XR[I * (Stride - 1) + (a - 1)] = slopeA / Z + bA;
	}
	else if (V_DATA[A + 1] - V_DATA[B + 1] != 0)
	for (int a = 3; a < Stride; a++)
	{
		float slopeA = (V_DATA[A + a] - V_DATA[B + a]) / (V_DATA[A + 1] - V_DATA[B + 1]);
		float bA = -slopeA * V_DATA[A + 1] + V_DATA[A + a];
		XR[I * (Stride - 1) + (a - 1)] = (slopeA * (float)LinePos + bA);


	}


	XR[I * (Stride - 1) + 0] = X;
	XR[I * (Stride - 1) + 1] = Z;
}

bool ScanLinePLUS(int Line, float* TRIS_DATA, int TRIS_SIZE, float* Intersects, int Stride)
{
	int IC = 0;
	for (int i = 0; i < TRIS_SIZE - 1; i++)
	{
		float y1 = TRIS_DATA[i * Stride + 1];
		float y2 = TRIS_DATA[(i + 1) * Stride + 1];

		if (y2 == y1 && Line == y2){
			LIPA_PLUS(Intersects, 0, TRIS_DATA, i, i + 1, Line, Stride);
			LIPA_PLUS(Intersects, 1, TRIS_DATA, i + 1, i, Line, Stride);
			return true;
		}

		if (y2 < y1){
			float t = y2;
			y2 = y1;
			y1 = t;
		}

		if (Line <= y2 && Line > y1){
			LIPA_PLUS(Intersects, IC, TRIS_DATA, i, i + 1, Line, Stride);
			IC++;
		}

		if (IC >= 2) return true;
	}

	if (IC < 2)
	{
		float y1 = TRIS_DATA[0 * Stride + 1];
		float y2 = TRIS_DATA[(TRIS_SIZE - 1) * Stride + 1];

		if (y2 == y1 && Line == y2){
			LIPA_PLUS(Intersects, 0, TRIS_DATA, 0, (TRIS_SIZE - 1), Line, Stride);
			LIPA_PLUS(Intersects, 1, TRIS_DATA, (TRIS_SIZE - 1), 0, Line, Stride);
			return true;
		}

		if (y2 < y1){
			float t = y2;
			y2 = y1;
			y1 = t;
		}

		if (Line <= y2 && Line > y1){
			LIPA_PLUS(Intersects, IC, TRIS_DATA, 0, TRIS_SIZE - 1, Line, Stride);
			IC++;
		}
	}

	if (IC == 2) return true;
	else return false;
}

struct GLData
{
	float nearZ;
	float farZ;

	float tanVert;
	float tanHorz;

	float ow;
	float oh;

	float rw;
	float rh;

	float fw;
	float fh;

	float ox;
	float oy;

	float iox;
	float ioy;

	float oValue;

	int renderWidth;
	int renderHeight;

	float matrixlerpv;
};

struct GLExtra
{
	int FACE_CULL;
	int WIRE_MODE;
	int wireColor;
	int* wire_ptr;
	float offset_wire;
	int depth_mode;
	float depth_offset;
};

inline void frtlzeromem(bool* dest, int count)
{
	for (int i = 0; i < count; ++i)
		dest[i] = false;
}

inline float BACKFACECULLS(float* VERTEX_DATA, int Stride)
{
	return ((VERTEX_DATA[Stride]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[Stride * 2 + 1]) - (VERTEX_DATA[1])) - ((VERTEX_DATA[Stride * 2]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[Stride + 1]) - (VERTEX_DATA[1]));
}

void DrawLineNoDATA(float* FromDATA, float* ToDATA, float* dptr, int* iptr, int color, float zoffset, int Stride, int VW, int VH, float farZ)
{
	if (FromDATA[0] == ToDATA[0] && FromDATA[1] == ToDATA[1])
		return;

	float aa = (FromDATA[0] - ToDATA[0]);
	float ba = (FromDATA[1] - ToDATA[1]);

	if (aa * aa > ba * ba)
	{
		float slope = (FromDATA[1] - ToDATA[1]) / (FromDATA[0] - ToDATA[0]);
		float b = -slope * FromDATA[0] + FromDATA[1];

		float slopeZ = (FromDATA[2] - ToDATA[2]) / (FromDATA[0] - ToDATA[0]);
		float bZ = -slopeZ * FromDATA[0] + FromDATA[2];

		if (FromDATA[0] > ToDATA[0])
		{
			float* temp = ToDATA;
			ToDATA = FromDATA;
			FromDATA = temp;
		}

		for (int i = (int)FromDATA[0]; i <= ToDATA[0]; i++)
		{
			int tY = (int)(i * slope + b);
			float depth = 1.0f / (slopeZ * (float)i + bZ);

			float s = farZ - depth;
			if (i < 0 || tY < 0 || tY >= VH || i >= VW) continue;

			int mem_addr = VW * tY + i;

			if (dptr[mem_addr] > s - zoffset) continue;
			dptr[mem_addr] = s;

			iptr[mem_addr] = color;
		}
	}
	else
	{
		float slope = (FromDATA[0] - ToDATA[0]) / (FromDATA[1] - ToDATA[1]);
		float b = -slope * FromDATA[1] + FromDATA[0];

		float slopeZ = (FromDATA[2] - ToDATA[2]) / (FromDATA[1] - ToDATA[1]);
		float bZ = -slopeZ * FromDATA[1] + FromDATA[2];

		if (FromDATA[1] > ToDATA[1])
		{
			float* temp = ToDATA;
			ToDATA = FromDATA;
			FromDATA = temp;
		}

		for (int i = (int)FromDATA[1]; i <= ToDATA[1]; i++)
		{
			int tY = (int)(i * slope + b);
			float depth = 1.0f / (slopeZ * (float)i + bZ);

			float s = farZ - depth;
			if (i < 0 || tY < 0 || tY >= VW || i >= VH) continue;

			int mem_addr = VW * i + tY;

			if (dptr[mem_addr] > s - zoffset) continue;
			dptr[mem_addr] = s;

			iptr[mem_addr] = color;
		}
	}
}



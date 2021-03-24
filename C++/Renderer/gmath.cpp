#include "gmath.h"

namespace rendering {

#pragma region Math

	int1::operator float1() const { return float1((float)this->x); }
	int1::operator uint1() const { return uint1((uint)this->x); }

	int2::operator float2() const { return float2((float)this->x, (float)this->y); }
	int2::operator uint2() const { return uint2((uint)this->x, (uint)this->y); }

	int3::operator float3() const { return float3((float)this->x, (float)this->y, (float)this->z); }
	int3::operator uint3() const { return uint3((uint)this->x, (uint)this->y, (uint)this->z); }

	int4::operator float4() const { return float4((float)this->x, (float)this->y, (float)this->z, (float)this->w); }
	int4::operator uint4() const { return uint4((uint)this->x, (uint)this->y, (uint)this->z, (uint)this->w); }

	int1x1::operator float1x1() const { return float1x1((float)this->_m00); }
	int1x1::operator uint1x1() const { return uint1x1((uint)this->_m00); }

	int1x2::operator float1x2() const { return float1x2((float)this->_m00, (float)this->_m01); }
	int1x2::operator uint1x2() const { return uint1x2((uint)this->_m00, (uint)this->_m01); }

	int1x3::operator float1x3() const { return float1x3((float)this->_m00, (float)this->_m01, (float)this->_m02); }
	int1x3::operator uint1x3() const { return uint1x3((uint)this->_m00, (uint)this->_m01, (uint)this->_m02); }

	int1x4::operator float1x4() const { return float1x4((float)this->_m00, (float)this->_m01, (float)this->_m02, (float)this->_m03); }
	int1x4::operator uint1x4() const { return uint1x4((uint)this->_m00, (uint)this->_m01, (uint)this->_m02, (uint)this->_m03); }

	int2x1::operator float2x1() const { return float2x1((float)this->_m00, (float)this->_m10); }
	int2x1::operator uint2x1() const { return uint2x1((uint)this->_m00, (uint)this->_m10); }

	int2x2::operator float2x2() const { return float2x2((float)this->_m00, (float)this->_m01, (float)this->_m10, (float)this->_m11); }
	int2x2::operator uint2x2() const { return uint2x2((uint)this->_m00, (uint)this->_m01, (uint)this->_m10, (uint)this->_m11); }

	int2x3::operator float2x3() const { return float2x3((float)this->_m00, (float)this->_m01, (float)this->_m02, (float)this->_m10, (float)this->_m11, (float)this->_m12); }
	int2x3::operator uint2x3() const { return uint2x3((uint)this->_m00, (uint)this->_m01, (uint)this->_m02, (uint)this->_m10, (uint)this->_m11, (uint)this->_m12); }

	int2x4::operator float2x4() const { return float2x4((float)this->_m00, (float)this->_m01, (float)this->_m02, (float)this->_m03, (float)this->_m10, (float)this->_m11, (float)this->_m12, (float)this->_m13); }
	int2x4::operator uint2x4() const { return uint2x4((uint)this->_m00, (uint)this->_m01, (uint)this->_m02, (uint)this->_m03, (uint)this->_m10, (uint)this->_m11, (uint)this->_m12, (uint)this->_m13); }

	int3x1::operator float3x1() const { return float3x1((float)this->_m00, (float)this->_m10, (float)this->_m20); }
	int3x1::operator uint3x1() const { return uint3x1((uint)this->_m00, (uint)this->_m10, (uint)this->_m20); }

	int3x2::operator float3x2() const { return float3x2((float)this->_m00, (float)this->_m01, (float)this->_m10, (float)this->_m11, (float)this->_m20, (float)this->_m21); }
	int3x2::operator uint3x2() const { return uint3x2((uint)this->_m00, (uint)this->_m01, (uint)this->_m10, (uint)this->_m11, (uint)this->_m20, (uint)this->_m21); }

	int3x3::operator float3x3() const { return float3x3((float)this->_m00, (float)this->_m01, (float)this->_m02, (float)this->_m10, (float)this->_m11, (float)this->_m12, (float)this->_m20, (float)this->_m21, (float)this->_m22); }
	int3x3::operator uint3x3() const { return uint3x3((uint)this->_m00, (uint)this->_m01, (uint)this->_m02, (uint)this->_m10, (uint)this->_m11, (uint)this->_m12, (uint)this->_m20, (uint)this->_m21, (uint)this->_m22); }

	int3x4::operator float3x4() const { return float3x4((float)this->_m00, (float)this->_m01, (float)this->_m02, (float)this->_m03, (float)this->_m10, (float)this->_m11, (float)this->_m12, (float)this->_m13, (float)this->_m20, (float)this->_m21, (float)this->_m22, (float)this->_m23); }
	int3x4::operator uint3x4() const { return uint3x4((uint)this->_m00, (uint)this->_m01, (uint)this->_m02, (uint)this->_m03, (uint)this->_m10, (uint)this->_m11, (uint)this->_m12, (uint)this->_m13, (uint)this->_m20, (uint)this->_m21, (uint)this->_m22, (uint)this->_m23); }

	int4x1::operator float4x1() const { return float4x1((float)this->_m00, (float)this->_m10, (float)this->_m20, (float)this->_m30); }
	int4x1::operator uint4x1() const { return uint4x1((uint)this->_m00, (uint)this->_m10, (uint)this->_m20, (uint)this->_m30); }

	int4x2::operator float4x2() const { return float4x2((float)this->_m00, (float)this->_m01, (float)this->_m10, (float)this->_m11, (float)this->_m20, (float)this->_m21, (float)this->_m30, (float)this->_m31); }
	int4x2::operator uint4x2() const { return uint4x2((uint)this->_m00, (uint)this->_m01, (uint)this->_m10, (uint)this->_m11, (uint)this->_m20, (uint)this->_m21, (uint)this->_m30, (uint)this->_m31); }

	int4x3::operator float4x3() const { return float4x3((float)this->_m00, (float)this->_m01, (float)this->_m02, (float)this->_m10, (float)this->_m11, (float)this->_m12, (float)this->_m20, (float)this->_m21, (float)this->_m22, (float)this->_m30, (float)this->_m31, (float)this->_m32); }
	int4x3::operator uint4x3() const { return uint4x3((uint)this->_m00, (uint)this->_m01, (uint)this->_m02, (uint)this->_m10, (uint)this->_m11, (uint)this->_m12, (uint)this->_m20, (uint)this->_m21, (uint)this->_m22, (uint)this->_m30, (uint)this->_m31, (uint)this->_m32); }

	int4x4::operator float4x4() const { return float4x4((float)this->_m00, (float)this->_m01, (float)this->_m02, (float)this->_m03, (float)this->_m10, (float)this->_m11, (float)this->_m12, (float)this->_m13, (float)this->_m20, (float)this->_m21, (float)this->_m22, (float)this->_m23, (float)this->_m30, (float)this->_m31, (float)this->_m32, (float)this->_m33); }
	int4x4::operator uint4x4() const { return uint4x4((uint)this->_m00, (uint)this->_m01, (uint)this->_m02, (uint)this->_m03, (uint)this->_m10, (uint)this->_m11, (uint)this->_m12, (uint)this->_m13, (uint)this->_m20, (uint)this->_m21, (uint)this->_m22, (uint)this->_m23, (uint)this->_m30, (uint)this->_m31, (uint)this->_m32, (uint)this->_m33); }

	float1::operator int1() const { return int1((int)this->x); }
	float1::operator uint1() const { return uint1((uint)this->x); }

	float2::operator int2() const { return int2((int)this->x, (int)this->y); }
	float2::operator uint2() const { return uint2((uint)this->x, (uint)this->y); }

	float3::operator int3() const { return int3((int)this->x, (int)this->y, (int)this->z); }
	float3::operator uint3() const { return uint3((uint)this->x, (uint)this->y, (uint)this->z); }

	float4::operator int4() const { return int4((int)this->x, (int)this->y, (int)this->z, (int)this->w); }
	float4::operator uint4() const { return uint4((uint)this->x, (uint)this->y, (uint)this->z, (uint)this->w); }

	float1x1::operator int1x1() const { return int1x1((int)this->_m00); }
	float1x1::operator uint1x1() const { return uint1x1((uint)this->_m00); }

	float1x2::operator int1x2() const { return int1x2((int)this->_m00, (int)this->_m01); }
	float1x2::operator uint1x2() const { return uint1x2((uint)this->_m00, (uint)this->_m01); }

	float1x3::operator int1x3() const { return int1x3((int)this->_m00, (int)this->_m01, (int)this->_m02); }
	float1x3::operator uint1x3() const { return uint1x3((uint)this->_m00, (uint)this->_m01, (uint)this->_m02); }

	float1x4::operator int1x4() const { return int1x4((int)this->_m00, (int)this->_m01, (int)this->_m02, (int)this->_m03); }
	float1x4::operator uint1x4() const { return uint1x4((uint)this->_m00, (uint)this->_m01, (uint)this->_m02, (uint)this->_m03); }

	float2x1::operator int2x1() const { return int2x1((int)this->_m00, (int)this->_m10); }
	float2x1::operator uint2x1() const { return uint2x1((uint)this->_m00, (uint)this->_m10); }

	float2x2::operator int2x2() const { return int2x2((int)this->_m00, (int)this->_m01, (int)this->_m10, (int)this->_m11); }
	float2x2::operator uint2x2() const { return uint2x2((uint)this->_m00, (uint)this->_m01, (uint)this->_m10, (uint)this->_m11); }

	float2x3::operator int2x3() const { return int2x3((int)this->_m00, (int)this->_m01, (int)this->_m02, (int)this->_m10, (int)this->_m11, (int)this->_m12); }
	float2x3::operator uint2x3() const { return uint2x3((uint)this->_m00, (uint)this->_m01, (uint)this->_m02, (uint)this->_m10, (uint)this->_m11, (uint)this->_m12); }

	float2x4::operator int2x4() const { return int2x4((int)this->_m00, (int)this->_m01, (int)this->_m02, (int)this->_m03, (int)this->_m10, (int)this->_m11, (int)this->_m12, (int)this->_m13); }
	float2x4::operator uint2x4() const { return uint2x4((uint)this->_m00, (uint)this->_m01, (uint)this->_m02, (uint)this->_m03, (uint)this->_m10, (uint)this->_m11, (uint)this->_m12, (uint)this->_m13); }

	float3x1::operator int3x1() const { return int3x1((int)this->_m00, (int)this->_m10, (int)this->_m20); }
	float3x1::operator uint3x1() const { return uint3x1((uint)this->_m00, (uint)this->_m10, (uint)this->_m20); }

	float3x2::operator int3x2() const { return int3x2((int)this->_m00, (int)this->_m01, (int)this->_m10, (int)this->_m11, (int)this->_m20, (int)this->_m21); }
	float3x2::operator uint3x2() const { return uint3x2((uint)this->_m00, (uint)this->_m01, (uint)this->_m10, (uint)this->_m11, (uint)this->_m20, (uint)this->_m21); }

	float3x3::operator int3x3() const { return int3x3((int)this->_m00, (int)this->_m01, (int)this->_m02, (int)this->_m10, (int)this->_m11, (int)this->_m12, (int)this->_m20, (int)this->_m21, (int)this->_m22); }
	float3x3::operator uint3x3() const { return uint3x3((uint)this->_m00, (uint)this->_m01, (uint)this->_m02, (uint)this->_m10, (uint)this->_m11, (uint)this->_m12, (uint)this->_m20, (uint)this->_m21, (uint)this->_m22); }

	float3x4::operator int3x4() const { return int3x4((int)this->_m00, (int)this->_m01, (int)this->_m02, (int)this->_m03, (int)this->_m10, (int)this->_m11, (int)this->_m12, (int)this->_m13, (int)this->_m20, (int)this->_m21, (int)this->_m22, (int)this->_m23); }
	float3x4::operator uint3x4() const { return uint3x4((uint)this->_m00, (uint)this->_m01, (uint)this->_m02, (uint)this->_m03, (uint)this->_m10, (uint)this->_m11, (uint)this->_m12, (uint)this->_m13, (uint)this->_m20, (uint)this->_m21, (uint)this->_m22, (uint)this->_m23); }

	float4x1::operator int4x1() const { return int4x1((int)this->_m00, (int)this->_m10, (int)this->_m20, (int)this->_m30); }
	float4x1::operator uint4x1() const { return uint4x1((uint)this->_m00, (uint)this->_m10, (uint)this->_m20, (uint)this->_m30); }

	float4x2::operator int4x2() const { return int4x2((int)this->_m00, (int)this->_m01, (int)this->_m10, (int)this->_m11, (int)this->_m20, (int)this->_m21, (int)this->_m30, (int)this->_m31); }
	float4x2::operator uint4x2() const { return uint4x2((uint)this->_m00, (uint)this->_m01, (uint)this->_m10, (uint)this->_m11, (uint)this->_m20, (uint)this->_m21, (uint)this->_m30, (uint)this->_m31); }

	float4x3::operator int4x3() const { return int4x3((int)this->_m00, (int)this->_m01, (int)this->_m02, (int)this->_m10, (int)this->_m11, (int)this->_m12, (int)this->_m20, (int)this->_m21, (int)this->_m22, (int)this->_m30, (int)this->_m31, (int)this->_m32); }
	float4x3::operator uint4x3() const { return uint4x3((uint)this->_m00, (uint)this->_m01, (uint)this->_m02, (uint)this->_m10, (uint)this->_m11, (uint)this->_m12, (uint)this->_m20, (uint)this->_m21, (uint)this->_m22, (uint)this->_m30, (uint)this->_m31, (uint)this->_m32); }

	float4x4::operator int4x4() const { return int4x4((int)this->_m00, (int)this->_m01, (int)this->_m02, (int)this->_m03, (int)this->_m10, (int)this->_m11, (int)this->_m12, (int)this->_m13, (int)this->_m20, (int)this->_m21, (int)this->_m22, (int)this->_m23, (int)this->_m30, (int)this->_m31, (int)this->_m32, (int)this->_m33); }
	float4x4::operator uint4x4() const { return uint4x4((uint)this->_m00, (uint)this->_m01, (uint)this->_m02, (uint)this->_m03, (uint)this->_m10, (uint)this->_m11, (uint)this->_m12, (uint)this->_m13, (uint)this->_m20, (uint)this->_m21, (uint)this->_m22, (uint)this->_m23, (uint)this->_m30, (uint)this->_m31, (uint)this->_m32, (uint)this->_m33); }

	uint1::operator float1() const { return float1((float)this->x); }
	uint1::operator int1() const { return int1((int)this->x); }

	uint2::operator float2() const { return float2((float)this->x, (float)this->y); }
	uint2::operator int2() const { return int2((int)this->x, (int)this->y); }

	uint3::operator float3() const { return float3((float)this->x, (float)this->y, (float)this->z); }
	uint3::operator int3() const { return int3((int)this->x, (int)this->y, (int)this->z); }

	uint4::operator float4() const { return float4((float)this->x, (float)this->y, (float)this->z, (float)this->w); }
	uint4::operator int4() const { return int4((int)this->x, (int)this->y, (int)this->z, (int)this->w); }

	uint1x1::operator float1x1() const { return float1x1((float)this->_m00); }
	uint1x1::operator int1x1() const { return int1x1((int)this->_m00); }

	uint1x2::operator float1x2() const { return float1x2((float)this->_m00, (float)this->_m01); }
	uint1x2::operator int1x2() const { return int1x2((int)this->_m00, (int)this->_m01); }

	uint1x3::operator float1x3() const { return float1x3((float)this->_m00, (float)this->_m01, (float)this->_m02); }
	uint1x3::operator int1x3() const { return int1x3((int)this->_m00, (int)this->_m01, (int)this->_m02); }

	uint1x4::operator float1x4() const { return float1x4((float)this->_m00, (float)this->_m01, (float)this->_m02, (float)this->_m03); }
	uint1x4::operator int1x4() const { return int1x4((int)this->_m00, (int)this->_m01, (int)this->_m02, (int)this->_m03); }

	uint2x1::operator float2x1() const { return float2x1((float)this->_m00, (float)this->_m10); }
	uint2x1::operator int2x1() const { return int2x1((int)this->_m00, (int)this->_m10); }

	uint2x2::operator float2x2() const { return float2x2((float)this->_m00, (float)this->_m01, (float)this->_m10, (float)this->_m11); }
	uint2x2::operator int2x2() const { return int2x2((int)this->_m00, (int)this->_m01, (int)this->_m10, (int)this->_m11); }

	uint2x3::operator float2x3() const { return float2x3((float)this->_m00, (float)this->_m01, (float)this->_m02, (float)this->_m10, (float)this->_m11, (float)this->_m12); }
	uint2x3::operator int2x3() const { return int2x3((int)this->_m00, (int)this->_m01, (int)this->_m02, (int)this->_m10, (int)this->_m11, (int)this->_m12); }

	uint2x4::operator float2x4() const { return float2x4((float)this->_m00, (float)this->_m01, (float)this->_m02, (float)this->_m03, (float)this->_m10, (float)this->_m11, (float)this->_m12, (float)this->_m13); }
	uint2x4::operator int2x4() const { return int2x4((int)this->_m00, (int)this->_m01, (int)this->_m02, (int)this->_m03, (int)this->_m10, (int)this->_m11, (int)this->_m12, (int)this->_m13); }

	uint3x1::operator float3x1() const { return float3x1((float)this->_m00, (float)this->_m10, (float)this->_m20); }
	uint3x1::operator int3x1() const { return int3x1((int)this->_m00, (int)this->_m10, (int)this->_m20); }

	uint3x2::operator float3x2() const { return float3x2((float)this->_m00, (float)this->_m01, (float)this->_m10, (float)this->_m11, (float)this->_m20, (float)this->_m21); }
	uint3x2::operator int3x2() const { return int3x2((int)this->_m00, (int)this->_m01, (int)this->_m10, (int)this->_m11, (int)this->_m20, (int)this->_m21); }

	uint3x3::operator float3x3() const { return float3x3((float)this->_m00, (float)this->_m01, (float)this->_m02, (float)this->_m10, (float)this->_m11, (float)this->_m12, (float)this->_m20, (float)this->_m21, (float)this->_m22); }
	uint3x3::operator int3x3() const { return int3x3((int)this->_m00, (int)this->_m01, (int)this->_m02, (int)this->_m10, (int)this->_m11, (int)this->_m12, (int)this->_m20, (int)this->_m21, (int)this->_m22); }

	uint3x4::operator float3x4() const { return float3x4((float)this->_m00, (float)this->_m01, (float)this->_m02, (float)this->_m03, (float)this->_m10, (float)this->_m11, (float)this->_m12, (float)this->_m13, (float)this->_m20, (float)this->_m21, (float)this->_m22, (float)this->_m23); }
	uint3x4::operator int3x4() const { return int3x4((int)this->_m00, (int)this->_m01, (int)this->_m02, (int)this->_m03, (int)this->_m10, (int)this->_m11, (int)this->_m12, (int)this->_m13, (int)this->_m20, (int)this->_m21, (int)this->_m22, (int)this->_m23); }

	uint4x1::operator float4x1() const { return float4x1((float)this->_m00, (float)this->_m10, (float)this->_m20, (float)this->_m30); }
	uint4x1::operator int4x1() const { return int4x1((int)this->_m00, (int)this->_m10, (int)this->_m20, (int)this->_m30); }

	uint4x2::operator float4x2() const { return float4x2((float)this->_m00, (float)this->_m01, (float)this->_m10, (float)this->_m11, (float)this->_m20, (float)this->_m21, (float)this->_m30, (float)this->_m31); }
	uint4x2::operator int4x2() const { return int4x2((int)this->_m00, (int)this->_m01, (int)this->_m10, (int)this->_m11, (int)this->_m20, (int)this->_m21, (int)this->_m30, (int)this->_m31); }

	uint4x3::operator float4x3() const { return float4x3((float)this->_m00, (float)this->_m01, (float)this->_m02, (float)this->_m10, (float)this->_m11, (float)this->_m12, (float)this->_m20, (float)this->_m21, (float)this->_m22, (float)this->_m30, (float)this->_m31, (float)this->_m32); }
	uint4x3::operator int4x3() const { return int4x3((int)this->_m00, (int)this->_m01, (int)this->_m02, (int)this->_m10, (int)this->_m11, (int)this->_m12, (int)this->_m20, (int)this->_m21, (int)this->_m22, (int)this->_m30, (int)this->_m31, (int)this->_m32); }

	uint4x4::operator float4x4() const { return float4x4((float)this->_m00, (float)this->_m01, (float)this->_m02, (float)this->_m03, (float)this->_m10, (float)this->_m11, (float)this->_m12, (float)this->_m13, (float)this->_m20, (float)this->_m21, (float)this->_m22, (float)this->_m23, (float)this->_m30, (float)this->_m31, (float)this->_m32, (float)this->_m33); }
	uint4x4::operator int4x4() const { return int4x4((int)this->_m00, (int)this->_m01, (int)this->_m02, (int)this->_m03, (int)this->_m10, (int)this->_m11, (int)this->_m12, (int)this->_m13, (int)this->_m20, (int)this->_m21, (int)this->_m22, (int)this->_m23, (int)this->_m30, (int)this->_m31, (int)this->_m32, (int)this->_m33); }

#pragma endregion

}
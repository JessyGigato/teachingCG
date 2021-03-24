// Renderer.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include "gmath.h"
#include "imaging.h"
#include "raster.h"

using namespace rendering;

typedef float3 PointTransform(float3);


/// <summary>
/// Fill a buffer with specific number of random positions in a centered box (-1,-1,-1) -> (1,1,1)
/// </summary>
static void RandomPositionsInBoxSurface(float3* points, int N)
{
    for (int i = 0; i < N; i++)
        points[i] = randomInBox();
}

static void ApplyTransform(float3* points, float3* result, int count, PointTransform freeTransform)
{
    // Transform points with a function
    for (int i = 0; i < count; i++)
        result[i] = freeTransform(points[i]);
}

static void ApplyTransform(float3* points, float3* result, int count, float4x4 transform)
{
    // Transform points with a function
    for (int i = 0; i < count; i++)
    {
        float4 h = float4(points[i], 1); // Get the homogeneous coordinates
        h = mul(h, transform); // Transform in homogeneous space
        result[i] = h.get_xyz() / h.w; // back to 3D
    }
}

static void DrawBox(Raster* raster, float3* boxPoints, int count, float4x4 transform)
{
    float4x4 transformingIntoBox = mul(float4x4(
        0.5f, 0, 0, 0,
        0, 0.5f, 0, 0,
        0, 0, 0.5f, 0,
        0.5f, 0.5f, 0.5f, 1
    ), transform);

    float3* pointsToDraw = new float3[count];
    ApplyTransform(boxPoints, pointsToDraw, count, transformingIntoBox);
    raster->DrawPoints(pointsToDraw, count);
    delete[] pointsToDraw;
}

static void DrawTableTop(Raster* raster, float3* boxPoints, int count, float4x4 transform)
{
    float4x4 transformingIntoLeg = mul(Transforms::Scale(2.2f, 0.2f, 2.2f), transform);
    DrawBox(raster, boxPoints, count, transformingIntoLeg);
}

static void DrawTableLeg(Raster* raster, float3* boxPoints, int count, float4x4 transform)
{
    float4x4 transformingIntoLeg = mul(Transforms::Scale(0.2f, 2, 0.2f), transform);
    DrawBox(raster, boxPoints, count, transformingIntoLeg);
}

static void DrawTable(Raster* raster, float3* boxPoints, int count, float4x4 transform)
{
    DrawTableLeg(raster, boxPoints, count, mul(Transforms::Translate(0.2f, 0, 0.2f), transform));
    DrawTableLeg(raster, boxPoints, count, mul(Transforms::Translate(1.6f, 0, 0.2f), transform));
    DrawTableLeg(raster, boxPoints, count, mul(Transforms::Translate(1.6f, 0, 1.6f), transform));
    DrawTableLeg(raster, boxPoints, count, mul(Transforms::Translate(0.2f, 0, 1.6f), transform));
    DrawTableTop(raster, boxPoints, count, mul(Transforms::Translate(0, 2, 0), transform));
}

static void DrawRoom(Raster* raster, float3* boxPoints, int count, float4x4 transform)
{
    DrawTable(raster, boxPoints, count, mul(Transforms::Translate(0, 0, 0), transform));
    DrawTable(raster, boxPoints, count, mul(Transforms::RotateRespectTo(float3(1, 0, 0), float3(0, 1, 0), pi / 2), transform));
}

static void DrawRoomTest(Raster* raster)
{
    raster->ClearRT(float4(0, 0, 0.2f, 1)); // clear with color dark blue.

    int N = 100000;
    // Create buffer with points to render
    float3* points = new float3[N];
    RandomPositionsInBoxSurface(points, N);

    float4x4 viewMatrix = Transforms::LookAtLH(float3(5.0f, 4.6f, 2), float3(0, 0, 0), float3(0, 1, 0));
    float4x4 projMatrix = Transforms::PerspectiveFovLH(pi_over_4, raster->RenderTarget->Height / (float)raster->RenderTarget->Width, 0.01f, 10);

    DrawRoom(raster, points, N, mul(viewMatrix, projMatrix));

    delete[] points;
}

static void FreeTransformTest(Raster* render)
{
    render->ClearRT(float4(0, 0, 0.2f, 1)); // clear with color dark blue.

    int N = 100000;
    // Create buffer with points to render
    float3* points = new float3[N];
    RandomPositionsInBoxSurface(points, N);

    // Creating boxy...
    ApplyTransform(points, points, N, float4x4(
        1.0f, 0, 0, 0,
        0, 1.57f, 0, 0,
        0, 0, 1.0f, 0,
        0, 0, 0, 1
    ));

    // Apply a free transform
    ApplyTransform(points, points, N, [](float3 p) { return float3(p.x * cosf(p.y) + p.z * sinf(p.y), p.y, p.x * sinf(p.y) - p.z * cosf(p.y)); });

    #pragma region viewing and projecting

    ApplyTransform(points, points, N, Transforms::LookAtLH(float3(5.0f, 2.6f, 4), float3(0, 0, 0), float3(0, 1, 0)));
    ApplyTransform(points, points, N, Transforms::PerspectiveFovLH(pi_over_4, render->RenderTarget->Height / (float)render->RenderTarget->Width, 0.01f, 10));

    #pragma endregion

    render->DrawPoints(points, N);
}


int main()
{
    Raster* raster = new Raster(1024, 512);
    //FreeTransformTest(raster);
    DrawRoomTest(raster);
    raster->RenderTarget->Save("test.rbm");
    std::cout << "Done.\n";
    delete raster;
}


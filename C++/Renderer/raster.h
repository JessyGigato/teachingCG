#ifndef RASTER_H
#define RASTER_H

#include "imaging.h"

namespace rendering {

	class Raster {

		bool ownTexture = false;

	public:
		
		/// <summary>
		/// Gets the specific texture this renderer is drawing at.
		/// </summary>
		Texture2D* const RenderTarget;

		/// <summary>
		/// Initializes a rasterizer to use a specific texture to draw at.
		/// </summary>
		Raster(Texture2D* renderTarget): RenderTarget(renderTarget), ownTexture(false) {
		}

		/// <summary>
		/// Initializes a rasterizer to use a new texture to draw at with specific dimensions.
		/// </summary>
		Raster(int width, int height) : RenderTarget(new Texture2D(width, height)), ownTexture(true) {
		}

		~Raster() {
			if (ownTexture && RenderTarget)
				delete RenderTarget;
		}

		/// <summary>
		/// Clears the render target with a specific color
		/// </summary>
		void ClearRT(float4 color)
		{
			for (int i = 0; i < RenderTarget->Height; i++)
				for (int j = 0; j < RenderTarget->Width; j++)
					RenderTarget->Write(j, i, color);
		}

		/// <summary>
		/// Render a set of points. Points are assumed to be in Normalized Device Coordinates.
		/// I.e. visible points have x and y between -1 and 1, and z between 0 and 1.
		/// Corresponding colors are used for each point.
		/// </summary>
		void DrawPoints(float3* points, float4* colors, int count)
		{
			if (!RenderTarget)
				return;

			for (int i = 0; i < count; i++)
			{
				float3 pto = points[i];
				if (pto.x < -1 || pto.x >= 1 || pto.y <= -1 || pto.y > 1 || pto.z < 0 || pto.z >= 1)
					continue; // clip no-visible point
							  // Convert to image coordinates (0,0) - (Width, Height)
				int px = (int)((pto.x * 0.5f + 0.5f) * RenderTarget->Width);
				int py = (int)((0.5f - pto.y * 0.5f) * RenderTarget->Height);

				RenderTarget->Write(px, py, colors[i]);
			}
		}

		/// <summary>
		/// Render a set of points. Points are assumed to be in Normalized Device Coordinates.
		/// I.e. visible points have x and y between -1 and 1, and z between 0 and 1.
		/// Color is used for every point.
		/// </summary>
		void DrawPoints(float3* points, float4 color, int count)
		{
			float4* colors = new float4[count];
			for (int i = 0; i < count; i++)
				colors[i] = color;
			DrawPoints(points, colors, count);
			delete[] colors;
		}

		/// <summary>
		/// Render a set of points. Points are assumed to be in Normalized Device Coordinates.
		/// I.e. visible points have x and y between -1 and 1, and z between 0 and 1.
		/// White color is used for every point.
		/// </summary>
		void DrawPoints(float3* points, int count)
		{
			DrawPoints(points, float4(1, 1, 1, 1), count);
		}
	};

}


#endif

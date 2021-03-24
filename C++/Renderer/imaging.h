#ifndef IMAGING_H
#define IMAGING_H

#include "gmath.h"

namespace rendering {

	class Texture2D {
		float4* data;

	public:
		const int Width;
		const int Height;

		/// <summary>
		/// Creates an empty texture 2D with a specific size.
		/// </summary>
		Texture2D(int width, int height) :Width(width), Height(height) {
			data = new float4[width * height];
			for (int i = 0; i < width * height; i++)
				data[i] = float4(0, 0, 0, 0);
		}

		~Texture2D() {
			delete[] data;
		}

		/// <summary>
		/// Writes a value in the texture at specific position.
		/// </summary>
		void Write(int x, int y, float4 value) {
			data[y * Width + x] = value;
		}

		/// <summary>
		/// Reads a value from a specific position.
		/// </summary>
		float4 Read(int x, int y) {
			return data[y * Width + x];
		}

		void Save(const char* filename) {
			FILE* writting;
			if (fopen_s(&writting, filename, "wb") != 0)
				return;
			fwrite(&Width, 4, 1, writting); // Write width
			fwrite(&Height, 4, 1, writting); // Write height
			fwrite((void*)data, 
				16, // 16 bytes per texel, 4 bytes per float
				Width * Height, // number of elements 
				writting);
			fclose(writting);
		}
	};
}


#endif
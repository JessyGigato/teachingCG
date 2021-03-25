using System;
using GMath;
using Rendering;
using static GMath.Gfx;

namespace Renderer {
    class Program {
        static void Main (string[] args) {
            Raster render = new Raster (1024, 512);
            //FreeTransformTest(render);
            DrawRoomTest (render);
            render.RenderTarget.Save ("test.rbm");
            Console.WriteLine ("Done.");
        }

        public static float3[] RandomPositionsInBoxSurface (int N) {
            float3[] points = new float3[N];

            for (int i = 0; i < N; i++)
                points[i] = randomInBox ();

            return points;
        }

        public static float3[] ApplyTransform (float3[] points, float4x4 matrix) {
            float3[] result = new float3[points.Length];

            // Transform points with a matrix
            // Linear transform in homogeneous coordinates
            for (int i = 0; i < points.Length; i++) {
                float4 h = float4 (points[i], 1);
                h = mul (h, matrix);
                result[i] = h.xyz / h.w;
            }

            return result;
        }

        public static float3[] ApplyTransform (float3[] points, Func<float3, float3> freeTransform) {
            float3[] result = new float3[points.Length];

            // Transform points with a function
            for (int i = 0; i < points.Length; i++)
                result[i] = freeTransform (points[i]);

            return result;
        }

        private static void FreeTransformTest (Raster render) {
            render.ClearRT (float4 (0, 0, 0.2f, 1)); // clear with color dark blue.

            int N = 100;
            // Create buffer with points to render
            float3[] points = RandomPositionsInBoxSurface (N);

            // Creating boxy...
            points = ApplyTransform (points, float4x4 (
                1f, 0, 0, 0,
                0, 1.57f, 0, 0,
                0, 0, 1f, 0,
                0, 0, 0, 1
            ));

            // Apply a free transform
            points = ApplyTransform (points, p => float3 (p.x * cos (p.y) + p.z * sin (p.y), p.y, p.x * sin (p.y) - p.z * cos (p.y)));

            #region viewing and projecting

            points = ApplyTransform (points, Transforms.LookAtLH (float3 (5f, 2.6f, 4), float3 (0, 0, 0), float3 (0, 1, 0)));
            points = ApplyTransform (points, Transforms.PerspectiveFovLH (pi_over_4, render.RenderTarget.Height / (float) render.RenderTarget.Width, 0.01f, 10));

            #endregion

            render.DrawPoints (points);
        }

        public static void DrawRoomTest (Raster raster) {
            // raster.ClearRT (float4 (0, 0, 0.2f, 1)); // clear with color dark blue.
            raster.ClearRT (float4 (.250f, .092f, .035f, 1)); // clear with orange-brown.

            int N = 5000;
            // Create buffer with points to render
            float3[] points = RandomPositionsInBoxSurface (N);

            float4x4 viewMatrix = Transforms.LookAtLH (float3 (5f, 4.6f, 2), float3 (0, 0, 0), float3 (0, 1, 0));
            float4x4 projMatrix = Transforms.PerspectiveFovLH (pi_over_4, raster.RenderTarget.Height / (float) raster.RenderTarget.Width, 0.01f, 10);

            DrawObjects (raster, points, mul (viewMatrix, projMatrix));
        }

        private static void DrawObjects (Raster raster, float3[] boxPoints, float4x4 transform) {
            DrawRoom (raster, boxPoints, mul (Transforms.RotateYGrad(-15), transform));
        }

        private static void DrawRoom (Raster raster, float3[] boxPoints, float4x4 transform) {
            DrawBookCases (raster, boxPoints, mul (Transforms.Scale (1f, .3f, 1f), mul (Transforms.RotateZGrad (-15), mul (Transforms.Translate (0, 1, -1), transform))));
            DrawMaximizingGlass (raster, boxPoints, mul (Transforms.Scale (.3f, .3f, .3f), mul (Transforms.RotateZ (15), mul (Transforms.RotateX (0), mul (Transforms.Translate (1f, -1.3f, -1f), transform)))));
        }

        private static void DrawBookCases (Raster raster, float3[] boxPoints, float4x4 transform) {
            float4 white = float4 (1, 1, 1, 1);
            float4 red = float4 (1, 0, 0, 1);
            float4 green = float4 (0, 1, 0, 1);
            float4 blue = float4 (0, 0, 1, 1);
            float4 dark_brown = float4 (1, 1, 0, 1);
            float4 brown = float4 (1, .5f, 0, 1);

            DrawBook (raster, boxPoints, mul (Transforms.Translate (.2f, -2f, -1), transform), red, red, white);
            DrawBook (raster, boxPoints, mul (Transforms.RotateYGrad (25), mul (Transforms.Translate (0, -1f, -.4f), transform)), brown, brown, white);
            DrawBook (raster, boxPoints, mul (Transforms.RotateYGrad (-90), mul (Transforms.Translate (2f, 0, -1.4f), transform)), dark_brown, dark_brown, white);
            DrawBook (raster, boxPoints, mul (Transforms.RotateYGrad (180), mul (Transforms.Translate (2.7f, 1f, 1), transform)), green, green, white);
            DrawBook (raster, boxPoints, mul (Transforms.RotateYGrad (-90), mul (Transforms.Translate (2f, 2f, -1), transform)), blue, blue, white);
            DrawBook (raster, boxPoints, mul (Transforms.RotateYGrad (200), mul (Transforms.Translate (2.5f, 3f, .7f), transform)), green, green, white);
            DrawBook (raster, boxPoints, mul (Transforms.Translate (.2f, 4f, -1), transform), blue, blue, white);
        }

        private static void DrawBook (Raster raster, float3[] boxPoints, float4x4 transform, float4 bc_color, float4 fb_color, float4 sh_color) {
            DrawSpine (raster, boxPoints, mul (Transforms.Translate (0, -.3f, -.05f), transform), bc_color);
            DrawCover (raster, boxPoints, mul (Transforms.Translate (0, -.34f, 0), transform), fb_color);
            DrawPages (raster, boxPoints, mul (Transforms.Translate (0, -.26f, 0), transform), sh_color);
            DrawCover (raster, boxPoints, mul (Transforms.Translate (0, .4f, 0), transform), fb_color);
        }
        private static void DrawSpine (Raster raster, float3[] boxPoints, float4x4 transform, float4 color) {
            float4x4 transformingIntoBookBase = mul (Transforms.Scale (2.2f, 0.7f, 0.05f), transform);
            DrawBox (raster, boxPoints, transformingIntoBookBase, color);
        }
        private static void DrawCover (Raster raster, float3[] boxPoints, float4x4 transform, float4 color) {
            float4x4 transformingIntoFacebook = mul (Transforms.Scale (2.2f, 0.1f, 1.53f), transform);
            DrawBox (raster, boxPoints, transformingIntoFacebook, color);
        }
        private static void DrawPages (Raster raster, float3[] boxPoints, float4x4 transform, float4 color) {
            float4x4 transformingIntoSheets = mul (Transforms.Scale (2.2f, 0.56f, 1.5f), transform);
            DrawBox (raster, boxPoints, transformingIntoSheets, color);
        }

        private static void DrawMaximizingGlass (Raster raster, float3[] boxPoints, float4x4 transform) {
            float4x4 transformingIntoGlass = mul (Transforms.Scale (1f, 1f, .2f), mul (Transforms.Translate (2f, -.4f, 0), mul (Transforms.RotateY (20.4f), transform)));
            float4x4 transformingIntoSostainer = mul (Transforms.Translate (0, 0, 2), mul (Transforms.Scale (.1f, .2f, 1f), transform));

            DrawCylinder (raster, boxPoints, transformingIntoGlass);
            DrawCylinder (raster, boxPoints, transformingIntoSostainer, float4 (.02f, .02f, .02f, 1));
        }

        #region base objects
        private static void DrawBox (Raster raster, float3[] boxPoints, float4x4 transform) {
            float4x4 transformingIntoBox = mul (float4x4 (
                0.5f, 0, 0, 0,
                0, .5f, 0, 0,
                0, 0, 0.5f, 0,
                0.5f, 0.5f, 0.5f, 1
            ), transform);

            float3[] pointsToDraw = ApplyTransform (boxPoints, transformingIntoBox);
            raster.DrawPoints (pointsToDraw);
        }

        private static void DrawBox (Raster raster, float3[] boxPoints, float4x4 transform, float4 color) {
            float4x4 transformingIntoBox = mul (float4x4 (
                0.5f, 0, 0, 0,
                0, .5f, 0, 0,
                0, 0, 0.5f, 0,
                0.5f, 0.5f, 0.5f, 1
            ), transform);

            float3[] pointsToDraw = ApplyTransform (boxPoints, transformingIntoBox);
            raster.DrawPoints (pointsToDraw, color);
        }

        private static void DrawSphere (Raster raster, float3[] boxPoints, float4x4 transform) {
            float3[] points = ApplyTransform (boxPoints, float4x4 (
                2, 0, 0, 0,
                0, 2, 0, 0,
                0, 0, 2, 0,
                0, 0, 0, 1
            ));

            // Apply a free transform
            points = ApplyTransform (points, p => float3 (p.x * sin (p.z) * cos (p.y), p.x * sin (p.z) * sin (p.y), p.x * cos (p.z)));
            float3[] pointsToDraw = ApplyTransform (points, transform);
            raster.DrawPoints (pointsToDraw);
        }
        private static void DrawCylinder (Raster raster, float3[] boxPoints, float4x4 transform) {

            float3[] points = ApplyTransform (boxPoints, float4x4 (
                2, 0, 0, 0,
                0, 1.56f, 0, 0,
                0, 0, 2, 0,
                0, 0, 0, 1
            ));

            // Apply a free transform
            points = ApplyTransform (points, p => float3 (p.x * cos (p.y), p.x * sin (p.y), p.z));
            float3[] pointsToDraw = ApplyTransform (points, transform);
            raster.DrawPoints (pointsToDraw);
        }
        private static void DrawCylinder (Raster raster, float3[] boxPoints, float4x4 transform, float4 color) {

            float3[] points = ApplyTransform (boxPoints, float4x4 (
                2, 0, 0, 0,
                0, 1.56f, 0, 0,
                0, 0, 2, 0,
                0, 0, 0, 1
            ));

            // Apply a free transform
            points = ApplyTransform (points, p => float3 (p.x * cos (p.y), p.x * sin (p.y), p.z));
            float3[] pointsToDraw = ApplyTransform (points, transform);
            raster.DrawPoints (pointsToDraw, color);
        }

        #endregion

        #region table

        private static void DrawTable (Raster raster, float3[] boxPoints, float4x4 transform) {
            DrawTableLeg (raster, boxPoints, mul (Transforms.Translate (0.2f, 0, 0.2f), transform));
            DrawTableLeg (raster, boxPoints, mul (Transforms.Translate (1.6f, 0, 0.2f), transform));
            DrawTableLeg (raster, boxPoints, mul (Transforms.Translate (1.6f, 0, 1.6f), transform));
            DrawTableLeg (raster, boxPoints, mul (Transforms.Translate (0.2f, 0, 1.6f), transform));
            DrawTableTop (raster, boxPoints, mul (Transforms.Translate (0, 2, 0), transform));
        }

        private static void DrawTableTop (Raster raster, float3[] boxPoints, float4x4 transform) {
            float4x4 transformingIntoLeg = mul (Transforms.Scale (2.2f, 0.2f, 2.2f), transform);
            DrawBox (raster, boxPoints, transformingIntoLeg);
        }

        private static void DrawTableLeg (Raster raster, float3[] boxPoints, float4x4 transform) {
            float4x4 transformingIntoLeg = mul (Transforms.Scale (0.2f, 2, 0.2f), transform);
            DrawBox (raster, boxPoints, transformingIntoLeg);
        }

        #endregion
    }
}
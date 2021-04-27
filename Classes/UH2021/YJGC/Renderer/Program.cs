using System;
using GMath;
using Rendering;
using static GMath.Gfx;
using System.Collections.Generic;

namespace Renderer {
    class Program {
        struct MyVertex : IVertex<MyVertex> {
            public float3 Position { get; set; }

            public MyVertex Add (MyVertex other) {
                return new MyVertex {
                    Position = this.Position + other.Position,
                };
            }

            public MyVertex Mul (float s) {
                return new MyVertex {
                    Position = this.Position * s,
                };
            }
        }

        struct MyProjectedVertex : IProjectedVertex<MyProjectedVertex> {
            public float4 Homogeneous { get; set; }

            public MyProjectedVertex Add (MyProjectedVertex other) {
                return new MyProjectedVertex {
                    Homogeneous = this.Homogeneous + other.Homogeneous
                };
            }

            public MyProjectedVertex Mul (float s) {
                return new MyProjectedVertex {
                    Homogeneous = this.Homogeneous * s
                };
            }
        }

        static void Main (string[] args) {
            Raster<MyVertex, MyProjectedVertex> render = new Raster<MyVertex, MyProjectedVertex> (1024, 512);
            GeneratingMeshes (render);
            render.RenderTarget.Save ("test.rbm");
            Console.WriteLine ("Done.");
        }

        static float3 EvalBezier (float3[] control, float t) {
            // DeCasteljau
            if (control.Length == 1)
                return control[0]; // stop condition
            float3[] nestedPoints = new float3[control.Length - 1];
            for (int i = 0; i < nestedPoints.Length; i++)
                nestedPoints[i] = lerp (control[i], control[i + 1], t);
            return EvalBezier (nestedPoints, t);
        }

        static Mesh<MyVertex> CreateModel () {
            // Parametric representation of a sphere.
            //return Manifold<MyVertex>.Surface (10, 10, (u, v) => {
            //    float alpha = u * 2 * pi;
            //    float beta = pi / 2 - v * pi;
            //    return float3 (cos (alpha) * cos (beta) / 10, sin (beta), sin (alpha) * cos (beta));
            //});

            // Generative model
            // return Manifold<MyVertex>.Generative (10, 10,
            //     // g function
            //     u => float3 (cos (2 * pi * u) / 6, 0, sin (2 * pi * u) / 6),
            //     // f function
            //     (p, v) => p + float3 (0, v * 3 / 2, 0)
            // );

            // return Manifold<MyVertex>.Lofted (10, 10,
            //     // g function
            //     v => float3 (v, v, 0),
            //     // f function
            //     v => float3 (v, 0, v)
            // ); 

            // like an spider web
            return Manifold<MyVertex>.Generative (10, 10,
                v => mul (float4 (-cos (2 * pi * v), -1, sin (2 * pi * v), 1), Transforms.Rotate (pi / 4, float3 (0, v, 0))).xyz,
                (v, u) => v + float3 (0, u, 0)
            );

            // Revolution Sample with Bezier
            // float3[] contourn = {
            // float3 (0, -.5f, 0),
            // float3 (0.8f, -0.5f, 0),
            // float3 (1f, -0.2f, 0),
            // float3 (0.6f, 1, 0),
            // float3 (0, 1, 0)
            // };

            // Revolution Sample with Bezier (Apple)
            // float3[] contourn = {
            //     float3 (0, -.3f, 0),
            //     float3 (1.2f, -0.8f, 0),
            //     float3 (.7f, -0.5f, 0),
            //     float3 (2f, 2.7f, 1),
            //     float3 (0f, .9f, 0)
            // };
            // return Manifold<MyVertex>.Revolution (30, 30, t => EvalBezier (contourn, t), float3 (0, 1, 0));
        }

        static Mesh<MyVertex> CreateApple () {

            // Revolution Sample with Bezier (Apple)
            float3[] contourn = {
                float3 (0, -.3f, 0),
                float3 (1.2f, -0.8f, 0),
                float3 (.7f, -0.5f, 0),
                float3 (2f, 2.7f, 1),
                float3 (0f, 1.1f, 0)
            };

            return Manifold<MyVertex>.Revolution (15, 15, t => EvalBezier (contourn, t), float3 (0, 1, 0));
        }

        static List<Mesh<MyVertex>> CreateBook (int slices, int stacks, float3 p, int deg) {
            return new List<Mesh<MyVertex>> () {
                Manifold<MyVertex>.Surface (slices, stacks, (u, v) => {
                        return mul (p + float3 (0, v / 5, u * 5 / 4), transform3x3to4x4 (Transforms.RotateYGrad (deg)));
                    }).ConvertTo (Topology.Lines),
                    Manifold<MyVertex>.Surface (slices, stacks, (u, v) => {
                        return mul (p + float3 (1, v / 5, u * 5 / 4), transform3x3to4x4 (Transforms.RotateYGrad (deg)));
                    }).ConvertTo (Topology.Lines),
                    Manifold<MyVertex>.Surface (slices, stacks, (u, v) => {
                        return mul (p + float3 (v, u / 5, 0 * 5 / 4), transform3x3to4x4 (Transforms.RotateYGrad (deg)));
                    }).ConvertTo (Topology.Lines),
                    Manifold<MyVertex>.Surface (slices, stacks, (u, v) => {
                        return mul (p + float3 (v, u / 5, 1 * 5 / 4), transform3x3to4x4 (Transforms.RotateYGrad (deg)));
                    }).ConvertTo (Topology.Lines),
                    Manifold<MyVertex>.Surface (slices, stacks, (u, v) => {
                        return mul (p + float3 (v, 0, u * 5 / 4), transform3x3to4x4 (Transforms.RotateYGrad (deg)));
                    }).ConvertTo (Topology.Lines),
                    Manifold<MyVertex>.Surface (slices, stacks, (u, v) => {
                        return mul (p + float3 (v, .2f, u * 5 / 4), transform3x3to4x4 (Transforms.RotateYGrad (deg)));
                    }).ConvertTo (Topology.Lines)
            };
        }

        static float3x3 transform3x3to4x4 (float4x4 f) {
            return float3x3 (f._m00, f._m01, f._m02, f._m10, f._m11, f._m12, f._m20, f._m21, f._m22);
        }

        static List<Mesh<MyVertex>> CreateMagnifyingGlass () {
            return new List<Mesh<MyVertex>> () {
                //glass
                Manifold<MyVertex>.Surface (10, 10, (u, v) => {
                        float alpha = u * 2 * pi;
                        float beta = pi / 2 - v * pi;
                        return mul (float3 (cos (alpha) * cos (beta) / 3, sin (beta) / 3 - 1, sin (alpha) * cos (beta) / 10), transform3x3to4x4 (Transforms.RotateYGrad (30)));
                    }).ConvertTo (Topology.Lines),

                    //aro
                    Manifold<MyVertex>.Generative (30, 5,
                        u => mul (float3 (-.1f, cos (2 * pi * u) / 3 - 1, sin (2 * pi * u) / 3), transform3x3to4x4 (Transforms.RotateYGrad (60))),
                        (p, v) => mul (p + float3 (v / 20, v / 12, 0), transform3x3to4x4 (Transforms.RotateYGrad (60)))
                    ).ConvertTo (Topology.Lines),

                    Manifold<MyVertex>.Lofted (10, 10,
                        // g function
                        u => mul (float3 (cos (2 * pi * u) / 15, sin (2 * pi * u) / 15 - 1, .3f), transform3x3to4x4 (Transforms.RotateYGrad (-45))),
                        // f function
                        u => mul (float3 (cos (2 * pi * u) / 15, sin (2 * pi * u) / 15 - 1, 1.2f), transform3x3to4x4 (Transforms.RotateYGrad (-25)))
                    ).ConvertTo (Topology.Lines)
            };
        }

        private static void GeneratingMeshes (Raster<MyVertex, MyProjectedVertex> render) {
            render.ClearRT (float4 (0, 0, 0.1f, 1)); // clear with color dark blue.

            // var primitive = CreateModel ();
            // var apple = CreateApple ();
            var cube0 = CreateBook (5, 5, float3 (-.3f, -.5f, .2f), 17);
            var cube1 = CreateBook (5, 5, float3 (-.7f, -.3f, .25f), 50);
            var cube2 = CreateBook (5, 5, float3 (-.9f, -.1f, -.2f), 100);
            var cube3 = CreateBook (5, 5, float3 (-.3f, .1f, .2f), 17);
            var cube4 = CreateBook (5, 5, float3 (-.9f, .3f, -.2f), 100);
            var cube5 = CreateBook (5, 5, float3 (-.7f, .5f, .25f), 50);
            var cube6 = CreateBook (5, 5, float3 (-.1f, .7f, .2f), 17);
            var magn_glass = CreateMagnifyingGlass ();

            /// Convert to a wireframe to render. Right now only lines can be rasterized.
            // primitive = primitive.ConvertTo (Topology.Lines);
            // apple = apple.ConvertTo (Topology.Lines);

            #region viewing and projecting

            float4x4 viewMatrix = Transforms.LookAtLH (float3 (2, 1f, 4), float3 (0, 0, 0), float3 (0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH (pi_over_4, render.RenderTarget.Height / (float) render.RenderTarget.Width, 0.01f, 20);

            // Define a vertex shader that projects a vertex into the NDC.
            render.VertexShader = v => {
                float4 hPosition = float4 (v.Position, 1);
                hPosition = mul (hPosition, viewMatrix);
                hPosition = mul (hPosition, projectionMatrix);
                return new MyProjectedVertex { Homogeneous = hPosition };
            };

            // Define a pixel shader that colors using a constant value
            // render.PixelShader = p => { return float4 (p.Homogeneous.x / 1024.0f, p.Homogeneous.y / 512.0f, 1, 1); };

            #endregion

            // Draw the mesh.
            // render.DrawMesh (primitive);
            // render.DrawMesh (apple);

            // Drawing
            render.PixelShader = p => { return float4 (1, 0, 0, 1); };
            for (int i = 0; i < cube0.Count; i++) {
                render.DrawMesh (cube0[i]);
            }

            render.PixelShader = p => { return float4 (.131f, .4f, .3f, 1); };
            for (int i = 0; i < cube1.Count; i++) {
                render.DrawMesh (cube1[i]);
            }

            render.PixelShader = p => { return float4 (0, 1, 0, 1); };
            for (int i = 0; i < cube2.Count; i++) {
                render.DrawMesh (cube2[i]);
            }

            render.PixelShader = p => { return float4 (.3f, 0, 1, 1); };
            for (int i = 0; i < cube3.Count; i++) {
                render.DrawMesh (cube3[i]);
            }

            render.PixelShader = p => { return float4 (.4f, .5f, 1, 1); };
            for (int i = 0; i < cube4.Count; i++) {
                render.DrawMesh (cube4[i]);
            }

            render.PixelShader = p => { return float4 (1, 0, .5f, 1); };
            for (int i = 0; i < cube5.Count; i++) {
                render.DrawMesh (cube5[i]);
            }

            render.PixelShader = p => { return float4 (.18f, .86f, .84f, 1); };
            for (int i = 0; i < cube6.Count; i++) {
                render.DrawMesh (cube6[i]);
            }

            render.PixelShader = p => { return float4 (1, 1, 1, 1); };
            for (int i = 0; i < magn_glass.Count; i++) {
                render.DrawMesh (magn_glass[i]);
            }
        }
    }
}
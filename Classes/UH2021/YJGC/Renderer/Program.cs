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

            // Parametric representation of a cube.
            return Manifold<MyVertex>.Surface (30, 30, (u, v) => {
                float alpha = u * 2 * pi;
                float beta = pi / 2 - v * pi;
                return float3 (0, v, u);
            });

            // Generative model
            // return Manifold<MyVertex>.Generative (30, 30,
            //     // g function
            //     u => float3 (cos (2 * pi * u), 0, sin (2 * pi * u)),
            //     // f function
            //     (p, v) => p + float3 (0, v / 4, 0)
            // );

            // return Manifold<MyVertex>.Lofted (10, 10,
            //     // g function
            //     v => float3 (v, v, 0),
            //     // f function
            //     v => float3 (v, 0, v)
            // );

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

            return Manifold<MyVertex>.Revolution (35, 35, t => EvalBezier (contourn, t), float3 (0, 1, 0));
        }

        static List<Mesh<MyVertex>> CreateCube () {
            return new List<Mesh<MyVertex>> () {
                Manifold<MyVertex>.Surface (20, 20, (u, v) => {
                        return float3 (0, v / 5, u);
                    }).ConvertTo (Topology.Lines),
                    Manifold<MyVertex>.Surface (20, 20, (u, v) => {
                        return float3 (1, v / 5, u);
                    }).ConvertTo (Topology.Lines),
                    Manifold<MyVertex>.Surface (20, 20, (u, v) => {
                        return float3 (v, u / 5, 0);
                    }).ConvertTo (Topology.Lines),
                    Manifold<MyVertex>.Surface (20, 20, (u, v) => {
                        return float3 (v, u / 5, 1);
                    }).ConvertTo (Topology.Lines),
                    Manifold<MyVertex>.Surface (20, 20, (u, v) => {
                        return float3 (v, 0, u);
                    }).ConvertTo (Topology.Lines),
                    Manifold<MyVertex>.Surface (20, 20, (u, v) => {
                        return float3 (v, .2f, u);
                    }).ConvertTo (Topology.Lines)
            };
        }

        static List<Mesh<MyVertex>> CreateMagnifyingGlass () {
            return new List<Mesh<MyVertex>> () {
                Manifold<MyVertex>.Surface (10, 10, (u, v) => {
                        float alpha = u * 2 * pi;
                        float beta = pi / 2 - v * pi;
                        return float3 (cos (alpha) * cos (beta) / 9, sin (beta), sin (alpha) * cos (beta));
                    }).ConvertTo (Topology.Lines),
                    Manifold<MyVertex>.Generative (30, 10,
                        // g function
                        u => float3 (0, cos (2 * pi * u), sin (2 * pi * u)),
                        // f function
                        (p, v) => p + float3 (v / 8, 0, 0)
                    ).ConvertTo (Topology.Lines),
                    Manifold<MyVertex>.Generative (30, 30,
                        // g function
                        u => float3 (cos (2 * pi * u) / 6, -1.5f, sin (2 * pi * u) / 6),
                        // f function
                        (p, v) => p + float3 (v * 3 / 2, 0, 0)
                    ).ConvertTo (Topology.Lines)
            };
        }

        private static void GeneratingMeshes (Raster<MyVertex, MyProjectedVertex> render) {
            render.ClearRT (float4 (0, 0, 0.2f, 1)); // clear with color dark blue.

            // var primitive = CreateModel ();
            // var apple = CreateApple ();
            // var cube = CreateCube ();
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
            render.PixelShader = p => {
                return float4 (p.Homogeneous.x / 1024.0f, p.Homogeneous.y / 512.0f, 1, 1);
                // return float4 (p.Homogeneous.x / 1024.0f, p.Homogeneous.y / 512.0f, p.Homogeneous.z / 128.0f, 1);
                // return float4 (.6f, 0, 0, 1);
            };

            #endregion

            // Draw the mesh.
            // render.DrawMesh (primitive);
            // render.DrawMesh (apple);
            // for (int i = 0; i < cube.Count; i++) {
            //     render.DrawMesh (cube[i]);
            // }
            for (int i = 0; i < magn_glass.Count; i++) {
                render.DrawMesh (magn_glass[i]);
            }
        }
    }
}
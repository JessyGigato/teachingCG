using System;
using GMath;
using Rendering;
using static GMath.Gfx;
using System.Collections.Generic;
using System.Diagnostics;

namespace Renderer {
    class Program {

        static void CreateScene (Scene<float3> scene) {
            // Adding elements of the scene
            scene.Add (Raycasting.UnitarySphere, Transforms.Translate (0, 1, 0));
            scene.Add (Raycasting.PlaneXZ, Transforms.Identity);
        }

        struct PositionNormal : INormalVertex<PositionNormal> {
            public float3 Position { get; set; }
            public float3 Normal { get; set; }

            public PositionNormal Add (PositionNormal other) {
                return new PositionNormal {
                    Position = this.Position + other.Position,
                        Normal = this.Normal + other.Normal
                };
            }

            public PositionNormal Mul (float s) {
                return new PositionNormal {
                    Position = this.Position * s,
                        Normal = this.Normal * s
                };
            }

            public PositionNormal Transform (float4x4 matrix) {
                float4 p = float4 (Position, 1);
                p = mul (p, matrix);

                float4 n = float4 (Normal, 0);
                n = mul (n, matrix);

                return new PositionNormal {
                    Position = p.xyz / p.w,
                        Normal = n.xyz
                };
            }
        }

        static void CreateScene (Scene<PositionNormal> scene) {
            // Adding elements of the scene
            scene.Add (Raycasting.UnitarySphere.AttributesMap (a => new PositionNormal { Position = a, Normal = normalize (a) }),
                Transforms.Translate (0, 1, 0));
            scene.Add (Raycasting.PlaneXZ.AttributesMap (a => new PositionNormal { Position = a, Normal = float3 (0, 1, 0) }),
                Transforms.Identity);
        }

        /// <summary>
        /// Payload used to pick a color from a hit intersection
        /// </summary>
        struct MyRayPayload {
            public float3 Color;
        }

        /// <summary>
        /// Payload used to flag when a ray was shadowed.
        /// </summary>
        struct ShadowRayPayload {
            public bool Shadowed;
        }

        static void SimpleRaycast (Texture2D texture) {
            Raytracer<MyRayPayload, float3> raycaster = new Raytracer<MyRayPayload, float3> ();

            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH (float3 (2, 1f, 4), float3 (0, 0, 0), float3 (0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH (pi_over_4, texture.Height / (float) texture.Width, 0.01f, 20);

            Scene<float3> scene = new Scene<float3> ();
            CreateScene (scene);

            raycaster.OnClosestHit += delegate (IRaycastContext context, float3 attribute, ref MyRayPayload payload) {
                payload.Color = attribute;
            };

            for (float px = 0.5f; px < texture.Width; px++)
                for (float py = 0.5f; py < texture.Height; py++) {
                    RayDescription ray = RayDescription.FromScreen (px, py, texture.Width, texture.Height, inverse (viewMatrix), inverse (projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload ();

                    raycaster.Trace (scene, ray, ref coloring);

                    texture.Write ((int) px, (int) py, float4 (coloring.Color, 1));
                }
        }

        static void LitRaycast (Texture2D texture) {
            // Scene Setup
            float3 CameraPosition = float3 (1, 3.5f, 2);
            float3 LightPosition = float3 (3, 5f, 4);
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH (CameraPosition, float3 (0, 1, 0), float3 (0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH (pi_over_4, texture.Height / (float) texture.Width, 0.01f, 20);

            Scene<PositionNormal> scene = new Scene<PositionNormal> ();
            CreateScene (scene);

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormal> shadower = new Raytracer<ShadowRayPayload, PositionNormal> ();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormal attribute, ref ShadowRayPayload payload) {
                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<MyRayPayload, PositionNormal> raycaster = new Raytracer<MyRayPayload, PositionNormal> ();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormal attribute, ref MyRayPayload payload) {
                // Move geometry attribute to world space
                attribute = attribute.Transform (context.FromGeometryToWorld);

                float3 V = normalize (CameraPosition - attribute.Position);
                float3 L = normalize (LightPosition - attribute.Position);
                float lambertFactor = max (0, dot (attribute.Normal, L));

                // Check ray to light...
                ShadowRayPayload shadow = new ShadowRayPayload ();
                shadower.Trace (scene,
                    RayDescription.FromTo (attribute.Position + attribute.Normal * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                        LightPosition), ref shadow);

                payload.Color = shadow.Shadowed ? float3 (0, 0, 0) : float3 (1, 1, 1) * lambertFactor;
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload) {
                payload.Color = float3 (0, 0, 1); // Blue, as the sky.
            };

            /// Render all points of the screen
            for (int px = 0; px < texture.Width; px++)
                for (int py = 0; py < texture.Height; py++) {
                    RayDescription ray = RayDescription.FromScreen (px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse (viewMatrix), inverse (projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload ();

                    raycaster.Trace (scene, ray, ref coloring);

                    texture.Write (px, py, float4 (coloring.Color, 1));
                }
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

        static float3x3 transform4x4to3x3 (float4x4 f) {
            return float3x3 (f._m00, f._m01, f._m02, f._m10, f._m11, f._m12, f._m20, f._m21, f._m22);
        }

        static Mesh<PositionNormal> CreateGround (){
            var model = Manifold<PositionNormal>.Surface (2, 1, (u, v) => {
                return float3 (v, 0, u);}).Weld ();
            model.ComputeNormals ();
            return model;
        }

        static List<Mesh<PositionNormal>> CreateBook (int slices, int stacks, float3 p, int deg) {
            var model1 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (0, v / 5, u * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model1.ComputeNormals ();

            var model2 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (1, v / 5, u * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model2.ComputeNormals ();

            var model3 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (v, u / 5, 0 * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model3.ComputeNormals ();

            var model4 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (v, u / 5, 1.25f), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model4.ComputeNormals ();

            var model5 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (v, 0, u * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model5.ComputeNormals ();

            var model6 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (v, .2f, u), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model6.ComputeNormals ();

            return new List<Mesh<PositionNormal>> () { model1, model2, model3, model4, model5, model6 };
        }

        static List<Mesh<PositionNormal>> CreateMagnifyingGlass (float y) {
            //glass
            var model1 = Manifold<PositionNormal>.Surface (10, 10, (u, v) => {
                float alpha = u * 2 * pi;
                float beta = pi / 2 - v * pi;
                return mul (float3 (sin (beta) / 4 + -0.1f,cos (alpha) * cos (beta) / 4  - .2f + y, sin (alpha) * cos (beta) / 10 + 1.7f), transform4x4to3x3 (Transforms.RotateYGrad (22)));
            }).Weld ();
            model1.ComputeNormals ();

            //aro
            var model2 = Manifold<PositionNormal>.Lofted (30, 1,
                // g function
                u => mul (float3 (cos (2 * pi * u) / 4, sin (2 * pi * u) / 4 - .2f + y, 1.65f), transform4x4to3x3 (Transforms.RotateYGrad (20))),
                // f function
                u => mul (float3 (cos (2 * pi * u) / 4 - .03f, sin (2 * pi * u) / 4 - .2f + y, 1.75f), transform4x4to3x3 (Transforms.RotateYGrad (20)))
            ).Weld ();
            model2.ComputeNormals ();

            var model3 = Manifold<PositionNormal>.Lofted (10, 1,
                // g function
                u => mul (float3 (cos (2 * pi * u) / 18 + .5f, sin (2 * pi * u) / 18 - .35f + y, 2f), transform4x4to3x3 (Transforms.RotateYGrad (-25))),
                u => mul (float3 (cos (2 * pi * u) / 18 + 1.5f, sin (2 * pi * u) / 18 - .2f + y, 1f), transform4x4to3x3 (Transforms.RotateYGrad (-45)))
                // f function
            ).Weld ();
            model3.ComputeNormals ();

            return new List<Mesh<PositionNormal>> () { model1, model2, model3 };
        }

        static Mesh<PositionNormal> CreateModel () {
            // Revolution Sample with Bezier
            float3[] contourn = {
                float3 (0, -.5f, 0),
                float3 (0.8f, -0.5f, 0),
                float3 (1f, -0.2f, 0),
                float3 (0.6f, 1, 0),
                float3 (0, 1, 0)
            };

            /// Creates the model using a revolution of a bezier.
            /// Only Positions are updated.
            var model = Manifold<PositionNormal>.Revolution (10, 10, t => EvalBezier (contourn, t), float3 (1, 1, 0)).Weld ();
            model.ComputeNormals ();
            return model;
        }

        static void CreateMeshScene (Scene<PositionNormal> scene) {
            var model = CreateModel ();
            scene.Add (model.AsRaycast (), Transforms.Identity);
        }

        static void CreateMyMeshScene (Scene<PositionNormal> scene) {
            //(0, 1, 0)
            scene.Add (Raycasting.PlaneXZ.AttributesMap (a => new PositionNormal { Position = a, Normal = float3 (1, 1, 1) }),
                Transforms.Identity);

            // /**/
            var book1 = CreateBook (2, 1, float3 (-.3f, .05f, .2f), 17);
            var book2 = CreateBook (2, 1, float3 (-.7f, .25f, .25f), 50);
            var book3 = CreateBook (2, 1, float3 (0, .45f, -.2f), -30);
            var book4 = CreateBook (2, 1, float3 (-.3f, .65f, .2f), 17);
            var book5 = CreateBook (2, 1, float3 (0, .85f, 0), -30);
            var book6 = CreateBook (2, 1, float3 (-.7f, 1.05f, .25f), 50);
            var book7 = CreateBook (2, 1, float3 (-.1f, 1.25f, .2f), 17);

            var magn_glass = CreateMagnifyingGlass (.5f);

            foreach (var m in book1) {
                scene.Add (m.AsRaycast (), Transforms.Identity);
            }
            foreach (var m in book2) {
                scene.Add (m.AsRaycast (), Transforms.Identity);
            }
            foreach (var m in book3) {
                scene.Add (m.AsRaycast (), Transforms.Identity);
            }
            foreach (var m in book4) {
                scene.Add (m.AsRaycast (), Transforms.Identity);
            }
            foreach (var m in book5) {
                scene.Add (m.AsRaycast (), Transforms.Identity);
            }
            foreach (var m in book6) {
                scene.Add (m.AsRaycast (), Transforms.Identity);
            }
            foreach (var m in book7) {
                scene.Add (m.AsRaycast (), Transforms.Identity);
            }
            foreach (var m in magn_glass) {
                scene.Add (m.AsRaycast (), Transforms.Identity);
            }
        }

        static void RaycastingMesh (Texture2D texture) {
            // Scene Setup
            float3 CameraPosition = float3 (3, 2, 6);
            float3 LightPosition = float3 (3, 2f, 5);
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH (CameraPosition, float3 (0, 1, 0), float3 (0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH (pi_over_4, texture.Height / (float) texture.Width, 0.01f, 20);

            Scene<PositionNormal> scene = new Scene<PositionNormal> ();
            // CreateMeshScene (scene);
            CreateMyMeshScene (scene);
            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormal> shadower = new Raytracer<ShadowRayPayload, PositionNormal> ();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormal attribute, ref ShadowRayPayload payload) {
                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<MyRayPayload, PositionNormal> raycaster = new Raytracer<MyRayPayload, PositionNormal> ();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormal attribute, ref MyRayPayload payload) {
                // Move geometry attribute to world space
                attribute = attribute.Transform (context.FromGeometryToWorld);

                float3 V = normalize (CameraPosition - attribute.Position);
                float3 L = normalize (LightPosition - attribute.Position);
                float lambertFactor = max (0, dot (attribute.Normal, L));

                // Check ray to light...
                ShadowRayPayload shadow = new ShadowRayPayload ();
                shadower.Trace (scene,
                    RayDescription.FromTo (attribute.Position + attribute.Normal * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                        LightPosition), ref shadow);

                payload.Color = shadow.Shadowed ? float3 (0, 0, 0) : float3 (1, 1, 1) * lambertFactor;
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload) {
                payload.Color = float3 (0, 0, 1); // Blue, as the sky.
            };

            /// Render all points of the screen
            for (int px = 0; px < texture.Width; px++)
                for (int py = 0; py < texture.Height; py++) {
                    int progress = (px * texture.Height + py);
                    if (progress % 100 == 0) {
                        Console.Write ("\r" + progress * 100 / (float) (texture.Width * texture.Height) + "%   ");
                    }

                    RayDescription ray = RayDescription.FromScreen (px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse (viewMatrix), inverse (projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload ();

                    raycaster.Trace (scene, ray, ref coloring);

                    texture.Write (px, py, float4 (coloring.Color, 1));
                }
        }

        static void Main (string[] args) {
            Stopwatch crono = new Stopwatch ();
            crono.Start ();
            // Texture to output the image.
            Texture2D texture = new Texture2D (512, 512);

            //SimpleRaycast(texture);
            // LitRaycast(texture);
            RaycastingMesh (texture);

            texture.Save ("test.rbm");
            Console.WriteLine ("Done.");
            crono.Stop ();
            System.Console.WriteLine ("Raycasting time was: {0} sec", crono.ElapsedMilliseconds / 1000);
        }
    }
}
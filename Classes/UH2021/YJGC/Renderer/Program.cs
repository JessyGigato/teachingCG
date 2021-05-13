using GMath;
using Rendering;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using static GMath.Gfx;

namespace Renderer
{
    class Program
    {

        static void CreateScene(Scene<float3> scene)
        {
            // Adding elements of the scene
            scene.Add(Raycasting.UnitarySphere, Transforms.Translate(0,1,0));
            scene.Add(Raycasting.PlaneXZ, Transforms.Identity);
        }

        struct PositionNormal : INormalVertex<PositionNormal>
        {
            public float3 Position { get; set; }
            public float3 Normal { get; set; }

            public PositionNormal Add(PositionNormal other)
            {
                return new PositionNormal
                {
                    Position = this.Position + other.Position,
                    Normal = this.Normal + other.Normal
                };
            }

            public PositionNormal Mul(float s)
            {
                return new PositionNormal
                {
                    Position = this.Position * s,
                    Normal = this.Normal * s
                };
            }

            public PositionNormal Transform(float4x4 matrix)
            {
                float4 p = float4(Position, 1);
                p = mul(p, matrix);
                
                float4 n = float4(Normal, 0);
                n = mul(n, matrix);

                return new PositionNormal
                {
                    Position = p.xyz / p.w,
                    Normal = n.xyz
                };
            }
        }

        static void CreateScene(Scene<PositionNormal> scene)
        {
            // Adding elements of the scene
            scene.Add(Raycasting.UnitarySphere.AttributesMap(a => new PositionNormal { Position = a, Normal = normalize(a) }),
                Transforms.Translate(0, 1, 0));
            scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormal { Position = a, Normal = float3(0, 1, 0) }),
                Transforms.Identity);
        }

        /// <summary>
        /// Payload used to pick a color from a hit intersection
        /// </summary>
        struct MyRayPayload
        {
            public float3 Color;
        }

        /// <summary>
        /// Payload used to flag when a ray was shadowed.
        /// </summary>
        struct ShadowRayPayload
        {
            public bool Shadowed;
        }

        static void SimpleRaycast(Texture2D texture)
        {
            Raytracer<MyRayPayload, float3> raycaster = new Raytracer<MyRayPayload, float3>();

            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(float3(2, 1f, 4), float3(0, 0, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<float3> scene = new Scene<float3>();
            CreateScene(scene);

            raycaster.OnClosestHit += delegate (IRaycastContext context, float3 attribute, ref MyRayPayload payload)
            {
                payload.Color = attribute;
            };

            for (float px = 0.5f; px < texture.Width; px++)
                for (float py = 0.5f; py < texture.Height; py++)
                {
                    RayDescription ray = RayDescription.FromScreen(px, py, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload();

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write((int)px, (int)py, float4(coloring.Color, 1));
                }
        }

        static void LitRaycast(Texture2D texture)
        {
            // Scene Setup
            float3 CameraPosition = float3(3, 2f, 4);
            float3 LightPosition = float3(3, 5, -2);
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormal> scene = new Scene<PositionNormal>();
            CreateScene(scene);

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormal> shadower = new Raytracer<ShadowRayPayload, PositionNormal>();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormal attribute, ref ShadowRayPayload payload)
            {
                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<MyRayPayload, PositionNormal> raycaster = new Raytracer<MyRayPayload, PositionNormal>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormal attribute, ref MyRayPayload payload)
            {
                // Move geometry attribute to world space
                attribute = attribute.Transform(context.FromGeometryToWorld);

                float3 V = normalize(CameraPosition - attribute.Position);
                float3 L = normalize(LightPosition - attribute.Position);
                float lambertFactor = max(0, dot(attribute.Normal, L));

                // Check ray to light...
                ShadowRayPayload shadow = new ShadowRayPayload();
                shadower.Trace(scene, 
                    RayDescription.FromTo(attribute.Position + attribute.Normal*0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                    LightPosition), ref shadow);

                payload.Color = shadow.Shadowed ? float3(0, 0, 0) : float3(1, 1, 1) * lambertFactor;
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload)
            {
                payload.Color = float3(0, 0, 1); // Blue, as the sky.
            };

            /// Render all points of the screen
            for (int px = 0; px < texture.Width; px++)
                for (int py = 0; py < texture.Height; py++)
                {
                    RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload();

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4(coloring.Color, 1));
                }
        }


        static float3 EvalBezier(float3[] control, float t)
        {
            // DeCasteljau
            if (control.Length == 1)
                return control[0]; // stop condition
            float3[] nestedPoints = new float3[control.Length - 1];
            for (int i = 0; i < nestedPoints.Length; i++)
                nestedPoints[i] = lerp(control[i], control[i + 1], t);
            return EvalBezier(nestedPoints, t);
        }


        static Mesh<PositionNormal> CreateModel()
        {
            // Revolution Sample with Bezier
            float3[] contourn =
            {
                float3(0, -.5f,0),
                float3(0.8f, -0.5f,0),
                float3(1f, -0.2f,0),
                float3(0.6f,1,0),
                float3(0,1,0)
            };
            
            /// Creates the model using a revolution of a bezier.
            /// Only Positions are updated.
            var model = Manifold<PositionNormal>.Revolution(30, 20, t => EvalBezier(contourn, t), float3(0, 1, 0)).Weld();
            model.ComputeNormals();
            return model;
        }

        static Mesh<PositionNormal> CreateSea()
        {
            /// Creates the model using a revolution of a bezier.
            /// Only Positions are updated.
            var model = Manifold<PositionNormal>.Surface(30, 20, (u, v) => 2*float3(2 * u - 1, sin(u*15)*0.02f+cos(v*13 + u*16)*0.03f, 2 * v - 1)).Weld();
            model.ComputeNormals();
            return model;
        }

        public delegate float3 BRDF(float3 N, float3 Lin, float3 Lout);

        static BRDF LambertBRDF(float3 diffuse)
        {
            return (N, Lin, Lout) => diffuse / pi;
        }

        static BRDF BlinnBRDF(float3 specular, float power)
        {
            return (N, Lin, Lout) =>
            {
                float3 H = normalize(Lin + Lout);
                return specular * pow(max(0, dot(H, N)), power) * (power + 2) / two_pi;
            };
        }

        static BRDF Mixture(BRDF f1, BRDF f2, float alpha)
        {
            return (N, Lin, Lout) => lerp(f1(N, Lin, Lout), f2(N, Lin, Lout), alpha);
        }

        static void CreateMeshScene(Scene<PositionNormal> scene)
        {
            var sea = CreateSea();
            scene.Add(sea.AsRaycast(RaycastingMeshMode.Grid), Transforms.Identity);

            var egg = CreateModel();
            scene.Add(egg.AsRaycast(RaycastingMeshMode.Grid), Transforms.Translate(0, 0.5f, 0));
        }


        static void RaycastingMesh (Texture2D texture)
        {
            // Scene Setup
            float3 CameraPosition = float3(3, 2f, 4);
            float3 LightPosition = float3(3, 5, -2);
            float3 LightIntensity = float3(1, 1, 1) * 100;

            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormal> scene = new Scene<PositionNormal>();
            CreateMeshScene(scene);

            BRDF[] brdfs =
            {
                LambertBRDF(float3(0.4f,0.5f,1f)),  // see
                Mixture(LambertBRDF(float3(0.7f,0.7f,0.3f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), // egg
            };

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormal> shadower = new Raytracer<ShadowRayPayload, PositionNormal>();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormal attribute, ref ShadowRayPayload payload)
            {
                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<MyRayPayload, PositionNormal> raycaster = new Raytracer<MyRayPayload, PositionNormal>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormal attribute, ref MyRayPayload payload)
            {
                // Move geometry attribute to world space
                attribute = attribute.Transform(context.FromGeometryToWorld);

                float3 V = normalize(CameraPosition - attribute.Position);
                float3 L = (LightPosition - attribute.Position);
                float d = length(L);
                L /= d; // normalize direction to light reusing distance to light

                float3 N = attribute.Normal;

                float lambertFactor = max(0, dot(N, L));

                // Check ray to light...
                ShadowRayPayload shadow = new ShadowRayPayload();
                shadower.Trace(scene,
                    RayDescription.FromDir(attribute.Position + N * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                    L), ref shadow);

                float3 Intensity = (shadow.Shadowed ? 0.0f : 1.0f) * LightIntensity / (d * d);

                payload.Color = brdfs[context.GeometryIndex](N, L, V) * Intensity * lambertFactor;
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload)
            {
                payload.Color = float3(0, 0, 1); // Blue, as the sky.
            };

            /// Render all points of the screen
            for (int px = 0; px < texture.Width; px++)
                for (int py = 0; py < texture.Height; py++)
                {
                    int progress = (px * texture.Height + py);
                    if (progress % 1000 == 0)
                    {
                        Console.Write("\r" + progress * 100 / (float)(texture.Width * texture.Height) + "%            ");
                    }

                    RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload();

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4(coloring.Color, 1));
                }
        }

        #region My Code
        
        static float3x3 transform4x4to3x3 (float4x4 f) {
            return float3x3 (f._m00, f._m01, f._m02, f._m10, f._m11, f._m12, f._m20, f._m21, f._m22);
        }
        
        static List<Mesh<PositionNormal>> CreateBook1 (int slices, int stacks, float3 p, int deg) {
            var model1 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (0, u / 5, v * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model1.ComputeNormals ();

            var model2 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (1, v / 5, u * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model2.ComputeNormals ();

            var model3 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (v, u / 5, 0), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model3.ComputeNormals ();

            var model4 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (v, u / 5, 1.25f), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model4.ComputeNormals ();

            var model5 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (u, 0, v * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model5.ComputeNormals ();

            var model6 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (u, .2f, v * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model6.ComputeNormals ();

            var model51 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (u, -.02f, v * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model51.ComputeNormals ();

            var model61 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (u, .22f, v * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model61.ComputeNormals ();

            return new List<Mesh<PositionNormal>> () { model1, model2, model3, model4, model5, model6, model51, model61 };
        }



        static Mesh<PositionNormal> CreateGround (){
            var model = Manifold<PositionNormal>.Surface (2, 1, (u, v) => {
                return float3 (v, 0, u);}).Weld ();
            model.ComputeNormals ();
            return model;
        }

        static List<Mesh<PositionNormal>> CreateBook (int slices, int stacks, float3 p, int deg) {
            var model1 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (0, u / 5, v * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model1.ComputeNormals ();

            var model2 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (1, v / 5, u * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model2.ComputeNormals ();

            var model3 = Manifold<PositionNormal>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (v, u / 5, 0), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
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
                return mul (p + float3 (u, .2f, v * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model6.ComputeNormals ();

            return new List<Mesh<PositionNormal>> () { model1, model2, model3, model4, model5, model6 };
        }

        static List<Mesh<PositionNormal>> CreateMagnifyingGlass (float y) {
            //glass
            var model1 = Manifold<PositionNormal>.Surface (10, 10, (u, v) => {
                float alpha = u * 2 * pi;
                float beta = pi / 2 - v * pi;
                return mul (float3 (sin (beta) / 4f - 0.13f,cos (alpha) * cos (beta) / 4f  - .25f + y, sin (alpha) * cos (beta) / 10 + 1.7f), transform4x4to3x3 (Transforms.RotateYGrad (22)));
            }).Weld ();
            model1.ComputeNormals ();

            //aro
            var model2 = Manifold<PositionNormal>.Lofted (30, 1,
                // g function
                u => mul (float3 (cos (2 * pi * u) / 4 - .03f, sin (2 * pi * u) / 4 - .25f + y, 1.8f), transform4x4to3x3 (Transforms.RotateYGrad (20))), //delante
               
                u => mul (float3 (cos (2 * pi * u) / 4 -.11f, sin (2 * pi * u) / 4 - .23f + y, 1.65f), transform4x4to3x3 (Transforms.RotateYGrad (20))) //atras
                // f function
            ).Weld ();
            model2.ComputeNormals ();

            var model3 = Manifold<PositionNormal>.Lofted (10, 1,    
                // g function
                u => mul (float3 (cos (2 * pi * u) / 18 + .4f, sin (2 * pi * u) / 18 - .4f + y, 1.9f), transform4x4to3x3 (Transforms.RotateYGrad (-25))),
                u => mul (float3 (cos (2 * pi * u) / 18 + 1.4f, sin (2 * pi * u) / 18 - .25f + y, 0.98f), transform4x4to3x3 (Transforms.RotateYGrad (-45)))
                // f function
            ).Weld ();
            model3.ComputeNormals ();

            return new List<Mesh<PositionNormal>> () { model1, model2, model3 };
        }

        
        static void CreateMyMeshScene (Scene<PositionNormal> scene) {
            //(0, 1, 0)
            scene.Add (Raycasting.PlaneXZ.AttributesMap (a => new PositionNormal { Position = a, Normal = float3 (1, 1, 1) }),
                Transforms.Identity);

            
            scene.Add (Raycasting.PlaneYZ.AttributesMap (a => new PositionNormal { Position = a, Normal = float3 (1, 1, 1) }),
                mul(Transforms.RotateYGrad(-60), Transforms.Translate(0, 0, -3)));

            var book1 = CreateBook1 (2, 1, float3 (-.3f, .05f, .2f), 17);
            var book2 = CreateBook1 (2, 1, float3 (-.7f, .30f, .2f), 50);
            var book3 = CreateBook1 (2, 1, float3 (0, .55f, -.2f), -30);
            var book4 = CreateBook1 (2, 1, float3 (-.36f, .8f, .2f), 17);
            var book5 = CreateBook1 (2, 1, float3 (0, 1.05f, 0), -30);
            var book6 = CreateBook1 (2, 1, float3 (-.7f, 1.3f, .25f), 50);
            var book7 = CreateBook1 (2, 1, float3 (-.1f, 1.55f, .2f), 17);

            var magn_glass = CreateMagnifyingGlass (.5f);

            foreach (var m in book1) {
                scene.Add (m.AsRaycast (RaycastingMeshMode.Naive), Transforms.Identity);
            }
            foreach (var m in book2) {
                scene.Add (m.AsRaycast (RaycastingMeshMode.Naive), Transforms.Identity);
            }
            foreach (var m in book3) {
                scene.Add (m.AsRaycast (RaycastingMeshMode.Naive), Transforms.Identity);
            }
            foreach (var m in book4) {
                scene.Add (m.AsRaycast (RaycastingMeshMode.Naive), Transforms.Identity);
            }
            foreach (var m in book5) {
                scene.Add (m.AsRaycast (RaycastingMeshMode.Naive), Transforms.Identity);
            }
            foreach (var m in book6) {
                scene.Add (m.AsRaycast (RaycastingMeshMode.Naive), Transforms.Identity);
            }
            foreach (var m in book7) {
                scene.Add (m.AsRaycast (RaycastingMeshMode.Naive), Transforms.Identity);
            }

            foreach (var m in magn_glass) {
                scene.Add (m.AsRaycast (), Transforms.Identity);
            }

        }

        static void RaycastingMyMesh (Texture2D texture)
        {
            // Scene Setup
            float3 CameraPosition = float3(3, 2f, 5); //3, 2f, 5
            float3 LightPosition = float3(5, 4, 5);
            float3 LightIntensity = float3(1, 1, 1) * 200;

            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormal> scene = new Scene<PositionNormal>();
            CreateMyMeshScene(scene);

            BRDF[] brdfs =
            { 

                //ground and background
                LambertBRDF(float3(0.255F, 0.128f, 0f)),
                LambertBRDF(float3(0.255F, 0.128f, 0f)),

                ////////////////////////////////////////////////////////////////
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f),
                Mixture(LambertBRDF(float3(1, 0, 0)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 0, 0)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                //////////////////////////////////////////////////////////////////////
                ////////////////////////////////////////////////////////////////
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f),
                Mixture(LambertBRDF(float3(3 * 0.255F, 3 * 0.128f, 0f)), BlinnBRDF(float3(1,1,1), 70), 0.3f),
                Mixture(LambertBRDF(float3(3 * 0.255F, 3 * 0.128f, 0f)), BlinnBRDF(float3(1,1,1), 70), 0.3f),
                //////////////////////////////////////////////////////////////////////
                ////////////////////////////////////////////////////////////////
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f),
                Mixture(LambertBRDF(float3(4 * 0.255F, 3 * 0.128f, 0f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(4 * 0.255F, 3 * 0.128f, 0f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                //////////////////////////////////////////////////////////////////////
                ////////////////////////////////////////////////////////////////
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f),
                Mixture(LambertBRDF(float3(0, .5f, 0f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(0, .5f, 0f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                //////////////////////////////////////////////////////////////////////
                ////////////////////////////////////////////////////////////////
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f),
                Mixture(LambertBRDF(float3(0, 0, 0.5f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(0, 0, 0.5f)), BlinnBRDF(float3(1,1,1), 70), 0.3f),  
                //////////////////////////////////////////////////////////////////////
                ////////////////////////////////////////////////////////////////
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f),
                Mixture(LambertBRDF(float3(0, 0.5f, 0f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(0, 0.5f, 0f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                //////////////////////////////////////////////////////////////////////
                ////////////////////////////////////////////////////////////////
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(1, 1, 1)), BlinnBRDF(float3(1,1,1), 70), 0.3f),
                Mixture(LambertBRDF(float3(0, 0, 0.5f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(0, 0, 0.5f)), BlinnBRDF(float3(1,1,1), 70), 0.3f),  
                //////////////////////////////////////////////////////////////////////
                
                // Magn Glass
                Mixture(LambertBRDF(float3(1f, 1f, 1f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(0.9f, 0.9f, 0.9f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
                Mixture(LambertBRDF(float3(0.2f, 0.2f, 0.2f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), 
            };

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormal> shadower = new Raytracer<ShadowRayPayload, PositionNormal>();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormal attribute, ref ShadowRayPayload payload)
            {
                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<MyRayPayload, PositionNormal> raycaster = new Raytracer<MyRayPayload, PositionNormal>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormal attribute, ref MyRayPayload payload)
            {
                // Move geometry attribute to world space
                attribute = attribute.Transform(context.FromGeometryToWorld);

                float3 V = normalize(CameraPosition - attribute.Position);
                float3 L = (LightPosition - attribute.Position);
                float d = length(L);
                L /= d; // normalize direction to light reusing distance to light

                float3 N = attribute.Normal;

                float lambertFactor = max(0, dot(N, L));

                // Check ray to light...
                ShadowRayPayload shadow = new ShadowRayPayload();
                shadower.Trace(scene,
                    RayDescription.FromDir(attribute.Position + N * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                    L), ref shadow);

                float3 Intensity = (shadow.Shadowed ? 0.0f : 1.0f) * LightIntensity / (d * d);

                payload.Color = brdfs[context.GeometryIndex](N, L, V) * Intensity * lambertFactor;
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload)
            {
                payload.Color = float3(0.254f, 0.188f, 0.135f); // Blue, as the sky.
            };

            /// Render all points of the screen
            for (int px = 0; px < texture.Width; px++)
                for (int py = 0; py < texture.Height; py++)
                {
                    int progress = (px * texture.Height + py);
                    if (progress % 1000 == 0)
                    {
                        Console.Write("\r" + progress * 100 / (float)(texture.Width * texture.Height) + "%            ");
                    }

                    RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload();

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4(coloring.Color, 1));
                }
        }



        #endregion

        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            // Texture to output the image.
            Texture2D texture = new Texture2D(512, 512);

            //SimpleRaycast(texture);
            //LitRaycast(texture);
            RaycastingMyMesh(texture);

            stopwatch.Stop();

            texture.Save("test.rbm");

            Console.WriteLine("Done. Rendered in " + stopwatch.ElapsedMilliseconds + " ms");
        }
    }
}

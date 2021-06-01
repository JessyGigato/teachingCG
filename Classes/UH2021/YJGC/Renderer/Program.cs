using GMath;
using Rendering;
using System;
using System.Diagnostics;
using static GMath.Gfx;
using System.Collections.Generic;

namespace Renderer
{
    class Program
    {
        public struct PositionNormalCoordinate : INormalVertex<PositionNormalCoordinate>, ICoordinatesVertex<PositionNormalCoordinate>
        {
            public float3 Position { get; set; }
            public float3 Normal { get; set; }

            public float2 Coordinates { get; set; }

            public PositionNormalCoordinate Add(PositionNormalCoordinate other)
            {
                return new PositionNormalCoordinate
                {
                    Position = this.Position + other.Position,
                    Normal = this.Normal + other.Normal,
                    Coordinates = this.Coordinates + other.Coordinates
                };
            }

            public PositionNormalCoordinate Mul(float s)
            {
                return new PositionNormalCoordinate
                {
                    Position = this.Position * s,
                    Normal = this.Normal * s,
                    Coordinates = this.Coordinates * s
                };
            }

            public PositionNormalCoordinate Transform(float4x4 matrix)
            {
                float4 p = float4(Position, 1);
                p = mul(p, matrix);
                
                float4 n = float4(Normal, 0);
                n = mul(n, matrix);

                return new PositionNormalCoordinate
                {
                    Position = p.xyz / p.w,
                    Normal = n.xyz,
                    Coordinates = Coordinates
                };
            }
        }

        public struct Impulse
        {
            public float3 Direction;
            public float3 Ratio;
        }

        public struct Material
        {
            public float3 Emissive;

            public Texture2D DiffuseMap;
            public Texture2D BumpMap;
            public Sampler TextureSampler;

            public float3 Diffuse;
            public float3 Specular;
            public float SpecularPower;
            public float RefractionIndex;

            // 4 float values with Diffuseness, Glossyness, Mirrorness, Fresnelness
            public float WeightDiffuse { get { return 1 - OneMinusWeightDiffuse; } set { OneMinusWeightDiffuse = 1 - value; } }
            float OneMinusWeightDiffuse; // This is intended for default values of the struct to work as 1, 0, 0, 0 weight initial settings
            public float WeightGlossy; 
            public float WeightMirror; 
            public float WeightFresnel;

            public float WeightNormalization
            {
                get { return max(0.0001f, WeightDiffuse + WeightGlossy + WeightMirror + WeightFresnel); }
            }

            public float3 EvalBRDF(PositionNormalCoordinate surfel, float3 wout, float3 win)
            {
                float3 diffuse = Diffuse * (DiffuseMap == null ? float3(1, 1, 1) : DiffuseMap.Sample(TextureSampler, surfel.Coordinates).xyz) / pi;
                float3 H = normalize(win + wout);
                float3 specular = Specular * pow(max(0, dot(H, surfel.Normal)), SpecularPower) * (SpecularPower + 2) / two_pi;
                return diffuse * WeightDiffuse / WeightNormalization + specular * WeightGlossy / WeightNormalization;
            }

            // Compute fresnel reflection component given the cosine of input direction and refraction index ratio.
            // Refraction can be obtained subtracting to one.
            // Uses the Schlick's approximation
            float ComputeFresnel(float NdotL, float ratio)
            {
                float f = pow((1 - ratio) / (1 + ratio), 2);
                return (f + (1.0f - f) * pow((1.0f - NdotL), 5));
            }

            public IEnumerable<Impulse> GetBRDFImpulses(PositionNormalCoordinate surfel, float3 wout)
            {
                if (!any(Specular))
                    yield break; // No specular => Ratio == 0

                float NdotL = dot(surfel.Normal, wout);
                // Check if ray is entering the medium or leaving
                bool entering = NdotL > 0;

                // Invert all data if leaving
                NdotL = entering ? NdotL : -NdotL;
                surfel.Normal = entering ? surfel.Normal : -surfel.Normal;
                float ratio = entering ? 1.0f / this.RefractionIndex : this.RefractionIndex / 1.0f; // 1.0f air refraction index approx

                // Reflection vector
                float3 R = reflect(wout, surfel.Normal);

                // Refraction vector
                float3 T = refract(wout, surfel.Normal, ratio);

                // Reflection quantity, (1 - F) will be the refracted quantity.
                float F = ComputeFresnel(NdotL, ratio);

                if (!any(T))
                    F = 1; // total internal reflection (produced with critical angles)

                if (WeightMirror + WeightFresnel * F > 0) // something is reflected
                    yield return new Impulse
                    {
                        Direction = R,
                        Ratio = Specular * (WeightMirror + WeightFresnel * F) / WeightNormalization
                    };

                if (WeightFresnel * (1 - F) > 0) // something to refract
                    yield return new Impulse
                    {
                        Direction = T,
                        Ratio = Specular * WeightFresnel * (1 - F) / WeightNormalization
                    };
            }
        }

        #region Scenes

        static float3x3 transform4x4to3x3 (float4x4 f) {
            return float3x3 (f._m00, f._m01, f._m02, f._m10, f._m11, f._m12, f._m20, f._m21, f._m22);
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

        static List<Mesh<PositionNormalCoordinate>> CreateBook1 (int slices, int stacks, float3 p, int deg) {
            var model1 = Manifold<PositionNormalCoordinate>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (0, u / 5, v * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model1.ComputeNormals ();

            var model2 = Manifold<PositionNormalCoordinate>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (1, v / 5, u * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model2.ComputeNormals ();

            var model3 = Manifold<PositionNormalCoordinate>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (v, u / 5, 0), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model3.ComputeNormals ();

            var model4 = Manifold<PositionNormalCoordinate>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (v, u / 5, 1.25f), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model4.ComputeNormals ();

            var model5 = Manifold<PositionNormalCoordinate>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (u, 0, v * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model5.ComputeNormals ();

            var model6 = Manifold<PositionNormalCoordinate>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (u, .2f, v * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model6.ComputeNormals ();

            var model51 = Manifold<PositionNormalCoordinate>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (u, -.01f, v * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model51.ComputeNormals ();

            var model61 = Manifold<PositionNormalCoordinate>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (u, .21f, v * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model61.ComputeNormals ();


            return new List<Mesh<PositionNormalCoordinate>> () { model1, model2, model3, model4, model5, model6, model51, model61 };
        }

        static Mesh<PositionNormalCoordinate> BookSpine(int slices, int stacks, float3 p, int deg){
            //model21
            var model = Manifold<PositionNormalCoordinate>.Surface (slices, stacks, (u, v) => {
                return mul (p + float3 (1, v / 5 + .01f, u * 5 / 4), transform4x4to3x3 (Transforms.RotateYGrad (deg)));
            }).Weld ();
            model.ComputeNormals ();
            return model;
        }

        static List<Mesh<PositionNormalCoordinate>> CreateMagnifyingGlass (float y) {
            //glass
            var model1 = Manifold<PositionNormalCoordinate>.Surface (10, 10, (u, v) => {
                float alpha = u * 2 * pi;
                float beta = pi / 2 - v * pi;
                return mul (float3 (sin (beta) / 4f - 0.13f,cos (alpha) * cos (beta) / 4f  - .25f + y, sin (alpha) * cos (beta) / 10 + 1.7f), transform4x4to3x3 (Transforms.RotateYGrad (22)));
            }).Weld ();
            model1.ComputeNormals ();

            //aro
            var model2 = Manifold<PositionNormalCoordinate>.Lofted (30, 1,
                // g function
                u => mul (float3 (cos (2 * pi * u) / 4 - .03f, sin (2 * pi * u) / 4 - .25f + y, 1.8f), transform4x4to3x3 (Transforms.RotateYGrad (20))), //delante
               
                u => mul (float3 (cos (2 * pi * u) / 4 -.11f, sin (2 * pi * u) / 4 - .23f + y, 1.65f), transform4x4to3x3 (Transforms.RotateYGrad (20))) //atras
                // f function
            ).Weld ();
            model2.ComputeNormals ();

            var model3 = Manifold<PositionNormalCoordinate>.Lofted (10, 1,    
                // g function
                u => mul (float3 (cos (2 * pi * u) / 18 + .4f, sin (2 * pi * u) / 18 - .4f + y, 1.9f), transform4x4to3x3 (Transforms.RotateYGrad (-25))),
                u => mul (float3 (cos (2 * pi * u) / 18 + 1.4f, sin (2 * pi * u) / 18 - .25f + y, 1.02f), transform4x4to3x3 (Transforms.RotateYGrad (-45)))
                // f function
            ).Weld ();
            model3.ComputeNormals ();

            return new List<Mesh<PositionNormalCoordinate>> () { model1, model2, model3 };
        }

        static Mesh<PositionNormalCoordinate> CreateMagnifyingGlass1 (float y) {
            //glass
            var model1 = Manifold<PositionNormalCoordinate>.Surface (10, 10, (u, v) => {
                float alpha = u * 2 * pi;
                float beta = pi / 2 - v * pi;
                return mul (float3 (sin (beta) / 10f - 0.13f,cos (alpha) * cos (beta) / 4f  - .25f + y, sin (alpha) * cos (beta) / 4 + 1.7f), transform4x4to3x3 (Transforms.RotateYGrad (282)));
            }).Weld ();
            model1.ComputeNormals ();  

            float3[] contourn =
            {
                float3(0, -.5f, 0),
                float3(0.2f, -0.5f, 0),
                float3(.4f, -0.5f, 0),
                float3(.6f, -0.5f, 0),
                float3(0.8f, -0.5f, 0),
                // float3(1f, -0.2f,0),
                // float3(0.6f,1,0),
                float3(.8f, -0.62f, 0),
                float3(.6f, -0.64f, 0),
                float3(.4f, -0.66f, 0),
                float3(.2f, -0.68f, 0),
                float3(0, -0.7f, 0)
            };

            // float3[] contourn =
            // {
            //     float3(0, -.58f, 0),
            //     float3(0.2f, -0.56f, 0),
            //     float3(.4f, -0.54f, 0),
            //     float3(.6f, -0.52f, 0),
            //     float3(0.8f, -0.5f, 0),
            //     // float3(1f, -0.2f,0),
            //     // float3(0.6f,1,0),
            //     float3(.8f, -0.64f, 0),
            //     float3(.6f, -0.64f, 0),
            //     float3(.4f, -0.64f, 0),
            //     float3(.2f, -0.64f, 0),
            //     float3(0, -0.64f, 0)
            // };
            
            var model2 = Manifold<PositionNormalCoordinate>.Revolution(30, 30, 
                t => EvalBezier(contourn, t), float3(0, 1, 0)
            ).Weld();
            model2.ComputeNormals();
            
            return model2;
        }


        static void CreateMyMeshScene (Scene<PositionNormalCoordinate, Material> scene) {

            //Materials

            var materials = new List<float3>(){
                //ground and background
                // float3(0.255F, 0.128f, 0f),
                // float3(0.255F, 0.128f, 0f),
                //book1 
                float3(1, 1, 1),
                float3(1, 0, 0),
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(1, 0, 0),
                float3(1, 0, 0),
                float3(1, 0, 0),
                float3(1, 0, 0),
                float3(1, 0, 0),
                //book2
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(3 * 0.255F, 3 * 0.128f, 0f),
                float3(3 * 0.255F, 3 * 0.128f, 0f),
                float3(3 * 0.255F, 3 * 0.128f, 0f),
                float3(3 * 0.255F, 3 * 0.128f, 0f),
                //book3
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(4 * 0.255F, 3 * 0.128f, 0f),
                float3(4 * 0.255F, 3 * 0.128f, 0f),
                float3(4 * 0.255F, 3 * 0.128f, 0f),
                float3(3 * 0.255F, 3 * 0.128f, 0f),
                //book4
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(0, .5f, 0f),
                float3(0, .5f, 0f),
                float3(0, .5f, 0f),
                float3(0, .5f, 0f),
                //book5
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(0, 0, 0.5f),
                float3(0, 0, 0.5f),
                float3(0, 0, 0.5f),
                float3(0, 0, 0.5f),
                //book6
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(0, .5f, 0f),
                float3(0, .5f, 0f),
                float3(0, .5f, 0f),
                float3(0, .5f, 0f),
                //book7
                float3(1, 1, 1),
                float3(0, 0, 0.5f),
                float3(1, 1, 1),
                float3(1, 1, 1),
                float3(0, 0, 0.5f),
                float3(0, 0, 0.5f),
                float3(0, 0, 0.5f),
                float3(0, 0, 0.5f),
                float3(0, 0, 0.5f),
                //magnifying glass
                // float3(1f, 1f, 1f),
                float3(0.95f, 0.95f, 0.95f),
                float3(0.1f, 0.1f, 0.1f)
            };

            Texture2D planeTexture = Texture2D.LoadFromFile("wood.jpeg");
            Texture2D pagesTexture = Texture2D.LoadFromFile("pages.jpg");

            scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(a.x*0.1f, a.z*0.1f), Normal = float3(0, 1, 0) }),
                new Material { DiffuseMap = planeTexture, Diffuse = float3(1, 1, 1), TextureSampler = new Sampler { Wrap = WrapMode.Repeat, MinMagFilter = Filter.Linear } },
                Transforms.RotateYGrad(34));//Transforms.Translate(float3(0, -1, 0)));//

            scene.Add(Raycasting.PlaneYZ.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(a.y*0.1f, a.z*0.1f), Normal = float3(0, 1, 0) }),
                new Material { DiffuseMap = planeTexture, Diffuse = float3(1, 1, 1), TextureSampler = new Sampler { Wrap = WrapMode.Repeat, MinMagFilter = Filter.Linear } },
                mul(Transforms.Translate(0, 0, -2),Transforms.RotateYGrad(-64)));

            // scene.Add (Raycasting.PlaneXZ.AttributesMap (a => new PositionNormalCoordinate { Position = a, Normal = float3 (1, 1, 1) }),
            //     Transforms.Identity);

            // scene.Add (Raycasting.PlaneYZ.AttributesMap (a => new PositionNormalCoordinate { Position = a, Normal = float3 (1, 1, 1) }),
            //     mul(Transforms.RotateYGrad(-60), Transforms.Translate(0, 0, -3)));

            var book1 = CreateBook1 (2, 1, float3 (-.3f, .02f, .2f), 17);
            var spine1 = BookSpine(2, 1, float3 (-.3f, .02f, .2f), 17);
            var book2 = CreateBook1 (2, 1, float3 (-.7f, .25f, .2f), 50);
            var book3 = CreateBook1 (2, 1, float3 (0, .48f, -.2f), -30);
            var book4 = CreateBook1 (2, 1, float3 (-.36f, .71f, .2f), 17);
            var book5 = CreateBook1 (2, 1, float3 (0, .94f, 0), -30);
            var book6 = CreateBook1 (2, 1, float3 (-.7f, 1.17f, .25f), 50);
            var book7 = CreateBook1 (2, 1, float3 (-.1f, 1.40f, .2f), 17);
            var spine2 = BookSpine(2, 1, float3 (-.1f, 1.40f, .2f), 17);

            var magn_glass = CreateMagnifyingGlass (.5f);
            var magn_glass1 = CreateMagnifyingGlass1 (.5f);


            int i = 0; //to iterate over the materials
            

            foreach (var m in book1) {
                scene.Add(m.AsRaycast(), new Material
                {
                    Specular = float3(1, 1, 1) * 0.1f,
                    SpecularPower = 60,
                    Diffuse = materials[i++]
                },
                Transforms.Identity);
            }

            scene.Add(spine1.AsRaycast(), new Material
                {
                    Specular = float3(1, 1, 1)*0.1f,
                    SpecularPower = 60,
                    Diffuse = materials[i++]
                },
                Transforms.Translate(0, -.01f, 0));

            foreach (var m in book2) {
                scene.Add(m.AsRaycast(), new Material
                {
                    Specular = float3(1, 1, 1)*0.1f,
                    SpecularPower = 60,
                    Diffuse = materials[i++]
                },
                Transforms.Identity);
            }
            foreach (var m in book3) {
                scene.Add(m.AsRaycast(), new Material
                {
                    Specular = float3(1, 1, 1)*0.1f,
                    SpecularPower = 60,
                    Diffuse = materials[i++]
                },
                Transforms.Identity);
            }
            foreach (var m in book4) {
                scene.Add(m.AsRaycast(), new Material
                {
                    Specular = float3(1, 1, 1)*0.1f,
                    SpecularPower = 60,
                    Diffuse = materials[i++]
                },
                Transforms.Identity);
            }
            foreach (var m in book5) {
                scene.Add(m.AsRaycast(), new Material
                {
                    Specular = float3(1, 1, 1)*0.1f,
                    SpecularPower = 60,
                    Diffuse = materials[i++]
                },
                Transforms.Identity);
            }
            foreach (var m in book6) {
                scene.Add(m.AsRaycast(), new Material
                {
                    Specular = float3(1, 1, 1)*0.1f,
                    SpecularPower = 60,
                    Diffuse = materials[i++]
                },
                Transforms.Identity);
            }
            foreach (var m in book7) {
                scene.Add(m.AsRaycast(), new Material
                {
                    Specular = float3(1, 1, 1)*0.1f,
                    SpecularPower = 60,
                    Diffuse = materials[i++]
                },
                Transforms.Identity);
            }
            scene.Add(spine2.AsRaycast(), new Material
                {
                    Specular = float3(1, 1, 1)*0.1f,
                    SpecularPower = 60,
                    Diffuse = materials[i++]
                },
                Transforms.Identity);
            

        //     // MagnifyingGlass

            // scene.Add(magn_glass[0].AsRaycast(), new Material
            // {
            //     Specular = float3(1, 1, 1),
            //     SpecularPower = 260,

            //     WeightDiffuse = 0,
            //     WeightFresnel = 1.0f, // Glass sphere
            //     RefractionIndex = 1.6f
            // },
            //     Transforms.Identity);

            scene.Add(magn_glass[1].AsRaycast(), new Material
                {
                    Specular = float3(1, 1, 1)*0.1f,
                    SpecularPower = 60,
                    Diffuse = materials[i++]
                },
                Transforms.Identity);

            scene.Add(magn_glass[2].AsRaycast(), new Material
                {
                    Specular = float3(1, 1, 1)*0.1f,
                    SpecularPower = 60,
                    Diffuse = materials[i++]
                },
                Transforms.Identity);
            
            scene.Add(magn_glass1.AsRaycast(), new Material
            {
                Specular = float3(1, 1, 1),
                SpecularPower = 300,

                WeightDiffuse = 0,
                WeightFresnel = 1.0f, // Glass sphere
                RefractionIndex = .8f
            },
               mul (Transforms.Scale(.4f, .4f, .4f), mul(Transforms.Translate(float3(.09f, -1.52f, .245f)), mul(Transforms.RotateXGrad(90), Transforms.RotateYGrad(15)))));
            //    mul(Transforms.Translate(float3(-1, -1f, .5f)), mul(Transforms.Scale(.4f, .4f, .4f), mul(Transforms.RotateXGrad(100), Transforms.RotateYGrad(0)))));

        }



        static void CreateRaycastScene(Scene<PositionNormalCoordinate, Material> scene)
        {
            Texture2D planeTexture = Texture2D.LoadFromFile("wood.jpeg");

            var sphereModel = Raycasting.UnitarySphere.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(atan2(a.z, a.x) * 0.5f / pi + 0.5f, a.y), Normal = normalize(a) });
            
            // Adding elements of the scene
            scene.Add(sphereModel, new Material
            {
                Specular = float3(1, 1, 1),
                SpecularPower = 360,

                WeightDiffuse = 0,
                WeightFresnel = 1.0f, // Glass sphere
                RefractionIndex = 1.3f
            },
                mul(Transforms.Scale(float3(.2f, 1, 1)), Transforms.Translate(2, 1, 1f)));

            // scene.Add(sphereModel, new Material
            // {
            //     Specular = float3(1, 1, 1),
            //     SpecularPower = 260,

            //     WeightDiffuse = 0,
            //     WeightMirror = 1.0f, // Mirror sphere
            // },
            //     Transforms.Translate(1.5f, 1, 0));

            scene.Add(sphereModel, new Material
            {
                Specular = float3(1, 1, 1)*0.1f,
                SpecularPower = 60,
                Diffuse = float3(.5f, .5f, 1)
            },
                Transforms.Translate(-1.5f, 1, 0));

            scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(a.x*0.2f, a.z*0.2f), Normal = float3(0, 1, 0) }),
                new Material { DiffuseMap = planeTexture, Diffuse = float3(1, 1, 1), TextureSampler = new Sampler { Wrap = WrapMode.Repeat, MinMagFilter = Filter.Linear } },
                Transforms.Identity);

            // Light source
            scene.Add(sphereModel, new Material
            {
                Emissive = LightIntensity / (4 * pi), // power per unit area
                WeightDiffuse = 0,
                WeightFresnel = 1.0f, // Glass sphere
                RefractionIndex = 1.0f
            },
               mul(Transforms.Scale(0.4f), Transforms.Translate(LightPosition)));
        }

        #endregion

        /// <summary>
        /// Payload used to pick a color from a hit intersection
        /// </summary>
        struct MyRayPayload
        {
            public float3 Color;
            public int Bounces; // Maximum value of allowed bounces
        }

        /// <summary>
        /// Payload used to flag when a ray was shadowed.
        /// </summary>
        struct ShadowRayPayload
        {
            public bool Shadowed;
        }

        // Scene Setup
        static float3 CameraPosition = float3(3.5f, 2f, 4f);//float3(7, 3, 2);//
        static float3 LightPosition = float3(5, 6, 5);//float3(4, 1f, 5);//
        static float3 LightIntensity = float3(1, 1, 1) * 200;

        static void Raytracing (Texture2D texture)
        {
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormalCoordinate, Material> scene = new Scene<PositionNormalCoordinate, Material>();
            CreateMyMeshScene(scene);
            // CreateRaycastScene(scene);

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormalCoordinate, Material> shadower = new Raytracer<ShadowRayPayload, PositionNormalCoordinate, Material>();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref ShadowRayPayload payload)
            {
                if (any(material.Emissive))
                    return HitResult.Discard; // Discard light sources during shadow test.

                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<MyRayPayload, PositionNormalCoordinate, Material> raycaster = new Raytracer<MyRayPayload, PositionNormalCoordinate, Material>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref MyRayPayload payload)
            {
                // Move geometry attribute to world space
                attribute = attribute.Transform(context.FromGeometryToWorld);

                float3 V = -normalize(context.GlobalRay.Direction);

                float3 L = (LightPosition - attribute.Position);
                float d = length(L);
                L /= d; // normalize direction to light reusing distance to light

                attribute.Normal = normalize(attribute.Normal);

                if (material.BumpMap != null)
                {
                    float3 T, B;
                    createOrthoBasis(attribute.Normal, out T, out B);
                    float3 tangentBump = material.BumpMap.Sample(material.TextureSampler, attribute.Coordinates).xyz * 2 - 1;
                    float3 globalBump = tangentBump.x * T + tangentBump.y * B + tangentBump.z * attribute.Normal;
                    attribute.Normal = globalBump;// normalize(attribute.Normal + globalBump * 5f);
                }

                float lambertFactor = max(0, dot(attribute.Normal, L));

                // Check ray to light...
                ShadowRayPayload shadow = new ShadowRayPayload();
                shadower.Trace(scene,
                    RayDescription.FromDir(attribute.Position + attribute.Normal * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                    L), ref shadow);

                float3 Intensity = (shadow.Shadowed ? 0.2f : 1.0f) * LightIntensity / (d * d);

                payload.Color = material.Emissive + material.EvalBRDF(attribute, V, L) * Intensity * lambertFactor; // direct light computation

                // Recursive calls for indirect light due to reflections and refractions
                if (payload.Bounces > 0)
                    foreach (var impulse in material.GetBRDFImpulses(attribute, V))
                    {
                        float3 D = impulse.Direction; // recursive direction to check
                        float3 facedNormal = dot(D, attribute.Normal) > 0 ? attribute.Normal : -attribute.Normal; // normal respect to direction

                        RayDescription ray = new RayDescription { Direction = D, Origin = attribute.Position + facedNormal * 0.001f, MinT = 0.0001f, MaxT = 10000 };

                        MyRayPayload newPayload = new MyRayPayload
                        {
                            Bounces = payload.Bounces - 1
                        };

                        raycaster.Trace(scene, ray, ref newPayload);

                        payload.Color += newPayload.Color * impulse.Ratio;
                    }
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload)
            {
                payload.Color = float3(0, 0, 0); // Blue, as the sky.
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
                    coloring.Bounces = 3;

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4(coloring.Color, 1));
                }
        }

        public static void Main()
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            // Texture to output the image.
            Texture2D texture = new Texture2D(512, 512);

            Raytracing(texture);

            stopwatch.Stop();

            texture.Save("test.rbm");

            Console.WriteLine("Done. Rendered in " + stopwatch.ElapsedMilliseconds + " ms");
        }
    }
}

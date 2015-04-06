﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTKExtensions;
using OpenTK.Graphics.OpenGL;
using System.IO;
using Utils;
using OpenTKExtensions.Framework;

namespace TerrainGeneration
{

    /// <summary>
    /// GPU-based particle erosion.
    /// 
    /// An attempt to get the original CPU-based particle erosion working on the GPU
    /// 
    /// Needs the following textures/buffers
    /// 
    /// Terrain Layers:  R = hard, G = soft, B = water depth, A = particle life (1.0 = fully alive, 0.0 = dead)
    /// L0: terrain layer texture (read)
    /// L1: terrain layer texture (write)
    /// 
    /// P0: particle state: RG = pos, B = carrying, A = ? (maybe death flag or carrying capacity)
    /// P1: particle state: RG = pos, B = carrying, A = ?
    /// NOTE: pos is normalized 0-1
    /// 
    /// V0: particle velocity: RG = vel, B = new carrying capacity, A = death flag
    /// V1: particle velocity: RG = vel, B = new carrying capacity, A = death flag
    /// 
    /// E: terrain erosion accumulation: R = particle count, G = total erosion potential, B = total material deposit
    /// 
    /// 
    /// Step 1: Determine particle velocities
    ///  - Render as quad over particles
    ///  - Input: L0, P0, V1
    ///  - Output: V0
    ///  Uses slope information from L0 at position P0 to calculate acceleration of particle
    ///  Takes velocity from V1.rg, applies acceleration, writes to V0.rg
    ///  Calculates new carrying capacity and writes to V0.b 
    ///  If slope information indicates particle is stuck, zero/reduce new carrying capacity, set death flag V0.a.
    ///  
    /// Step 2: Accumulate erosion/sediment
    ///  - Render as particles over layer
    ///  - Input: P0, V0
    ///  - Output: E
    ///   Vertex shader sets particle position in E based on P0.
    ///   Calculate particle potential as (new carrying capacity - carrying amount).
    ///   Writes R:1 G:potential B:deposit
    ///   Blend mode: add
    ///  
    /// Step 3: Update terrain layers
    ///  - Render as quad over layer
    ///  - Input: L0, E
    ///  - Output: L1
    ///  Adds deposit amount E.b to soft L0.g
    ///  Subtracts material from soft, then hard.
    ///  Modifies water depth from particle count (E.r).
    ///  
    /// Step 4: Update particle state
    ///  - Render as quad over particles
    ///  - Input: P0, V0, E, L0
    ///  - Output: P1
    ///  Replicate particle potential calc from Step 2 to get deposit/erode potentials.
    ///  Subtract deposit amount from carrying amount P0.b, write to P1.b
    ///  Apply same calculation as step 3 to determine how much soft/hard is being eroded from L0(P0.rg).
    ///  Add material carried based on particle share of total V0.b / E(P0.rg).g -> P1.b
    ///  Calculate new particle position by intersecting ray P0.rg->V0.rg against 
    ///    cell boundaries. Add small offset to avoid boundary problems. Writes to P1.rg
    ///  If death flag indicates particle recycle, init particle at random position.
    ///  
    /// Step 5: Slip flow
    /// in: L1
    /// out: flow
    /// 
    /// Step 6: Slip transport
    /// in: L1,flow
    /// out: L0    (avoids L0/L1 switch)
    /// 
    /// 
    /// Final:
    /// Copy V0->V1
    /// Switch P0/P1
    ///  
    /// 
    /// Visualisation:
    /// - Render as lines over view gbuffer
    /// - Input: P0,P1,Heightmap
    /// - Use P0.rg->P1.rg as the line.
    /// 
    /// 
    /// 
    /// 
    /// </summary>
    public class GPUParticleErosion : GameComponentBase, ITerrainGen, IListTextures, IReloadable
    {
        private const string P_DEPOSITRATE = "erosion-depositrate";
        private const string P_EROSIONRATE = "erosion-erosionrate";
        private const string P_DELTATIME = "erosion-deltatime";
        private const string P_HARDFACTOR = "erosion-hardfactor";
        private const string P_CARRYCAPLOWPASS = "erosion-capacitylowpass";
        private const string P_CARRYSPEED = "erosion-carryingspeed";
        private const string P_WATERHEIGHT = "erosion-waterheight";
        private const string P_WATERDECAY = "erosion-waterdecay";
        private const string P_PARTICLEWATERDEPTH = "erosion-particlewaterdepth";
        private const string P_SLIPTHRESHOLD = "erosion-slipthreshold";
        private const string P_SLIPRATE = "erosion-sliprate";
        private const string P_SATURATIONSLIP = "erosion-saturationslip";
        private const string P_SATURATIONRATE = "erosion-saturationrate";
        private const string P_SATURATIONTHRESHOLD = "erosion-satthreshold";
        private const string P_DEATHRATE = "erosion-deathrate";

        private const string P_FALLRAND = "erosion-fallrandom";



        const int FILEMAGIC = 0x54455230;
        public bool NeedThread { get { return false; } }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int ParticleTexWidth { get; set; }
        public int ParticleTexHeight { get; set; }

        public float WaterHeightFactor { get { return (float)this.Parameters[P_WATERHEIGHT].GetValue(); } }

        private float initialMinHeight = 0f;
        private float initialMaxHeight = 1000f;

        public GPUParticleErosion(int width, int height, int particleTexWidth, int particleTexHeight)
        {
            this.Width = width;
            this.Height = height;
            this.ParticleTexWidth = particleTexWidth;
            this.ParticleTexHeight = particleTexHeight;

            this.Loading += GPUParticleErosion_Loading;
        }

        void GPUParticleErosion_Loading(object sender, EventArgs e)
        {
            Init(); 
        }


        /// <summary>
        /// Terrain layer texture.
        /// 
        /// R: Rock (hard).
        /// G: Soil (soft).
        /// B: Water depth.
        /// A: Water saturation (long-term low-pass of particle count in cell)
        /// 
        /// managed here, copied to terrain tile and terrain global
        /// need 2 copies as we ping-pong between them each iteration
        /// </summary>
        private Texture[] TerrainTexture = new Texture[2];
        public Texture CurrentTerrainTexture { get { return this.TerrainTexture[0]; } }
        public Texture TempTerrainTexture { get { return this.TerrainTexture[1]; } }

        /// <summary>
        /// Particle state texture.
        /// 
        /// RG: xy of particle, 0-width/height in terrain space.
        /// B: carrying amount
        /// </summary>
        private Texture[] ParticleStateTexture = new Texture[2];
        public Texture CurrentParticleTexture { get { return this.ParticleStateTexture[0]; } }

        /// <summary>
        /// Particle velocity texture.
        /// 
        /// RG: velocity
        /// B: new carrying capacity
        /// A: death flag
        /// </summary>
        private Texture[] VelocityTexture = new Texture[2];


        /// <summary>
        /// terrain erosion accumulation: R = particle count, G = total erosion potential, B = total material deposit, A = total sediment carried
        /// </summary>
        private Texture ErosionAccumulationTexture;
        public Texture ErosionTex { get { return ErosionAccumulationTexture; } }


        /// <summary>
        /// Outflow due to soft material creep 
        /// Ortho + Diagonal
        /// 
        /// RGBA = N S W E 
        /// RGBA = NW NE SW SE
        /// </summary>
        private Texture[] SlipFlowTexture = new Texture[2];



        // particle VBOs for accumulation rendering

        // Step 1: Determine particle velocities
        private GBufferShaderStep ComputeVelocityStep = new GBufferShaderStep("gpupe-1-velocity");

        // Step 2: Accumulate erosion/sediment
        private VBO ParticleVertexVBO = new VBO("particle-vertex");
        private VBO ParticleIndexVBO = new VBO("particle-index", BufferTarget.ElementArrayBuffer);
        private GBufferShaderStep DepositionAccumulationStep = new GBufferShaderStep("gpupe-2-depositaccumulation");
        private GBufferShaderStep ErosionAccumulationStep = new GBufferShaderStep("gpupe-2b-erosionaccumulation");
        //private ShaderProgram ErosionAccumulationProgram = new ShaderProgram("erosion-accumulation");
        //private GBuffer ErosionAccumulationGBuffer = new GBuffer("erosion-accumulation");

        // Step 3: Update terrain layers
        private GBufferShaderStep UpdateLayersStep = new GBufferShaderStep("gpupe-3-updatelayers");

        // Step 4: Update particle state
        private GBufferShaderStep UpdateParticlesStep = new GBufferShaderStep("gpupe-4-updateparticles");

        // Step 5,6: Slip 
        private GBufferShaderStep SlippageFlowStep = new GBufferShaderStep("erosion-slipflow");
        private GBufferShaderStep SlippageTransportStep = new GBufferShaderStep("erosion-sliptransport");

        // Copy particles from buffer 1 to buffer 0
        private GBufferShaderStep CopyParticlesStep = new GBufferShaderStep("gpupe-copyparticles");
        private GBufferShaderStep CopyVelocityStep = new GBufferShaderStep("gpupe-copyvelocity");


        private ParameterCollection parameters = new ParameterCollection();
        public ParameterCollection Parameters { get { return parameters; } }

        private IEnumerable<GBufferShaderStep> Steps()
        {
            yield return ComputeVelocityStep;
            yield return ErosionAccumulationStep;
            yield return UpdateLayersStep;
            yield return UpdateParticlesStep;
            yield return SlippageFlowStep;
            yield return SlippageTransportStep;
            yield return CopyParticlesStep;
            yield return CopyVelocityStep;
        }


        public IEnumerable<Texture> Textures()
        {
            for (int i = 0; i < 2; i++)
            {
                yield return this.TerrainTexture[i];
                yield return this.ParticleStateTexture[i];
                yield return this.VelocityTexture[i];
                yield return this.SlipFlowTexture[i];
            }
            yield return this.ErosionAccumulationTexture;
        }



        public float GetHeightAt(float x, float y)
        {
            return 0f;
        }

        public void Init()
        {
            // setup parameters
            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_DEPOSITRATE, 0.06f, 0.0f, 1.0f));
            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_EROSIONRATE, 0.05f, 0.0f, 1.0f));
            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_HARDFACTOR, 0.05f, 0.0f, 1.0f));
            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_DELTATIME, 0.5f, 0.0f, 1.0f));

            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_CARRYCAPLOWPASS, 0.3f, 0.0f, 1.0f));
            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_CARRYSPEED, 0.25f, 0.0f, 10.0f, 0.001f));

            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_WATERHEIGHT, 0.00f, 0.0f, 1.0f, 0.001f));
            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_WATERDECAY, 0.94f, 0.0f, 1.0f, 0.001f));
            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_PARTICLEWATERDEPTH, 0.003f, 0.0f, 0.1f, 0.001f));

            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_SLIPTHRESHOLD, 1.0f, 0.0f, 4.0f, 0.001f));
            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_SLIPRATE, 0.001f, 0.0f, 0.1f, 0.001f));
            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_SATURATIONSLIP, 50.0f, 0.0f, 200.0f, 0.01f));
            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_SATURATIONTHRESHOLD, 0.0016f, 0.0f, 0.1f, 0.001f));
            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_SATURATIONRATE, 0.1f, 0.0f, 10.0f, 0.001f));

            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_DEATHRATE, 0.002f, 0.0f, 0.1f, 0.001f));

            this.Parameters.Add(Parameter<float>.NewLinearParameter(P_FALLRAND, 0.02f, 0.0f, 2.0f, 0.001f));

            // setup textures
            for (int i = 0; i < 2; i++)
            {
                this.TerrainTexture[i] =
                    new Texture("Terrain" + i.ToString(), this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

                this.TerrainTexture[i].UploadEmpty();

                this.ParticleStateTexture[i] =
                    new Texture("ParticleState" + i.ToString(), this.ParticleTexWidth, this.ParticleTexHeight, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

                this.ParticleStateTexture[i].UploadEmpty();

                this.VelocityTexture[i] =
                    new Texture("Velocity" + i.ToString(), this.ParticleTexWidth, this.ParticleTexHeight, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

                this.VelocityTexture[i].UploadEmpty();

                this.SlipFlowTexture[i] =
                    new Texture("Slip" + i.ToString(),this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                    .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

                this.SlipFlowTexture[i].UploadEmpty();
            }

            this.ErosionAccumulationTexture = new Texture("Erosion",this.Width, this.Height, TextureTarget.Texture2D, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float)
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat))
                .SetParameter(new TextureParameterInt(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat));

            this.ErosionAccumulationTexture.UploadEmpty();




            // init particle vbos
            InitParticlesVBOs();

            // random start
            RandomizeParticles(this.ParticleStateTexture[0]);

            // Step 1: Determine particle velocities
            //  - Render as quad over particles
            //  - Input: L0, P0, V1
            //  - Output: V0
            //  Uses slope information from L0 at position P0 to calculate acceleration of particle
            //  Takes velocity from V1.rg, applies acceleration, writes to V0.rg
            //  Calculates new carrying capacity and writes to V0.b 
            //  If slope information indicates particle is stuck, zero/reduce new carrying capacity, set death flag V0.a.
            ComputeVelocityStep.SetOutputTexture(0, "out_Velocity", this.VelocityTexture[0]);
            ComputeVelocityStep.Init(@"BasicQuad.vert", @"ParticleErosion.glsl|ComputeVelocity");



            // Step 2: Accumulate erosion/sediment
            //  - Render as particles over layer
            //  - Input: P0, V0
            //  - Output: E
            //   Vertex shader sets particle position in E based on P0.
            //   Calculate particle potential as (new carrying capacity - carrying amount).
            //   Writes R:1 G:potential B:deposit
            //   Blend mode: add
            //
            // Split into deposit and erosion - erosion takes place from position + velocity
            DepositionAccumulationStep.SetOutputTexture(0, "out_Erosion", this.ErosionAccumulationTexture);
            DepositionAccumulationStep.Init(@"ParticleErosion.glsl|DepositVertex", @"ParticleErosion.glsl|Deposit");

            ErosionAccumulationStep.SetOutputTexture(0, "out_Erosion", this.ErosionAccumulationTexture);
            ErosionAccumulationStep.Init(@"ParticleErosion.glsl|ErosionVertex", @"ParticleErosion.glsl|Erosion");


            // Step 3: Update terrain layers
            //  - Render as quad over layer
            //  - Input: L0, E
            //  - Output: L1
            //  Adds deposit amount E.b to soft L0.g
            //  Subtracts material from soft, then hard.
            //  Modifies water depth from particle count (E.r).
            UpdateLayersStep.SetOutputTexture(0, "out_Terrain", this.TerrainTexture[1]);
            UpdateLayersStep.Init(@"BasicQuad.vert", @"ParticleErosion.glsl|UpdateTerrain");


            // Step 4: Update particle state
            //  - Render as quad over particles
            //  - Input: P0, V0, E, L0
            //  - Output: P1
            //  Replicate particle potential calc from Step 2 to get deposit/erode potentials.
            //  Subtract deposit amount from carrying amount P0.b, write to P1.b
            //  Apply same calculation as step 3 to determine how much soft/hard is being eroded from L0(P0.rg).
            //  Add material carried based on particle share of total V0.b / E(P0.rg).g -> P1.b
            //  Calculate new particle position by intersecting ray P0.rg->V0.rg against 
            //    cell boundaries. Add small offset to avoid boundary problems. Writes to P1.rg
            //  If death flag indicates particle recycle, init particle at random position.
            UpdateParticlesStep.SetOutputTexture(0, "out_Particle", this.ParticleStateTexture[1]);
            UpdateParticlesStep.Init(@"BasicQuad.vert", @"ParticleErosion.glsl|UpdateParticles");

            //  
            // Step 5: Slip flow
            // in: L1
            // out: flow
            // 
            // Step 6: Slip transport
            // in: L1,flow
            // out: L0    (avoids L0/L1 switch)

            SlippageFlowStep.SetOutputTexture(0, "out_SlipO", this.SlipFlowTexture[0]);
            SlippageFlowStep.SetOutputTexture(1, "out_SlipD", this.SlipFlowTexture[1]);
            SlippageFlowStep.Init(@"BasicQuad.vert", @"SoftSlip.glsl|Outflow");

            SlippageTransportStep.SetOutputTexture(0, "out_Terrain", this.TerrainTexture[0]);
            SlippageTransportStep.Init(@"BasicQuad.vert", @"SoftSlip.glsl|Transport");


            // copy particles
            CopyParticlesStep.SetOutputTexture(0, "out_Particle", this.ParticleStateTexture[0]);
            CopyParticlesStep.Init(@"BasicQuad.vert", @"ParticleErosion.glsl|CopyParticles");

            CopyVelocityStep.SetOutputTexture(0, "out_Velocity", this.VelocityTexture[1]);
            CopyVelocityStep.Init(@"BasicQuad.vert", @"ParticleErosion.glsl|CopyVelocity");
        }


        public void ModifyTerrain()
        {
            var rand = new Random();

            float deltaTime = (float)this.Parameters[P_DELTATIME].GetValue();
            float depositRate = (float)this.Parameters[P_DEPOSITRATE].GetValue();
            float erosionRate = (float)this.Parameters[P_EROSIONRATE].GetValue();
            float hardErosionFactor = (float)this.Parameters[P_HARDFACTOR].GetValue();

            ComputeVelocityStep.Render(
                () =>
                {
                    this.TerrainTexture[0].Bind(TextureUnit.Texture0);
                    this.ParticleStateTexture[0].Bind(TextureUnit.Texture1);
                    this.VelocityTexture[1].Bind(TextureUnit.Texture2);
                },
                (sp) =>
                {
                    sp.SetUniform("terraintex", 0);
                    sp.SetUniform("particletex", 1);
                    sp.SetUniform("velocitytex", 2);
                    sp.SetUniform("texsize", (float)this.Width);
                    sp.SetUniform("carryingCapacityLowpass", (float)this.Parameters[P_CARRYCAPLOWPASS].GetValue());
                    sp.SetUniform("speedCarryingCoefficient", (float)this.Parameters[P_CARRYSPEED].GetValue());
                    sp.SetUniform("waterHeightFactor", (float)this.Parameters[P_WATERHEIGHT].GetValue());
                    sp.SetUniform("fallRand", (float)this.Parameters[P_FALLRAND].GetValue());
                    sp.SetUniform("randSeed", (float)rand.NextDouble());
                });

            // accumulate erosion

            DepositionAccumulationStep.Render(
                () =>
                {
                    this.ParticleStateTexture[0].Bind(TextureUnit.Texture0);
                    this.VelocityTexture[0].Bind(TextureUnit.Texture1);
                },
                (sp) =>
                {
                    sp.SetUniform("particletex", 0);
                    sp.SetUniform("velocitytex", 1);
                    sp.SetUniform("deltatime", deltaTime);
                    sp.SetUniform("depositRate", depositRate);
                    sp.SetUniform("erosionRate", erosionRate);
                },
                () =>
                {
                    GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
                    GL.Clear(ClearBufferMask.ColorBufferBit);
                    GL.Enable(EnableCap.Blend);
                    GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
                    this.ParticleVertexVBO.Bind(this.ErosionAccumulationStep.ShaderVariableLocation("vertex"));
                    this.ParticleIndexVBO.Bind();
                    GL.DrawElements(BeginMode.Points, this.ParticleIndexVBO.Length, DrawElementsType.UnsignedInt, 0);
                    GL.Disable(EnableCap.Blend);
                }
            );

            ErosionAccumulationStep.Render(
                () =>
                {
                    this.ParticleStateTexture[0].Bind(TextureUnit.Texture0);
                    this.VelocityTexture[0].Bind(TextureUnit.Texture1);
                },
                (sp) =>
                {
                    sp.SetUniform("particletex", 0);
                    sp.SetUniform("velocitytex", 1);
                    sp.SetUniform("deltatime", deltaTime);
                    sp.SetUniform("depositRate", depositRate);
                    sp.SetUniform("erosionRate", erosionRate);
                },
                () =>
                {
                    //GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
                    //GL.Clear(ClearBufferMask.ColorBufferBit);
                    GL.Enable(EnableCap.Blend);
                    GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
                    this.ParticleVertexVBO.Bind(this.ErosionAccumulationStep.ShaderVariableLocation("vertex"));
                    this.ParticleIndexVBO.Bind();
                    GL.DrawElements(BeginMode.Points, this.ParticleIndexVBO.Length, DrawElementsType.UnsignedInt, 0);
                    GL.Disable(EnableCap.Blend);
                }
            );


            // Step 3: Update terrain layers
            //  - Render as quad over layer
            //  - Input: L0, E
            //  - Output: L1
            //  Adds deposit amount E.b to soft L0.g
            //  Subtracts material from soft, then hard.
            //  Modifies water depth from particle count (E.r).
            UpdateLayersStep.Render(
                () =>
                {
                    this.TerrainTexture[0].Bind(TextureUnit.Texture0);
                    this.ErosionAccumulationTexture.Bind(TextureUnit.Texture1);
                },
                (sp) =>
                {
                    sp.SetUniform("terraintex", 0);
                    sp.SetUniform("erosiontex", 1);
                    sp.SetUniform("hardErosionFactor", hardErosionFactor);
                    sp.SetUniform("waterLowpass", (float)this.Parameters[P_WATERDECAY].GetValue());
                    sp.SetUniform("waterDepthFactor", (float)this.Parameters[P_PARTICLEWATERDEPTH].GetValue());
                });

            // Step 4: Update particle state
            //  - Render as quad over particles
            //  - Input: P0, V0, E, L0
            //  - Output: P1
            //  Replicate particle potential calc from Step 2 to get deposit/erode potentials.
            //  Subtract deposit amount from carrying amount P0.b, write to P1.b
            //  Apply same calculation as step 3 to determine how much soft/hard is being eroded from L0(P0.rg).
            //  Add material carried based on particle share of total V0.b / E(P0.rg).g -> P1.b
            //  Calculate new particle position by intersecting ray P0.rg->V0.rg against 
            //    cell boundaries. Add small offset to avoid boundary problems. Writes to P1.rg
            //  If death flag indicates particle recycle, init particle at random position.
            UpdateParticlesStep.Render(
                () =>
                {
                    this.ParticleStateTexture[0].Bind(TextureUnit.Texture0);
                    this.VelocityTexture[0].Bind(TextureUnit.Texture1);
                    this.ErosionAccumulationTexture.Bind(TextureUnit.Texture2);
                    this.TerrainTexture[0].Bind(TextureUnit.Texture3);
                },
                (sp) =>
                {
                    sp.SetUniform("particletex", 0);
                    sp.SetUniform("velocitytex", 1);
                    sp.SetUniform("erosiontex", 2);
                    sp.SetUniform("terraintex", 3);
                    sp.SetUniform("deltatime", deltaTime);
                    sp.SetUniform("depositRate", depositRate);
                    sp.SetUniform("erosionRate", erosionRate);
                    sp.SetUniform("hardErosionFactor", hardErosionFactor);
                    sp.SetUniform("texsize", (float)this.Width);
                });


            // step 5 - slippage flow calc
            // in: terrain
            // out: slip-flow
            SlippageFlowStep.Render(
                () =>
                {
                    this.TerrainTexture[1].Bind(TextureUnit.Texture0);
                },
                (sp) =>
                {
                    sp.SetUniform("terraintex", 0);
                    sp.SetUniform("texsize", (float)this.Width);
                    sp.SetUniform("maxdiff", (float)this.Parameters[P_SLIPTHRESHOLD].GetValue());
                    sp.SetUniform("sliprate", (float)this.Parameters[P_SLIPRATE].GetValue());
                    sp.SetUniform("saturationslip", (float)this.Parameters[P_SATURATIONSLIP].GetValue());
                    sp.SetUniform("saturationthreshold", (float)this.Parameters[P_SATURATIONTHRESHOLD].GetValue());
                    sp.SetUniform("saturationrate", (float)this.Parameters[P_SATURATIONRATE].GetValue());

                    //sp.SetUniform("threshold", (float)this.Parameters[P_SLIPRATE].GetValue());
                    //sp.SetUniform("minslip", (float)this.Parameters[P_SLIPRATE].GetValue());
                    //sp.SetUniform("maxslip", (float)this.Parameters[P_SLIPRATE].GetValue());
                });

            // step 6 - slippage transport
            SlippageTransportStep.Render(
                () =>
                {
                    this.TerrainTexture[1].Bind(TextureUnit.Texture0);
                    this.SlipFlowTexture[0].Bind(TextureUnit.Texture1);
                    this.SlipFlowTexture[1].Bind(TextureUnit.Texture2);
                },
                (sp) =>
                {
                    sp.SetUniform("terraintex", 0);
                    sp.SetUniform("flowOtex", 1);
                    sp.SetUniform("flowDtex", 2);
                    sp.SetUniform("texsize", (float)this.Width);
                });




            CopyParticlesStep.Render(
                () =>
                {
                    this.ParticleStateTexture[1].Bind(TextureUnit.Texture0);
                },
                (sp) =>
                {
                    sp.SetUniform("particletex", 0);
                    sp.SetUniform("particleDeathRate", (float)this.Parameters[P_DEATHRATE].GetValue());
                    sp.SetUniform("randSeed", (float)rand.NextDouble());
                });

            CopyVelocityStep.Render(
                () =>
                {
                    this.VelocityTexture[0].Bind(TextureUnit.Texture0);
                },
                (sp) =>
                {
                    sp.SetUniform("velocitytex", 0);
                });

        }


        public void Reload()
        {
            foreach (var step in Steps())
            {
                step.ReloadShader();
            }
        }


        private void InitParticlesVBOs()
        {
            Vector3[] vertex = new Vector3[this.ParticleTexWidth * this.ParticleTexHeight];

            ParallelHelper.For2D(this.ParticleTexWidth, this.ParticleTexHeight, (x, y, i) =>
            {
                vertex[i] = new Vector3((float)x / (float)this.ParticleTexWidth, (float)y / (float)this.ParticleTexHeight, 0f);
            });

            this.ParticleVertexVBO.SetData(vertex);

            uint[] index = new uint[this.ParticleTexWidth * this.ParticleTexHeight];

            for (int i = 0; i < this.ParticleTexWidth * this.ParticleTexHeight; i++)
            {
                index[i] = (uint)i;
            }

            this.ParticleIndexVBO.SetData(index);
        }

        public void Unload()
        {
            for (int i = 0; i < 2; i++)
            {
                this.TerrainTexture[i].Unload();
                this.ParticleStateTexture[i].Unload();
                this.VelocityTexture[i].Unload();
                this.SlipFlowTexture[i].Unload();
            }
            this.ErosionAccumulationTexture.Unload();

        }


        private void UploadTerrain(float[] data)
        {
            this.TerrainTexture[0].Upload(data);
        }

        private float[] DownloadTerrain()
        {
            return this.TerrainTexture[0].GetLevelDataFloat(0);
        }

        public float[] GetRawData()
        {
            return DownloadTerrain();
        }


        public void Load(string filename)
        {
            var data = new float[this.Width * this.Height * 4];
            this.initialMinHeight = float.MaxValue;
            this.initialMaxHeight = float.MinValue;

            try
            {
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None, 256 * 1024))
                {
                    using (var sr = new BinaryReader(fs))
                    {
                        int magic = sr.ReadInt32();
                        if (magic != FILEMAGIC)
                        {
                            throw new Exception("Not a terrain file");
                        }

                        int w, h;

                        w = sr.ReadInt32();
                        h = sr.ReadInt32();

                        if (w != this.Width || h != this.Height)
                        {
                            // TODO: handle size changes
                            throw new Exception(
                                string.Format("Terrain size {0}x{1} did not match generator size {2}x{3}", w, h,
                                    this.Width, this.Height));
                        }

                        for (int i = 0; i < w * h; i++)
                        {
                            data[i * 4 + 0] = sr.ReadSingle();
                            data[i * 4 + 1] = sr.ReadSingle();
                            data[i * 4 + 2] = 0f;
                            sr.ReadSingle();
                            data[i * 4 + 3] = 0f;
                            sr.ReadSingle();

                            float hh = data[i * 4 + 0] + data[i * 4 + 1];
                            this.initialMinHeight = hh < this.initialMinHeight ? hh : this.initialMinHeight;
                            this.initialMaxHeight = hh > this.initialMaxHeight ? hh : this.initialMaxHeight;
                        }

                        sr.Close();
                    }
                    fs.Close();
                }
            }
            catch (Exception)
            {
            }

            UploadTerrain(data);

        }

        public void Save(string filename)
        {
            var data = this.DownloadTerrain();

            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, 256 * 1024))
            {
                using (var sw = new BinaryWriter(fs))
                {
                    sw.Write(FILEMAGIC);
                    sw.Write(this.Width);
                    sw.Write(this.Height);

                    for (int i = 0; i < this.Width * this.Height; i++)
                    {
                        sw.Write(data[i * 4 + 0]);
                        sw.Write(data[i * 4 + 1]);
                        sw.Write(0f);
                        sw.Write(0f);
                    }

                    sw.Close();
                }
                fs.Close();
            }
        }


        public void ResetTerrain()
        {
            Snowscape.TerrainStorage.Terrain terrain = new Snowscape.TerrainStorage.Terrain(this.Width, this.Height);

            terrain.Clear(0.0f);
            terrain.AddSimplexNoise(4, 0.16f / (float)this.Width, 300.0f, h => h, h => Math.Abs(h));
            terrain.AddSimplexNoise(14, 1.0f / (float)this.Width, 200.0f, h => Math.Abs(h), h => h);
            //terrain.AddSimplexNoise(6, 19.0f / (float)this.Width, 20.0f, h => h*h, h => h);
            terrain.AddLooseMaterial(4.0f);
            terrain.SetBaseLevel();

            var data = new float[this.Width * this.Height * 4];

            ParallelHelper.For2D(this.Width, this.Height, (i) =>
            {
                data[i * 4 + 0] = terrain[i].Hard;
                data[i * 4 + 1] = terrain[i].Loose;
                data[i * 4 + 2] = 0.0f;  // water
                data[i * 4 + 3] = 0.0f;
            });

            UploadTerrain(data);

            this.initialMinHeight = terrain.GetMinHeight();
            this.initialMaxHeight = terrain.GetMaxHeight();

        }

        private void RandomizeParticles(Texture destination)
        {
            float[] data = new float[this.ParticleTexWidth * this.ParticleTexHeight * 4];
            var r = new Random();

            for (int i = 0; i < this.ParticleTexWidth * this.ParticleTexHeight; i++)
            {
                data[i * 4 + 0] = (float)r.NextDouble();  // x
                data[i * 4 + 1] = (float)r.NextDouble();  // y 
                data[i * 4 + 2] = 0f;    // carrying nothing
                data[i * 4 + 3] = (float)r.NextDouble();  // particle life
            }

            destination.Upload(data);
        }



        public IEnumerable<IParameter> GetParameters()
        {
            return this.Parameters;
        }


        public float GetMinHeight()
        {
            return initialMinHeight;
        }

        public float GetMaxHeight()
        {
            return initialMaxHeight;
        }


    }
}

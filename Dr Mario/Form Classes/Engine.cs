using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using DX11 = SlimDX.Direct3D11;
using DX10 = SlimDX.Direct3D10;
using D3C = SlimDX.D3DCompiler;
using DXGI = SlimDX.DXGI;
using Buffer = SlimDX.Direct3D11.Buffer;
using Color = System.Drawing.Color;
using Dr_Mario;

namespace Dr_Mario.Form_Classes
{
    public class Engine
    {
        public enum TextRenderers
        {
            Normal = 0,
            LargeCountdown=1,
            Larger,
        }

        private static DXGI.SwapChain swapChain;
        private static DX11.Device device11;
        private static SlimDX.Direct3D10_1.Device1 device10_1;
        private static SlimDX.Windows.RenderForm mainForm;
        private static DX11.DeviceContext context;
        
        private static DX11.RenderTargetView renderTarget;
        private static DXGI.KeyedMutex mutexD3D10;
        private static DXGI.KeyedMutex mutexD3D11;
        private static SlimDX.Direct2D.RenderTarget dwRenderTarget;
        private static DX11.Texture2D textureD3D11;
        private static DXGI.Resource sharedResource;
        private static DX10.Texture2D textureD3D10;
        private static DX11.Effect effect;
        private static SlimDX.DirectWrite.TextFormat[] textFormat;
        private static DX11.InputLayout layoutText;
        private static Buffer vertexBufferText;
        private static DX11.BlendState BlendState_Transparent;
        public static SlimDX.Direct2D.SolidColorBrush WhiteBrush { get; private set; }
        private static SpriteTextRenderer.SpriteRenderer spriteRenderer;
        private static SlimDX.DirectWrite.Factory dwFactory;
        private static DXGI.Surface surface;
        public static System.Drawing.Color[] ValidTextColors =
        {
            Color.White,
            Color.Red,
            Color.Orange,
            Color.Yellow,
            Color.Green,
            Color.LightBlue,
            Color.LimeGreen,
            Color.Pink,
            Color.PowderBlue,
            Color.LightSeaGreen,
            Color.LawnGreen,
            Color.Lavender,
            Color.Ivory,
            Color.Honeydew,
            Color.FloralWhite,
            Color.DodgerBlue,
            Color.DeepPink,
            Color.DarkKhaki,
            Color.Cyan,
            Color.Crimson,
            Color.CadetBlue,
            Color.Aquamarine
        };


        public static int Width { get; private set; }
        public static int Height { get; private set; }
        public static float YMultiply { get; private set; }

        public static void Pause(bool value)
        {
            if (swapChain !=null && swapChain.IsFullScreen != (!value))
                swapChain.IsFullScreen = !value &&!Program.Debug;
        }

        public static void OnFormResizing(object sender, EventArgs e)
        {
            Width = mainForm.Width;
            Height = mainForm.Height;
            YMultiply = (float)Height / (float)Width;

        }

        public static void Initialize(SlimDX.Windows.RenderForm form)
        {
            mainForm = form;
            Width = mainForm.Width;
            Height = mainForm.Height;
            YMultiply = (float)Height / (float)Width;
            // DirectX DXGI 1.1 factory
            DXGI.Factory1 factory1 = new DXGI.Factory1();

            // The 1st graphics adapter
            DXGI.Adapter1 adapter1 = factory1.GetAdapter1(0);


            var description = new DXGI.SwapChainDescription()
            {
                BufferCount = 2,
                Usage = DXGI.Usage.RenderTargetOutput,
                OutputHandle = mainForm.Handle,
                IsWindowed = Program.Debug,
                ModeDescription = new DXGI.ModeDescription(0, 0, new Rational(60, 1), DXGI.Format.R8G8B8A8_UNorm),
                SampleDescription = new DXGI.SampleDescription(1, 0),
                Flags = DXGI.SwapChainFlags.AllowModeSwitch,
                SwapEffect = DXGI.SwapEffect.Discard
            };

            SlimDX.Direct3D11.Device.CreateWithSwapChain(adapter1, DX11.DeviceCreationFlags.Debug, description, out device11, out swapChain);

            // create a view of our render target, which is the backbuffer of the swap chain we just created
            using (var resource = DX11.Resource.FromSwapChain<DX11.Texture2D>(swapChain, 0))
                renderTarget = new DX11.RenderTargetView(device11, resource);

            // setting a viewport is required if you want to actually see anything
            context = device11.ImmediateContext;
            DX11.Viewport viewport = new DX11.Viewport(0.0f, 0.0f, Width, Height);
            context.OutputMerger.SetTargets(renderTarget);
            context.Rasterizer.SetViewports(viewport);

            // A DirectX 10.1 device is required because DirectWrite/Direct2D are unable
            // to access DirectX11.  BgraSupport is required for DXGI interaction between
            // DirectX10/Direct2D/DirectWrite.
            device10_1 = new SlimDX.Direct3D10_1.Device1(
                adapter1,
                SlimDX.Direct3D10.DriverType.Hardware,
                SlimDX.Direct3D10.DeviceCreationFlags.BgraSupport | SlimDX.Direct3D10.DeviceCreationFlags.Debug,
                SlimDX.Direct3D10_1.FeatureLevel.Level_10_1
            );

            // Create the DirectX11 texture2D.  This texture will be shared with the DirectX10
            // device.  The DirectX10 device will be used to render text onto this texture.  DirectX11
            // will then draw this texture (blended) onto the screen.
            // The KeyedMutex flag is required in order to share this resource.
            textureD3D11 = new DX11.Texture2D(device11, new DX11.Texture2DDescription
            {
                Width = Engine.Width,
                Height = Engine.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = DXGI.Format.B8G8R8A8_UNorm,
                SampleDescription = new DXGI.SampleDescription(1, 0),
                Usage = DX11.ResourceUsage.Default,
                BindFlags = DX11.BindFlags.RenderTarget | DX11.BindFlags.ShaderResource,
                CpuAccessFlags = DX11.CpuAccessFlags.None,
                OptionFlags = DX11.ResourceOptionFlags.KeyedMutex
            });

            // A DirectX10 Texture2D sharing the DirectX11 Texture2D
            sharedResource = new DXGI.Resource(textureD3D11);
            textureD3D10 = device10_1.OpenSharedResource<DX10.Texture2D>(sharedResource.SharedHandle);

            // The KeyedMutex is used just prior to writing to textureD3D11 or textureD3D10.
            // This is how DirectX knows which DirectX (10 or 11) is supposed to be writing
            // to the shared texture.  The keyedMutex is just defined here, they will be used
            // a bit later.
            mutexD3D10 = new DXGI.KeyedMutex(textureD3D10);
            mutexD3D11 = new DXGI.KeyedMutex(textureD3D11);

            // Direct2D Factory
            SlimDX.Direct2D.Factory d2Factory = new SlimDX.Direct2D.Factory(
                SlimDX.Direct2D.FactoryType.SingleThreaded,
                SlimDX.Direct2D.DebugLevel.Information
            );

            // Direct Write factory
            dwFactory = new SlimDX.DirectWrite.Factory(
                SlimDX.DirectWrite.FactoryType.Isolated
            );

            // The textFormat we will use to draw text with
            textFormat = new SlimDX.DirectWrite.TextFormat[5];
            textFormat[0] = new SlimDX.DirectWrite.TextFormat(
               dwFactory,
               "Segoe Print",
               SlimDX.DirectWrite.FontWeight.Bold,
               SlimDX.DirectWrite.FontStyle.Normal,
               SlimDX.DirectWrite.FontStretch.Normal,
               Convert.ToInt32(mainForm.Width / 53.3),
               "en-US"
           );

            textFormat[0].TextAlignment = SlimDX.DirectWrite.TextAlignment.Leading;
            textFormat[0].ParagraphAlignment = SlimDX.DirectWrite.ParagraphAlignment.Near;

            textFormat[1] = new SlimDX.DirectWrite.TextFormat(
                dwFactory,
                "Arial Black",
                SlimDX.DirectWrite.FontWeight.Bold,
                SlimDX.DirectWrite.FontStyle.Normal,
                SlimDX.DirectWrite.FontStretch.Normal,
                Convert.ToInt32(mainForm.Width / 5),
                "en-US"
            );

            textFormat[1].TextAlignment = SlimDX.DirectWrite.TextAlignment.Leading;
            textFormat[1].ParagraphAlignment = SlimDX.DirectWrite.ParagraphAlignment.Center;

            // Query for a IDXGISurface.
            // DirectWrite and DirectX10 can interoperate thru DXGI.
           surface = textureD3D10.AsSurface();
            SlimDX.Direct2D.RenderTargetProperties rtp = new SlimDX.Direct2D.RenderTargetProperties();
            rtp.MinimumFeatureLevel = SlimDX.Direct2D.FeatureLevel.Direct3D10;
            rtp.Type = SlimDX.Direct2D.RenderTargetType.Hardware;
            rtp.Usage = SlimDX.Direct2D.RenderTargetUsage.None;
            rtp.PixelFormat = new SlimDX.Direct2D.PixelFormat(DXGI.Format.Unknown, SlimDX.Direct2D.AlphaMode.Premultiplied);
            dwRenderTarget = SlimDX.Direct2D.RenderTarget.FromDXGI(d2Factory, surface, rtp);
            d2Factory.Dispose();

            // Brush used to DrawText
            WhiteBrush = new SlimDX.Direct2D.SolidColorBrush(
                dwRenderTarget,
                new Color4(1, 1, 1, 1)
            );
                
            // Think of the shared textureD3D10 as an overlay.
            // The overlay needs to show the text but let the underlying triangle (or whatever)
            // show thru, which is accomplished by blending.
            DX11.BlendStateDescription bsd = new DX11.BlendStateDescription();
            bsd.RenderTargets[0].BlendEnable = true;
            bsd.RenderTargets[0].SourceBlend = DX11.BlendOption.SourceAlpha;
            bsd.RenderTargets[0].DestinationBlend = DX11.BlendOption.InverseSourceAlpha;
            bsd.RenderTargets[0].BlendOperation = DX11.BlendOperation.Add;
            bsd.RenderTargets[0].SourceBlendAlpha = DX11.BlendOption.One;
            bsd.RenderTargets[0].DestinationBlendAlpha = DX11.BlendOption.Zero;
            bsd.RenderTargets[0].BlendOperationAlpha = DX11.BlendOperation.Add;
            bsd.RenderTargets[0].RenderTargetWriteMask = DX11.ColorWriteMaskFlags.All;
            BlendState_Transparent = DX11.BlendState.FromDescription(device11, bsd);

            // Load Effect. This includes both the vertex and pixel shaders.
            // Also can include more than one technique.
            D3C.ShaderBytecode shaderByteCode = D3C.ShaderBytecode.CompileFromFile(
                "Textures/Text.fx",
                "fx_5_0",
                D3C.ShaderFlags.EnableStrictness,
                D3C.EffectFlags.None);

            effect = new DX11.Effect(device11, shaderByteCode);
            shaderByteCode.Dispose();

            // create text vertex data, making sure to rewind the stream afterward
            // Top Left of screen is -1, +1
            // Bottom Right of screen is +1, -1
            var verticesText = new DataStream(VertexPositionTexture.SizeInBytes * 4, true, true);
            verticesText.Write(
                new VertexPositionTexture(
                        new Vector3(-1, 1, 0),
                        new Vector2(0, 0f)
                )
            );
            verticesText.Write(
                new VertexPositionTexture(
                    new Vector3(1, 1, 0),
                    new Vector2(1, 0)
                )
            );
            verticesText.Write(
                new VertexPositionTexture(
                    new Vector3(-1, -1, 0),
                    new Vector2(0, 1)
                )
            );
            verticesText.Write(
                new VertexPositionTexture(
                    new Vector3(1, -1, 0),
                    new Vector2(1, 1)
                )
            );

            verticesText.Position = 0;

            // create the text vertex layout and buffer
            layoutText = new DX11.InputLayout(device11, 
                effect.GetTechniqueByName("Text").GetPassByIndex(0).Description.Signature, 
                VertexPositionTexture.inputElements);
            vertexBufferText = new Buffer(device11, verticesText, (int)verticesText.Length,
                           DX11.ResourceUsage.Default, DX11.BindFlags.VertexBuffer,
                           DX11.CpuAccessFlags.None, DX11.ResourceOptionFlags.None, 0);
            verticesText.Close();

            // prevent DXGI handling of alt+enter, which doesn't work properly with Winforms
            factory1.SetWindowAssociation(mainForm.Handle, DXGI.WindowAssociationFlags.IgnoreAltEnter);
            factory1.Dispose();
            spriteRenderer = new SpriteTextRenderer.SpriteRenderer(device11, 128);
        }

        private static Dictionary<System.Drawing.Rectangle, string> textOutputLeft = new Dictionary<System.Drawing.Rectangle, string>();
        private static Dictionary<System.Drawing.Rectangle, string> textOutputRight = new Dictionary<System.Drawing.Rectangle, string>();
        private static List<Object_Classes.DrawTextStruct> textOutputSpecial = new List<Object_Classes.DrawTextStruct>();

        public static void DrawText(Object_Classes.DrawTextStruct textStruct)
        {
            textOutputSpecial.Add(textStruct);
        }

        public static void DrawText(string text, Vector2 location, SlimDX.DirectWrite.TextAlignment textAlign)
        {
            DrawText(text, new System.Drawing.Rectangle(Convert.ToInt32((location.X + 1) * Width * 0.5), Convert.ToInt32((location.Y - 1) * Height * -0.5), Engine.Width / 4, Engine.Height / 8), textAlign);
        }

        public static void DrawText(string text, System.Drawing.Rectangle textArea, SlimDX.DirectWrite.TextAlignment textAlign)
        {
            switch (textAlign)
            {
                case SlimDX.DirectWrite.TextAlignment.Leading:
                    textOutputLeft.Add(textArea, text);
                    break;
                case SlimDX.DirectWrite.TextAlignment.Trailing:
                    textOutputRight.Add(textArea, text);
                    break;
            }
        }



        public static void Draw()
        {
                //form_GotFocus(null, null);
                //form.BottleInit();
                // clear the render target to a soothing blue
                context.ClearRenderTargetView(renderTarget, new Color4(0f, 0f, 0f));// new Color4((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble()));
                //form.PaintTimer_Elapsed(form, EventArgs.Empty);
                spriteRenderer.Flush();
                {
                    // Draw Text on the shared Texture2D
                    // Need to Acquire the shared texture for use with DirectX10
                    mutexD3D10.Acquire(0, 100);
                    dwRenderTarget.BeginDraw();
                    dwRenderTarget.Clear(new Color4(0, 0, 0, 0));
                    textFormat[0].TextAlignment = SlimDX.DirectWrite.TextAlignment.Leading;
                    foreach (var t in textOutputLeft)
                        dwRenderTarget.DrawText(t.Value, textFormat[0], t.Key, WhiteBrush);
                       textFormat[0].TextAlignment = SlimDX.DirectWrite.TextAlignment.Trailing;
                    foreach (var t in textOutputRight)
                        dwRenderTarget.DrawText(t.Value, textFormat[0], t.Key, WhiteBrush);
                    foreach (var textStruct in textOutputSpecial)
                    {
                        textFormat[(int)textStruct.TextRenderer].TextAlignment = textStruct.textAlign;
                        dwRenderTarget.DrawText(textStruct.Text, textFormat[(int)textStruct.TextRenderer], textStruct.textArea, textStruct.Brush);
                    }
                    textOutputLeft.Clear();
                    textOutputRight.Clear();
                    textOutputSpecial.Clear();

                    dwRenderTarget.EndDraw();
                    mutexD3D10.Release(0);

                    // Draw the shared texture2D onto the screen
                    // Need to Aquire the shared texture for use with DirectX11
                    mutexD3D11.Acquire(0, 100);
                    DX11.ShaderResourceView srv = new DX11.ShaderResourceView(device11, textureD3D11);
                    effect.GetVariableByName("g_textOverlay").AsResource().SetResource(srv);
                    context.InputAssembler.InputLayout = layoutText;
                    context.InputAssembler.PrimitiveTopology = DX11.PrimitiveTopology.TriangleStrip;
                    context.InputAssembler.SetVertexBuffers(0, new DX11.VertexBufferBinding(vertexBufferText, VertexPositionTexture.SizeInBytes, 0));
                    context.OutputMerger.BlendState = BlendState_Transparent;
                    DX11.EffectTechnique currentTechnique = effect.GetTechniqueByName("Text");

                         for (int pass = 0; pass < currentTechnique.Description.PassCount; ++pass)
                         {
                             var Pass = currentTechnique.GetPassByIndex(pass);
                             System.Diagnostics.Debug.Assert(Pass.IsValid, "Invalid EffectPass");
                             Pass.Apply(context);
                             context.Draw(4, 0);
                         }
                     
                    srv.Dispose();
                    mutexD3D11.Release(0);
                }
                swapChain.Present(0, DXGI.PresentFlags.None);
        }

        public static void Destroy()
        {
            swapChain.Dispose();
            device11.Dispose();
            device10_1.Dispose();
            mainForm.Dispose();
            context.Dispose();
            
            renderTarget.Dispose();
            mutexD3D10.Dispose();
            mutexD3D11.Dispose();
            dwRenderTarget.Dispose();
            textureD3D11.Dispose();
            sharedResource.Dispose();
            textureD3D10.Dispose();
            effect.Dispose();
            foreach(var tf in textFormat)
                if(tf != null)
                    tf.Dispose();
            layoutText.Dispose();
            vertexBufferText.Dispose();
            BlendState_Transparent.Dispose();
            WhiteBrush.Dispose();
            spriteRenderer.Dispose();
            dwFactory.Dispose();
            surface.Dispose();
        }

        public static void DrawSprite(DX11.ShaderResourceView sprite, Vector2 location, Vector2 size)
        {
            spriteRenderer.Draw(sprite, location, size, SpriteTextRenderer.CoordinateType.SNorm);
        }

        public static void DrawSprite(DX11.ShaderResourceView sprite, Vector2 location, Vector2 size, Color4 colorMultiplier)
        {
            spriteRenderer.Draw(sprite, location, size, colorMultiplier, SpriteTextRenderer.CoordinateType.SNorm);
        }

        public static DX11.ShaderResourceView CreateView(byte[] pictureData)
        {
      //      var t2d = DX11.Texture2D.FromMemory(device11, pictureData);
            return  DX11.ShaderResourceView.FromMemory(device11, pictureData);
        }

        public static DX11.ShaderResourceView CreateView(string fileName)
        {
            return DX11.ShaderResourceView.FromFile(device11, fileName);
        }
        /// <summary>
        /// Future version... not working like I want right now.
        /// </summary>
        /// <param name="playerCenter"></param>
        /// <param name="colors"></param>
        /// <returns></returns>
        public static SlimDX.Direct2D.Brush CreateBrush(Vector2 playerCenter, params Color[] colors)
        {
            if (colors.Length == 1)
                return new SlimDX.Direct2D.SolidColorBrush(dwRenderTarget, colors[0]);

            SlimDX.Direct2D.GradientStop[] rainbow = new SlimDX.Direct2D.GradientStop[colors.Length * 3];
            for (int iRain = 0; iRain < rainbow.Length; iRain++)
                rainbow[iRain] = new SlimDX.Direct2D.GradientStop() { Position = iRain * 0.01f, Color = colors[iRain % (colors.Length - 1)] };

            using (SlimDX.Direct2D.GradientStopCollection gsc = new SlimDX.Direct2D.GradientStopCollection(dwRenderTarget, rainbow))
            {
                return new SlimDX.Direct2D.RadialGradientBrush(
                    dwRenderTarget,
                    gsc,
                    new SlimDX.Direct2D.RadialGradientBrushProperties()
                    {
                        CenterPoint = new System.Drawing.PointF(playerCenter.X, playerCenter.Y),
                        VerticalRadius = 3000,
                        GradientOriginOffset = new System.Drawing.PointF(0, 0),
                        HorizontalRadius = 100
                    });
            }

        }


    }
}
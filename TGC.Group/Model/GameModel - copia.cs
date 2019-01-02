using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;
using System;
using System.Drawing;
using System.Windows.Forms;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;
using Effect = Microsoft.DirectX.Direct3D.Effect;

namespace TGC.Group.Model
{
    /// <summary>
    ///     Ejemplo para implementar el TP.
    ///     Inicialmente puede ser renombrado o copiado para hacer más ejemplos chicos, en el caso de copiar para que se
    ///     ejecute el nuevo ejemplo deben cambiar el modelo que instancia GameForm <see cref="Form.GameForm.InitGraphics()" />
    ///     line 97.
    /// </summary>
    public class GameModel : TgcExample
    {

        public CScene ESCENA;
        public CShip SHIP;
        public float dist_cam = 50;
        private Effect effect;
        public float ftime; // frame time
        private Surface g_pDepthStencil; // Depth-stencil buffer
        private Surface g_pDepthStencilOld; // Depth-stencil buffer
        private Texture g_pRenderTarget, g_pRenderTarget2, g_pRenderTarget3, g_pRenderTarget4, g_pRenderTarget5;

        private VertexBuffer g_pVBV3D;
        public bool mouseCaptured;
        public Point mouseCenter;
        private string MyShaderDir;

        public bool paused;
        public int tipo_camara = 0;
        // camara fija
        public TGCVector3 camara_LA = new TGCVector3(1000, 0, 0);
        public TGCVector3 camara_LF = new TGCVector3(1000, 1000, 1000);
        // chasing camera
        public float chase_1 = 160;
        public float chase_2 = 20;
        public float chase_3 = 500;

        public float time;
        public bool camara_ready = false;

        public TGCVector3 cam_la_vel = new TGCVector3(0, 0, 0);
        public TGCVector3 cam_pos_vel = new TGCVector3(0, 0, 0);

        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="shadersDir">Ruta donde esta la carpeta con los shaders</param>
        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;
        }

        public override void Init()
        {
            var d3dDevice = D3DDevice.Instance.Device;

            MyShaderDir = ShadersDir;

            ESCENA = new CScene(MediaDir);
            SHIP = new CShip(MediaDir, ESCENA);

            //Cargar Shader personalizado
            string compilationErrors;
            effect = Effect.FromFile(D3DDevice.Instance.Device, MyShaderDir + "shaders.fx",
                null, null, ShaderFlags.PreferFlowControl, null, out compilationErrors);
            if (effect == null)
            {
                throw new Exception("Error al cargar shader. Errores: " + compilationErrors);
            }
            //Configurar Technique dentro del shader
            effect.Technique = "DefaultTechnique";


            // para capturar el mouse
            var focusWindows = D3DDevice.Instance.Device.CreationParameters.FocusWindow;
            mouseCenter = focusWindows.PointToScreen(new Point(focusWindows.Width / 2, focusWindows.Height / 2));
            mouseCaptured = false;
            //Cursor.Hide();

            // stencil
            g_pDepthStencil = d3dDevice.CreateDepthStencilSurface(d3dDevice.PresentationParameters.BackBufferWidth,
                d3dDevice.PresentationParameters.BackBufferHeight,
                DepthFormat.D24S8, MultiSampleType.None, 0, true);
            g_pDepthStencilOld = d3dDevice.DepthStencilSurface;
            // inicializo el render target
            g_pRenderTarget = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                Format.X8R8G8B8, Pool.Default);
            g_pRenderTarget2 = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                Format.X8R8G8B8, Pool.Default);
            g_pRenderTarget3 = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                Format.X8R8G8B8, Pool.Default);
            g_pRenderTarget4 = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                Format.X8R8G8B8, Pool.Default);
            g_pRenderTarget5 = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                Format.X8R8G8B8, Pool.Default);

            // Resolucion de pantalla
            effect.SetValue("screen_dx", d3dDevice.PresentationParameters.BackBufferWidth);
            effect.SetValue("screen_dy", d3dDevice.PresentationParameters.BackBufferHeight);

            CustomVertex.PositionTextured[] vertices =
            {
                new CustomVertex.PositionTextured(-1, 1, 1, 0, 0),
                new CustomVertex.PositionTextured(1, 1, 1, 1, 0),
                new CustomVertex.PositionTextured(-1, -1, 1, 0, 1),
                new CustomVertex.PositionTextured(1, -1, 1, 1, 1)
            };
            //vertex buffer de los triangulos
            g_pVBV3D = new VertexBuffer(typeof(CustomVertex.PositionTextured),
                4, d3dDevice, Usage.Dynamic | Usage.WriteOnly,
                CustomVertex.PositionTextured.Format, Pool.Default);
            g_pVBV3D.SetData(vertices, 0, LockFlags.None);

            time = 0;

            ESCENA.pos_en_ruta = 10;
            SHIP.updatePos();


        }


        public override void Update()
        {
            PreUpdate();
            if (ElapsedTime <= 0 || ElapsedTime > 1000)
                return;

            // camara debug
            if (tipo_camara == 3)
            {
                TGCVector3 viewDir = camara_LA - camara_LF;
                viewDir.Normalize();
                TGCVector3 N = TGCVector3.Cross(viewDir, TGCVector3.Up);
                N.Normalize();
                TGCVector3 B = TGCVector3.Cross(viewDir, N);
                B.Normalize();

                if (Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT))
                {
                    float dx = Input.XposRelative * 0.05f;
                    float dy = -Input.YposRelative * 0.1f;

                    camara_LF.TransformCoordinate(TGCMatrix.Translation(-camara_LA) *
                        TGCMatrix.RotationY(dx) * TGCMatrix.RotationAxis(N, dy) * TGCMatrix.Translation(camara_LA));
                    camara_LF.Y += dy;

                }

                if (Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_MIDDLE))
                {
                    float dx = Input.XposRelative * 11.5f;
                    float dy = -Input.YposRelative * 11.1f;
                    camara_LF += N * dx + B * dy;
                    camara_LA += N * dx + B * dy;

                }

                if (Input.WheelPos != 0)
                {
                    float ds = Input.WheelPos * 51.1f;
                    camara_LF += viewDir * ds;
                }


            }

            if (ESCENA.pos_en_ruta > ESCENA.cant_ptos_ruta - 15)
            {
                ESCENA.pos_en_ruta = 10;
                SHIP.updatePos();
            }

            if (Input.keyPressed(Key.NumPad1))
                chase_1 += 10 * (Input.keyDown(Key.LeftShift) ? 1 : -1);
            if (Input.keyPressed(Key.NumPad2))
                chase_2 += 10 * (Input.keyDown(Key.LeftShift) ? 1 : -1);
            if (Input.keyPressed(Key.NumPad3))
                chase_3 += 10 * (Input.keyDown(Key.LeftShift) ? 1 : -1);

            if (Input.keyPressed(Key.C))
                tipo_camara = (tipo_camara + 1) % 5;

            if (Input.keyPressed(Key.P))
                paused = !paused;

            if (paused)
                return;

            if (Input.keyPressed(Key.M))
            {
                mouseCaptured = !mouseCaptured;
                if (mouseCaptured)
                    Cursor.Hide();
                else
                    Cursor.Show();
            }

            time += ElapsedTime;

            // actualizo la nave
            SHIP.Update(Input, ESCENA, ElapsedTime);

            // actualizo la camara
            TGCVector3 dirN = SHIP.P.dir;
            TGCVector3 Up = ESCENA.Normal[ESCENA.pos_en_ruta];
            TGCVector3 Tg = ESCENA.Binormal[ESCENA.pos_en_ruta];
            //TGCVector3 pos = SHIP.P.pos;        // ESCENA.pos_central;
            float s = 0.75f;
            TGCVector3 pos = SHIP.P.pos * s + ESCENA.pos_central * (1-s);


            switch (tipo_camara)
            {
                default:
                case 0:
                    // chasing camara
                    {
                        pos = ESCENA.pos_central;
                        //pos = SHIP.P.pos;
                        TGCVector3 Desired_Pos = pos - dirN * chase_1 + Up * chase_2;
                        TGCVector3 Desired_LookAt = pos + dirN * chase_3;

                        TGCVector3 Pos;
                        TGCVector3 LookAt;

                        if (camara_ready)
                        {
                            cam_pos_vel = Desired_Pos - Camara.Position;
                            Pos = Camara.Position+ cam_pos_vel *0.1f;
                            cam_la_vel = Desired_LookAt - Camara.LookAt;
                            LookAt = Camara.LookAt+ cam_la_vel * 0.1f;
                        }
                        else
                        {
                            camara_ready = true;
                            Pos = Desired_Pos;
                            LookAt = Desired_LookAt;
                        }
                        Camara.SetCamera(Pos,LookAt, Up);
                    }
                    break;
                case 1:
                    // camara lateral
                    Camara.SetCamera(pos- Tg * 100, pos, Up);
                    break;
                case 2:
                    // camara superior
                    Camara.SetCamera(pos- dirN * 10 + new TGCVector3(0, 250, 0), pos, dirN);
                    break;

                case 3:
                    // camara fija
                    Camara.SetCamera(camara_LF, camara_LA, TGCVector3.Up);
                    break;

                case 4:
                    // first pirson
                    Camara.SetCamera(SHIP.P.pos, SHIP.P.pos + SHIP.P.dir, Up);
                    break;


            }

            Camara.UpdateCamera(ElapsedTime);

            PostUpdate();
        }

        public override void Render()
        {
            ClearTextures();

            var device = D3DDevice.Instance.Device;
            effect.Technique = "DefaultTechnique";
            effect.SetValue("time", time);

            // guardo el Render target anterior y seteo la textura como render target
            var pOldRT = device.GetRenderTarget(0);
            var pSurf = g_pRenderTarget.GetSurfaceLevel(0);
            device.SetRenderTarget(0, pSurf);
            // hago lo mismo con el depthbuffer, necesito el que no tiene multisampling
            var pOldDS = device.DepthStencilSurface;
            device.DepthStencilSurface = g_pDepthStencil;

            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            device.BeginScene();
            effect.SetValue("eyePosition", TGCVector3.Vector3ToFloat4Array(Camara.Position));
            ESCENA.render(effect);

            if(tipo_camara!=4)
                SHIP.Render(effect);

            // -------------------------------------
            device.EndScene();
            pSurf.Dispose();

            // Ultima pasada vertical va sobre la pantalla pp dicha
            device.SetRenderTarget(0, pOldRT);
            device.DepthStencilSurface = g_pDepthStencilOld;
            device.BeginScene();
            //effect.Technique = "FrameCopy";
            effect.Technique = "FrameMotionBlur";

            device.VertexFormat = CustomVertex.PositionTextured.Format;
            device.SetStreamSource(0, g_pVBV3D, 0);
            effect.SetValue("g_RenderTarget", g_pRenderTarget);
            effect.SetValue("g_RenderTarget2", g_pRenderTarget2);
            effect.SetValue("g_RenderTarget3", g_pRenderTarget3);
            effect.SetValue("g_RenderTarget4", g_pRenderTarget4);
            effect.SetValue("g_RenderTarget5", g_pRenderTarget5);
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            effect.Begin(FX.None);
            effect.BeginPass(0);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            effect.EndPass();
            effect.End();

            DrawText.drawText("Tramo:" + ESCENA.pos_en_ruta, 440, 10, Color.Yellow);
            //DrawText.drawText("pos_t :" + Math.Floor(ESCENA.pos_t * 100), 440, 325, Color.Yellow);
            //DrawText.drawText("Y:" + Math.Floor(pos.Y), 440, 325, Color.Yellow);
            //DrawText.drawText("chase_1=" + Math.Floor(chase_1), 440, 325, Color.Yellow);
            //DrawText.drawText("chase_2=" + Math.Floor(chase_2), 440, 350, Color.Yellow);
            //DrawText.drawText("chase_3=" + Math.Floor(chase_3), 440, 375, Color.Yellow);

            DrawText.drawText("Daño :" + Math.Floor(ESCENA.cant_colisiones / 1000.0f) + "%", 10, 10, Color.Yellow);

            RenderFPS();
            //RenderAxis();
            device.EndScene();
            device.Present();

            ftime += ElapsedTime;
            if (ftime > 0.01f)
            {
                ftime = 0;
                var aux = g_pRenderTarget5;
                g_pRenderTarget5 = g_pRenderTarget4;
                g_pRenderTarget4 = g_pRenderTarget3;
                g_pRenderTarget3 = g_pRenderTarget2;
                g_pRenderTarget2 = g_pRenderTarget;
                g_pRenderTarget = aux;
            }
        }

        public override void Dispose()
        {
            ESCENA.dispose();
            SHIP.Dispose();
            effect.Dispose();
            g_pRenderTarget.Dispose();
            g_pRenderTarget2.Dispose();
            g_pRenderTarget3.Dispose();
            g_pRenderTarget4.Dispose();
            g_pRenderTarget5.Dispose();
            g_pDepthStencil.Dispose();
            g_pVBV3D.Dispose();
        }

    }

}
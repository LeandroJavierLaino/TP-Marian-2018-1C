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

    // Vertex format posicion y color
    public struct VERTEX_POS_COLOR
    {
        public float x, y, z;		// Posicion
        public int color;		// Color
    };

    // Vertex format para dibujar en 2d 
    public struct VERTEX2D
    {
        public float x, y, z, rhw;		// Posicion
        public int color;		// Color
    };

    /// <summary>
    ///     Ejemplo para implementar el TP.
    ///     Inicialmente puede ser renombrado o copiado para hacer más ejemplos chicos, en el caso de copiar para que se
    ///     ejecute el nuevo ejemplo deben cambiar el modelo que instancia GameForm <see cref="Form.GameForm.InitGraphics()" />
    ///     line 97.
    /// </summary>
    public class GameModel : TgcExample
    {

        public CScene ESCENA;
        public const int cant_players = 6;
        public CShip [] PLAYERS = new CShip[cant_players];
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
        public float chase_1 = 140;
        public float chase_2 = 35;
        public float chase_3 = 500;

        public float time;
        public bool camara_ready = false;

        public TGCVector3 cam_la_vel = new TGCVector3(0, 0, 0);
        public TGCVector3 cam_pos_vel = new TGCVector3(0, 0, 0);


        // interface 2d
        public Sprite sprite;
        public Microsoft.DirectX.Direct3D.Font font;


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
            for (int i = 0; i < cant_players; ++i)
            {
                PLAYERS[i] = new CShip(MediaDir, ESCENA, i==0? "nave\\Swoop+Bike-TgcScene.xml" : "nave\\Enemy-TgcScene.xml");
                PLAYERS[i].P.keyboard_input = i == 0 ? true : false;
                PLAYERS[i].P.speed = i == 0 ? 6000 : 3000 + i*300;
                //PLAYERS[i].P.speed = i == 0 ? 100 : 100;

                PLAYERS[i].P.pos_en_ruta = 10 + i * 35;
                PLAYERS[i].updatePos();

            }
            ESCENA.player_one = PLAYERS[0];
            ESCENA.PLAYERS = PLAYERS;
            ESCENA.cant_players = cant_players;



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

            sprite = new Sprite(d3dDevice);
            // Fonts
            font = new Microsoft.DirectX.Direct3D.Font(d3dDevice, 24, 0, FontWeight.Light, 0, false, CharacterSet.Default,
                    Precision.Default, FontQuality.Default, PitchAndFamily.DefaultPitch, "Lucida Console");
            font.PreloadGlyphs('0', '9');
            font.PreloadGlyphs('a', 'z');
            font.PreloadGlyphs('A', 'Z');


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

            CShip SHIP = PLAYERS[0];

            if (SHIP.P.pos_en_ruta > ESCENA.cant_ptos_ruta - 15)
            {
                SHIP.P.pos_en_ruta = 10;
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
            for (int i = 0; i < cant_players; ++i)
                PLAYERS[i].Update(Input, ESCENA, ElapsedTime);


            // actualizo la camara
            TGCVector3 dirN = SHIP.P.dir;
            TGCVector3 Up = ESCENA.Normal[SHIP.P.pos_en_ruta];
            TGCVector3 Tg = ESCENA.Binormal[SHIP.P.pos_en_ruta];
            //TGCVector3 pos = SHIP.P.pos;        // ESCENA.pos_central;
            float s = 0.75f;
            TGCVector3 pos = SHIP.P.pos * s + SHIP.P.pos_central * (1-s);


            switch (tipo_camara)
            {
                default:
                case 0:
                    // chasing camara
                    {
                        pos = SHIP.P.pos_central;
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
                PLAYERS[0].Render(effect);

            for(int i=1;i<cant_players;++i)
                PLAYERS[i].Render(effect);

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

            //DrawText.drawText("Tramo:" + PLAYERS[0].P.pos_en_ruta, 440, 10, Color.Yellow);
            //DrawText.drawText("pos_t :" + Math.Floor(ESCENA.pos_t * 100), 440, 325, Color.Yellow);
            //DrawText.drawText("Y:" + Math.Floor(pos.Y), 440, 325, Color.Yellow);
            //DrawText.drawText("chase_1=" + Math.Floor(chase_1), 440, 325, Color.Yellow);
            //DrawText.drawText("chase_2=" + Math.Floor(chase_2), 440, 350, Color.Yellow);
            //DrawText.drawText("chase_3=" + Math.Floor(chase_3), 440, 375, Color.Yellow);
            //DrawText.drawText("Daño :" + Math.Floor(PLAYERS[0].P.cant_colisiones / 1000.0f) + "%", 10, 10, Color.Yellow);

            //DrawText.drawText("SPEED: " + Math.Floor(PLAYERS[0].P.speed), 10, 10, Color.Yellow);

            RenderFPS();
            //RenderAxis();

            // dibujo el scoreboard, tiempo, vidas, etc (y fps)
            RenderHUD();

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

        public void RenderHUD()
        {
            var device = D3DDevice.Instance.Device;
            var screen_dx = device.PresentationParameters.BackBufferWidth;
            var screen_dy = device.PresentationParameters.BackBufferHeight;

            bool ant_zenable = device.RenderState.ZBufferEnable;
            device.RenderState.ZBufferEnable = false;

            //FillText(50, 50, "Speed =" + Math.Floor(PLAYERS[0].P.speed), Color.Yellow, true);
            FillText(50, 10, "Tramo:" + PLAYERS[0].P.pos_en_ruta, Color.Red, false);

            float x0 = 50;
            float y0 = 50;
            float dx = 500;
            float dy = 50;
            FillRect(x0, y0, x0 + dx, y0 + dy, Color.Beige);
            float ptje = PLAYERS[0].P.speed / PLAYERS[0].P.max_speed;
            float rango_1 = Math.Min(ptje, 0.5f);
            float rango_2 = Math.Min(ptje, 0.75f);
            float rango_3 = Math.Min(ptje, 0.95f);

            FillRect(x0 + 5, y0 + 5, x0 + (dx - 10) * rango_3, y0 + dy - 5, Color.White);
            FillRect(x0 + 5, y0 + 5, x0 + (dx - 10) * rango_2, y0 + dy - 5, Color.Red);
            FillRect(x0 + 5, y0 + 5, x0 + (dx - 10) * rango_1, y0 + dy - 5, Color.Blue);

            int puesto = 1;
            for (int i = 1; i < cant_players; ++i)
                if (PLAYERS[i].P.pos_en_ruta > PLAYERS[0].P.pos_en_ruta)
                    ++puesto;
            FillText(screen_dx - 300, 10, "PUESTO: " + puesto, Color.Red, false);

            device.RenderState.ZBufferEnable = ant_zenable;

        }

        public void DrawLine(TGCVector3 p0, TGCVector3 p1, TGCVector3 up, float dw, Color color)
        {

            TGCVector3 v = p1 - p0;
            v.Normalize();
            TGCVector3 n = TGCVector3.Cross(v, up);
            TGCVector3 w = TGCVector3.Cross(n, v);

            TGCVector3[] p = new TGCVector3[8];

            dw *= 0.5f;
            p[0] = p0 - n * dw;
            p[1] = p1 - n * dw;
            p[2] = p1 + n * dw;
            p[3] = p0 + n * dw;
            for (int i = 0; i < 4; ++i)
            {
                p[4 + i] = p[i] + w * dw;
                p[i] -= w * dw;
            }

            int[] index_buffer = { 0, 1, 2, 0, 2, 3,
                                       4, 5, 6, 4, 6, 7,
                                       0, 1, 5, 0, 5, 4,
                                       3, 2, 6, 3, 6, 7 };

            VERTEX_POS_COLOR[] pt = new VERTEX_POS_COLOR[index_buffer.Length];
            for (int i = 0; i < index_buffer.Length; ++i)
            {
                int index = index_buffer[i];
                pt[i].x = p[index].X;
                pt[i].y = p[index].Y;
                pt[i].z = p[index].Z;
                pt[i].color = color.ToArgb();
            }

            // dibujo como lista de triangulos
            var device = D3DDevice.Instance.Device;
            device.VertexFormat = VertexFormats.Position | VertexFormats.Diffuse;
            device.DrawUserPrimitives(PrimitiveType.TriangleList, index_buffer.Length / 3, pt);
        }


        // line 2d
        public void DrawLine(float x0, float y0, float x1, float y1, int dw, Color color)
        {
            TGCVector2[] V = new TGCVector2[4];
            V[0].X = x0;
            V[0].Y = y0;
            V[1].X = x1;
            V[1].Y = y1;

            if (dw < 1)
                dw = 1;

            // direccion normnal
            TGCVector2 v = V[1] - V[0];
            v.Normalize();
            TGCVector2 n = new TGCVector2(-v.Y, v.X);

            V[2] = V[1] + n * dw;
            V[3] = V[0] + n * dw;

            VERTEX2D[] pt = new VERTEX2D[16];
            // 1er triangulo
            pt[0].x = V[0].X;
            pt[0].y = V[0].Y;
            pt[1].x = V[1].X;
            pt[1].y = V[1].Y;
            pt[2].x = V[2].X;
            pt[2].y = V[2].Y;

            // segundo triangulo
            pt[3].x = V[0].X;
            pt[3].y = V[0].Y;
            pt[4].x = V[2].X;
            pt[4].y = V[2].Y;
            pt[5].x = V[3].X;
            pt[5].y = V[3].Y;

            for (int t = 0; t < 6; ++t)
            {
                pt[t].z = 0.5f;
                pt[t].rhw = 1;
                pt[t].color = color.ToArgb();
                ++t;
            }

            // dibujo como lista de triangulos
            var device = D3DDevice.Instance.Device;
            device.VertexFormat = VertexFormats.Transformed | VertexFormats.Diffuse;
            device.DrawUserPrimitives(PrimitiveType.TriangleList, 2, pt);
        }

        public void DrawRect(float x0, float y0, float x1, float y1, int dw, Color color)
        {
            DrawLine(x0, y0, x1, y0, dw, color);
            DrawLine(x0, y1, x1, y1, dw, color);
            DrawLine(x0, y0, x0, y1, dw, color);
            DrawLine(x1, y0, x1, y1, dw, color);
        }

        public void FillRect(float x0, float y0, float x1, float y1, Color color)
        {
            TGCVector2[] V = new TGCVector2[4];
            V[0].X = x0;
            V[0].Y = y0;
            V[1].X = x0;
            V[1].Y = y1;
            V[2].X = x1;
            V[2].Y = y1;
            V[3].X = x1;
            V[3].Y = y0;

            VERTEX2D[] pt = new VERTEX2D[16];
            // 1er triangulo
            pt[0].x = V[0].X;
            pt[0].y = V[0].Y;
            pt[1].x = V[1].X;
            pt[1].y = V[1].Y;
            pt[2].x = V[2].X;
            pt[2].y = V[2].Y;

            // segundo triangulo
            pt[3].x = V[0].X;
            pt[3].y = V[0].Y;
            pt[4].x = V[2].X;
            pt[4].y = V[2].Y;
            pt[5].x = V[3].X;
            pt[5].y = V[3].Y;

            for (int t = 0; t < 6; ++t)
            {
                pt[t].z = 0.5f;
                pt[t].rhw = 1;
                pt[t].color = color.ToArgb();
                ++t;
            }

            // dibujo como lista de triangulos
            var device = D3DDevice.Instance.Device;
            device.VertexFormat = VertexFormats.Transformed | VertexFormats.Diffuse;
            device.DrawUserPrimitives(PrimitiveType.TriangleList, 2, pt);
        }


        public void FillText(int x, int y, string text, Color color, bool center = false)
        {
            var device = D3DDevice.Instance.Device;
            var screen_dx = device.PresentationParameters.BackBufferWidth;
            var screen_dy = device.PresentationParameters.BackBufferHeight;
            // elimino cualquier textura que me cague el modulate del vertex color
            device.SetTexture(0, null);
            // Desactivo el zbuffer
            bool ant_zenable = device.RenderState.ZBufferEnable;
            device.RenderState.ZBufferEnable = false;
            // pongo la matriz identidad
            Microsoft.DirectX.Matrix matAnt = sprite.Transform * Microsoft.DirectX.Matrix.Identity;
            sprite.Transform = Microsoft.DirectX.Matrix.Identity;
            sprite.Begin(SpriteFlags.AlphaBlend);
            if (center)
            {
                Rectangle rc = new Rectangle(0, y, screen_dx, y + 100);
                font.DrawText(sprite, text, rc, DrawTextFormat.Center, color);
            }
            else
            {
                Rectangle rc = new Rectangle(x, y, x + 600, y + 100);
                font.DrawText(sprite, text, rc, DrawTextFormat.NoClip | DrawTextFormat.Top | DrawTextFormat.Left, color);
            }
            sprite.End();
            // Restauro el zbuffer
            device.RenderState.ZBufferEnable = ant_zenable;
            // Restauro la transformacion del sprite
            sprite.Transform = matAnt;
        }



        public override void Dispose()
        {
            ESCENA.dispose();
            for (int i = 0; i < cant_players; ++i)
                PLAYERS[i].Dispose();
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
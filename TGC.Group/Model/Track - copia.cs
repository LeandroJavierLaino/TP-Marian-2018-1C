using Microsoft.DirectX.Direct3D;
using System;
using System.Drawing;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;
using TGC.Core.Shaders;


namespace TGC.Group.Model
{

    public class CSkyBox
    {
        Microsoft.DirectX.Direct3D.Device device;
        Texture textura;
        public string MyMediaDir;
        CScene T;
        public VertexBuffer vb = null;


        public CSkyBox(String tx_skybox, CScene pT)
        {
            T = pT;
            MyMediaDir = T.path_media + "Texturas\\";
            device = D3DDevice.Instance.Device;
            textura = Texture.FromBitmap(device, (Bitmap)Image.FromFile(MyMediaDir + tx_skybox),
                    Usage.None, Pool.Managed);

            //Cargar vertices
            int dataIdx = 0;
            int totalVertices = 36*3;

            vb = new VertexBuffer(typeof(CustomVertex.PositionTextured), totalVertices, D3DDevice.Instance.Device,
                Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionTextured.Format, Pool.Default);

            float ep = 0.005f;
            CustomVertex.PositionTextured[] data = new CustomVertex.PositionTextured[totalVertices];
            float[,] v = {
                {-1, -1, -1, 0.25f+ep, 1.0f / 3.0f +ep},      //v0
                {1, -1, -1, 0.5f-ep, 1.0f/3.0f+ep},           //v1
                {-1, 1, -1, 0.25f+ep, 2.0f / 3.0f -ep},       //v2
                { 1, 1, -1, 0.5f-ep, 2.0f / 3.0f -ep},        //v3
                { -1, -1, 1, 0.0f+ep, 1.0f / 3.0f +ep},       //v4
                { -1, 1, 1, 0.0f+ep, 2.0f / 3.0f -ep},        //v5
                { -1, -1, 1, 1.0f-ep, 1.0f / 3.0f +ep},        //v6
                { -1, 1, 1, 1.0f-ep, 2.0f / 3.0f-ep },        //v7
                { -1, -1, 1, 0.25f+ep, 0.0f +ep},             //v8
                { 1, -1, 1, 0.5f-ep, 0.0f +ep},               //v9
                { -1, 1, 1, 0.25f+ep, 1.0f-ep},              //v10
                { 1, 1, 1, 0.5f-ep, 1.0f -ep},                //v11
                { 1, -1, 1, 0.75f-ep, 1.0f / 3.0f +ep},       //v12
                { 1, 1, 1, 0.75f-ep, 2.0f / 3.0f -ep},        //v13
                };

         
            int[] index = {
                0, 1, 3,        // face1 = v0 - v1 - v3
                0, 3, 2,        // face2 = v0 - v3 - v2 
                0, 2, 4,        // face3 = v0 - v2 - v4 
                2, 4, 5,        // face4 = v2 - v4 - v5 
                1, 3, 12,       // face5 = v1 - v3 - v12 
                12, 13,3,       // face6 = v12 - v13 - v3 
                6, 7, 12,       // face7 = v6 - v7 - v12 
                7, 12, 13,      // face8 = v7 - v12 - v13
                2, 3, 10,       // face9 = v2 - v3 - v10 
                3, 10, 11,      // face10 = v3 - v10 - v11
                0, 1, 8,        // face11 = v0 - v1 - v8 
                1, 8, 9         // face12 = v1 - v8 - v9
            };

            for(int face = 0;face<12;++face)
            {
                for(int i=0;i<3;++i)
                {
                    int j = index[face * 3 + i];
                    TGCVector3 pos = new TGCVector3(v[j, 0], v[j, 1], v[j, 2]);
                    data[dataIdx++] = new CustomVertex.PositionTextured(pos, v[j, 3] , v[j, 4]);
                }
            }

            vb.SetData(data, 0, LockFlags.None);


        }

        public void Render(Effect effect)
        {
            effect.Technique = "SkyBox";
            effect.SetValue("texDiffuseMap", textura);
            device.SetStreamSource(0, vb, 0);
            device.VertexFormat = CustomVertex.PositionTextured.Format;
            int numPasses = effect.Begin(0);
            for (var n = 0; n < numPasses; n++)
            {
                effect.BeginPass(n);
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 12);
                effect.EndPass();
            }
            effect.End();

        }


        public void Dispose()
        {
            if (vb != null)
            {
                vb.Dispose();
                vb = null;
            }

            if (textura != null)
            {
                textura.Dispose();
                textura = null;
            }
        }


    }


    public class CBaseTrack
    {
        public bool transparente = false;
        public int ds;          // separacion (en puntos de la ruta)
        public int cant_v = 12;      // cantidad de vertices x item
        public int cant_items;
        public int totalVertices;
        public int inicio = 0;      // punto inicial de la ruta
        public int fin = 0;      // punto final de la ruta
        public VertexBuffer vb = null;
        public string tx_fname;
        public Texture textura;
        public Microsoft.DirectX.Direct3D.Device device;

        public CScene T;
        public string MyMediaDir;
        public TGCVector3[] pt_ruta;
        public TGCVector3[] Binormal;
        public TGCVector3[] Normal;
        public float[] peralte;
        public float dr;
        public int dataIdx;

        public static CBaseTrack Create(int cant, int pinicio, int sep, CScene pT)
        {
            CBaseTrack obj = new CBaseTrack();
            obj.Init(cant, pinicio, sep, pT);
            return obj;
        }

        public virtual void Init(int cant, int pinicio, int sep, CScene pT)
        {
            T = pT;
            MyMediaDir = T.path_media + "Texturas\\";
            pt_ruta = T.pt_ruta;
            Binormal = T.Binormal;
            Normal = T.Normal;
            peralte = T.peralte;
            dr = T.ancho_ruta/2;
            cant_items = cant;
            ds = sep;
            inicio = pinicio;
            fin = inicio + cant * ds;

            //Crear vertexBuffer
            totalVertices = cant * cant_v;
            vb = new VertexBuffer(typeof(CustomVertex.PositionNormalTextured), totalVertices, D3DDevice.Instance.Device,
                Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionNormalTextured.Format, Pool.Default);

            //Cargar vertices
            dataIdx = 0;
            CustomVertex.PositionNormalTextured[] data = new CustomVertex.PositionNormalTextured[totalVertices];
            for (var t = 0; t < cant; t++)
            {
                FillVertexBuffer(t, data);
            }
            vb.SetData(data, 0, LockFlags.None);

            device = D3DDevice.Instance.Device;
            if (tx_fname.Length > 1)
                textura = Texture.FromBitmap(device, (Bitmap)Image.FromFile(MyMediaDir + tx_fname),
                    Usage.None, Pool.Managed);
            else
                textura = null;
        
        }

        public virtual void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
        }

        public void Render(Effect effect)
        {
            int pr = T.pos_en_ruta-8;

            // verifico si el objeto esta dentro del conjunto ptencialmente visible 
            if (pr > fin)     // el objeto quedo atras en la ruta
                return;
            if (pr + T.fov < inicio)       // esta muy adelante, todavia no entro en campo visual
                return;

            var p = pr - inicio;            // posicion relativa
            if (p < 0)
                p = 0;

            int start = cant_v * (int)(p / ds);
            int end = cant_v * (int)( (p+T.fov) / ds);
            if (end >= start + totalVertices)
                end = totalVertices + start;
            int cant_vertices = end - start;
            int cant_primitivas = cant_vertices / 3;
			
			SetTextures(effect);
            device.SetStreamSource(0, vb, 0);
            device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
			
            int numPasses = effect.Begin(0);
            for (var n = 0; n < numPasses; n++)
            {
                effect.BeginPass(n);
                device.DrawPrimitives(PrimitiveType.TriangleList, start, cant_primitivas);
                effect.EndPass();
            }
            effect.End();

        }
		
		public virtual void SetTextures(Effect effect)
		{
            effect.Technique = "DefaultTechnique";
            effect.SetValue("texDiffuseMap", textura);
		}
			

        public void Dispose()
        {
            if (vb != null)
            {
                vb.Dispose();
                vb = null;
            }

            if(textura!=null)
            {
                textura.Dispose();
                textura = null;
            }
        }



    }



    public class CCartel : CBaseTrack
    {
        public CCartel()
        {
            cant_v = 12;      // cantidad de vertices x item
            tx_fname = "cartel3.png";
            transparente = true;
        }

        public static new CCartel Create(int cant, int pinicio, int sep, CScene pT)
        {
            CCartel obj = new CCartel();
            obj.Init(cant, pinicio, sep, pT);
            return obj;
        }

        public override void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
            var i = inicio + t * ds;
            TGCVector3 p0, p1, p2, p3;
            p0 = pt_ruta[i] - Binormal[i] * (dr - 10) + Normal[i] * (peralte[i] + 30);
            p1 = pt_ruta[i] - Binormal[i] * (dr - 30) + Normal[i] * (peralte[i] + 70);
            p2 = pt_ruta[i + 4] - Binormal[i + 4] * (dr - 10) + Normal[i + 4] * (peralte[i + 4] + 30);
            p3 = pt_ruta[i + 4] - Binormal[i + 4] * (dr - 30) + Normal[i + 4] * (peralte[i + 4] + 70);

            TGCVector3 N = new TGCVector3(0, 1, 0);

            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, 0, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, 1, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, 1, 1);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, 0, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, 1, 1);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, 0, 1);

            p0 = pt_ruta[i] + Binormal[i] * (dr - 10) + Normal[i] * (30);
            p1 = pt_ruta[i] + Binormal[i] * (dr - 30) + Normal[i] * (70);
            p2 = pt_ruta[i + 4] + Binormal[i + 4] * (dr - 10) + Normal[i + 4] * (30);
            p3 = pt_ruta[i + 4] + Binormal[i + 4] * (dr - 30) + Normal[i + 4] * (70);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0,N, 0, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, 1, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, 1, 1);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, 0, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, 1, 1);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, 0, 1);
        }

    }

    public class CPuente : CBaseTrack
    {
        public CPuente()
        {
            cant_v = 6;      // cantidad de vertices x item
            tx_fname = "puente.png";
            transparente = true;
        }

        public static new CPuente Create(int cant, int pinicio, int sep, CScene pT)
        {
            CPuente obj = new CPuente();
            obj.Init(cant, pinicio, sep, pT);
            return obj;
        }

        public override void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
            var i = inicio + t * ds;
            TGCVector3 p0, p1, p2, p3;
            p0 = pt_ruta[i] - Binormal[i] * (dr + 160) - Normal[i] * 50;
            p1 = pt_ruta[i] - Binormal[i] * (dr + 160) + Normal[i] * 180;
            p2 = pt_ruta[i] + Binormal[i] * (dr + 160) - Normal[i] * 50;
            p3 = pt_ruta[i] + Binormal[i] * (dr + 160) + Normal[i] * 180;
            TGCVector3 N = new TGCVector3(0, 1, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, 0, 1);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, 0, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, 1, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, 0, 1);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, 1, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, 1, 1);
        }

    }


    public class CGuardRail : CBaseTrack
    {
        public float ancho_guarray = 5;
        public float alto_guarray = 30;
        public float Kr = 0.05f;
        public float KBN = 1;
        public float KP = 1;


        public CGuardRail()
        {
            cant_v = 6;      // cantidad de vertices x item
            tx_fname = "guardrail3.png";
            transparente = true;
        }

        public static CGuardRail Create(int cant, int pinicio, int sep, CScene pT, bool izq)
        {
            CGuardRail obj = new CGuardRail();
            obj.Init(cant, pinicio, sep, pT,izq);
            return obj;
        }

        public void Init(int cant, int pinicio, int sep, CScene pT, bool izq)
        {
            KBN = izq ? -1 : 1;
            KP = izq ? 0 : 1;
            Init(cant, pinicio, sep, pT);
        }


        public override void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
            var i = inicio + t;
            var p0 = pt_ruta[i] - Binormal[i]* KBN * dr + Normal[i] * peralte[i] * KP;
            var p1 = pt_ruta[i] - Binormal[i] * KBN * (dr + ancho_guarray) + Normal[i] * (peralte[i] * KP + alto_guarray);
            var p2 = pt_ruta[i+1] - Binormal[i+1] * KBN * dr + Normal[i+1] * peralte[i+1] * KP;
            var p3 = pt_ruta[i+1] - Binormal[i] * KBN * (dr + ancho_guarray) + Normal[i+1] * (peralte[i+1] * KP + alto_guarray);
            TGCVector3 N = new TGCVector3(0, 1, 0);

            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, i * Kr, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, i * Kr, 1);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, (i+1) * Kr, 0);

            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, (i + 1) * Kr, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, i * Kr, 1);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, (i +1) * Kr, 1);

        }

    }


    public class CPiso : CBaseTrack
    {

        public CPiso()
        {
            cant_v = 6;      // cantidad de vertices x item
            tx_fname = "f1piso4.png";
            transparente = false;
        }

        public static new CPiso Create(int cant, int pinicio, int sep, CScene pT)
        {
            CPiso obj = new CPiso();
            obj.Init(cant, pinicio, sep, pT);
            return obj;
        }

        public override void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
            var i = inicio + t;
            var Kr = 0.05f;

            var p0 = pt_ruta[i] - Binormal[i] * dr + Normal[i] * peralte[i];
            var p1 = pt_ruta[i] + Binormal[i] * dr;
            var p2 = pt_ruta[i+1] - Binormal[i + 1] * dr + Normal[i+1] * peralte[i+1];
            var p3 = pt_ruta[i + 1] + Binormal[i + 1] * dr;

            TGCVector3 N = new TGCVector3(0, 1, 0);

            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, 0 , i * Kr);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, 1, i * Kr);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, 0 , (i + 1) * Kr);

            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, 0 , (i + 1) * Kr);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, 1 , i * Kr);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, 1 , (i + 1) * Kr);

        }

    }


    public class CCubos : CBaseTrack
    {
        public int cubosxpt = 10;
        public TGCVector3 color;
        public Random rnd;
        public CCubos()
        {
            cant_v = 36 * cubosxpt;      // cantidad de vertices x item
            tx_fname = "";
            rnd = new Random();
            transparente = false;
        }

        public void Init(int cant, int pinicio, int sep, CScene pT, TGCVector3 pcolor)
        {
            color = pcolor;
            Init(cant, pinicio, sep, pT);
        }

        public static CCubos Create(int cant, int pinicio, int sep, CScene pT,TGCVector3 pcolor)
        {
            CCubos obj = new CCubos();
            obj.Init(cant, pinicio, sep, pT,pcolor);
            return obj;
        }

        public override void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
            var i = inicio + t * ds;
            for (var j = 0; j < cubosxpt; ++j)
            {
                TGCVector3[] Pt = new TGCVector3[8];
                que_puntos(i,j,Pt);
                float ex = 1;
                float ey = 1;

                // abajo
                TGCVector3 N = new TGCVector3(0, -1, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[0], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[1], N, 0, 1*ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[2], N, 1*ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[0], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[2], N, 1 * ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[3], N, 1 * ex, 0);

                // arriba
                N = new TGCVector3(0, 1, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[4], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[5], N, 0, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[6], N, 1 * ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[4], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[6], N, 1 * ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[7], N, 1 * ex, 0);

                // cara atras
                N = new TGCVector3(0, 0, -1);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[0], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[4], N, 0, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[1], N, 1 * ex, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[1], N, 1 * ex, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[4], N, 0, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[5], N, 1 * ex, 1 * ey);

                // cara adelante
                N = new TGCVector3(0, 0, 1);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[3], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[7], N, 0, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[2], N, 1 * ex, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[2], N, 1 * ex, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[7], N, 0, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[6], N, 1 * ex, 1 * ey);

                // cara derecha
                N = new TGCVector3(1, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[2], N, 1 * ex, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[6], N, 1 * ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[1], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[1], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[6], N, 1 * ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[5], N, 0, 1 * ey);

                // cara izquierda
                N = new TGCVector3(-1, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[3], N, 1 * ex, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[7], N, 1 * ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[0], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[0], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[7], N, 1 * ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[4], N, 0, 1 * ey);
            }
        }

      


        public virtual void que_puntos(int i,int j,TGCVector3 []pt) 
        {
            var dx = rnd.Next(30, 80);
            var dy = rnd.Next(30, 100);
            var dz = rnd.Next(30, 80);
            TGCVector3 p0 = new TGCVector3(0, 0, 0);

            switch ((i * j) % 3)
            {
                default:
                case 0:
                    p0 = pt_ruta[i] + Binormal[i] * rnd.Next((int)dr, (int)dr + 300) + Normal[i] * rnd.Next(50, 250);
                    break;
                case 1:
                    p0 = pt_ruta[i] + Binormal[i] * rnd.Next(-(int)dr - 300, -(int)dr) + Normal[i] * rnd.Next(50, 250);
                    break;
                case 2:
                    p0 = pt_ruta[i] + Binormal[i] * rnd.Next(-(int)dr, (int)dr) + Normal[i] * rnd.Next(300, 700);
                    break;
            }

            pt[0] = p0;
            pt[1] = p0 + new TGCVector3(dx, 0, 0);
            pt[2] = p0 + new TGCVector3(dx, 0, dz);
            pt[3] = p0 + new TGCVector3(0, 0, dz); 
            pt[4] = p0 + new TGCVector3(0, dy, 0); 
            pt[5] = pt[1] + new TGCVector3(0, dy, 0);
            pt[6] = pt[2] + new TGCVector3(0, dy, 0);
            pt[7] = pt[3] + new TGCVector3(0, dy, 0);
        }

        public override void SetTextures(Effect effect)
        {
            effect.Technique = "EdgeCube";
            effect.SetValue("cube_color", TGCVector3.Vector3ToFloat4Array(color));
        }


    }

    public class CBuildings : CCubos
    {
        public CBuildings()
        {
            cubosxpt = 10;
            cant_v = 36 * cubosxpt;      // cantidad de vertices x item
            tx_fname = "";
            rnd = new Random();
            transparente = false;
        }

        public static new CBuildings Create(int cant, int pinicio, int sep, CScene pT, TGCVector3 pcolor)
        {
            CBuildings obj = new CBuildings();
            obj.Init(cant, pinicio, sep, pT, pcolor);
            return obj;
        }


        public override void que_puntos(int i, int j, TGCVector3[] pt)
        {
            var dx = rnd.Next(60, 100);
            var dy = rnd.Next(100, 700);
            var dz = rnd.Next(40, 100);
            TGCVector3[] Tangent = T.Tangent;
            TGCVector3 p0 = new TGCVector3(0, 0, 0);

            switch ((i + j) % 2)
            {
                default:
                case 0:
                    p0 = pt_ruta[i] + Binormal[i] * (dr + 60 + j * 40);
                    break;
                case 1:
                    p0 = pt_ruta[i] - Binormal[i] * (dr + 60 + j * 40);
                    break;
            }

            p0 = p0 - Normal[i] * 50 - Binormal[i] * dx * 0.5f;
            pt[0] = p0;
            pt[1] = p0 + Binormal[i] * dx;
            pt[2] = p0 + Binormal[i] * dx + Tangent[i] * dz;
            pt[3] = p0 + Tangent[i] * dz;
            pt[4] = p0 + Normal[i] * dy;
            pt[5] = pt[1] + Normal[i] * (dy + rnd.Next(0, 30));
            pt[6] = pt[2] + Normal[i] * (dy + rnd.Next(0, 30));
            pt[7] = pt[3] + Normal[i] * (dy + rnd.Next(0, 30));
        }

        public override void SetTextures(Effect effect)
        {
            effect.Technique = "Buildings";
            effect.SetValue("cube_color", TGCVector3.Vector3ToFloat4Array(color));
        }


    }

    public class CPatchSideTrack : CBaseTrack
    {
        public int quadsxpt = 30;
        public int cant_patches;

        public CPatchSideTrack()
        {
            cant_v = 12 * quadsxpt;
            tx_fname = "";
            transparente = false;
        }

        public static new CPatchSideTrack Create(int cant, int pinicio, int sep, CScene pT)
        {
            CPatchSideTrack obj = new CPatchSideTrack();
            obj.Init(cant, pinicio, sep, pT);
            return obj;
        }

        public override void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
            float r = 1.5F * dr;
            float dp = 35;
            float dh = 30;
			var i = inicio + t;
			for (var K = -1; K <= 1; K += 2)
			{
				for (var j = 0; j < quadsxpt; ++j)
				{
					float dj = (dr + dp * j);
					float dj1 = (dr + dp * (j + 1));

					var p0 = pt_ruta[i] - Binormal[i] * dj * K;
					var p1 = pt_ruta[i] - Binormal[i] * dj1 * K;
					var p2 = pt_ruta[i + 1] - Binormal[i + 1] * dj * K;
					var p3 = pt_ruta[i + 1] - Binormal[i + 1] * dj1 * K;

					p0 = p0 + Normal[i] * (noise(p0 * 0.01f) * dh * j - 20);
					p1 = p1 + Normal[i] * (noise(p1 * 0.01f) * dh * (j + 1) - 20);
					p2 = p2 + Normal[i] * (noise(p2 * 0.01f) * dh * j - 20);
					p3 = p3 + Normal[i] * (noise(p3 * 0.01f) * dh * (j + 1) - 20);

					var N = TGCVector3.Cross(p2 - p0, p3 - p0) * (-K);
					N.Normalize();
					data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, 0, 0);
					data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, 0, 1);
					data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, 1, 1);

					N = TGCVector3.Cross(p3 - p0, p1 - p0) * (-K);
					N.Normalize();
					data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, 0, 0);
					data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, 1, 1);
					data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, 1, 0);
				}
			}
        }


		public override void SetTextures(Effect effect)
		{
            effect.SetValue("texDiffuseMap", textura);
            effect.SetValue("cube_color", TGCVector3.Vector3ToFloat4Array(new TGCVector3(1, 0.5f, 0)));
            if (textura != null)
            {
                effect.SetValue("texDiffuseMap", textura);
                effect.Technique = "DefaultTechnique";
            }
            else
                effect.Technique = "Terrain";
            device.RenderState.AlphaBlendEnable = true;
		}
		
        // helpers
        public float lerp(float x, float y, float s)
        {
            if (s < 0)
                s = 0;
            else
            if (s > 1)
                s = 1;
            return x * (1 - s) + y * s;
        }

        public float noise(TGCVector3 x)
        {
            TGCVector3 p = new TGCVector3((float)Math.Floor(x.X), (float)Math.Floor(x.Y), (float)Math.Floor(x.Z));
            TGCVector3 f = p - x;
            f.X = f.X * f.X * (3.0f - 2.0f * f.X);
            f.Y = f.Y * f.Y * (3.0f - 2.0f * f.Y);
            f.Z = f.Z * f.Z * (3.0f - 2.0f * f.Z);

            float n = p.X + p.Y * 57.0f + 113.0f * p.Z;
            float rta = lerp(lerp(lerp(hash(n + 0.0f), hash(n + 1.0f), f.X),
                lerp(hash(n + 57.0f), hash(n + 58.0f), f.X), f.Y),
                lerp(lerp(hash(n + 113.0f), hash(n + 114.0f), f.X),
                    lerp(hash(n + 170.0f), hash(n + 171.0f), f.X), f.Y), f.Z);
            return rta;
        }

        float hash(float n)
        {
            float s = (float)Math.Sin(n) * 43758.5453f;
            float rta = s - (float)Math.Floor(s);
            return rta;
        }

    }

    public class CTunelSideTrack : CPatchSideTrack
    {
        public bool perturbar = true;

        public void Init(int cant, int pinicio, int sep, CScene pT, string p_tx_fname, bool p_perturbar)
        {
            perturbar = p_perturbar;
            tx_fname = p_tx_fname;
            Init(cant, pinicio, sep, pT);
        }

        public static CTunelSideTrack Create(int cant, int pinicio, int sep, CScene pT, string p_tx_fname, bool p_perturbar)
        {
            CTunelSideTrack obj = new CTunelSideTrack();
            obj.Init(cant, pinicio, sep, pT,p_tx_fname,p_perturbar);
            return obj;
        }

        public override void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
            float r = 1.5F * dr;
            float dp = 35;
            float dh = 30;
            var i = inicio + t;
            for (var j = 0; j < 2 * quadsxpt; ++j)
            {
                float s0 = (float)j / (2.0f * (float)quadsxpt);
                float an = lerp(-0.5f, (float)Math.PI + .5f, s0);
                float X = (float)Math.Cos(an) * r;
                float Y = (float)Math.Sin(an) * r;

                float s1 = (float)(j + 1) / (2.0f * (float)quadsxpt);
                float an1 = lerp(-0.5f, (float)Math.PI + .5f, s1);
                float X1 = (float)Math.Cos(an1) * r;
                float Y1 = (float)Math.Sin(an1) * r;

                var n0 = Binormal[i] * X + Normal[i] * Y;
                var n1 = Binormal[i] * X1 + Normal[i] * Y1;
                var n2 = Binormal[i + 1] * X + Normal[i + 1] * Y;
                var n3 = Binormal[i + 1] * X1 + Normal[i + 1] * Y1;

                var p0 = pt_ruta[i] + n0;
                var p1 = pt_ruta[i] + n1;
                var p2 = pt_ruta[i + 1] + n2;
                var p3 = pt_ruta[i + 1] + n3;

                // perturbacion
                bool perturbacion = true;
                if (perturbacion)
                {
                    n0.Normalize();
                    n1.Normalize();
                    n2.Normalize();
                    n3.Normalize();
                    p0 = p0 - n0 * (noise(p0 * 0.01f) * dh);
                    p1 = p1 - n1 * (noise(p1 * 0.01f) * dh);
                    p2 = p2 - n2 * (noise(p2 * 0.01f) * dh);
                    p3 = p3 - n3 * (noise(p3 * 0.01f) * dh);
                }

                float t0 = (float)i / (2.0f * (float)quadsxpt);
                float t1 = (float)(i + 1) / (2.0f * (float)quadsxpt);


                var N = TGCVector3.Cross(p2 - p0, p3 - p0) * (-1);
                N.Normalize();
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, s0, t0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, s0, t1);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, s1, t1);

                N = TGCVector3.Cross(p3 - p0, p1 - p0) * (-1);
                N.Normalize();
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, s0, t0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, s1, t1);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, s1, t0);
            }

        }




    }

    public class CScene
    {
        const int MAX_POINTS = 5000;
        const int MAX_TRACKS = 100;
        const int max_pt = 250;
        public float ancho_ruta = 200;
        public int fov = 250;   // cuantos puntos de la ruta puedo ver hacia adelante

        public int cant_ptos_ruta;
        public float dh = 3; // alto de la pared
        public float M_PI = 3.14151f;
        public int pos_en_ruta;
        public float pos_s;
        public float pos_t;
        public TGCVector3[] pt_ruta = new TGCVector3[MAX_POINTS];
        public TGCVector3[] Normal = new TGCVector3[MAX_POINTS];
        public TGCVector3[] Tangent = new TGCVector3[MAX_POINTS];
        public TGCVector3[] Binormal = new TGCVector3[MAX_POINTS];
        public float []peralte = new float[MAX_POINTS];
        public float scaleXZ = 20;
        public float scaleY = 15;
        public TGCVector3 pos_central = new TGCVector3(0,0,0);

        public CBaseTrack[] tracks = new CBaseTrack[MAX_TRACKS];
        int cant_tracks;
        public TGCVector3 pt_colision = new TGCVector3();
        public int tipo_colision = 0;
        public int cant_colisiones = 0;
        public string path_media;

        public CSkyBox skybox;

        public CScene(string mediaDir)
        {
            path_media = mediaDir;
            CrearRuta(mediaDir);
            skybox = new CSkyBox("skybox2.jpg", this);
        }


       public int rt_Spline(TGCVector3[] pt ,  TGCVector3  []P, int cant_p, float alfa)
        {
            int cant = 0;
            float fi = alfa != 0 ? (1 / alfa) : 1;

            for (int i = 1; i < cant_p - 3; ++i)
            {
                // Segmento Pi - Pi+1
                // Calculo los 4 puntos de control de la curva de Bezier
                TGCVector3 B0 = P[i];
                TGCVector3 B3 = P[i + 1];
                // Calculo las derivadas en los extremos
                TGCVector3 B0p = (P[i + 1] - P[i - 1]) * fi;
                TGCVector3 B1p = (P[i + 2] - P[i]) * fi;

                // los otros 2 ptos de control usando las derivadas
                TGCVector3 B1 = B0p * (1.0f / 3.0f) + B0;
                TGCVector3 B2 = B3 - B1p * (1.0f / 3.0f);

                // Ahora dibujo la curva F(t), con 0<=t<=1
                // Interpolo con rectas
                float dt = 0.05f;
                for (float t = 0; t < 1-dt; t += dt)
                {
                    // calculo los coeficientes del polinomio de Bezier
                    float k0 = (1f - t) * (1f - t) * (1f - t);
                    float k1 = 3f * (1f - t) * (1f - t) * t;
                    float k2 = 3f * (1f - t) * t * t;
                    float k3 = t * t * t;
                    // calculo el valor del polinimio pp dicho 
                    TGCVector3 Bt = B0 * k0 + B1 * k1 + B2 * k2 + B3 * k3;
                    pt[cant++] = Bt;
                }

                //pt[cant++] = P[i];
                // paso al siguiente segmento
            }
            pt[cant++] = P[cant_p - 1];
            return cant;
        }


        // Carga los ptos de la ruta
        public void load_pt_ruta()
        {
            // Genero el path de la ruta
            cant_ptos_ruta = 0;

            TGCVector3[] p = new TGCVector3[max_pt];
            TGCVector3[] n = new TGCVector3[max_pt];
            var rnd = new Random();
            TGCVector3 dir = new TGCVector3(5, 1, 0);
            dir.Normalize();
            TGCVector3 Q = new TGCVector3(0, 0, 0);
            TGCVector3 N = new TGCVector3(0, 1, 0);
            int status = 0;
            int step = 4;
            for (int i = 0; i < max_pt; ++i)
            {


                p[i] = Q;
                n[i] = Q + N;

                TGCVector3 B = TGCVector3.Cross(dir, N);
                B.Normalize();


                //alfa += (float)Math.PI * rnd.Next(-15, 15) / 180.0f;
                switch (status)
                {
                    default:
                        // linea recta  test
                        Q = Q + dir * 500;
                        break;

                    case 0:
                        // linea recta 
                        --step;
                        if (step == 0)
                        {
                            step = 5;
                            status = 1;
                        }
                        Q = Q + dir * 500;
                        break;
                    case 1:
                        {
                            // curva en UP
                            dir.TransformNormal(TGCMatrix.RotationAxis(B, 0.3f));
                            --step;
                            if (step == 0)
                            {
                                step = 40;
                                status = 2;
                            }
                            Q = Q + dir * 300;
                        }
                        break;
                    case 2:
                        // linea recta 
                        --step;
                        if (step == 0)
                        {
                            step = 5;
                            status = 0;
                        }
                        Q = Q + dir * 500;
                        dir.TransformNormal(TGCMatrix.RotationAxis(N, (float)Math.PI * rnd.Next(-20, 20) / 180.0f));
                        dir.TransformNormal(TGCMatrix.RotationAxis(B, (float)Math.PI * rnd.Next(-15, 15) / 180.0f));
                        break;

                }

              
                // computo la siguiente normal
                TGCVector3 BN = TGCVector3.Cross(dir, N);
                BN.Normalize();
                N = TGCVector3.Cross(BN, dir);
                N.Normalize();
            }

            // puntos de la ruta (sobre el piso)
            cant_ptos_ruta = rt_Spline(pt_ruta, p, max_pt, 2.0f);
            // puntos de la ruta elevados 
            rt_Spline(Normal, n, max_pt, 2.0f);
            // computo las normales , tangente y binormal
            for(int i=0;i < cant_ptos_ruta;++i)
            {
                Tangent[i] = pt_ruta[i + 1] - pt_ruta[i];
                Tangent[i].Normalize();

                TGCVector3 Up = Normal[i] - pt_ruta[i];
                Up.Normalize();

                Binormal[i] = TGCVector3.Cross(Tangent[i] , Up);
                Binormal[i].Normalize();

                Normal[i] = TGCVector3.Cross(Binormal[i] , Tangent[i]);
                Normal[i].Normalize();

            }
            --cant_ptos_ruta; // me aseguro que siempre exista el i+1
        }

        public void CrearRuta(string mediaDir)
        {
            // Cargo la ruta
            load_pt_ruta();

            // peralte
            var rnd = new Random();
            for (var i = 0; i < cant_ptos_ruta; ++i)
            {
                peralte[i] = (float)Math.Sin((double)i * 0.05) * 50;
            }


            // Creo los tracks pp dichos
            tracks[cant_tracks++] = CCubos.Create(300, 0, 1, this, new TGCVector3(1, 0.75f, 0.5f));
            tracks[cant_tracks++] = CCubos.Create(300, 300, 1, this, new TGCVector3(0.25f, 0.75f, 1.0f));
            tracks[cant_tracks++] = CBuildings.Create(100, 600, 5, this, new TGCVector3(0, 0.5f, 1));

            tracks[cant_tracks++] = CPiso.Create(cant_ptos_ruta, 0, 1, this);

            tracks[cant_tracks++] = CCartel.Create(25, 50, 17, this);
            tracks[cant_tracks++] = CPuente.Create(25, 50, 30, this);
            tracks[cant_tracks++] = CGuardRail.Create(cant_ptos_ruta, 0, 1, this, true);
            tracks[cant_tracks++] = CGuardRail.Create(cant_ptos_ruta, 0, 1, this, false);

            tracks[cant_tracks++] = CTunelSideTrack.Create(400, 1200, 1, this, "", true);
            tracks[cant_tracks++] = CPatchSideTrack.Create(500, 1600, 1, this);
            tracks[cant_tracks++] = CTunelSideTrack.Create(400, 2100, 1, this, "tunel2.png", false);
            tracks[cant_tracks++] = CTunelSideTrack.Create(400, 2500, 1, this, "tunel.png", false);

        }


        public void render(Effect effect)
        {
            var device = D3DDevice.Instance.Device;

            // opacos
            effect.Technique = "DefaultTechnique";
            TgcShaders.Instance.setShaderMatrixIdentity(effect);
            device.RenderState.AlphaBlendEnable = false;
            skybox.Render(effect);


            for (int i = 0; i < cant_tracks; ++i)
                if(!tracks[i].transparente)
                    tracks[i].Render(effect);

            // Ahora los objetos transparentes guarda rail, y los carteles
            device.RenderState.AlphaBlendEnable = true;
            for (int i = 0; i < cant_tracks; ++i)
                if (tracks[i].transparente)
                    tracks[i].Render(effect);
        }



        public float que_angulo_actual()
        {
            float rta = 0;
            if(pos_en_ruta!=-1)
            {
                int i = pos_en_ruta;
                float P = peralte[i] * (1 - pos_s) + peralte[i] * pos_s;
                rta = (float)Math.Atan2(-P, ancho_ruta);
            }
            return rta;
        }

        public void proyectar(TGCVector3 p, float desfH , int i ,
            out float X, out float Z , out TGCVector3 p0,out TGCVector3 pc)
        {
            var dr = ancho_ruta / 2 - 50.0f;
            var v = p - pt_ruta[i];
            Z = TGCVector3.Dot(v, Tangent[i]);
            X = TGCVector3.Dot(v, Binormal[i]);

            if (X < -dr)
            {
                X = -dr;
                // colision izquierda
                tipo_colision = 2;
                cant_colisiones++;
            }

            if (X > dr)
            {
                X = dr;
                // colision derecha
                tipo_colision = 1;
                cant_colisiones++;
            }

            float dP = peralte[i] * (1 - (X + dr) / (2 * dr));
            p0 = pt_ruta[i] + Tangent[i] * Z + Binormal[i] * X + Normal[i] * (desfH + dP);
            pc = pt_ruta[i] + Tangent[i] * Z + Normal[i] * (desfH + dP);
        }


        // busco la posicion mas cercana dentro de la pista
        public TGCVector3 updatePos(TGCVector3 p, float desfH)
        {
            tipo_colision = 0;
            var aux_tramo = -1;
            var mdist = -1f;
            var dr = ancho_ruta / 2 - 50.0f;
            // busco el punto mas cercano en un entorno de pos_en_ruta
            for (var i = pos_en_ruta; i < pos_en_ruta + 15 && i < cant_ptos_ruta - 1; ++i)
            {
                var dist = (pt_ruta[i] - p).LengthSq();
                if (dist < mdist || mdist < 0.0f)
                {
                    aux_tramo = i;
                    mdist = dist;
                }
            }

            if (aux_tramo != -1)
            {
                int i = aux_tramo;

                // interpolacion lineal


                TGCVector3 q0 = pt_ruta[i - 1];
                TGCVector3 q1 = pt_ruta[i];
                TGCVector3 q2 = pt_ruta[i + 1];

                if ((q0 - p).LengthSq() < (q2 - p).LengthSq())
                {
                    // interpolo entre i-1 y i
                    i = --aux_tramo;
                }


                proyectar(p, desfH, i, out float X0, out float Z0, out TGCVector3 p0, out TGCVector3 pc0);
                float d0 = (pt_ruta[i] - p).LengthSq();

                proyectar(p, desfH, i+1, out float X1, out float Z1, out TGCVector3 p1, out TGCVector3 pc1);
                float d1 = (pt_ruta[i+1] - p).LengthSq();

                float S = d0 + d1;
                float k = d0 / S;
                p = p0 * (1-k) + p1 * k;
                pos_central = pc0 * (1-k) + pc1 * k;

                float X = X0 * (1-k) + X1 * k;
                float Z = Z0 * (1-k) + Z1 * k;




                /*
                // proyecto la posicion sobre el espacio de la ruta
                var v = p - pt_ruta[i];
                var Z = TGCVector3.Dot(v, Tangent[i]);
                var X = TGCVector3.Dot(v, Binormal[i]);
                if (X < -dr)
                {
                    X = -dr;
                    // colision izquierda
                    tipo_colision = 2;
                    cant_colisiones++;
                }

                if (X > dr)
                {
                    X = dr;
                    // colision derecha
                    tipo_colision = 1;
                    cant_colisiones++;
                }

                float dP = peralte[i] * (1 - (X + dr) / (2 * dr));
                p = pt_ruta[i] + Tangent[i] * Z + Binormal[i] * X + Normal[i]* (desfH + dP);
                pos_central = pt_ruta[i] + Tangent[i] * Z + Normal[i] * (desfH + dP);
                */


                // actualizo la posicion en ruta
                pos_en_ruta = aux_tramo;

            }
            return p;
        }


        public TGCVector3 que_dir(TGCVector3 v)
        {
            int i = pos_en_ruta;
            var Z = TGCVector3.Dot(v, Tangent[i]);
            var X = TGCVector3.Dot(v, Binormal[i]);
            v = Tangent[i] * Z + Binormal[i] * X;
            v.Normalize();
            return v;
        }

        

        public void dispose()
        {
            for (var i = 0; i < cant_tracks; ++i)
                tracks[i].Dispose();
        }

    }
}
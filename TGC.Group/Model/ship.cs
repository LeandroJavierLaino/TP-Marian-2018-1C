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
using Microsoft.DirectX.DirectInput;

namespace TGC.Group.Model
{
    public class CSimpleShipPhysics
    {
        public CScene S;
        public int pos_en_ruta;
        public float pos_s;
        public float pos_t;
        public TGCVector3 pt_colision = new TGCVector3();
        public int tipo_colision = 0;
        public int cant_colisiones = 0;
        public bool keyboard_input = true;


        public TGCVector3 pos;
        public TGCVector3 dir;
        public TGCVector3 dirN = new TGCVector3(0, 0, 0);
        public TGCVector3 pos_central = new TGCVector3(0, 0, 0);
        public float speed = 5000;
        public float max_speed = 10000;
        public float yaw = 0;
        public float pitch = 0;
        public float roll = 0.5f;
        public float rotationSpeed = 0.0f;
        public float desfH = 30;
        public float acel_angular = 0.0f;
        public float acel_lineal = 0.0f;
        public float muD = 0.0f;       // coef. de rozamiento dinamico

        public TGCVector3 F= new TGCVector3(0, 0, 0);

        public CSimpleShipPhysics(CScene pS)
        {
            S = pS;
        }

        public void updatePos()
        {
            pos = S.pt_ruta[pos_en_ruta];
            dir = S.pt_ruta[pos_en_ruta + 1] - pos;
            dir.Normalize();
            dirN = S.Normal[pos_en_ruta];
            pos += dirN * desfH;

        }


        // computa todas las fuerzas que invervienen
        // necesito el Input, ya que las teclas generan fuerzas 
        public void computeForces(TgcD3dInput Input , float ElapsedTime)
        {
            // limpio cualquier otra fuerza 
            acel_angular = 0.0f;
            acel_lineal = 0.0f;

            if (keyboard_input)
            {
                // colision entre el player one y el resto
                F = new TGCVector3(0, 0, 0);
                for (int i = 1; i < S.cant_players; ++i)
                {
                    TGCVector3 impulse = pos - S.PLAYERS[i].P.pos;
                    float d = impulse.LengthSq();
                    if (d < 2500)
                    {
                        F += impulse;
                        S.PLAYERS[i].P.F -= impulse;
                    }
                }
            }

        

            // 1- fuerza de aceleracion angular
            if ((keyboard_input && Input.keyDown(Key.Right)) || tipo_colision == 1)
                acel_angular = 2.5f;
            else
            if ((keyboard_input && Input.keyDown(Key.Left)) || tipo_colision == 2)
                acel_angular = -2.5f;

            // 2- fuerza de aceleracion linear
            if (keyboard_input && Input.keyDown(Key.Up))
            {
                acel_lineal = 850.0f;
                if (pitch < 0.1f)
                    pitch += 0.5f * ElapsedTime;

            }
            else
            if (keyboard_input && Input.keyDown(Key.Down))
            {
                acel_lineal = -850.0f;
                if (pitch > -0.1f)
                    pitch -= 0.5f * ElapsedTime;
            }
            if (Math.Abs(pitch) > 0.0001f)
            {
                pitch -= 0.2f * ElapsedTime * Math.Sign(pitch);
            }

        }



        // integra (simple euler) las fuerzas
        public void integrateForces(float ElapsedTime)
        {
            // velocidad angular
            rotationSpeed += acel_angular * ElapsedTime;
            float rozamiento = rotationSpeed * 2.3f;
            rotationSpeed -= rozamiento * ElapsedTime;

            // transformo la direccion actual de acuerdo a la velocidad de rotacion
            var Tg = S.Binormal[pos_en_ruta];
            var Up = S.Normal[pos_en_ruta];
            dir.TransformNormal(TGCMatrix.RotationAxis(Up, rotationSpeed * ElapsedTime));

            // velocidad lineal
            speed += acel_lineal * ElapsedTime;

            // rotamiento
            rozamiento = speed * muD;
            speed -= rozamiento * ElapsedTime;

            if (speed > max_speed)
                speed = max_speed;

            if(F.LengthSq()>0)
            {
                TGCVector3 resultante = dir * speed + F * 0.3f;
                dir = resultante;
                speed = dir.Length();
                dir.Normalize();

                // limpio la fuerza asociada al collision response
                F = new TGCVector3(0, 0, 0);

            }

            // computo la velocidad y la posicion siguiente (pos deseada)
            TGCVector3 vel = dir * speed;
            TGCVector3 Desired_pos = pos + vel * ElapsedTime;
            // aprovecho para girar la nave
            roll = rotationSpeed * 0.7f;
            roll += que_angulo_actual();

            // interpolo suavemente entre la posicion actual y la deseada
            Desired_pos = updatePosRuta(Desired_pos, desfH);
            float k = 0.1f;
            pos = pos * (1 - k) + Desired_pos * k;

            // interpolo suavemente entre la direccion actual y la deseada
            // (direccion normalizada sobre el espacio del ESCENA)
            TGCVector3 Desired_dir = que_dir(dir);
            float q = 0.001f;
            dir = dir * (1 - q) + Desired_dir * q;


        }


        public void update(TgcD3dInput Input, CScene S, float ElapsedTime)
        {
            // calculo las fuerzas 
            computeForces(Input, ElapsedTime);
            // integracion
            integrateForces(ElapsedTime);
        }

        public float que_angulo_actual()
        {
            float rta = 0;
            if (pos_en_ruta != -1)
            {
                int i = pos_en_ruta;
                float P = S.peralte[i] * (1 - pos_s) + S.peralte[i] * pos_s;
                rta = (float)Math.Atan2(-P, S.ancho_ruta);
            }
            return rta;
        }

        // busco la posicion mas cercana dentro de la pista
        public TGCVector3 updatePosRuta(TGCVector3 p, float desfH)
        {
            tipo_colision = 0;
            var aux_tramo = -1;
            var mdist = -1f;
            var dr = S.ancho_ruta / 2 - 50.0f;
            // busco el punto mas cercano en un entorno de pos_en_ruta
            for (var i = pos_en_ruta; i < pos_en_ruta + 15 && i < S.cant_ptos_ruta - 1; ++i)
            {
                var dist = (S.pt_ruta[i] - p).LengthSq();
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
                TGCVector3 q0 = S.pt_ruta[i - 1];
                TGCVector3 q1 = S.pt_ruta[i];
                TGCVector3 q2 = S.pt_ruta[i + 1];

                if ((q0 - p).LengthSq() < (q2 - p).LengthSq())
                {
                    // interpolo entre i-1 y i
                    i = --aux_tramo;
                }


                proyectar(p, desfH, i, out float X0, out float Z0, out TGCVector3 p0, out TGCVector3 pc0);
                float d0 = (S.pt_ruta[i] - p).LengthSq();

                proyectar(p, desfH, i + 1, out float X1, out float Z1, out TGCVector3 p1, out TGCVector3 pc1);
                float d1 = (S.pt_ruta[i + 1] - p).LengthSq();

                float D = d0 + d1;
                float k = d0 / D;
                p = p0 * (1 - k) + p1 * k;
                pos_central = pc0 * (1 - k) + pc1 * k;

                float X = X0 * (1 - k) + X1 * k;
                float Z = Z0 * (1 - k) + Z1 * k;

                // actualizo la posicion en ruta
                pos_en_ruta = aux_tramo;

            }
            return p;
        }

        public void proyectar(TGCVector3 p, float desfH, int i,
            out float X, out float Z, out TGCVector3 p0, out TGCVector3 pc)
        {
            var dr = S.ancho_ruta / 2 - 50.0f;
            var v = p - S.pt_ruta[i];
            Z = TGCVector3.Dot(v, S.Tangent[i]);
            X = TGCVector3.Dot(v, S.Binormal[i]);

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

            if (tipo_colision != 0 && keyboard_input)
                speed *= 0.9999f;

            float dP = S.peralte[i] * (1 - (X + dr) / (2 * dr));
            p0 = S.pt_ruta[i] + S.Tangent[i] * Z + S.Binormal[i] * X + S.Normal[i] * (desfH + dP);
            pc = S.pt_ruta[i] + S.Tangent[i] * Z + S.Normal[i] * (desfH + dP);
        }



        public TGCVector3 que_dir(TGCVector3 v)
        {
            int i = pos_en_ruta;
            var Z = TGCVector3.Dot(v, S.Tangent[i]);
            var X = TGCVector3.Dot(v, S.Binormal[i]);
            v = S.Tangent[i] * Z + S.Binormal[i] * X;
            v.Normalize();
            return v;
        }



    }

    public class CShip
    {
        public TgcMesh mesh;
        public TGCVector3 car_Scale = new TGCVector3(1,1,1) * 0.5f;
        public CScene S;
        public CSimpleShipPhysics P;
        public TGCBox box ,bb;

        public CShip(string MediaDir, CScene pscene , String fname)
        {
            S = pscene;
            P = new CSimpleShipPhysics(S);
            var loader = new TgcSceneLoader();
            mesh = loader.loadSceneFromFile(MediaDir + fname).Meshes[0];
            mesh.AutoTransform = false;
            mesh.Technique = "DefaultTechnique";

            box = TGCBox.fromExtremes(new TGCVector3(0, 0, 0), new TGCVector3(100, 40, 40),
                TgcTexture.createTexture(MediaDir + "texturas\\plasma.png"));
            box.AutoTransform = false;
            box.Technique = "Fire";

            bb = TGCBox.fromExtremes(new TGCVector3(0, 0, 0), new TGCVector3(1, 1,1),
                TgcTexture.createTexture(MediaDir + "texturas\\plasma.png"));
            bb.AutoTransform = false;
            bb.Technique = "DefaultTechnique";

        }

        public void updatePos()
        {
            P.updatePos();
        }

        
        public void Update(TgcD3dInput Input, CScene S, float ElapsedTime)
        {
            // actualizo a traves del CSimplePhysics
            P.update(Input, S, ElapsedTime);

            // actualizo la posicion de la nave
            mesh.Transform = CalcularMatriz(P.pos, car_Scale, -P.dir, S.Normal[P.pos_en_ruta]);


        }

        public void Render(Microsoft.DirectX.Direct3D.Effect effect)
        {

            // verifico si el objeto esta dentro del conjunto ptencialmente visible 
            if (P.pos_en_ruta < S.player_one.P.pos_en_ruta - 8)
                // esta muy atras del jugador 
                return;

            if (P.pos_en_ruta > S.player_one.P.pos_en_ruta +  S.fov)
                // esta muy adelante, todavia no entro en campo visual
                return;

            var device = D3DDevice.Instance.Device;

            mesh.Effect = effect;
            device.RenderState.AlphaBlendEnable = false;
            mesh.Render();

            //bb.Effect = effect;
            //bb.Transform = TGCMatrix.Scaling(new TGCVector3(100.0f, 20.0f, 100.0f)) * mesh.Transform;
            //bb.Render();

            box.Effect = effect;

            // los mesh importados de skp tienen 100 de tamaño normalizado, primero trabajo en el espacio normalizado
            // del mesh y luego paso al worldspace con la misma matriz de la nave

            float e = 1.0f + 2.0f * P.speed/ P.max_speed;
            float dl = 50.0f + 50.0f* e;
            TGCVector3[] T = new TGCVector3[11];
            T[0] = new TGCVector3(dl, -5.0f, 0.0f);
            T[1] = new TGCVector3(dl, -5.0f, -1.0f);
            T[2] = new TGCVector3(dl, -5.0f, 1.0f);
            T[3] = new TGCVector3(dl, -6.0f, -1.0f);
            T[4] = new TGCVector3(dl, -4.0f, 1.0f);

            T[5] = new TGCVector3(dl, -5.0f, 16.0f);
            T[6] = new TGCVector3(dl, -5.0f, 16.0f - 0.3f);
            T[7] = new TGCVector3(dl, -5.0f, 16.0f + 0.3f);

            T[8] = new TGCVector3(dl, -5.0f, -16.0f);
            T[9] = new TGCVector3(dl, -5.0f, -16.0f - 0.3f);
            T[10] = new TGCVector3(dl, -5.0f, -16.0f + 0.3f);


            for (int i = 0; i < 11; ++i)
            {
                box.Transform = TGCMatrix.Scaling(new TGCVector3(e, 0.1f, 0.1f)) *
                            TGCMatrix.Translation(T[i]) * mesh.Transform;
                device.RenderState.AlphaBlendEnable = true;
                box.Render();
            }

        }

        // helper
        public TGCMatrix CalcularMatriz(TGCVector3 Pos, TGCVector3 Scale, TGCVector3 Dir, TGCVector3 VUP)
        {
            var matWorld = TGCMatrix.Scaling(Scale);

            // xxx
            matWorld = matWorld * TGCMatrix.RotationY(-(float)Math.PI / 2.0f) 
                    * TGCMatrix.RotationYawPitchRoll(P.yaw, P.pitch, P.roll);


            // determino la orientacion
            var U = TGCVector3.Cross(VUP, Dir);
            U.Normalize();
            var V = TGCVector3.Cross(Dir, U);
            V.Normalize();
            TGCMatrix Orientacion = new TGCMatrix();
            Orientacion.M11 = U.X;
            Orientacion.M12 = U.Y;
            Orientacion.M13 = U.Z;
            Orientacion.M14 = 0;

            Orientacion.M21 = V.X;
            Orientacion.M22 = V.Y;
            Orientacion.M23 = V.Z;
            Orientacion.M24 = 0;

            Orientacion.M31 = Dir.X;
            Orientacion.M32 = Dir.Y;
            Orientacion.M33 = Dir.Z;
            Orientacion.M34 = 0;

            Orientacion.M41 = 0;
            Orientacion.M42 = 0;
            Orientacion.M43 = 0;
            Orientacion.M44 = 1;
            matWorld = matWorld * Orientacion;

            // traslado
            matWorld = matWorld * TGCMatrix.Translation(Pos);
            return matWorld;
        }


        public TGCMatrix CalcularMatriz2(TGCVector3 Pos, TGCVector3 Scale, TGCVector3 Dir, TGCVector3 VUP)
        {
            var matWorld = TGCMatrix.Scaling(Scale);
            matWorld = matWorld * TGCMatrix.RotationY(-(float)Math.PI / 2.0f)
                    * TGCMatrix.RotationYawPitchRoll(P.yaw, P.pitch, 0);

            // determino la orientacion
            var U = TGCVector3.Cross(VUP, Dir);
            U.Normalize();
            var V = TGCVector3.Cross(Dir, U);
            V.Normalize();
            TGCMatrix Orientacion = new TGCMatrix();
            Orientacion.M11 = U.X;
            Orientacion.M12 = U.Y;
            Orientacion.M13 = U.Z;
            Orientacion.M14 = 0;

            Orientacion.M21 = V.X;
            Orientacion.M22 = V.Y;
            Orientacion.M23 = V.Z;
            Orientacion.M24 = 0;

            Orientacion.M31 = Dir.X;
            Orientacion.M32 = Dir.Y;
            Orientacion.M33 = Dir.Z;
            Orientacion.M34 = 0;

            Orientacion.M41 = 0;
            Orientacion.M42 = 0;
            Orientacion.M43 = 0;
            Orientacion.M44 = 1;
            matWorld = matWorld * Orientacion;

            // traslado
            matWorld = matWorld * TGCMatrix.Translation(Pos);
            return matWorld;
        }

        public TGCVector3 rotar_xz(TGCVector3 v, float an)
        {
            return new TGCVector3((float)(v.X * Math.Cos(an) - v.Z * Math.Sin(an)), v.Y,
                (float)(v.X * Math.Sin(an) + v.Z * Math.Cos(an)));
        }


        public void Dispose()
        {
            mesh.Dispose();
        }


    }


}
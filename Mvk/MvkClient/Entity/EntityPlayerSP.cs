﻿using MvkClient.Renderer;
using MvkClient.Renderer.Chunk;
using MvkClient.Util;
using MvkClient.World;
using MvkServer;
using MvkServer.Entity;
using MvkServer.Glm;
using MvkServer.Network.Packets;
using MvkServer.Util;
using SharpGL;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MvkClient.Entity
{
    /// <summary>
    /// Сущность основного игрока тикущего клиента
    /// </summary>
    public class EntityPlayerSP : EntityPlayerClient
    {
        /// <summary>
        /// Плавное перемещение угла обзора
        /// </summary>
        public SmoothFrame Fov { get; protected set; }
        /// <summary>
        /// Плавное перемещение глаз, сел/встал
        /// </summary>
        public SmoothFrame Eye { get; protected set; }
        /// <summary>
        /// массив матрицы перспективу камеры 3D
        /// </summary>
        public float[] Projection { get; protected set; }
        /// <summary>
        /// массив матрицы расположения камеры в пространстве
        /// </summary>
        public float[] LookAt { get; protected set; }
        
        /// <summary>
        /// Принудительно обработать FrustumCulling
        /// </summary>
        public bool IsFrustumCulling { get; protected set; } = false;
        /// <summary>
        /// Объект расчёта FrustumCulling
        /// </summary>
        public Frustum FrustumCulling { get; protected set; } = new Frustum();
        /// <summary>
        /// Массив чанков которые попадают под FrustumCulling для рендера
        /// </summary>
        public ChunkRender[] ChunkFC { get; protected set; } = new ChunkRender[0];
        /// <summary>
        /// Вид камеры
        /// </summary>
        public EnumViewCamera ViewCamera { get; protected set; } = EnumViewCamera.Eye;

        

        protected vec3 positionFrame;
        protected float pitchFrame;
        protected float yawHeadFrame;
        protected float yawBodyFrame;
        protected float eyeFrame;

        protected uint dListLookAt;
        /// <summary>
        /// массив векторов расположения камеры в пространстве для DisplayList
        /// </summary>
        protected vec3[] lookAtDL;

        public EntityPlayerSP(WorldClient world) : base(world)
        {
            Fov = new SmoothFrame(1.22f);
            Eye = new SmoothFrame(GetEyeHeight());
            interpolation.Start();
        }

        #region DisplayList

        /// <summary>
        /// Обновить матрицу
        /// </summary>
        protected void UpMatrixProjection()
        {
            if (lookAtDL != null && lookAtDL.Length == 3)
            {
                GLRender.ListDelete(dListLookAt);
                dListLookAt = GLRender.ListBegin();
                GLWindow.gl.MatrixMode(OpenGL.GL_PROJECTION);
                GLWindow.gl.LoadIdentity();
                GLWindow.gl.Perspective(glm.degrees(Fov.ValueFrame), (float)GLWindow.WindowWidth / (float)GLWindow.WindowHeight, 0.001f, OverviewChunk * 22.624f * 2f);
                GLWindow.gl.LookAt(lookAtDL[0].x, lookAtDL[0].y, lookAtDL[0].z,
                    lookAtDL[1].x, lookAtDL[1].y, lookAtDL[1].z,
                    lookAtDL[2].x, lookAtDL[2].y, lookAtDL[2].z);
                GLWindow.gl.MatrixMode(OpenGL.GL_MODELVIEW);
                GLWindow.gl.LoadIdentity();
                // Код с фиксированной функцией может использовать альфа-тестирование
                // Чтоб корректно прорисовывался кактус
                GLWindow.gl.AlphaFunc(OpenGL.GL_GREATER, 0.1f);
                GLWindow.gl.Enable(OpenGL.GL_ALPHA_TEST);
                //GLWindow.gl.Enable(OpenGL.GL_TEXTURE_2D);
                //GLWindow.gl.PolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_FILL);
                GLRender.ListEnd();
            }
        }

        /// <summary>
        /// Прорисовать матрицу для DisplayList
        /// </summary>
        public void CameraMatrixProjection() => GLRender.ListCall(dListLookAt);

        #endregion

        /// <summary>
        /// Вызывается для обновления позиции / логики объекта
        /// </summary>
        public override void Update()
        {
            base.Update();
            if (RotationEquals()) IsFrustumCulling = true;
            if (isMotionServer)
            {
                IsFrustumCulling = true;
                isMotionServer = false;
            }
            Eye.Update();
            Fov.Update();
            // Расчёт амплитуды конечностей, при движении
            UpLimbSwing();

            // Скрыть прорисовку себя если вид с глаз
            IsHidden = ViewCamera == EnumViewCamera.Eye;
        }

        /// <summary>
        /// Обновление в каждом тике, если были требования по изминению позицыи, вращения, бег, сидеть и тп.
        /// </summary>
        protected override void UpdateLiving()
        {
            base.UpdateLiving();
            Eye.Set(GetEyeHeight(), 4);
            Fov.Set(IsSprinting ? 1.22f : 1.43f, 4);
            ClientWorld.ClientMain.TrancivePacket(new PacketB20Player().Position(Position, IsSneaking, OnGround));
        }

        /// <summary>
        /// Проверка изменения вращения
        /// </summary>
        /// <returns>True - разные значения, надо InitFrustumCulling</returns>
        public bool RotationEquals()
        {
            if (RotationPitchLast != RotationPitch || RotationYawLast != RotationYawHead
                || RotationYaw != RotationYawPrev)
            {
                //RotationYaw = RotationYawLast;
                //RotationPitch = RotationPitchLast;
                //SetRotation(RotationYawLast, RotationPitchLast);
                SetRotationHead(RotationYawLast, RotationYaw, RotationPitchLast);
                float yawHead = RotationYawHead;
                float yawBody = RotationYaw;
                float pitch = RotationPitch;
                Task.Factory.StartNew(() =>
                {
                    ClientWorld.ClientMain.TrancivePacket(new PacketB20Player().YawPitch(yawHead, yawBody, pitch));
                });
                return true;
            }
            return false;
        }

        public void MouseMove(float deltaX, float deltaY)
        {
            // Чувствительность мыши
            float speedMouse = 1.5f;

            if (deltaX == 0 && deltaY == 0) return;
            float pitch = RotationPitchLast - deltaY / (float)GLWindow.WindowHeight * speedMouse;
            float yaw = RotationYawLast + deltaX / (float)GLWindow.WindowWidth * speedMouse;

            if (pitch < -glm.radians(89.0f)) pitch = -glm.radians(89.0f);
            if (pitch > glm.radians(89.0f)) pitch = glm.radians(89.0f);
            if (yaw > glm.pi) yaw -= glm.pi360;
            if (yaw < -glm.pi) yaw += glm.pi360;

            RotationYawLast = yaw;
            RotationPitchLast = pitch;
        }

        /// <summary>
        /// Обновить матрицу камеры
        /// </summary>
        public bool UpLookAt(float timeIndex)
        {
            vec3 pos = GetPositionEyeFrame(timeIndex);
            vec3 front = GetLookFrame(timeIndex).normalize();

            if (ViewCamera == EnumViewCamera.Back)
            {
                // вид сзади, но надо check на кализию камеры
                pos = GetPositionCamera(pos, front * -1f);
            } else if (ViewCamera == EnumViewCamera.Front)
            {
                // вид сзади, но надо check на кализию камеры
                pos = GetPositionCamera(pos, front);
                front *= -1f;
            }

            vec3 up = new vec3(0, 1, 0);
            if (!IsFlying && MvkGlobal.WIGGLE_EFFECT)
            {
                // Эффект болтания когда игрок движется 
                vec3 right = glm.cross(up, front).normalize();
                float limbS = GetLimbSwingAmountFrame(timeIndex) * .12f;
                // эффект лево-право
                float limb = glm.cos(LimbSwing * 0.3331f) * limbS;
                pos += right * limb;
                up = glm.cross(front, right);
                // эффект вверх-вниз
                limb = glm.cos(LimbSwing * 0.6662f) * limbS;
                pos += up * limb;
            }
            float[] lookAt = glm.lookAt(pos, pos + front, up).to_array();
            if (!Mth.EqualsArrayFloat(lookAt, LookAt, 0.00001f))
            {
                LookAt = lookAt;
                lookAtDL = new vec3[] { pos, pos + front, up };
                UpMatrixProjection();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Обновить перспективу камеры
        /// </summary>
        public void UpProjection()
        {
            Projection = glm.perspective(Fov.ValueFrame, (float)GLWindow.WindowWidth / (float)GLWindow.WindowHeight, 0.001f, OverviewChunk * 22.624f * 2f).to_array();
            UpMatrixProjection();
        }

        /// <summary>
        /// Обновление в кадре
        /// </summary>
        public void UpdateFrame(float timeIndex)
        {
            // Меняем угол обзора (как правило при изменении скорости)
            if (Fov.UpdateFrame(timeIndex)) UpProjection();
            // Меняем положения глаз
            Eye.UpdateFrame(timeIndex);

            eyeFrame = Eye.ValueFrame;
            positionFrame = base.GetPositionFrame(timeIndex);
            yawBodyFrame = GetRotationYawBodyFrame(timeIndex);
            yawHeadFrame = GetRotationYawFrame(timeIndex);
            pitchFrame = GetRotationPitchFrame(timeIndex);


            // Изменяем матрицу глаз игрока
            if (UpLookAt(timeIndex))// || IsFrustumCulling)
            {
                // Если имеется вращение камеры или было перемещение, то запускаем расчёт FrustumCulling
                //if (IsFrustumCulling)
                InitFrustumCulling();
            }
        }

        /// <summary>
        /// Требуется перерасчёт FrustumCulling
        /// </summary>
        public void UpFrustumCulling() => IsFrustumCulling = true;

        //public void ClearFrustumCulling() => ChunkFC = new ChunkRender[0];
        /// <summary>
        /// Перерасчёт FrustumCulling
        /// </summary>
        public void InitFrustumCulling()
        {
            if (LookAt == null || Projection == null) return;

            FrustumCulling.Init(LookAt, Projection);

            int countAll = 0;
            int countFC = 0;
            vec2i chunkPos = GetChunkPos();
            List<ChunkRender> listC = new List<ChunkRender>();

            for (int i = 0; i < DistSqrt.Length; i++)
            {
                int xc = DistSqrt[i].x + chunkPos.x;
                int zc = DistSqrt[i].y + chunkPos.y;
                int xb = xc << 4;
                int zb = zc << 4;

                if (FrustumCulling.IsBoxInFrustum(xb, 0, zb, xb + 15, 255, zb + 15))
                {
                    countFC++;
                    listC.Add(ClientWorld.ChunkPrClient.GetChunkRender(new vec2i(xc, zc), false));
                }
                countAll++;
            }
            ChunkFC = listC.ToArray();
            IsFrustumCulling = false;
        }

        /// <summary>
        /// Следующий вид камеры
        /// </summary>
        public void ViewCameraNext()
        {
            int count = Enum.GetValues(typeof(EnumViewCamera)).Length - 1;
            int value = (int)ViewCamera;
            value++;
            if (value > count) value = 0;
            ViewCamera = (EnumViewCamera)value;
        }

        /// <summary>
        /// Определить положение камеры, при виде сзади и спереди, проверка RayCast
        /// </summary>
        /// <param name="pos">позиция глаз</param>
        /// <param name="vec">направляющий вектор к расположению камеры</param>
        protected vec3 GetPositionCamera(vec3 pos, vec3 vec)
        {
            MovingObjectPosition moving = World.RayCast(pos, vec, MvkGlobal.CAMERA_DIST);
            return pos + vec * (moving.IsBlock() ? glm.distance(pos, new vec3(moving.Put) + new vec3(.5f)) : MvkGlobal.CAMERA_DIST);
        }

        #region Frame

        /// <summary>
        /// Высота глаз для кадра
        /// </summary>
        public override float GetEyeHeightFrame() => eyeFrame;

        /// <summary>
        /// Получить позицию сущности для кадра
        /// </summary>
        /// <param name="timeIndex">коэфициент между тактами</param>
        public override vec3 GetPositionFrame(float timeIndex) => positionFrame;

        /// <summary>
        /// Получить вектор направления камеры тела
        /// </summary>
        /// <param name="timeIndex">Коэфициент между тактами</param>
        public override vec3 GetLookBodyFrame(float timeIndex) => GetRay(yawBodyFrame, pitchFrame);

        /// <summary>
        /// Получить вектор направления камеры от головы
        /// </summary>
        /// <param name="timeIndex">Коэфициент между тактами</param>
        public override vec3 GetLookFrame(float timeIndex) => GetRay(yawHeadFrame, pitchFrame);

        #endregion
    }
}

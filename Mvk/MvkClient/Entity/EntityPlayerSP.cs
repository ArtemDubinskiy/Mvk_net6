﻿using MvkClient.Renderer;
using MvkClient.Renderer.Chunk;
using MvkClient.Util;
using MvkClient.World;
using MvkServer;
using MvkServer.Entity;
using MvkServer.Glm;
using MvkServer.Management;
using MvkServer.Network.Packets.Client;
using MvkServer.Util;
using MvkServer.World.Block;
using SharpGL;
using System;
using System.Collections.Generic;

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
        /// Вектор луча
        /// </summary>
        public vec3 RayLook { get; protected set; }

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
        public FrustumStruct[] ChunkFC { get; protected set; } = new FrustumStruct[0];
        /// <summary>
        /// Список сущностей которые попали в луч
        /// </summary>
        public EntityPlayerMP[] EntitiesLook { get; protected set; } = new EntityPlayerMP[0];
        /// <summary>
        /// Вид камеры
        /// </summary>
        public EnumViewCamera ViewCamera { get; protected set; } = EnumViewCamera.Eye;
        /// <summary>
        /// Последнее значение поворота вокруг своей оси
        /// </summary>
        public float RotationYawLast { get; protected set; }
        /// <summary>
        /// Последнее значение поворота вверх вниз
        /// </summary>
        public float RotationPitchLast { get; protected set; }
        /// <summary>
        /// Выбранный блок
        /// </summary>
        public BlockBase SelectBlock { get; protected set; } = null;

        /// <summary>
        /// Позиция с учётом итерполяции кадра
        /// </summary>
        private vec3 positionFrame;
        /// <summary>
        /// Поворот Pitch с учётом итерполяции кадра
        /// </summary>
        private float pitchFrame;
        /// <summary>
        /// Поворот Yaw головы с учётом итерполяции кадра
        /// </summary>
        private float yawHeadFrame;
        /// <summary>
        /// Поворот Yaq тела с учётом итерполяции кадра
        /// </summary>
        private float yawBodyFrame;
        /// <summary>
        /// Высота глаз с учётом итерполяции кадра
        /// </summary>
        private float eyeFrame;

        /// <summary>
        /// Лист DisplayList матрицы 
        /// </summary>
        private uint dListLookAt;
        /// <summary>
        /// массив векторов расположения камеры в пространстве для DisplayList
        /// </summary>
        private vec3[] lookAtDL;

        /// <summary>
        /// Счётчик паузы анимации левого удара, такты
        /// </summary>
        private int leftClickPauseCounter = 0;
        /// <summary>
        /// Холостой удар
        /// </summary>
        private bool blankShot = false;
        /// <summary>
        /// Активна ли рука в действии
        /// 0 - не активна, 1 - активна левая, 2 - активна правая
        /// </summary>
        private ActionHand handAction = ActionHand.None;
        /// <summary>
        /// Объект работы над игроком разрушения блока, итомы и прочее
        /// </summary>
        private ItemInWorldManager itemInWorldManager;
        /// <summary>
        /// Сущность по которой ударил игрок, если null то нет атаки
        /// </summary>
        private EntityBase entityAtack;

        public EntityPlayerSP(WorldClient world) : base(world)
        {
            Fov = new SmoothFrame(1.43f);
            Eye = new SmoothFrame(GetEyeHeight());
            itemInWorldManager = new ItemInWorldManager(world, this);
        }

        /// <summary>
        /// Возвращает true, если эта вещь названа
        /// </summary>
        public override bool HasCustomName() => false;

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
        /// Надо ли обрабатывать LivingUpdate, для мобов на сервере, и игроки у себя
        /// </summary>
        protected override bool IsLivingUpdate() => true;

        /// <summary>
        /// Вызывается для обновления позиции / логики объекта
        /// </summary>
        public override void Update()
        {
            // Такты изминения глаз при присидании, и угол обзора при ускорении. Должны быть до base.Update()
            Eye.Update();
            Fov.Update();
            LastTickPos = PositionPrev = Position;
            RotationPitchPrev = RotationPitch;
            RotationYawPrev = RotationYaw;
            RotationYawHeadPrev = RotationYawHead;

            base.Update();

            // Обновление курсоро, не зависимо от действия игрока, так как рядом может быть изминение
            UpCursor();

            if (RotationEquals())
            {
                IsFrustumCulling = true;
            }
            if (ActionChanged != EnumActionChanged.None)
            {
                IsFrustumCulling = true;
                UpdateActionPacket();
            }

            // счётчик паузы для анимации удара
            leftClickPauseCounter++;

            // обновляем счётчик удара
            ItemInWorldManager.StatusAnimation statusUpdate = itemInWorldManager.UpdateBlock();
            if (statusUpdate == ItemInWorldManager.StatusAnimation.Animation)
            {
                leftClickPauseCounter = 100;
            }
            else if (statusUpdate == ItemInWorldManager.StatusAnimation.NotAnimation)
            {
                leftClickPauseCounter = 0;
            }

            if (!AtackUpdate())
            {
                // Если нет атаки то проверяем установку или разрушение блока
                if (handAction != ActionHand.None)
                {
                    HandActionUpdate();
                    if (leftClickPauseCounter > 5 || blankShot)
                    {
                        SwingItem();
                        blankShot = false;
                        leftClickPauseCounter = 0;
                    }
                }
            }

            // Скрыть прорисовку себя если вид с глаз
            Type = Health > 0 && ViewCamera == EnumViewCamera.Eye ? EnumEntities.PlayerHand : EnumEntities.Player;
            //Type = EnumEntities.PlayerHand;
        }

        /// <summary>
        /// Удар рукой по сущности в такте игры
        /// </summary>
        private bool AtackUpdate()
        {
            if (entityAtack != null)
            {
                if (!entityAtack.IsDead)
                {
                    SwingItem();

                    vec3 pos = entityAtack.Position + new vec3(.5f);
                    for (int i = 0; i < 5; i++)
                    {
                        ClientWorld.SpawnParticle(EnumParticle.Test, pos + new vec3((rand.Next(16) - 8) / 16f, (rand.Next(12) - 6) / 16f, (rand.Next(16) - 8) / 16f), new vec3(0));
                    }
                    ClientWorld.ClientMain.TrancivePacket(new PacketC03UseEntity(entityAtack.Id, entityAtack.Position - Position));
                    entityAtack = null;
                    return true;
                }
                entityAtack = null;
            }
            return false;
        }

        /// <summary>
        /// Размахивает предметом, который держит игрок
        /// </summary>
        public override void SwingItem()
        {
            base.SwingItem();
            ClientWorld.ClientMain.TrancivePacket(new PacketC0AAnimation());
        }

        /// <summary>
        /// Обновление в каждом тике, если были требования по изминению позицыи, вращения, бег, сидеть и тп.
        /// </summary>
        private void UpdateActionPacket()
        {
            bool isSS = false;
            if (ActionChanged.HasFlag(EnumActionChanged.IsSneaking))
            {
                Eye.Set(GetEyeHeight(), 4);
                isSS = true;
            }
            if (ActionChanged.HasFlag(EnumActionChanged.IsSprinting))
            {
                Fov.Set(IsSprinting ? 1.22f : 1.43f, 4);
                isSS = true;
            }

            bool isPos = ActionChanged.HasFlag(EnumActionChanged.Position);
            bool isLook = ActionChanged.HasFlag(EnumActionChanged.Look);

            if (isPos && isLook)
            {
                ClientWorld.ClientMain.TrancivePacket(new PacketC06PlayerPosLook(
                    Position, RotationYawHead, RotationPitch, IsSneaking, IsSprinting
                ));
            }
            else if (isLook)
            {
                ClientWorld.ClientMain.TrancivePacket(new PacketC05PlayerLook(RotationYawHead, RotationPitch, IsSneaking));
            }
            else if (isPos || isSS)
            {
                ClientWorld.ClientMain.TrancivePacket(new PacketC04PlayerPosition(Position, IsSneaking, IsSprinting));
            }
            ActionNone();
        }

        /// <summary>
        /// Проверка изменения вращения
        /// </summary>
        /// <returns>True - разные значения, надо InitFrustumCulling</returns>
        public bool RotationEquals()
        {
            if (RotationPitchLast != RotationPitch || RotationYawLast != RotationYawHead || RotationYaw != RotationYawPrev)
            {
                SetRotationHead(RotationYawLast, RotationPitchLast);
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

            UpCursor();
        }

        /// <summary>
        /// Обновить курсор
        /// </summary>
        private void UpCursor()
        {
            MovingObjectPosition moving = RayCast(10f);
            SelectBlock = moving.Block;
            Debug.DStr = moving.ToString();
        }

        /// <summary>
        /// Обновить матрицу камеры
        /// </summary>
        public bool UpLookAt(float timeIndex)
        {
            vec3 pos = new vec3(0, GetEyeHeightFrame() + GetPositionFrame(timeIndex).y, 0);
            vec3 front = GetLookFrame(timeIndex).normalize();
            vec3 up = new vec3(0, 1, 0);

            if (ViewCamera == EnumViewCamera.Back)
            {
                // вид сзади
                pos = GetPositionCamera(pos, front * -1f, MvkGlobal.CAMERA_DIST);
            } else if (ViewCamera == EnumViewCamera.Front)
            {
                // вид спереди
                pos = GetPositionCamera(pos, front, MvkGlobal.CAMERA_DIST);
                front *= -1f;
            } else
            {
                //vec3 right = glm.cross(up, front).normalize();
                //vec3 f2 = (front * -1f - right * .7f).normalize();
                //pos = GetPositionCamera(pos, f2, 2f);
            }

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
                RayLook = front;
                lookAtDL = new vec3[] { pos, pos + front, up };
                UpMatrixProjection();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Обновить перспективу камеры
        /// </summary>
        public override void UpProjection()
        {
            Projection = glm.perspective(Fov.ValueFrame, (float)GLWindow.WindowWidth / (float)GLWindow.WindowHeight, 0.001f, OverviewChunk * 22.624f * 2f).to_array();
            UpMatrixProjection();
        }

        /// <summary>
        /// Обновление в кадре
        /// </summary>
        public void UpdateFrame(float timeIndex)
        {
            // Меняем положения глаз
            Eye.UpdateFrame(timeIndex);
            eyeFrame = Eye.ValueFrame;

            positionFrame = base.GetPositionFrame(timeIndex);
            yawBodyFrame = GetRotationYawBodyFrame(timeIndex);
            yawHeadFrame = GetRotationYawFrame(timeIndex);
            pitchFrame = GetRotationPitchFrame(timeIndex);

            // Меняем угол обзора (как правило при изменении скорости)
            if (Fov.UpdateFrame(timeIndex)) { }
            UpProjection();

            ClientWorld.RenderEntityManager.SetCamera(positionFrame, yawHeadFrame, pitchFrame);

            // Изменяем матрицу глаз игрока
            if (UpLookAt(timeIndex) || IsFrustumCulling)
            {
                // Если имеется вращение камеры или было перемещение, то запускаем расчёт FrustumCulling
                InitFrustumCulling();
            }
        }

        /// <summary>
        /// Требуется перерасчёт FrustumCulling
        /// </summary>
        public void UpFrustumCulling() => IsFrustumCulling = true;

        /// <summary>
        /// Перерасчёт FrustumCulling
        /// </summary>
        public void InitFrustumCulling()
        {
            if (LookAt == null || Projection == null) return;

            FrustumCulling.Init(LookAt, Projection);

            int countAll = 0;
            int countFC = 0;
            vec2i chunkPos = new vec2i(Mth.Floor(positionFrame.x) >> 4, Mth.Floor(positionFrame.z) >> 4);
            List<FrustumStruct> listC = new List<FrustumStruct>();

            if (DistSqrt != null)
            {
                for (int i = 0; i < DistSqrt.Length; i++)
                {
                    int xc = DistSqrt[i].x;
                    int zc = DistSqrt[i].y;
                    int xb = xc << 4;
                    int zb = zc << 4;

                    if (FrustumCulling.IsBoxInFrustum(xb - 15, 0, zb - 15, xb + 15, 255, zb + 15))
                    {
                        countFC++;
                        vec2i coord = new vec2i(xc + chunkPos.x, zc + chunkPos.y);
                        ChunkRender chunk = ClientWorld.ChunkPrClient.GetChunkRender(coord);
                        if (chunk == null) listC.Add(new FrustumStruct(coord));
                        else listC.Add(new FrustumStruct(chunk));
                        //listC.Add(ClientWorld.ChunkPrClient.GetChunkRender(new vec2i(xc + chunkPos.x, zc + chunkPos.y)));
                    }
                    countAll++;
                }
            }
            ChunkFC = listC.ToArray();
            IsFrustumCulling = false;
        }

        /// <summary>
        /// Проверить не догруженые чанки и догрузить если надо
        /// </summary>
        public void CheckChunkFrustumCulling()
        {
            for (int i = 0; i < ChunkFC.Length; i++)
            {
                FrustumStruct fs = ChunkFC[i];
                if (!fs.IsChunk())
                {
                    ChunkRender chunk = ClientWorld.ChunkPrClient.GetChunkRender(fs.GetCoord());
                    if (chunk != null) ChunkFC[i] = new FrustumStruct(chunk);
                }
            }
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
        /// Занести массив сущностей попадающих в луч
        /// </summary>
        public void SetEntitiesLook(List<EntityPlayerMP> entities) => EntitiesLook = entities.ToArray();

        /// <summary>
        /// Определить положение камеры, при виде сзади и спереди, проверка RayCast
        /// </summary>
        /// <param name="pos">позиция глаз</param>
        /// <param name="vec">направляющий вектор к расположению камеры</param>
        private vec3 GetPositionCamera(vec3 pos, vec3 vec, float dis)
        {
            vec3 offset = ClientWorld.RenderEntityManager.CameraOffset;
            MovingObjectPosition moving = World.RayCastBlock(pos + offset, vec, dis);
            return pos + vec * (moving.IsBlock() ? glm.distance(pos, moving.RayHit + new vec3(moving.Norm) * .5f - offset) : dis);
        }

        /// <summary>
        /// Получить объект по тикущему лучу
        /// </summary>
        /// <param name="maxDis">дистанция луча</param>
        protected MovingObjectPosition RayCast(float maxDis)
        {
            vec3 pos = GetPositionFrame();
            pos.y += GetEyeHeightFrame();
            vec3 dir = RayLook;
            vec3 offset = ClientWorld.RenderEntityManager.CameraOffset;
            MovingObjectPosition movingObjectBlock = World.RayCastBlock(pos, dir, maxDis);

            MapListEntity listEntity = World.GetEntitiesWithinAABBExcludingEntity(this, BoundingBox.AddCoordBias(dir * maxDis));
            float entityDis = movingObjectBlock.IsBlock() ? glm.distance(pos, movingObjectBlock.RayHit) : maxDis;
            EntityBase pointedEntity = null;
            float step = .5f;

            while(listEntity.Count > 0)
            {
                EntityBase entity = listEntity.FirstRemove();

                if (entity.CanBeCollidedWith())
                {
                    float size = entity.GetCollisionBorderSize();
                    AxisAlignedBB aabb = entity.BoundingBox.Expand(new vec3(size));
                    for (float addStep = 0; addStep <= entityDis; addStep += step)
                    {
                        if (aabb.IsVecInside(pos + dir * addStep))
                        {
                            pointedEntity = entity;
                            entityDis = addStep;
                            break;
                        }
                    }
                }
            }
            return pointedEntity != null ? new MovingObjectPosition(pointedEntity) : movingObjectBlock;
        }

        /// <summary>
        /// Действие правой рукой
        /// </summary>
        private void HandActionUpdate()
        {
            MovingObjectPosition moving = RayCast(10f);

            if (handAction == ActionHand.Left)
            {
                // Разрушаем блок
                if (itemInWorldManager.IsDestroyingBlock && ((moving.IsBlock() && !itemInWorldManager.BlockPosDestroy.Position.Equals(moving.Block.Position.Position)) || !moving.IsBlock()))
                {
                    // Отмена разбитие блока, сменился блок
                    ClientMain.TrancivePacket(new PacketC07PlayerDigging(itemInWorldManager.BlockPosDestroy, PacketC07PlayerDigging.EnumDigging.About));
                    itemInWorldManager.DestroyAbout();
                }
                else if (itemInWorldManager.IsDestroyingBlock)
                {
                    // Этап разбития блока
                    if (itemInWorldManager.IsDestroy())
                    {
                        // Стоп, окончено разбитие
                        ClientMain.TrancivePacket(new PacketC07PlayerDigging(itemInWorldManager.BlockPosDestroy, PacketC07PlayerDigging.EnumDigging.Stop));
                        itemInWorldManager.DestroyStop();
                    }
                }
                else if (itemInWorldManager.NotPauseUpdate)
                {
                    DestroyingBlockStart(moving, false);
                }
            }
            else if (handAction == ActionHand.Right && itemInWorldManager.NotPauseUpdate)
            {
                // устанавливаем блок
                PutBlockStart(moving, false);
            }
        }

        /// <summary>
        /// Действие правой рукой
        /// </summary>
        public void HandAction()
        {
            MovingObjectPosition moving = RayCast(10f);
            if (moving.IsEntity())
            {
                // Курсор попал на сущность
                entityAtack = moving.Entity;
                handAction = ActionHand.None;
            }
            else
            {
                DestroyingBlockStart(moving, true);
                handAction = ActionHand.Left;
            }
        }

        /// <summary>
        /// Действие правй рукой, ставим блок
        /// </summary>
        public void HandActionTwo()
        {
            MovingObjectPosition moving = RayCast(10f);
            PutBlockStart(moving, true);
        }

        private void PutBlockStart(MovingObjectPosition moving, bool start)
        {
            if (moving.IsBlock() && slot != 0)
            {
                BlockPos blockPos = new BlockPos(moving.Put);
                vec3 facing = moving.RayHit - new vec3(moving.Put);
                // устанавливаем блок
                BlockBase blockNew = Blocks.GetBlock((EnumBlock)slot, blockPos);
                bool putAbout = true;
                if (blockNew != null && !blockNew.IsAir)
                {
                    AxisAlignedBB axisBlock = blockNew.GetCollision();
                    // Проверка коллизии игрока и блока
                    if (!BoundingBox.IntersectsWith(axisBlock) && World.GetEntitiesWithinAABBExcludingEntity(this, axisBlock).Count == 0)
                    {
                        ClientMain.TrancivePacket(new PacketC08PlayerBlockPlacement(blockPos, facing));
                        itemInWorldManager.Put(blockPos, facing, blockNew.EBlock);
                        itemInWorldManager.PutPause(start);
                        putAbout = false;
                    }
                }
                if (putAbout) itemInWorldManager.PutAbout();
                handAction = ActionHand.Right;
            }
        }


        /// <summary>
        /// Начало разрушения блока
        /// </summary>
        private void DestroyingBlockStart(MovingObjectPosition moving, bool start)
        {
            if (!itemInWorldManager.IsDestroyingBlock && moving.IsBlock())
            {
                // Начало разбитие блока
                ClientWorld.ClientMain.TrancivePacket(new PacketC07PlayerDigging(moving.Block.Position, PacketC07PlayerDigging.EnumDigging.Start));
                itemInWorldManager.DestroyStart(moving.Block.Position);
            }
            else if (start)
            {
                blankShot = true;
            }
        }


        /// <summary>
        /// Отмена действия правой рукой
        /// </summary>
        public void UndoHandAction()
        {
            if (itemInWorldManager.IsDestroyingBlock)
            {
                ClientMain.TrancivePacket(new PacketC07PlayerDigging(itemInWorldManager.BlockPosDestroy, PacketC07PlayerDigging.EnumDigging.About));
                itemInWorldManager.DestroyAbout();
            }
            handAction = ActionHand.None;
        }

        /// <summary>
        /// Нет нажатий
        /// </summary>
        public override void InputNone()
        {
            base.InputNone();
            UndoHandAction();
        }

        public override void Respawn()
        {
            base.Respawn();
            ClientWorld.ClientMain.GameMode();
        }

        /// <summary>
        /// Падение
        /// </summary>
        protected override void Fall()
        {
            if (fallDistanceResult > 0)
            {
                ClientWorld.ClientMain.TrancivePacket(new PacketC0CPlayerAction(PacketC0CPlayerAction.EnumAction.Fall, fallDistanceResult));
                ParticleFall(fallDistanceResult);
                fallDistanceResult = 0;
            }
        }

        /// <summary>
        /// Задать место положение игрока, при спавне, телепорте и тп
        /// </summary>
        public override void SetPosLook(vec3 pos, float yaw, float pitch)
        {
            base.SetPosLook(pos, yaw, pitch);
            RotationYawLast = RotationYaw;
            RotationPitchLast = RotationPitch;
        }

        /// <summary>
        /// Задать обзор чанков у клиента
        /// </summary>
        public override void SetOverviewChunk(int overviewChunk)
        {
            OverviewChunkPrev = OverviewChunk = overviewChunk;
            DistSqrt = MvkStatic.GetSqrt(OverviewChunk);
        }

        #region Frame

        /// <summary>
        /// Высота глаз для кадра
        /// </summary>
        public override float GetEyeHeightFrame() => eyeFrame;

        /// <summary>
        /// Получить позицию сущности для кадра
        /// </summary>
        public vec3 GetPositionFrame() => positionFrame;

        /// <summary>
        /// Получить вектор направления камеры тела для кадра
        /// </summary>
        /// <param name="timeIndex">Коэфициент между тактами</param>
        public override vec3 GetLookBodyFrame(float timeIndex) => GetRay(yawBodyFrame, pitchFrame);

        /// <summary>
        /// Получить вектор направления камеры от головы для кадра
        /// </summary>
        /// <param name="timeIndex">Коэфициент между тактами</param>
        public override vec3 GetLookFrame(float timeIndex) => GetRay(yawHeadFrame, pitchFrame);

        #endregion


        /// <summary>
        /// Действие рук
        /// </summary>
        private enum ActionHand
        {
            /// <summary>
            /// Нет действий
            /// </summary>
            None,
            /// <summary>
            /// Левой рукой
            /// </summary>
            Left,
            /// <summary>
            /// Правой рукой
            /// </summary>
            Right
        }
    }
}

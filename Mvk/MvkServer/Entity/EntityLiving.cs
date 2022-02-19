﻿using MvkServer.Entity.Mob;
using MvkServer.Entity.Player;
using MvkServer.Glm;
using MvkServer.Network.Packets;
using MvkServer.Network.Packets.Server;
using MvkServer.Util;
using MvkServer.World;
using System.Collections.Generic;

namespace MvkServer.Entity
{
    /// <summary>
    /// Объект жизни сущьности, отвечает за движение вращение и прочее
    /// </summary>
    public abstract class EntityLiving : EntityBase
    {
        /// <summary>
        /// Объект ввода кликов клавиатуры
        /// </summary>
        public EnumInput Input { get; protected set; } = EnumInput.None;
        /// <summary>
        /// Счётчик движения. Влияет на то, где в данный момент находятся ноги и руки при качании. 
        /// </summary>
        public float LimbSwing { get; protected set; } = 0;
        /// <summary>
        /// Движение из-за смещения
        /// </summary>
        public vec3 MotionPush { get; set; } = new vec3(0);

        

        /// <summary>
        /// Нужна ли амплитуда конечностей
        /// </summary>
        protected bool isLimbSwing = true;
        /// <summary>
        /// Скорость движения. Влияет на то, где в данный момент находятся ноги и руки при качании. 
        /// </summary>
        private float limbSwingAmount = 0;
        /// <summary>
        /// Скорость движения на предыдущем тике. Влияет на то, где в данный момент находятся ноги и руки при качании. 
        /// </summary>
        private float limbSwingAmountPrev = 0;
        /// <summary>
        /// Анимация движения руки 0..1
        /// </summary>
        public float swingProgress = 0;
        /// <summary>
        /// Анимация движения руки предыдущего такта
        /// </summary>
        private float swingProgressPrev = 0;
        /// <summary>
        /// Счётчик тактов для анимации руки 
        /// </summary>
        private int swingProgressInt = 0;
        /// <summary>
        /// Запущен ли счётчик анимации руки
        /// </summary>
        private bool isSwingInProgress = false;
        
        /// <summary>
        /// Количество тактов для запрета повторного прыжка
        /// </summary>
        private int jumpTicks = 0;
        /// <summary>
        /// Дистанция падения
        /// </summary>
        private float fallDistance = 0;
        /// <summary>
        /// Результат падения, отладка!!!
        /// </summary>
        protected float fallDistanceResult = 0;

        protected vec3 motionDebug = new vec3(0);

        /// <summary>
        /// Объект времени c последнего тпс
        /// </summary>
      //  private InterpolationTime interpolation = new InterpolationTime();

        public EntityLiving(WorldBase world) : base(world)
        {
            //interpolation.Start();
        }


        #region Input

        /// <summary>
        /// Нет нажатий
        /// </summary>
        public void InputNone() => Input = EnumInput.None;
        /// <summary>
        /// Добавить нажатие
        /// </summary>
        public void InputAdd(EnumInput input) => Input |= input;
        /// <summary>
        /// Убрать нажатие
        /// </summary>
        public void InputRemove(EnumInput input)
        {
            if (Input.HasFlag(input)) Input ^= input;
        }

        #endregion

        /// <summary>
        /// Надо ли обрабатывать LivingUpdate, для мобов на сервере, и игроки у себя
        /// </summary>
        protected virtual bool IsLivingUpdate()
        {
            bool server = World is WorldServer;
            bool player = this is EntityChicken;
            return server && player;
        }

        public override void Update()
        {
            base.Update();
            
            //vec3 motionPrev = Motion;

            EntityUpdate();

            // Если 
            if (!IsDead && IsLivingUpdate())
            {
                LivingUpdate();

                // Пометка что было какое-то движение, вращения, бег, сидеть и тп.
                // Параметр стартового падения между -0.16 и  -0.15
                if (Motion.x != 0 || Motion.z != 0 || Motion.y > -.15f || Motion.y < -.16f || !OnGround)
                {
                    UpdateIsMotion();
                    //isMotionServer = true;
                }
                
            }

            // Расчёт амплитуды движения
            UpLimbSwing();

            //// Расчёт амплитуды конечностей, при движении
            //UpLimbSwing();
            //// Просчёт взмаха руки
            //UpdateArmSwingProgress();
        }

        protected void EntityUpdate()
        {
            // метод определения если есть ускорение и мы не на воде, определяем по нижниму блоку какой спавн частиц и спавним их
            // ... func_174830_Y

            // метод проверки нахождения по кализии в воде ли мы, и меняем статус IsWater
            // ... handleWaterMovement

            // определяем горим ли мы, и раз в секунду %20, наносим урон
            // ...

            // определяем в лаве ли мы по кализии
            // ... func_180799_ab

            // если мы ниже -64 по Y убиваем игрока
            if (Position.y > 128 || Position.y < -64) Kill();

            // если нет хп обновлям смертельную картинку
            if (Health <= 0f) DeathUpdate();

            // Счётчик получения урона для анимации
            if (DamageTime > 0) DamageTime--;

            // Если был толчёк, мы его дабавляем и обнуляем
            if (!MotionPush.Equals(new vec3(0)))
            {
                vec3 motionPush = MotionPush;
                MotionPush = new vec3(0);
                // Защита от дабл прыжка, и если сущность летает, нет броска
                if (Motion.y > 0 || IsFlying) motionPush.y = 0;
                Motion += motionPush;
            }
        }

        /// <summary>
        /// Обновляет активное действие, и возращает strafe, forward, vertical через vec3
        /// </summary>
        /// <returns>strafe, forward</returns>
        protected vec2 UpdateEntityActionState()
        {
            float strafe = 0f;
            float forward = 0f;

            if (IsFlying)
            {
                float vertical = (Input.HasFlag(EnumInput.Up) ? 1f : 0f) - (Input.HasFlag(EnumInput.Down) ? 1f : 0f);
                float y = Motion.y;
                y += vertical * Speed.Vertical;
                Motion = new vec3(Motion.x, y, Motion.z);
                IsJumping = false;
            }

            // Прыжок, только выживание
            else if (IsJumping)
            {
                // для воды свои правила, плыть вверх
                //... updateAITick
                // для лавы свои
                //... func_180466_bG
                // Для прыжка надо стоять на земле, и чтоб счётик прыжка был = 0
                if (OnGround && jumpTicks == 0)
                {
                    Jump();
                    jumpTicks = 10;
                }
            }
            else
            {
                jumpTicks = 0;
            }

            strafe = (Input.HasFlag(EnumInput.Right) ? 1f : 0) - (Input.HasFlag(EnumInput.Left) ? 1f : 0);
            forward = (Input.HasFlag(EnumInput.Back) ? 1f : 0f) - (Input.HasFlag(EnumInput.Forward) ? 1f : 0f);

            // Обновить положение сидя
            if (!IsFlying && OnGround && Input.HasFlag(EnumInput.Down) && !IsSneaking)
            {
                // Только в выживании можно сесть
                IsSneaking = true;
                Sitting();
            }
            // Если хотим встать
            if (!Input.HasFlag(EnumInput.Down) && IsSneaking)
            {
                // Проверка коллизии вверхней части при положении стоя
                Standing();
                // TODO:: хочется как-то ловить колизию положение встать в MoveCheckCollision
                if (NoClip || !World.Collision.IsCollisionBody(this, new vec3(Position)))
                {
                    IsSneaking = false;
                }
                else
                {
                    Sitting();
                }
            }

            // Sprinting
            bool isSprinting = Input.HasFlag(EnumInput.Sprinting | EnumInput.Forward) && !IsSneaking;
            if (IsSprinting != isSprinting)
            {
                IsSprinting = isSprinting;
            }

            // Jumping
            IsJumping = Input.HasFlag(EnumInput.Up);

            return new vec2(strafe, forward);
        }

        /// <summary>
        /// Метод отвечает за жизнь сущности, точнее её управление, перемещения, мобы Ai
        /// должен работать у клиента для EntityPlayerSP и на сервере для мобов
        /// так же может работать у клиента для всех сущностей эффектов вне сервера.
        /// </summary>
        protected void LivingUpdate()
        {
            // счётчик прыжка
            if (jumpTicks > 0) jumpTicks--;

            // Продумать перемещение по тактам, с параметром newPosRotationIncrements

            // Если нет перемещения по тактам, запускаем трение воздуха
            Motion = new vec3(Motion.x * .98f, Motion.y, Motion.z * .98f);

            // Если мелочь убираем
            Motion = new vec3(
                Mth.Abs(Motion.x) < 0.005f ? 0 : Motion.x,
                Mth.Abs(Motion.y) < 0.005f ? 0 : Motion.y,
                Mth.Abs(Motion.z) < 0.005f ? 0 : Motion.z
            );

            float strafe = 0f;
            float forward = 0f;

            if (!IsMovementBlocked())
            {
                // Если нет блокировки
                vec2 sf = UpdateEntityActionState();
                strafe = sf.x;
                forward = sf.y;
            }

           // if (Health <= 0) InputNone();

            // Тут правильнее сделать метод updateEntityActionState 
            // где SP присваивает strafe и forward
            // или сервер Ai для мобов

            if (IsFlying)
            {
                float y = Motion.y;
                MoveWithHeading(strafe, forward, .6f * Speed.Forward * (IsSprinting ? Speed.Sprinting : 1f));
                Motion = new vec3(Motion.x, y * .6f, Motion.z);
            }
            else
            {
                MoveWithHeading(strafe, forward, .04f);
            }
        }

        /// <summary>
        /// Проверка колизии по вектору движения
        /// </summary>
        /// <param name="motion">вектор движения</param>
        public void UpMoveCheckCollision(vec3 motion)
        {
            MoveEntity(motion);
        }

        /// <summary>
        /// Мертвые и спящие существа не могут двигаться
        /// </summary>
        protected bool IsMovementBlocked() => Health <= 0f;

        /// <summary>
        /// Обновление в каждом тике, если были требования по изминению позицыи, вращения, бег, сидеть и тп.
        /// </summary>
        protected virtual void UpdateIsMotion() { }

        /// <summary>
        /// Поворот тела от движения и поворота головы 
        /// </summary>
        protected virtual void HeadTurn() { }

        /// <summary>
        /// Конвертация от направления движения в XYZ координаты с кооректировками скоростей
        /// </summary>
        protected void MoveWithHeading(float strafe, float forward, float jumpMovementFactor)
        {
            vec3 motion = new vec3();

            // делим на три части, вода, лава, остальное

            // расматриваем остальное
            {
                // Коэффициент трения блока
                //float study = .954f;// 0.91f; // для воздух
                //if (OnGround) study = 0.739F;// 0.546f; // трение блока, определить на каком блоке стоим (.6f блок) * .91f
                float study = 0.91f; // для воздух
                if (OnGround) study = 0.546f; // трение блока, определить на каком блоке стоим (.6f блок) * .91f

                //float param = 0.403583419f / (study * study * study);
                float param = 0.16277136f / (study * study * study);

                // трение, по умолчанию параметр ускорения падения вниз 
                float friction = jumpMovementFactor;
                if (OnGround)
                {
                    // если на земле, определяем скорость, можно в отдельном методе, у каждого моба может быть свои параметры

                    float speed = Mth.Max(Speed.Strafe * Mth.Abs(strafe), Speed.Forward * Mth.Abs(forward));
                    if (IsSprinting && forward < 0 && !IsSneaking)
                    {
                        // Бег 
                        speed *= Speed.Sprinting;
                    }
                    else if (!IsFlying && (forward != 0 || strafe != 0) && IsSneaking)
                    {
                        // Крадёмся
                        speed *= Speed.Sneaking;
                    }

                    // корректировка скорости, с трением
                    friction = speed * param;
                }

                motion = MotionAngle(strafe, forward, friction);

                // Тут надо корректировать леcтницу, вверх вниз Motion.y
                // ...

                // Проверка столкновения
                MoveEntity(motion);

                motion = Motion;
                // если есть горизонтальное столкновение и это лестница, то 
                // ... Motion.y = 0.2f;

                // Параметр падение 
                motion.y -= .16f;
                //motion.y -= .08f;

                motion.y *= .98f;
                motion.x *= study;
                motion.z *= study;
            }

            

            Motion = motion;
        }

        /// <summary>
        /// Значения для првжка
        /// </summary>
        protected void Jump()
        {
            // Стартовое значение прыжка, чтоб на 6 так допрыгнут наивысшую точку в 2,5 блока
            vec3 motion = new vec3(0, .84f, 0);
            //vec3 motion = new vec3(0, .42f, 0);
            if (IsSprinting)
            {
                // Если прыжок с бегом, то скорость увеличивается
                motion.x += glm.sin(RotationYaw) * 0.4f;
                motion.z -= glm.cos(RotationYaw) * 0.4f;
                //motion.x += glm.sin(RotationYaw) * 0.2f;
                //motion.z -= glm.cos(RotationYaw) * 0.2f;
            }
            Motion = new vec3(Motion.x + motion.x, motion.y, Motion.z + motion.z);
        }

        /// <summary>
        /// Определение вращения
        /// </summary>
        protected vec3 MotionAngle(float strafe, float forward, float friction)
        {
            vec3 motion = Motion;

            float sf = strafe * strafe + forward * forward;
            if (sf >= 0.0001f)
            {
                sf = Mth.Sqrt(sf);
                if (sf < 1f) sf = 1f;
                sf = friction / sf;
                strafe *= sf;
                forward *= sf;
                float yaw = GetRotationYaw();
                float ysin = glm.sin(yaw);
                float ycos = glm.cos(yaw);
                motion.x += ycos * strafe - ysin * forward;
                motion.z += ycos * forward + ysin * strafe;
            }
            return motion;
        }

        /// <summary>
        /// Получить градус поворота по Yaw
        /// </summary>
        protected virtual float GetRotationYaw() => RotationYaw;

        protected void UpPositionMotion()
        {
            motionDebug = Motion;
            SetPosition(Position + Motion);
        }

        /// <summary>
        /// Проверка перемещения со столкновением
        /// </summary>
        protected void MoveEntity(vec3 motion)
        {
            // Без проверки столкновения
            if (NoClip)
            {
                Motion = motion;
                UpPositionMotion();
            }

            AxisAlignedBB boundingBox = BoundingBox.Clone();
            AxisAlignedBB aabbEntity = boundingBox.Clone();
            List<AxisAlignedBB> aabbs;

            float x0 = motion.x;
            float y0 = motion.y;
            float z0 = motion.z;

            float x = x0;
            float y = y0;
            float z = z0;

            // Защита от падения с края блока если сидишь и являешься игроком
            if (OnGround && IsSneaking && this is EntityPlayer)
            {
                // TODO::2022-02-01 замечена бага, иногда падаешь! По Х на 50000
                // Шаг проверки смещения
                float step = 0.05f;
                for (; x != 0f && World.Collision.GetCollidingBoundingBoxes(boundingBox.Offset(new vec3(x, -1, 0))).Count == 0; x0 = x)
                {
                    if (x < step && x >= -step) x = 0f;
                    else if (x > 0f) x -= step;
                    else x += step;
                }
                for (; z != 0f && World.Collision.GetCollidingBoundingBoxes(boundingBox.Offset(new vec3(0, -1, z))).Count == 0; z0 = z)
                {
                    if (z < step && z >= -step) z = 0f;
                    else if (z > 0f) z -= step;
                    else z += step;
                }
                for (; x != 0f && z0 != 0f && World.Collision.GetCollidingBoundingBoxes(boundingBox.Offset(new vec3(x0, -1, z0))).Count == 0; z0 = z)
                {
                    if (x < step && x >= -step) x = 0f;
                    else if (x > 0f) x -= step;
                    else x += step;
                    x0 = x;
                    if (z < step && z >= -step) z = 0f;
                    else if (z > 0f) z -= step;
                    else z += step;
                }
            }

            aabbs = World.Collision.GetCollidingBoundingBoxes(boundingBox.AddCoord(new vec3(x, y, z)));

            // Находим смещение по Y
            foreach (AxisAlignedBB axis in aabbs) y = axis.CalculateYOffset(aabbEntity, y);
            aabbEntity = aabbEntity.Offset(new vec3(0, y, 0));

            // Не прыгаем (момент взлёта)
            bool isNotJump = OnGround || motion.y != y && motion.y < 0f;

            // Находим смещение по X
            foreach (AxisAlignedBB axis in aabbs) x = axis.CalculateXOffset(aabbEntity, x);
            aabbEntity = aabbEntity.Offset(new vec3(x, 0, 0));

            // Находим смещение по Z
            foreach (AxisAlignedBB axis in aabbs) z = axis.CalculateZOffset(aabbEntity, z);
            aabbEntity = aabbEntity.Offset(new vec3(0, 0, z));

            
            // Запуск проверки авто прыжка
            if (StepHeight > 0f && isNotJump && (x0 != x || z0 != z))
            {
                // Кэш для откада, если авто прыжок не допустим
                vec3 monCache = new vec3(x, y, z);

                float stepHeight = StepHeight;
                // Если сидим авто прыжок в двое ниже
                if (IsSneaking) stepHeight *= 0.5f;

                y = stepHeight;
                aabbs = World.Collision.GetCollidingBoundingBoxes(boundingBox.AddCoord(new vec3(x0, y, z0)));
                AxisAlignedBB aabbEntity2 = boundingBox.Clone();
                AxisAlignedBB aabb = aabbEntity2.AddCoord(new vec3(x0, 0, z0));

                // Находим смещение по Y
                float y2 = y;
                foreach (AxisAlignedBB axis in aabbs) y2 = axis.CalculateYOffset(aabb, y2);
                aabbEntity2 = aabbEntity2.Offset(new vec3(0, y2, 0));

                // Находим смещение по X
                float x2 = x0;
                foreach (AxisAlignedBB axis in aabbs) x2 = axis.CalculateXOffset(aabbEntity2, x2);
                aabbEntity2 = aabbEntity2.Offset(new vec3(x2, 0, 0));

                // Находим смещение по Z
                float z2 = z0;
                foreach (AxisAlignedBB axis in aabbs) z2 = axis.CalculateZOffset(aabbEntity2, z2);
                aabbEntity2 = aabbEntity2.Offset(new vec3(0, 0, z2));

                AxisAlignedBB aabbEntity3 = boundingBox.Clone();

                // Находим смещение по Y
                float y3 = y;
                foreach (AxisAlignedBB axis in aabbs) y3 = axis.CalculateYOffset(aabbEntity3, y3);
                aabbEntity3 = aabbEntity3.Offset(new vec3(0, y3, 0));

                // Находим смещение по X
                float x3 = x0;
                foreach (AxisAlignedBB axis in aabbs) x3 = axis.CalculateXOffset(aabbEntity3, x3);
                aabbEntity3 = aabbEntity3.Offset(new vec3(x3, 0, 0));

                // Находим смещение по Z
                float z3 = z0;
                foreach (AxisAlignedBB axis in aabbs) z3 = axis.CalculateZOffset(aabbEntity3, z3);
                aabbEntity3 = aabbEntity3.Offset(new vec3(0, 0, z3));

                if (x2 * x2 + z2 * z2 > x3 * x3 + z3 * z3)
                {
                    x = x2;
                    z = z2;
                    aabbEntity = aabbEntity2;
                } else
                {
                    x = x3;
                    z = z3;
                    aabbEntity = aabbEntity3;
                }
                y = -stepHeight;

                // Находим итоговое смещение по Y
                foreach (AxisAlignedBB axis in aabbs) y = axis.CalculateYOffset(aabbEntity, y);

                if (monCache.x * monCache.x + monCache.z * monCache.z >= x * x + z * z)
                {
                    // Нет авто прыжка, откатываем значение обратно
                    x = monCache.x;
                    y = monCache.y;
                    z = monCache.z;
                }
                else
                {
                    // Авто прыжок
                    SetPosition(Position + new vec3(0, y + stepHeight, 0));
                    y = 0;
                }
            }

            IsCollidedHorizontally = x0 != x || z0 != z;
            IsCollidedVertically = y0 != y;
            OnGround = IsCollidedVertically && y0 < 0.0f;
            IsCollided = IsCollidedHorizontally || IsCollidedVertically;

            // Определение дистанции падения, и фиксаия падения
            if (y < 0f) fallDistance -= y;
            if (OnGround)
            {
                if (IsFlying)
                {
                    ModeSurvival();
                    fallDistance = 0f;
                }
                else if (fallDistance > 0f)
                {
                    // Упал
                    fallDistanceResult = fallDistance;
                    Fall(fallDistance);
                    fallDistance = 0f;
                }
            }

            Motion = new vec3(x0 != x ? 0 : x, y, z0 != z ? 0 : z);
            UpPositionMotion();
        }

        /// <summary>
        /// Скорость движения для кадра
        /// </summary>
        /// <param name="timeIndex">коэфициент между тактами</param>
        public float GetLimbSwingAmountFrame(float timeIndex)
        {
            if (timeIndex >= 1.0f || limbSwingAmount.Equals(limbSwingAmountPrev)) return limbSwingAmount;
            return limbSwingAmountPrev + (limbSwingAmount - limbSwingAmountPrev) * timeIndex;
        }

        /// <summary>
        /// Получить анимацию руки для кадра
        /// </summary>
        /// <param name="timeIndex">коэфициент между тактами</param>
        public virtual float GetSwingProgressFrame(float timeIndex)
        {
            if (isSwingInProgress)
            {
                if (timeIndex >= 1.0f || swingProgress.Equals(swingProgressPrev)) return swingProgress;
                return swingProgressPrev + (swingProgress - swingProgressPrev) * timeIndex;
            }
            return 0;
        }

        /// <summary>
        /// Расчёт амплитуды конечностей, при движении
        /// </summary>
        protected void UpLimbSwing()
        {
            if (isLimbSwing)
            {
                limbSwingAmountPrev = limbSwingAmount;
                float xx = Position.x - PositionPrev.x;
                float zz = Position.z - PositionPrev.z;
                float xxzz = xx * xx + zz * zz;
                float xz = Mth.Sqrt(xxzz) * 2.0f;
                if (xz > 1.0f) xz = 1.0f;
                limbSwingAmount += (xz - limbSwingAmount) * 0.4f;
                LimbSwing += limbSwingAmount;
            }
        }

        /// <summary>
        /// Скакой скоростью анимируется удар рукой, в тактах, менять можно от инструмента, чар и навыков
        /// </summary>
        private int GetArmSwingAnimationEnd() => 6; 

        /// <summary>
        /// Размахивает предметом, который держит игрок
        /// </summary>
        public void SwingItem()
        {
            if (!isSwingInProgress || swingProgressInt >= GetArmSwingAnimationEnd() / 2 || swingProgressInt < 0)
            {
                swingProgressInt = -1;
                isSwingInProgress = true;

                if (World is WorldServer)
                {
                    ((WorldServer)World).Tracker.SendToAllTrackingEntity(this, new PacketS0BAnimation(Id, PacketS0BAnimation.EnumAnimation.SwingItem));
                    //((WorldServer)World).Players.ResponsePacketAll(new PacketS0BAnimation(Id, PacketS0BAnimation.EnumAnimation.SwingItem), Id);
                }
            }
        }

        /// <summary>
        /// Обновляет счетчики прогресса взмаха руки и прогресс анимации. 
        /// </summary>
        protected void UpdateArmSwingProgress()
        {
            swingProgressPrev = swingProgress;

            int asa = GetArmSwingAnimationEnd();

            if (isSwingInProgress)
            {
                swingProgressInt++;
                if (swingProgressInt >= asa)
                {
                    swingProgressInt = 0;
                    isSwingInProgress = false;
                }
            }
            else
            {
                swingProgressInt = 0;
            }

            swingProgress = (float)swingProgressInt / (float)asa;
        }

        /// <summary>
        /// Падение
        /// </summary>
        protected virtual void Fall(float distance) { }

        /// <summary>
        /// Получить коэффициент времени от прошлого пакета перемещения сервера в диапазоне 0 .. 1
        /// где 0 это начало, 1 финиш
        /// </summary>
       // public float TimeIndex() => interpolation.TimeIndex();
        /// <summary>
        /// Коэффициент интерполяции перезапускаем
        /// </summary>
       // protected void InterolationReset() => interpolation.Restart();

        /// <summary>
        /// Задать позицию от сервера
        /// </summary>
        public virtual void SetMotionServer(vec3 pos, float yaw, float pitch, bool sneaking)
        {
            if (IsSneaking != sneaking)
            {
                IsSneaking = sneaking;
                if (IsSneaking) Sitting(); else Standing();
            }
            PositionPrev = Position;
            SetPosition(pos);

            //RotationPitchPrev = RotationPitch;
            //RotationYawPrev = RotationYaw;

            //interpolation.Restart();
            //InterolationReset();
        }

        
    }
}

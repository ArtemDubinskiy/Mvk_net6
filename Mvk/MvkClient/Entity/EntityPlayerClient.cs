﻿using MvkClient.Util;
using MvkClient.World;
using MvkServer.Entity.Player;
using MvkServer.Glm;
using MvkServer.Network.Packets;

namespace MvkClient.Entity
{
    /// <summary>
    /// Сущность игрока для клиента
    /// </summary>
    public abstract class EntityPlayerClient : EntityPlayer
    {
        /// <summary>
        /// Клиентский объект мира
        /// </summary>
        public WorldClient ClientWorld { get; protected set; }
        /// <summary>
        /// Скрыта ли сущность на экране
        /// </summary>
        public bool IsHidden { get; protected set; } = true;
        /// <summary>
        /// Объект времени c последнего тпс
        /// </summary>
        protected InterpolationTime interpolation = new InterpolationTime();

        public EntityPlayerClient(WorldClient world) : base()
        {
            ClientWorld = world;
            World = world;
            interpolation.Start();
        }

        /// <summary>
        /// Входящие данные запуска игрока
        /// </summary>
        public void OnPacketS12Success(PacketS12Success packet)
        {
            Name = packet.Name;
            UUID = packet.GetUuid();
            Id = packet.GetId();
            SetPosition(packet.Pos);
            RotationYawLast = RotationYawPrev = RotationYaw = RotationYawHead = RotationYawHeadPrev = packet.Yaw;
            RotationPitchLast = RotationPitchPrev = RotationPitch = packet.Pitch;
            //SetRotation(packet.Yaw, packet.Pitch);
            PositionPrev = Position;
        }

        /// <summary>
        /// Получить коэффициент времени от прошлого пакета перемещения сервера в диапазоне 0 .. 1
        /// где 0 это начало, 1 финиш
        /// </summary>
        public float TimeIndex() => interpolation.TimeIndex();

        /// <summary>
        /// Задать позицию от сервера
        /// </summary>
        public void SetPositionServer(vec3 pos, bool sneaking, bool onGround)
        {
           // interpolation.Restart();
            if (IsSneaking != sneaking)
            {
                IsSneaking = sneaking;
                if (IsSneaking) Sitting(); else Standing();
            }
            OnGround = onGround;
            PositionPrev = Position;
            SetPosition(pos);
        }

        /// <summary>
        /// Задать вращение от сервера
        /// </summary>
        public void SetRotationServer(float yawHead, float yawBody, float pitch)
        {
            interpolation.Restart();
            RotationPitchPrev = RotationPitch;
            RotationYawPrev = RotationYaw;
            RotationYawHeadPrev = RotationYawHead;
            SetRotationHead(yawHead, yawBody, pitch);
            //SetRotation(yawHead, pitch);
            //RotationYaw = yaw;
            //RotationPitch = pitch;
        }

        public override string ToString()
        {
            return Name + "\r\n" + base.ToString();
        }
    }
}

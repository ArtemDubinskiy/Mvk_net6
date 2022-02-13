﻿using MvkServer.Glm;

namespace MvkServer.Network.Packets.Client
{
    /// <summary>
    /// Пакет расположения игрока вместе с камерой
    /// </summary>
    public struct PacketC06PlayerPosLook : IPacket
    {
        private vec3 pos;
        private float yaw;
        private float pitch;
        private bool sneaking;

        public vec3 GetPos() => pos;
        public float GetYaw() => yaw;
        public float GetPitch() => pitch;
        public bool IsSneaking() => sneaking;

        public PacketC06PlayerPosLook(vec3 pos, float yaw, float pitch, bool sneaking)
        {
            this.pos = pos;
            this.yaw = yaw;
            this.pitch = pitch;
            this.sneaking = sneaking;
        }

        public void ReadPacket(StreamBase stream)
        {
            pos = new vec3(stream.ReadFloat(), stream.ReadFloat(), stream.ReadFloat());
            yaw = stream.ReadFloat();
            pitch = stream.ReadFloat();
            sneaking = stream.ReadBool();
        }

        public void WritePacket(StreamBase stream)
        {
            stream.WriteFloat(pos.x);
            stream.WriteFloat(pos.y);
            stream.WriteFloat(pos.z);
            stream.WriteFloat(yaw);
            stream.WriteFloat(pitch);
            stream.WriteBool(sneaking);
        }
    }
}

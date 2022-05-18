﻿using MvkServer.Glm;
using System;

namespace MvkServer.Util
{
    /// <summary>
    /// Объект перечня сторон
    /// </summary>
    public class EnumFacing
    {
        /// <summary>
        /// Нормализованный вектор, указывающий в направлении этой облицовки
        /// </summary>
        public static vec3i DirectionVec(Pole pole)
        {
            switch (pole)
            {
                case Pole.Up: return new vec3i(0, 1, 0);
                case Pole.Down: return new vec3i(0, -1, 0);
                case Pole.East: return new vec3i(1, 0, 0);
                case Pole.West: return new vec3i(-1, 0, 0);
                case Pole.North: return new vec3i(0, 0, -1);
                case Pole.South: return new vec3i(0, 0, 1);
                default: throw new ArgumentNullException("Не существует такой стороны");
            }
        }
        /// <summary>
        /// Нормализованный вектор, указывающий в направлении этой облицовки
        /// </summary>
        public static vec3i DirectionVec(int pole)
        {
            switch (pole)
            {
                case 0: return new vec3i(0, 1, 0);
                case 1: return new vec3i(0, -1, 0);
                case 2: return new vec3i(1, 0, 0);
                case 3: return new vec3i(-1, 0, 0);
                case 4: return new vec3i(0, 0, -1);
                case 5: return new vec3i(0, 0, 1);
                default: throw new ArgumentNullException("Не существует такой стороны");
            }
        }

        /// <summary>
        /// Получите облицовку, соответствующую заданному углу (0-360). Угол 0 - SOUTH, угол 90 - WEST.
        /// </summary>
        /// <param name="angle">угол в градусах</param>
        public static Pole FromAngle(float angle)
        {
            if (angle >= -45f && angle <= 45f) return Pole.North;
            else if (angle > 45f && angle < 135f) return Pole.West;
            else if (angle < -45f && angle > -135f) return Pole.East;
            return Pole.South;
        }

        /// <summary>
        /// Проверить левее ли от тикущего полюса
        /// </summary>
        /// <param name="angle">угол в градусах</param>
        public static bool IsFromAngleLeft(float angle, Pole pole)
        {
            if (pole == Pole.North) return angle > 0;
            if (pole == Pole.West) return angle > 90f;
            if (pole == Pole.South) return angle < 0;
            if (pole == Pole.East) return angle > -90f;
            return true;
        }


        /// <summary>
        /// Получите сторону по его горизонтальному индексу (0-3). Заказ S-W-N-E.
        /// </summary>
        /// <param name="index">индекс (0-3)</param>
        public static Pole GetHorizontal(int index)
        {
            switch (index)
            {
                case 2: return Pole.North;
                case 0: return Pole.South;
                case 1: return Pole.West;
                case 3: return Pole.East;
                default: throw new ArgumentNullException("Не существует такой стороны");
            }
        }

        /// <summary>
        /// Вернуть массив всех горизонтальных сторон (0-3). Заказ S-W-N-E.
        /// </summary>
        /// <returns></returns>
        public static Pole[] ArrayHorizontal() => new Pole[] { Pole.South, Pole.West, Pole.North, Pole.East };

        /// <summary>
        /// Нормализованный горизонтальный вектор, указывающий в направлении этой облицовки
        /// </summary>
        public static vec3i DirectionHorizontalVec(Pole pole)
        {
            switch (pole)
            {
                case Pole.East: return new vec3i(1, 0, 0);
                case Pole.West: return new vec3i(-1, 0, 0);
                case Pole.North: return new vec3i(0, 0, -1);
                case Pole.South: return new vec3i(0, 0, 1);
                default: throw new ArgumentNullException("Не существует такой стороны");
            }
        }

        /// <summary>
        /// Нормализованный горизонтальный вектор, (0-3). Заказ S-W-N-E.
        /// </summary>
        /// <param name="index">индекс (0-3)</param>
        public static vec3i DirectionHorizontalVec(int index) => DirectionHorizontalVec(GetHorizontal(index));

        /// <summary>
        /// Получите противоположную ориентацию DOWN => UP
        /// </summary>
        public static Pole GetOpposite(Pole pole)
        {
            switch (pole)
            {
                case Pole.Up: return Pole.Down;
                case Pole.Down: return Pole.Up;
                case Pole.East: return Pole.West;
                case Pole.West: return Pole.East;
                case Pole.North: return Pole.South;
                case Pole.South: return Pole.North;
                default: throw new ArgumentNullException("Не существует такой стороны");
            }
        }
    }
}

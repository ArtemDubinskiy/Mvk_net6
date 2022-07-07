﻿namespace MvkServer.World.Block
{
    /// <summary>
    /// Тип блока
    /// </summary>
    public enum EnumBlock
    {
        /// <summary>
        /// Отсутствие блока, он же воздух, но с коллизией, пройти через него нельзя
        /// </summary>
        None = -1,
        /// <summary>
        /// Воздух
        /// </summary>
        Air = 0,
        /// <summary>
        /// Камень
        /// </summary>
        Stone = 1,
        /// <summary>
        /// Булыжник
        /// </summary>
        Cobblestone = 2,
        /// <summary>
        /// Земля
        /// </summary>
        Dirt = 3,
        /// <summary>
        /// Дёрн
        /// </summary>
        Turf = 4,
        /// <summary>
        /// Вода
        /// </summary>
        Water = 5,
        /// <summary>
        /// Стекло
        /// </summary>
        Glass = 6,
        /// <summary>
        /// Стекло красное
        /// </summary>
        GlassRed = 7,
        /// <summary>
        /// Брол
        /// </summary>
        Brol = 8,
        /// <summary>
        /// Бревно
        /// </summary>
        Log = 9,
        /// <summary>
        /// Блок высокой травы
        /// </summary>
        TallGrass = 10

    }

    /// <summary>
    /// Количество блоков
    /// </summary>
    public class BlocksCount
    {
        public const int COUNT = 10;
    }
}

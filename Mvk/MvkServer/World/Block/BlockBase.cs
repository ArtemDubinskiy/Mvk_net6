﻿using MvkServer.Glm;
using MvkServer.Util;

namespace MvkServer.World.Block
{
    /// <summary>
    /// Базовый объект Блока
    /// </summary>
    public abstract class BlockBase
    {
        /// <summary>
        /// Коробки
        /// </summary>
        public Box[] Boxes { get; protected set; } = new Box[] { new Box() };
        /// <summary>
        /// Вся ли прорисовка, аналог кактус, забор...
        /// </summary>
        public bool AllDrawing { get; protected set; } = false;
        /// <summary>
        /// Получить тип блока
        /// </summary>
        public EnumBlock EBlock { get; protected set; }
        /// <summary>
        /// Позиция блока в мире
        /// </summary>
        public BlockPos Position { get; protected set; } = new BlockPos();
        /// <summary>
        /// Явлыется ли блок небом
        /// </summary>
        public bool IsAir => EBlock == EnumBlock.Air;
        /// <summary>
        /// Трава ли это
        /// </summary>
        public bool IsGrass { get; protected set; } = false;
        /// <summary>
        /// Есть ли столкновение
        /// </summary>
        public bool IsCollision { get; protected set; } = true;

        /// <summary>
        /// Минимальные координаты ограничительной рамки
        /// </summary>
        protected vec3 min = new vec3(0f);
        /// <summary>
        /// Максимальные координаты ограничительной рамки
        /// </summary>
        protected vec3 max = new vec3(1f);
        /// <summary>
        /// Ограничительная рамка занимает весь блок, для оптимизации, без проверки AABB блока
        /// </summary>
        public bool IsBoundingBoxAll { get; protected set; } = true;

        /// <summary>
        /// Задать позицию блока
        /// </summary>
        public void SetPosition(BlockPos pos) => Position = pos;
        /// <summary>
        /// Задать тип блока
        /// </summary>
        public void SetEnumBlock(EnumBlock enumBlock) => EBlock = enumBlock;

        /// <summary>
        /// Получить ограничительную рамку блока
        /// </summary>
        protected AxisAlignedBB GetBoundingBox() => new AxisAlignedBB(
            new vec3(Position.X + min.x, Position.Y + min.y, Position.Z + min.z),
            new vec3(Position.X + max.x, Position.Y + max.y, Position.Z + max.z));

        /// <summary>
        /// Передать список  ограничительных рамок блока
        /// </summary>
        public virtual AxisAlignedBB[] GetCollisionBoxesToList() => new AxisAlignedBB[] { GetBoundingBox() };

        /// <summary>
        /// Получить высоту блока
        /// </summary>
        public float GetHeight() => max.y;

        /// <summary>
        /// Строка
        /// </summary>
        public override string ToString() => EBlock.ToString() + " " + Position.ToString();
    }
}

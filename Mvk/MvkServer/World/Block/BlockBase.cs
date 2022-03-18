﻿using MvkServer.Entity.Player;
using MvkServer.Glm;
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
        /// Может ли блок сталкиваться
        /// </summary>
        public bool IsCollidable { get; protected set; } = true;
        /// <summary>
        /// Цвет блока по умолчани, биом потом заменит
        /// Для частички
        /// </summary>
        public vec3 Color { get; protected set; } = new vec3(1);
        /// <summary>
        /// Индекс картинки частички
        /// </summary>
        public int Particle { get; protected set; } = 0;

        /// <summary>
        /// Ограничительная рамка занимает весь блок, для оптимизации, без проверки AABB блока
        /// </summary>
        public bool IsBoundingBoxAll { get; protected set; } = true;

        /// <summary>
        /// Сколько ударов требуется, чтобы сломать блок в тактах (20 тактов = 1 секунда)
        /// </summary>
        public int Hardness { get; protected set; } = 0;

        /// <summary>
        /// Задать позицию блока
        /// </summary>
        public void SetPosition(BlockPos pos) => Position = pos;
        /// <summary>
        /// Задать тип блока
        /// </summary>
        public void SetEnumBlock(EnumBlock enumBlock) => EBlock = enumBlock;

        /// <summary>
        /// Передать список  ограничительных рамок блока
        /// </summary>
        public virtual AxisAlignedBB[] GetCollisionBoxesToList()
        {
            vec3 min = Position.ToVec3();
            return new AxisAlignedBB[] { new AxisAlignedBB(min, min + 1f) };
        }

        /// <summary>
        /// Получить одну большую рамку блока, если их несколько они объеденяться
        /// </summary>
        public AxisAlignedBB GetCollision()
        {
            AxisAlignedBB[] axes = GetCollisionBoxesToList();
            if (axes.Length > 0)
            {
                AxisAlignedBB aabb = axes[0];
                for (int i = 1; i < axes.Length; i++)
                {
                    aabb = aabb.AddCoord(axes[i].Min).AddCoord(axes[i].Max);
                }
                return aabb;
            }
            vec3 min = Position.ToVec3();
            return new AxisAlignedBB(min, min + 1f);
        }

        /// <summary>
        /// Проверить колизию блока на пересечение луча
        /// </summary>
        /// <param name="pos">точка от куда идёт лучь</param>
        /// <param name="dir">вектор луча</param>
        /// <param name="maxDist">максимальная дистания</param>
        public bool CollisionRayTrace(vec3 pos, vec3 dir, float maxDist)
        {
            if (IsCollidable)
            {
                if (IsBoundingBoxAll) return true;

                // Если блок не полный, обрабатываем хитбокс блока
                RayCross ray = new RayCross(pos, dir, maxDist);
                return ray.IsCrossAABBs(GetCollisionBoxesToList());
            }
            return false;
        }

        /// <summary>
        /// Значение для разрушения в тактах
        /// </summary>
        //public virtual int GetDamageValue() => 0;

       // public float GetBlockHardness()

        /// <summary>
        /// Получите твердость этого блока относительно способности данного игрока
        /// Тактов чтоб сломать
        /// </summary>
        public int GetPlayerRelativeBlockHardness(EntityPlayer playerIn)
        {
            //return 0; // креатив
            return Hardness; // выживание
            //return hardness < 0.0F ? 0.0F : (!playerIn.canHarvestBlock(this) ? playerIn.func_180471_a(this) / hardness / 100.0F : playerIn.func_180471_a(this) / hardness / 30.0F);
        }

        /// <summary>
        /// Проверка равенства блока по координатам и типу
        /// </summary>
        public bool Equals(BlockBase block)
        {
            return block.Position.X == Position.X && block.Position.Y == Position.Y && block.Position.Z == Position.Z
                && block.EBlock == EBlock;
        }
        /// <summary>
        /// Строка
        /// </summary>
        public override string ToString() => EBlock.ToString() + " " + Position.ToString();
    }
}

﻿using MvkServer.Item;
using MvkServer.Item.List;
using MvkServer.World.Block;
using System.Collections;

namespace MvkClient.Renderer.Entity
{
    /// <summary>
    /// Рендер предмета
    /// </summary>
    public class RenderItem
    {
        /// <summary>
        /// Перечень всех блоков RenderEntityBlock
        /// </summary>
        private Hashtable blocks = new Hashtable();

        private RenderEntityBlock GetRenderBlock(EnumBlock enumBlock)
        {
            if (blocks.ContainsKey(enumBlock))
            {
                return blocks[enumBlock] as RenderEntityBlock;
            } else
            {
                RenderEntityBlock renderBlock = new RenderEntityBlock(enumBlock);
                blocks.Add(enumBlock, renderBlock);
                return renderBlock;
            }
        }

        public void Render(ItemStack stack) => Render(stack.Item);
        public void Render(ItemBase item)
        {
            if (item is ItemBlock itemBlock)
            {
                RenderEntityBlock renderBlock = GetRenderBlock(itemBlock.Block.EBlock);
                renderBlock.Render();
            }
        }
    }
}

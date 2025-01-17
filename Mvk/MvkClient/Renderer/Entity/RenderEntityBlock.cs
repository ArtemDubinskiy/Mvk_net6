﻿using MvkClient.Renderer.Block;
using MvkServer.World.Block;

namespace MvkClient.Renderer.Entity
{
    public class RenderEntityBlock : RenderDL
    {
        /// <summary>
        /// Тип блока
        /// </summary>
        private readonly EnumBlock enumBlock;

        public RenderEntityBlock(EnumBlock enumBlock) => this.enumBlock = enumBlock;

        protected override void DoRender()
        {
            BlockGuiRender render = new BlockGuiRender(Blocks.GetBlockCache(enumBlock));
            render.RenderVBOtoDL();
        }
    }
}

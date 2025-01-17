﻿using MvkServer.Glm;
using MvkServer.Sound;

namespace MvkServer.World.Block.List
{
    /// <summary>
    /// Блок Песок
    /// </summary>
    public class BlockSand : BlockBase
    {
        /// <summary>
        /// Блок Песок
        /// </summary>
        public BlockSand()
        {
            Material = EnumMaterial.Loose;
            samplesPut = samplesBreak = new AssetsSample[] { AssetsSample.DigSand1, AssetsSample.DigSand2, AssetsSample.DigSand3, AssetsSample.DigSand4 };
            samplesStep = new AssetsSample[] { AssetsSample.StepSand1, AssetsSample.StepSand2, AssetsSample.StepSand3, AssetsSample.StepSand4 };
            Particle = 68;
            InitBoxs(68, false, new vec3(.95f, .91f, .73f));
        }

        /// <summary>
        /// Сколько ударов требуется, чтобы сломать блок в тактах (20 тактов = 1 секунда)
        /// </summary>
        public override int Hardness(BlockState state) => 5;
    }
}

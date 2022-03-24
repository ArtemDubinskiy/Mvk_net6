﻿using MvkClient.Renderer.Chunk;
using MvkServer.Glm;
using MvkServer.Util;
using MvkServer.World.Block;
using MvkServer.World.Chunk;
using SharpGL;
using System.Collections.Generic;

namespace MvkClient.Renderer.Block
{
    /// <summary>
    /// Объект рендера блока
    /// </summary>
    public class BlockRender
    {
        /// <summary>
        /// Объект рендера чанков
        /// </summary>
        public ChunkRender Chunk { get; protected set; }
        /// <summary>
        /// Объект блока
        /// </summary>
        public BlockBase Block { get; protected set; }
        /// <summary>
        /// Пометка, разрушается ли блок и его стадия
        /// -1 не разрушается, 0-9 разрушается
        /// </summary>
        public int DamagedBlocksValue { get; set; } = -1;

        /// <summary>
        /// позиция блока в чанке
        /// </summary>
        protected vec3i posChunk;
        /// <summary>
        /// кэш коробка
        /// </summary>
        protected Box cBox;
        /// <summary>
        /// кэш сторона блока
        /// </summary>
        protected Face cFace;
        /// <summary>
        /// кэш Направление
        /// </summary>
        protected Pole cSide;

        public BlockRender(ChunkRender chunkRender, BlockBase block)
        {
            Chunk = chunkRender;
            Block = block;
            // позиция блока в чанке
            posChunk = new vec3i(Block.Position.X & 15, Block.Position.Y, Block.Position.Z & 15);
        }


        /// <summary>
        /// Получть Сетку блока
        /// </summary>
        /// <param name="pos">позиция блока</param>
        /// <returns>сетка</returns>
        public float[] RenderMesh(bool check)
        {
            List<float> buffer = new List<float>();

            foreach (Box box in Block.Boxes)
            {
                cBox = box;
                foreach (Face face in box.Faces)
                {
                    cFace = face;
                    if (face.GetSide() == Pole.All)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            cSide = (Pole)i;
                            if (check) RenderMeshSideCheck(buffer);
                            else buffer.AddRange(RenderMeshFace());
                        }
                    }
                    else
                    {
                        cSide = face.GetSide();
                        if (check) RenderMeshSideCheck(buffer);
                        else buffer.AddRange(RenderMeshFace());
                    }
                }
            }
            return buffer.ToArray();
        }

        /// <summary>
        /// Получть Сетку стороны блока с проверкой соседнего блока и разрушения его
        /// </summary>
        protected void RenderMeshSideCheck(List<float> buffer)
        {
            EnumBlock enumBlock = GetEBlock(posChunk + EnumFacing.DirectionVec(cSide));
            //_br = BlockedLight(posChunk + EnumFacing.DirectionVec(side));
            //   if (Blk.AllDrawing || _br.IsDraw)
            if (enumBlock == EnumBlock.Air || Block.AllDrawing || enumBlock == EnumBlock.Cobblestone)
            {
                buffer.AddRange(RenderMeshFace());
                if (DamagedBlocksValue != -1)
                {
                    Face face = cFace;
                    cFace = new Face(cSide, 4032 + DamagedBlocksValue, true, cFace.GetColor());
                    buffer.AddRange(RenderMeshFace());
                    cFace = face;
                }

            }
        }


        /// <summary>
        /// Получить смещение на текстуру блока
        /// </summary>
        /// <returns>x = u1, Y = v1, z = u2, w = v2</returns>
        private vec4 GetUV()
        {
            float u1 = (cFace.GetNumberTexture() % 64) * 0.015625f;// 0.015625f;
            float v2 = cFace.GetNumberTexture() / 64 * 0.015625f;
            return new vec4(
                u1 + cBox.UVFrom.x, v2 + cBox.UVTo.y,
                u1 + cBox.UVTo.x, v2 + cBox.UVFrom.y
                );
        }

        /// <summary>
        /// Генерация сетки стороны коробки
        /// </summary>
        /// <param name="pos">позиция блока</param>
        /// <param name="side">направление блока</param>
        /// <param name="face">объект стороны блока</param>
        /// <param name="box">объект коробки</param>
        /// <param name="lightValue">яркость дневного соседнего блока</param>
        /// <returns></returns>
        protected float[] RenderMeshFace()
        {
            vec4 uv = GetUV();
            vec4 color = cFace.GetIsColor() ? new vec4(cFace.GetColor(), 1f) : new vec4(1f);
            float l = 1f - LightPole();
            color.x -= l; if (color.x < 0) color.x = 0;
            color.y -= l; if (color.y < 0) color.y = 0;
            color.z -= l; if (color.z < 0) color.z = 0;
            vec3 col = new vec3(color.x, color.y, color.z);
            //col = new vec3(1f);
            vec3i posi = Block.Position.Position;
            vec3 pos = new vec3(posi.x & 15, posi.y, posi.z & 15);
            //vec3 pos = Block.Position.ToVec3();
            BlockFaceUV blockUV = new BlockFaceUV(col, pos);

            blockUV.SetVecUV(
                pos + cBox.From,
                pos + cBox.To,
                new vec2(uv.x, uv.y),
                new vec2(uv.z, uv.w)
            );

            blockUV.RotateYaw(cBox.RotateYaw);
            blockUV.RotatePitch(cBox.RotatePitch);

            return blockUV.Side(cSide);
        }


        /// <summary>
        /// Затемнение стороны от стороны блока
        /// </summary>
        protected float LightPole()
        {
            switch (cSide)
            {
                case Pole.Up: return 1f;
                case Pole.South: return 0.85f;
                case Pole.East: return 0.7f;
                case Pole.West: return 0.7f;
                case Pole.North: return 0.85f;
            }
            return 0.6f;
        }


        /// <summary>
        /// Поиск тип блока
        /// </summary>
        public EnumBlock GetEBlock(vec3i pos)
        {
            if (pos.y < 0 || pos.y > 255) return EnumBlock.Air;

            int xc = Chunk.Position.x + (pos.x >> 4);
            int zc = Chunk.Position.y + (pos.z >> 4);
            int xv = pos.x & 15;
            int zv = pos.z & 15;

            if (xc == Chunk.Position.x && zc == Chunk.Position.y)
            {
                // Соседний блок в этом чанке
                return Chunk.GetEBlock(new vec3i(xv, pos.y, zv));
            }
            // Соседний блок в соседнем чанке
            ChunkBase chunk = Chunk.World.ChunkPr.GetChunk(new vec2i(xc, zc));
            if (chunk != null) return chunk.GetEBlock(new vec3i(xv, pos.y, zv));

            return EnumBlock.Air;
        }

        /// <summary>
        /// Рендер блока VBO, конвертация из  VBO в DisplayList
        /// </summary>
        public void RenderVBOtoDL()
        {
            float[] buffer = RenderMesh(false);

            GLRender.PushMatrix();
            {
                GLRender.Begin(OpenGL.GL_TRIANGLES);
                for (int i = 0; i < buffer.Length; i +=10)
                {
                    GLRender.Color(buffer[i + 5], buffer[i + 6], buffer[i + 7]);
                    GLRender.TexCoord(buffer[i + 3], buffer[i + 4]);
                    GLRender.Vertex(buffer[i] - .5f, buffer[i + 1] - .5f, buffer[i + 2] - .5f);
                }
                GLRender.End();
            }
            GLRender.PopMatrix();
        }
    }
}

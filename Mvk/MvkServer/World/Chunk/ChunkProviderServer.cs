﻿using MvkServer.Glm;
using MvkServer.Util;
using System.Collections;

namespace MvkServer.World.Chunk
{
    /// <summary>
    /// Объект сервер который хранит и отвечает за кэш чанков
    /// </summary>
    public class ChunkProviderServer : ChunkProvider
    {
        /// <summary>
        /// Список чанков которые надо выгрузить
        /// </summary>
        public MapListVec2i DroppedChunks { get; protected set; } = new MapListVec2i();

        public ChunkProviderServer(WorldServer worldIn) => world = worldIn;

        /// <summary>
        /// Загрузить чанк
        /// </summary>
        public ChunkBase LoadChunk(vec2i pos)
        {
            
            DroppedChunks.Remove(pos);
            ChunkBase chunk = GetChunk(pos);
            if (chunk == null || chunk.DoneStatus < 4)
            {
                ((WorldServer)world).countGetChunck++;
                chunk = GetStatusChunk(pos, 4);
            }
            chunk.OnChunkLoad();
            return chunk;
        }

        /// <summary>
        /// Получить чанк по статусу, если статуса не хватает, догружаем рядом лежащие пока не получим нужный статус
        /// </summary>
        protected ChunkBase GetStatusChunk(vec2i pos, int status)
        {
            DroppedChunks.Remove(pos);
            ChunkBase chunk = GetChunk(pos);
            if (chunk == null)
            {
                chunk = new ChunkBase(world, pos);
                chunk.ChunkLoadGen();
                //chunk.OnChunkLoad();
                chunkMapping.Set(chunk);
            }
            //TODO :: оптимизировать надо!!! чтоб проходы были быстрее
            if (chunk.DoneStatus < status)
            {
                for (int i = 0; i < 8; i++)
                {
                    GetStatusChunk(pos + MvkStatic.AreaOne8[i], status - 1);
                }
                //System.Threading.Thread.Sleep(1);
                chunk.DoneStatus = status;
            }
            return chunk;
        }

        /// <summary>
        /// Выгрузка ненужных чанков Для сервера
        /// </summary>
        public void UnloadQueuedChunks()
        {
            int i = 0;
            while (DroppedChunks.Count > 0 && i < 100) // 100
            {
                vec2i pos = DroppedChunks.FirstRemove();
                ChunkBase chunk = chunkMapping.Get(pos);
                if (chunk != null)
                {
                    chunk.OnChunkUnload();
                    // TODO::Тут сохраняем чанк
                    chunkMapping.Remove(pos);
                    i++;
                }
            }
        }

        /// <summary>
        /// Добавить в список удаляющих чанков которые не полного статуса
        /// </summary>
        public void DroopedChunkStatusMin(Hashtable playersClone) => chunkMapping.DroopedChunkStatusMin(DroppedChunks, playersClone);
    }
}

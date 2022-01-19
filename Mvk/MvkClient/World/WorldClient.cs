﻿using MvkClient.Entity;
using MvkClient.Renderer;
using MvkClient.Renderer.Chunk;
using MvkClient.Setitings;
using MvkClient.Util;
using MvkServer;
using MvkServer.Glm;
using MvkServer.Util;
using MvkServer.World;
using System.Collections;

namespace MvkClient.World
{
    /// <summary>
    /// Клиентский объект мира
    /// </summary>
    public class WorldClient : WorldBase
    {
        /// <summary>
        /// Основной клиент
        /// </summary>
        public Client ClientMain { get; protected set; }
        /// <summary>
        /// Объект клиента
        /// </summary>
        public EntityPlayerSP Player { get; protected set; }
        /// <summary>
        /// Посредник клиентоского чанка
        /// </summary>
        public ChunkProviderClient ChunkPrClient => ChunkPr as ChunkProviderClient;
        /// <summary>
        /// Мир для рендера и прорисовки
        /// </summary>
        public WorldRenderer WorldRender { get; protected set; }
        /// <summary>
        /// Список сущностей игроков
        /// </summary>
        public Hashtable PlayerEntities { get; protected set; } = new Hashtable();

        /// <summary>
        /// Объект времени c последнего тпс
        /// </summary>
        protected InterpolationTime interpolation = new InterpolationTime();
        /// <summary>
        /// фиксатор чистки мира
        /// </summary>
        protected uint previousTotalWorldTime;
        /// <summary>
        /// Перечень игроков, отладка
        /// </summary>
        protected string strPlayers = "";

        public WorldClient(Client client) : base()
        {
            ChunkPr = new ChunkProviderClient(this);
            ClientMain = client;
            interpolation.Start();
            WorldRender = new WorldRenderer(this);
            Player = new EntityPlayerSP(this);
            Player.SetOverviewChunk(Setting.OverviewChunk, 0);
            UpStrPlayers();
        }

        /// <summary>
        /// Обработка каждый тик
        /// </summary>
        public override void Tick()
        {
            interpolation.Restart();
            uint time = ClientMain.TickCounter;

            base.Tick();
            

            if (time - previousTotalWorldTime > MvkGlobal.CHUNK_CLEANING_TIME)
            {
                previousTotalWorldTime = time;
                ChunkPrClient.FixOverviewChunk(Player);
            }
        }

        /// <summary>
        /// Проверить загружены ли все ближ лижащие чанки кроме центра
        /// </summary>
        /// <param name="pos">позиция чанка</param>
        public bool IsChunksSquareLoaded(vec2i pos)
        {
            for (int i = 0; i < MvkStatic.AreaOne8.Length; i++)
            {
                ChunkRender chunk = ChunkPrClient.GetChunkRender(pos + MvkStatic.AreaOne8[i], false);
                if (chunk == null || !chunk.IsChunkLoaded) return false;
            }
            return true; 
        }

        /// <summary>
        /// Остановка мира, удаляем все элементы
        /// </summary>
        public void StopWorldDelete()
        {
            ChunkPrClient.ClearAllChunks();
        }

        /// <summary>
        /// Получить коэффициент времени от прошлого TPS клиента в диапазоне 0 .. 1
        /// где 0 это начало, 1 финиш
        /// </summary>
        public float TimeIndex() => interpolation.TimeIndex();

        /// <summary>
        /// Получить объект игрока по сети, по имени
        /// </summary>
        public EntityPlayerMP GetPlayerMP(ushort id)
        {
            if (PlayerEntities.ContainsKey(id))
            {
                return PlayerEntities[id] as EntityPlayerMP;
            }
            return null;
        }

        /// <summary>
        /// Заменить объект игрока по сети
        /// </summary>
        public void SetPlayerMP(EntityPlayerMP entity)
        {
            if (!PlayerEntities.ContainsKey(entity.Id))
            {
                PlayerEntities.Add(entity.Id, entity);
            }
            else
            {
                PlayerEntities[entity.Id] = entity;
            }
            UpStrPlayers();
        }

        /// <summary>
        /// Удалить игрока с базы
        /// </summary>
        public void RemovePlayerMP(ushort id)
        {
            PlayerEntities.Remove(id);
            UpStrPlayers();
        }

        /// <summary>
        /// Обновить перечень игроков, отладка
        /// </summary>
        protected void UpStrPlayers()
        {
            strPlayers = "[" + PlayerEntities.Count + "] ";
            if (PlayerEntities.Count > 0)
            {
                foreach (EntityPlayerMP entity in PlayerEntities.Values)
                {
                    strPlayers += entity.Name + " ";
                }
            }
        }

        /// <summary>
        /// Строка для дебага
        /// </summary>
        public override string ToStringDebug()
        {
            return string.Format("t {2} Ch {0} ChDel {3}\r\n{4} {1}", 
                ChunkPr.Count, Player, ClientMain.TickCounter / 20, 
                ChunkPrClient.RemoteMeshChunks.Count, strPlayers);
        }
    }
}

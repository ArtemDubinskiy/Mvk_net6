﻿using MvkAssets;
using MvkClient.Setitings;
using MvkClient.Util;

namespace MvkClient
{
    /// <summary>
    /// Объект подготовки загрузки звуковых файлов в буфер, текстур и прочего
    /// </summary>
    public class Loading
    {
        /// <summary>
        /// Количество процессинга
        /// </summary>
        public int Count { get; protected set; } = 70;
        /// <summary>
        /// Основной объект клиента
        /// </summary>
        private Client client;

        public Loading(Client client)
        {
            this.client = client;

            
            // Определяем максимальное количество для счётчика
            Count = 1 // Загрузка опций
                + Enum.GetValues(typeof(AssetsSample)).Length + Enum.GetValues(typeof(AssetsTexture)).Length 
                - 4 // 3 текстуры загружаются до загрузчика (шрифты и логотип)
                + 1; // Финишный такт
        }

        /// <summary>
        /// Запуск загрузчика
        /// </summary>
        public void LoadStart()
        {
            Task.Factory.StartNew(() =>
            {
                // Опции
                Setting.Load();
                Language.SetLanguage((AssetsLanguage)Setting.Language);
                OnTick(new ObjectKeyEventArgs(ObjectKey.LoadStep));

                // Загрузка семплов
                foreach (MvkServer.Sound.AssetsSample key in Enum.GetValues(typeof(MvkServer.Sound.AssetsSample)))
                {
                    client.Sample.InitializeSample(key);
                    OnTick(new ObjectKeyEventArgs(ObjectKey.LoadStep));
                }

                // Atlas
                BufferedImage buffered = new BufferedImage(AssetsTexture.Atlas, Assets.Atlas);
                //buffered.SetImages(new BufImage[]
                //{
                //    new BufImage(Assets.Atlas8),
                //    new BufImage(Assets.Atlas4),
                //    new BufImage(Assets.Atlas2),
                //    new BufImage(Assets.Atlas1),
                //});

                OnTick(new ObjectKeyEventArgs(ObjectKey.LoadStepTexture, buffered));

                int i = 0;
                foreach (AssetsTexture key in Enum.GetValues(typeof(AssetsTexture)))
                {
                    i++;
                    if (i < 6) continue;
                    OnTick(new ObjectKeyEventArgs(ObjectKey.LoadStepTexture, new BufferedImage(key, Assets.GetBitmap(key))));
                }
                //System.Threading.Thread.Sleep(2000); // Тест пауза чтоб увидеть загрузчик
                OnTick(new ObjectKeyEventArgs(ObjectKey.LoadedMain));
            });
        }

        /// <summary>
        /// Событие такта
        /// </summary>
        public event ObjectKeyEventHandler Tick;
        protected virtual void OnTick(ObjectKeyEventArgs e) => Tick?.Invoke(this, e);
    }
}

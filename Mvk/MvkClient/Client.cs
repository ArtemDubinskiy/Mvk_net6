﻿using MvkAssets;
using MvkClient.Actions;
using MvkClient.Audio;
using MvkClient.Entity;
using MvkClient.Gui;
using MvkClient.Network;
using MvkClient.Renderer;
using MvkClient.Setitings;
using MvkClient.Util;
using MvkClient.World;
using MvkServer;
using MvkServer.Glm;
using MvkServer.Network;
using MvkServer.Network.Packets.Client;
using MvkServer.Util;
using SharpGL;
using System;
using System.Diagnostics;

namespace MvkClient
{
    public class Client
    {
        /// <summary>
        /// Объект лога
        /// </summary>
        public Logger Log { get; protected set; }
        /// <summary>
        /// Клиентский объект мира
        /// </summary>
        public WorldClient World { get; protected set; }
        /// <summary>
        /// Объект звуков
        /// </summary>
        public AudioBase Sample { get; protected set; } = new AudioBase();
        /// <summary>
        /// Увеличивается каждый тик 
        /// </summary>
        public uint TickCounter { get; protected set; } = 0;
        /// <summary>
        /// Screen Gui
        /// </summary>
        public GuiScreen Screen { get; private set; }
        /// <summary>
        /// Объект клиента
        /// </summary>
        public EntityPlayerSP Player { get; protected set; }
        /// <summary>
        /// Пинг к серверу
        /// </summary>
        public int Ping { get; protected set; } = -1;

        /// <summary>
        /// Счётчик тиков без синхронизации с сервером, отсчёт от запуска программы
        /// </summary>
        protected uint tickCounterClient = 0;
        /// <summary>
        /// Тикер Fps
        /// </summary>
        protected Ticker tickerFps;
        /// <summary>
        /// Тикер Fps
        /// </summary>
        protected Ticker tickerTps;
        /// <summary>
        /// Локальный сервер
        /// </summary>
        protected LocalServer locServer;
        /// <summary>
        /// Объект работы с пакетами
        /// </summary>
        protected ProcessClientPackets packets;
        /// <summary>
        /// Закрывается ли окно
        /// </summary>
        protected bool isClosing = false;
        /// <summary>
        /// Пауза в игре
        /// </summary>
        protected bool isGamePaused = false;
        /// <summary>
        /// Объект времени с момента запуска проекта
        /// </summary>
        private static Stopwatch stopwatch = new Stopwatch();
        /// <summary>
        /// Атрибут запуска управления мыши
        /// </summary>
        protected bool firstMouse;
        /// <summary>
        /// Режим 3д управление мышки
        /// </summary>
        protected bool isMouseGamePlay = true;

        #region EventsWindow

        /// <summary>
        /// Создание окна
        /// #1
        /// </summary>
        public void Initialize()
        {
            //Log = new Logger("client");
            
            Sample.Initialize();
            glm.Initialized();
            MvkStatic.Initialized();
            Screen = new GuiScreen(this);
            Screen.Changed += Screen_Changed;
            packets = new ProcessClientPackets(this);

            tickerFps = new Ticker();
            tickerFps.Tick += TickerFps_Tick;
            tickerFps.Closeded += (sender, e) => OnCloseded();

            tickerTps = new Ticker();
            tickerTps.SetWishTick(20);
            tickerTps.Tick += TickerTps_Tick;

            locServer = new LocalServer();
            locServer.ObjectKeyTick += Server_ObjectKeyTick;
            locServer.RecievePacket += (sender, e) => packets.ReceiveBufferClient(e.Packet.Bytes);
        }

        /// <summary>
        /// Загружено окно
        /// #3
        /// </summary>
        public void WindowLoad()
        {
            tickerFps.Start();
            
            // Загрузка
            Loading loading = new Loading(this);
            Screen.LoadingSetMax(loading.Count);
            loading.Tick += (sender, e) => OnThreadSend(e);
            loading.LoadStart();
        }

        /// <summary>
        /// Начало закрытия окна
        /// </summary>
        /// <returns>true - отменить закрытие</returns>
        public bool WindowClosing()
        {
            isClosing = true;
            //Log.Close();
            if (locServer.IsStartWorld)
            {
                ExitingWorld("");
                return true;
            }
            if (tickerFps.IsRuning)
            {
                tickerFps.Stoping();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Деактивация окна
        /// </summary>
        public void WindowDeactivate()
        {
            // Если режим активного управления
            // Выходим из режима управления 3д
            MouseGamePlay(false);
            // запускаем меню игры
            // Screen.InGameMenu();
        }

        /// <summary>
        /// Контрол OpenGL в окне стал активным
        /// </summary>
        public void WindowGLEnter()
        {
            
        }

        /// <summary>
        /// Инициализировать, первый запуск OpenGL
        /// #2
        /// </summary>
        public void GLInitialize(OpenGL gl)
        {
            stopwatch.Start();
            GLWindow.Initialize(gl);
            Screen.Begin();
        }

        /// <summary>
        /// Прорисовка каждого кадра
        /// </summary>
        public void GLDraw() => GLWindow.Draw(this);

        /// <summary>
        /// Изменён размер окна
        /// </summary>
        public void GLResized(int width, int height)
        {
            GLWindow.Resized(width, height);
            Screen.Resized();
            if (IsGamePlay && Player != null) Player.UpProjection();
        }

        /// <summary>
        /// Нажата клавиша
        /// </summary>
        /// <param name="key">индекс клавиши</param>
        public void KeyDown(int key)
        {
            //locServer.TrancivePacket(new PacketTFFTest(key.ToString()));
            Debug.DInt = key;

            if (World != null && IsGamePlayAction())
            {
                if (key == 9) MouseGamePlay(false); // Tab
                else if (isMouseGamePlay) World.Key.Down(key);
            }
            else if (key == 114) Debug.IsDraw = !Debug.IsDraw; // F3
        }
        
        /// <summary>
        /// Нажата клавиша в char формате
        /// </summary>
        public void KeyPress(char key)
        {
            Screen.KeyPress(key);
        }

        /// <summary>
        /// Отпущена клавиша
        /// </summary>
        /// <param name="key">индекс клавиши</param>
        public void KeyUp(int key)
        {
            if (World != null && isMouseGamePlay) World.Key.Up(key);
        }

        /// <summary>
        /// Активация или деактивация управление мыши от одного лица в 3д
        /// </summary>
        /// <param name="action">true - активация</param>
        public void MouseGamePlay(bool action)
        {
            if (IsGamePlayAction() && isMouseGamePlay != action)
            {
                isMouseGamePlay = action;
                if (isMouseGamePlay) firstMouse = true;
                CursorShow(!action);
            }
            if (Player != null && !action) Player.InputNone();
        }

        /// <summary>
        /// Нажатие клавиши мышки
        /// </summary>
        public void MouseDown(MouseButton button, int x, int y)
        {
            // Если надо, то включаем режим управления 3д
            MouseGamePlay(true);
            // Действия клика мышки передаём в GUI скрина
            if (!Screen.MouseDown(button, x, y))
            {
                if (World != null && Player != null && IsGamePlayAction())
                {
                    World.MouseDown(button);
                    if (button == MouseButton.Left)
                    {
                        vec3 pos = Player.GetPositionFrame();
                        pos.y += Player.GetEyeHeightFrame();
                        vec3 dir = Player.RayLook;//.GetLookFrame();

                        MovingObjectPosition moving = World.RayCast(pos, dir, 10f);
                        MovingObjectPosition movingE = World.RayCastEntity();

                        Player.Action();
                        // луч
                        Debug.DStr = moving.ToString() + "\r\n" + movingE.ToString(); // + " --" + string.Format("{0:0.00}",tf);
                    }
                }
            }
        }

        /// <summary>
        /// Отпущена клавиша мышки
        /// </summary>
        public void MouseUp(MouseButton button, int x, int y)
        {
            Screen.MouseUp(button, x, y);
        }

        /// <summary>
        /// Перемещение мышки
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="deltaX">растояние по X от центра</param>
        /// <param name="deltaY">растояние по Y от центра</param>
        /// <returns>true - сбросить на центр</returns>
        public bool MouseMove(int x, int y, int deltaX, int deltaY)
        {
            if (IsGamePlayAction() && isMouseGamePlay && Player != null)
            {
                if (firstMouse)
                {
                    firstMouse = false;
                    deltaX = 0;
                    deltaY = 0;
                }
                Player.MouseMove(deltaX, deltaY);
                return true;
            }
            Screen.MouseMove(x, y);
            return false;
        }

        /// <summary>
        /// Вращение колёсика мыши
        /// </summary>
        /// <param name="delta">смещение</param>
        public void MouseWheel(int delta, int x, int y)
        {

        }

        #endregion

        /// <summary>
        /// Задать желаемый фпс
        /// </summary>
        public void SetWishFps(int fps) => tickerFps.SetWishTick(fps);
        /// <summary>
        /// Получить желаемый фпс
        /// </summary>
        public int GetWishFps() => tickerFps.WishTick;

        /// <summary>
        /// Получить события из других пакетов
        /// </summary>
        public void ThreadReceive(ObjectKeyEventArgs e)
        {
            switch(e.Key)
            {
                case ObjectKey.LoadStep: Screen.LoadingStep(); break; // Шаг для загрузчиков
                case ObjectKey.LoadStepTexture:  // Шаг загрузки текстуры
                    GLWindow.Texture.InitializeKey(e.Tag as BufferedImage);
                    Screen.LoadingStep();
                    break;
                case ObjectKey.LoadedMain: Screen.LoadingMainEnd(); break; // Закончена первоночальная загрузка
                case ObjectKey.ServerStoped: // Мир сервера остановлен
                    if (e.Tag == null || e.Tag.ToString() == "")
                    {
                        if (isClosing) WindowClosing(); else Screen.MainMenu();
                    }
                    else Screen.ScreenError(e.Tag.ToString()); // выход с ошибкой
                    break;
                case ObjectKey.Error: Screen.ScreenError(e.Tag.ToString()); break;// Ошибка
                case ObjectKey.RenderDebug: Debug.RenderDebug(); break;
                case ObjectKey.GameMode: CursorShow(false); break;
                case ObjectKey.GameOver: Screen.GameOver(e.Tag.ToString()); break;
            }
        }

        /// <summary>
        /// Загрузить сетевой мир
        /// </summary>
        /// <param name="ip">адрес</param>
        public void LoadWorldNet(string ip)
        {
            Screen.ScreenProcess(Language.T("gui.process"));
            locServer.StartServerNet(ip);
            BeginWorld();
        }
        /// <summary>
        /// Загрузить мир
        /// </summary>
        /// <param name="slot">Номер слота</param>
        public void LoadWorld(int slot)
        {
            locServer.StartServer(slot);
            BeginWorld();

            //TODO:: надо отсюда начать запускать сервер, который создаст мир, и продублирует на клиенте мир.
            // Продумать tps только на стороне сервера, но должна быть сенхронизация с клиентом
            // Синхронизация времени раз в секунду
        }

        protected void BeginWorld()
        {
            World = new WorldClient(this);
            //World.GuiGameOver += World_GuiGameOver;
        }

        private void World_GuiGameOver(object sender, EventArgs e)
        {
           // Screen.GameOver();
            //throw new NotImplementedException();
        }

        private void Server_ObjectKeyTick(object sender, ObjectKeyEventArgs e)
        {
            if (e.Key == ObjectKey.LoadCountWorld)
            {
                Screen.LoadingSetMax((int)e.Tag);
            }
            else
            {
                OnThreadSend(e);
            }
        }

        /// <summary>
        /// Мир загружен один игрок
        /// </summary>
        public void LoadedWorld()
        {
            // ставим экран загрузки
            Screen.ScreenProcess(Language.T("gui.process"));
        }

        /// <summary>
        /// Выход с мира
        /// </summary>
        /// <param name="error">ошибка</param>
        public void ExitingWorld(string error)
        {
            tickerTps.Stoping();
            StringDebugTps();
            // ставим экран сохранения
            Screen.ScreenProcess(Language.T("gui.saving"));
            // отправялем на сервер, выход мира, с возможной ошибкой
            locServer.ExitingWorld(error);
            World.StopWorldDelete();
            World = null;
        }

        /// <summary>
        /// Убрать Gui, переход в режим игры при старте
        /// </summary>
        public void GameModeBegin()
        {
            tickerTps.Start();
            GameMode();
        }

        /// <summary>
        /// Убрать Gui, переход в режим игры
        /// </summary>
        public void GameMode()
        {
            Screen.GameMode();
            OnThreadSend(new ObjectKeyEventArgs(ObjectKey.GameMode));
        }

        /// <summary>
        /// Задать GUI
        /// </summary>
        /// <param name="key">Вариант</param>
        public void SetScreen(ObjectKey key) => OnThreadSend(new ObjectKeyEventArgs(key));
        /// <summary>
        /// Задать GUI
        /// </summary>
        /// <param name="key">Вариант</param>
        /// <param name="obj">Дополнительный объект</param>
        public void SetScreen(ObjectKey key, object obj) => OnThreadSend(new ObjectKeyEventArgs(key, obj));

        /// <summary>
        /// Задать время с сервера
        /// </summary>
        public void SetTickCounter(uint time) => TickCounter = time;

        /// <summary>
        /// Отправить пакет на сервер
        /// </summary>
        public void TrancivePacket(IPacket packet) => locServer.TrancivePacket(packet);

        /// <summary>
        /// Запущен ли локальный сервер
        /// </summary>
        public bool IsServerLocalRun() => locServer.IsLoacl ? locServer.IsStartWorld : false;
        /// <summary>
        /// Открыта ли сеть
        /// </summary>
        public bool IsOpenNet() => locServer.IsOpenNet();
        /// <summary>
        /// Открыть сеть
        /// </summary>
        public void OpenNet() => locServer.OpenNet();
        /// <summary>
        /// Режим игры
        /// </summary>
        public bool IsGamePlay => tickerTps.IsRuning;
        /// <summary>
        /// Режим игры, режим активного управления 3d
        /// </summary>
        public bool IsGamePlayAction() => IsGamePlay && Screen.IsEmptyScreen();
        /// <summary>
        /// Задать время пинга
        /// </summary>
        public void SetPing(long time) => Ping = (Ping * 3 + (int)(Time() - time)) / 4;

        /// <summary>
        /// Дебага, формируется по запросу
        /// </summary>
        protected void StringDebugTps() => Debug.strClient = (!IsGamePlay || World == null) ? "" : "ping: " + Ping + " ms\r\n" + World.ToStringDebug();

        /// <summary>
        /// Такт каждого ФПС
        /// </summary>
        private void TickerFps_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!Screen.IsEmptyScreen())
                {
                    // GUI что-то есть
                    OnThreadSend(new ObjectKeyEventArgs(ObjectKey.RenderDebug));
                }
                OnDraw();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Локальный ТПС 20
        /// </summary>
        private void TickerTps_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!isGamePaused)
                {
                    //Log.Log(TickCounter.ToString());
                    tickCounterClient++;
                    TickCounter++;

                    // Обновить игрока
                    Player.Update();
                    if ((Player.IsDead || Player.Health == 0) && !Screen.IsScreenGameOver())
                    {
                        // GameOver надо указать причину смерти
                        SetScreen(ObjectKey.GameOver, "Boom");
                    }

                    try
                    {
                        World.Tick();
                    }
                    catch (Exception ex)
                    {
                        //Log.Error("Server.Error.Tick {0}", ex.Message);
                        throw;
                    }

                    try
                    {
                        World.UpdateEntities();
                    }
                    catch (Exception ex)
                    {
                        //Log.Error("Server.Error.UpdateEntities {0}", ex.Message);
                        throw;
                    }

                    if (tickCounterClient % 4 == 0)
                    {
                        // лог статистика за это время

                        if (MvkGlobal.IS_DRAW_DEBUG_CHUNK)
                        {
                            // отладка чанков
                            Debug.ListChunks.listChunkPlayer = World.ChunkPr.GetListDebug();
                            Debug.ListChunks.isRender = true;
                        }
                    }

                    if (tickCounterClient % 40 == 0)
                    {
                        // Раз в секунду перепинговка
                        TrancivePacket(new PacketC00Ping(Time()));
                    }
                }

                StringDebugTps();
                if (Screen.IsEmptyScreen())
                {
                    // GUI нет
                    OnThreadSend(new ObjectKeyEventArgs(ObjectKey.RenderDebug));
                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }



        /// <summary>
        /// Изменён GUI скрин
        /// </summary>
        private void Screen_Changed(object sender, EventArgs e)
        {
            // определить паузу
            isGamePaused = IsGamePlay && Screen.IsScreenPause() && !locServer.IsOpenNet() && locServer.IsLoacl;
            locServer.SetGamePauseSingle(isGamePaused);

            if (IsGamePlay && Screen.IsEmptyScreen())
            {
                SetWishFps(Setting.Fps);
                // Центрирование мыши
                firstMouse = true;
                CursorShow(false);
            } else
            {
                CursorShow(true);
            }
        }

        /// <summary>
        /// Включить или выключить курсор
        /// </summary>
        protected void CursorShow(bool bShow)
        {
            OnCursorClipBounds(!bShow);
            if ((!bShow && CursorExtensions.IsVisible()) || (bShow && !CursorExtensions.IsVisible()))
            {
                CursorExtensions.Show(bShow);
            }
        }

        /// <summary>
        /// Получить время в милисекундах с момента запуска проекта
        /// </summary>
        public static long Time() => stopwatch.ElapsedMilliseconds;

        #region Player

        /// <summary>
        /// Создать Игрока
        /// </summary>
        public void PlayerCreate(WorldClient world) => Player = new EntityPlayerSP(world);
        /// <summary>
        /// Удалить игрока
        /// </summary>
        public void PlayerRemove() => Player = null;

        #endregion

        #region Event

        /// <summary>
        /// Событие прорисовка кадра
        /// </summary>
        public event EventHandler Draw;
        protected virtual void OnDraw() => Draw?.Invoke(this, new EventArgs());

        /// <summary>
        /// Событие закрыть
        /// </summary>
        public event EventHandler Closeded;
        protected virtual void OnCloseded() => Closeded?.Invoke(this, new EventArgs());

        /// <summary>
        /// Из потока в основной поток
        /// </summary>
        public event ObjectKeyEventHandler ThreadSend;
        protected virtual void OnThreadSend(ObjectKeyEventArgs e) => ThreadSend?.Invoke(this, e);

        /// <summary>
        /// Событие Курсор только в окне
        /// </summary>
        public event CursorEventHandler CursorClipBounds;
        protected virtual void OnCursorClipBounds(bool isBounds) => CursorClipBounds?.Invoke(this, new CursorEventArgs(isBounds));

        #endregion
    }
}

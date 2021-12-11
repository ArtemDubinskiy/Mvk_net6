﻿namespace MvkClient.Util
{
    public delegate void ObjectEventHandler(object sender, ObjectEventArgs e);
    public class ObjectEventArgs
    {
        /// <summary>
        /// Вспомогательный объект при необходимости
        /// </summary>
        public object Tag { get; protected set; }
        /// <summary>
        /// Ключ запроса объекта
        /// </summary>
        public ObjectKey Key { get; protected set; }

        public ObjectEventArgs(ObjectKey key) => Key = key;
        public ObjectEventArgs(ObjectKey key, object obj) : this(key) => Tag = obj;
    }

    /// <summary>
    /// Перечень ключей запросов
    /// </summary>
    public enum ObjectKey
    {
        /// <summary>
        /// Шаг загрузки
        /// </summary>
        LoadStep,
        /// <summary>
        /// Шаг загрузки текстуры
        /// </summary>
        LoadStepTexture,
        /// <summary>
        /// Закончена основная загрузка
        /// </summary>
        LoadingStopMain,
        /// <summary>
        /// Закончена загрузка мира
        /// </summary>
        LoadingStopWorld,
        /// <summary>
        /// Количество тактов в загрузчике
        /// </summary>
        LoadingCountWorld,
        /// <summary>
        /// Сервер остановлен
        /// </summary>
        ServerStoped,
        /// <summary>
        /// Ошибка
        /// </summary>
        Error
    }
}

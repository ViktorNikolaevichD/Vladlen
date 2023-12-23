using System.Diagnostics;

namespace Vladlen
{
    class Program
    {
        static void Main(string[] args)
        {
            MPI.Environment.Run(ref args, comm =>
            {
            // Экземпляр локальной базы данных
            LocalDataBase localDb = new LocalDataBase();
            // Предзагрузка базы данных
            if (comm.Rank == 0)
            {
                // 0 процесс загружает локальную базу данных
                localDb = Commands.DownloadDb(comm.Rank, comm.Size);
            }
            // Ожидание всем процессами завершения загрузки базы данных 0 процессом
            comm.Barrier();
            if (comm.Rank != 0)
            {
                // все !0 процессы загружают локальную базу данных
                localDb = Commands.DownloadDb(comm.Rank, comm.Size);
            }
            // Объект для измерения времени
            Stopwatch stopWatch = new Stopwatch();
            // Переменная для команд пользователя
            string? command = null;
            // Бесконечное ожидание команд и их выполнение, пока не quit
            while (command != "quit")
            {
                // Выполняет 0 процесс
                if (comm.Rank == 0)
                {
                    // Список команд в консоль
                    Console.Write("Введите команду: ");
                    // Команда пользователя
                    command = Console.ReadLine();
                }
                // Рассылка команды из 0 процесса по остальным процессам
                comm.Broadcast(ref command, 0);
                switch (command)
                {
                    // Генерация данных в БД
                    case "generate":
                        // Количество строк
                        int count = 0;
                        if (comm.Rank == 0)
                        {
                            Console.Write("Какое количество строк сгенерировать: ");
                            count = Convert.ToInt32(Console.ReadLine());

                            // Сообщить о начале генерации
                            Console.WriteLine("Начинаем генерацию");
                        }
                        // Начать замер времени всеми процессами
                        stopWatch.Restart();
                        if (comm.Rank == 0)
                            // Генерация данных
                            Commands.GenerateData(count);
                        // Дождаться генерации базы данных 0 процессом
                        comm.Barrier();
                        // Обновление локальной базы данных всеми процессами
                        localDb = Commands.DownloadDb(comm.Rank, comm.Size);
                        // Завершить замер времени
                        stopWatch.Stop();
                        if (comm.Rank == 0)
                            Console.WriteLine("Данные сгенерированы");
                        break;
                        // Любая неизвестная команда
                        default:
                            if (comm.Rank == 0 && command != "quit")
                                Console.WriteLine("Неизвестная команда");
                            break;
                    }
                    // Собрать все процессы в одном месте, чтобы одновременно вывести
                    comm.Barrier();
                    // Вывод времени работы в консоль каждым процессом
                    TimeSpan ts = stopWatch.Elapsed;
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                    Console.WriteLine($"RunTime {comm.Rank} " + elapsedTime);
                    // Подождать окончания вывода замеров всеми процессами
                    comm.Barrier();
                }
            });
        }
    }
}
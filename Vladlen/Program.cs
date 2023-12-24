using Vladlen.Entities;
using System.Diagnostics;
using System.Text.Json;

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
                        Console.Write(
                            "Введите команду" +
                            "\nupdate - обновить базу данных;" +
                            "\ncost - изменить стоимость продукта;" +
                            "\ndelete - удалить документ (отмена сделки);" +
                            "\ndocuments - вывести документы о покупках;" +
                            "\nsell - продать продукт;" +
                            "\nclient - вывести информацию о клиенте;" +
                            "\ngenerate - сгенерировать данные;" +
                            "\nquit - выйти: "
                            );
                        // Команда пользователя
                        command = Console.ReadLine();
                    }
                    // Рассылка команды из 0 процесса по остальным процессам
                    comm.Broadcast(ref command, 0);
                    switch (command)
                    {
                        // Обновление базы данных на сервере
                        case "update":
                            stopWatch.Restart();
                            if (comm.Rank == 0)
                                Console.WriteLine("Обновляем базу");
                            // Обновление базы данных
                            Commands.UpdateDataBase(localDb);
                            if (comm.Rank == 0)
                                Console.WriteLine("База данных обновлена");
                            stopWatch.Stop();
                            break;
                        // Обновление стоимости товара
                        case "cost":
                            // Айди продукта
                            int productId = 0;
                            // Новая стоимость
                            int newCost = 0;
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите Id продукта, чтобы изменить стоимость: ");
                                productId = Convert.ToInt32(Console.ReadLine());
                                Console.Write("Введите новую стоимость продукта: ");
                                newCost = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("Обновляем стоимость");
                            }
                            // Разослать по процессам айди продукта
                            comm.Broadcast(ref productId, 0);
                            // Разослать по процессам новую стоимость продукта
                            comm.Broadcast(ref newCost, 0);
                            // Изменить стоимость продукта
                            Commands.EditCost(localDb, productId, newCost);
                            if (comm.Rank == 0)
                                Console.WriteLine("Стоимость обновлена");
                            break;
                        // Удалить документ о продаже (отмена сделки)
                        case "delete":
                            int documentId = 0;
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите Id документа, который хотите удалить: ");
                                documentId = Convert.ToInt32(Console.ReadLine());
                            }
                            stopWatch.Restart();
                            if (comm.Rank == 0)
                            {
                                // Удалить документ о покупке
                                Document? document = Commands.DeleteDocument(documentId);
                                if (document == null)
                                {
                                    Console.WriteLine("Такого документа не нашлось");
                                    stopWatch.Stop();
                                    break;
                                }
                                Console.WriteLine($"Удален документ:" +
                                    $"\nId: {document.DocumentId} Product: {document.Product.ProductName} Cost: {document.Product.Cost} Count: {document.Count}" +
                                    $"\nClientName: {document.Client.FullName}"); 
                            }
                            // Все процессы дожидаются удаления данных
                            comm.Barrier();
                            // Обновление локальной базы данных после обновления
                            localDb = Commands.DownloadDb(comm.Rank, comm.Size);
                            stopWatch.Stop();
                            break;
                        // Продать продукт
                        case "sell":
                            // Айди клиента
                            int clientId = 0;
                            // Айди продукта
                            productId = 0;
                            // Количество товара
                            int countProduct = 0;
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите Id клиента для вывода документов: ");
                                clientId = Convert.ToInt32(Console.ReadLine());
                                Console.Write("Введите Id продукта для оформления документов: ");
                                productId = Convert.ToInt32(Console.ReadLine());
                                Console.Write("Введите количество продуктов: ");
                                countProduct = Convert.ToInt32(Console.ReadLine());

                                // Продать продукт пользователю
                                Commands.SellProduct(clientId, productId, countProduct);
                            }
                            // Все процессы дожидаются добавления документов
                            comm.Barrier();
                            // Обновить локальную базу данных
                            localDb = Commands.DownloadDb(comm.Rank, comm.Size);
                            stopWatch.Stop();
                            break;
                        // Вывести документы о покупках
                        case "documents":
                            // Айди клиента
                            clientId = 0;
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите Id клиента для вывода документов: ");
                                clientId = Convert.ToInt32(Console.ReadLine());
                            }
                            // Разослать всем процессам Id клиента
                            comm.Broadcast(ref clientId, 0);
                            stopWatch.Restart();
                            if (comm.Rank == 0)
                            {
                                // Собрать в переменную данные со всех процессов
                                var gatherData = comm.Gather(JsonSerializer.Serialize(Commands.GetDocuments(localDb, clientId)), 0);
                                // Десериализовать данные
                                List<Document> documentsInfo = gatherData
                                .Select(x => JsonSerializer.Deserialize<List<Document>>(x)!)
                                .Where(x => x != null)
                                .Aggregate((a, b) => a.Concat(b).ToList());
                                
                                // Проверка, чтобы результат был не пустой
                                if (!documentsInfo.Any() || documentsInfo.Count() < 1)
                                {
                                    Console.WriteLine("Документов не найдено");
                                    stopWatch.Stop();
                                    break;
                                }
                                // Вывод данных о документах
                                foreach(var document in documentsInfo)
                                {
                                    Console.WriteLine($"{document.DocumentId} {document.Product.ProductName} {document.Count}");
                                }
                            }
                            else
                            {
                                // Собрать в 0 процессе данные о запрошенных документах
                                comm.Gather(JsonSerializer.Serialize(Commands.GetDocuments(localDb, clientId)), 0);
                            }
                            stopWatch.Stop();
                            break;
                        // Вывести данные о клиенте по Id
                        case "client":
                            // Айди клиента
                            clientId = 0;
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите Id клиента для вывода информации: ");
                                clientId = Convert.ToInt32(Console.ReadLine());
                            }
                            // Разослать всем процессам Id клиента
                            comm.Broadcast(ref clientId, 0);
                            stopWatch.Restart();
                            if (comm.Rank == 0)
                            {
                                // Собрать в переменную данные со всех процессов
                                var gatherData = comm.Gather(JsonSerializer.Serialize(Commands.GetClient(localDb, clientId)), 0);
                                // Отсеить null значения и получить строчку клиента или null значение(если пользователя нет нигде)
                                Client? clientInfo = gatherData
                                .Select(x => JsonSerializer.Deserialize<Client>(x))
                                .Where(x => x != null)
                                .FirstOrDefault();

                                if (clientInfo == null)
                                {
                                    Console.WriteLine("Клиента не найдено");
                                    stopWatch.Stop();
                                    break;
                                }
                                // Вывод данных о пользователе
                                Console.WriteLine($"Id: {clientInfo.ClientId} FullName: {clientInfo.FullName} PhoneNumber: {clientInfo.PhoneNumber}");
                            }
                            else
                            {
                                // Собрать в 0 процессе данные о запрошенном клиенте
                                comm.Gather(JsonSerializer.Serialize(Commands.GetClient(localDb, clientId)), 0);
                            }
                            stopWatch.Stop();
                            break;
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
                    if (comm.Rank == 0)
                    {
                        // Вывод времени работы в консоль 0 процессом
                        TimeSpan ts = stopWatch.Elapsed;
                        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                        Console.WriteLine($"RunTime {comm.Rank} " + elapsedTime);
                    }
                    comm.Barrier();
                }
            });
        }
    }
}
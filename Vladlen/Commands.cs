using Microsoft.EntityFrameworkCore;
using Vladlen.Entities;

namespace Vladlen
{
    public class Commands
    {
        // Обновить базу данных на сервере
        public static void UpdateDataBase(LocalDataBase localDb)
        {
            using (var db = new AppDbContext())
            {
                // Обновить таблицу клиентов
                db.Clients.UpdateRange(localDb.Clients);
                // Обновить таблицу продуктов
                db.Products.UpdateRange(localDb.Products);
                // Обновить таблицу документов
                db.Documents.UpdateRange(localDb.Documents);
                
                // Зафиксировать измения в БД
                db.SaveChanges();
            }
        }
        // Изменить стоимость продукта
        public static void EditCost(LocalDataBase localDb, int idProduct, decimal newCost)
        {
            // Продукт в таблице Products
            var product = localDb.Products.Where(p => p.ProductId == idProduct).FirstOrDefault();
            // Если нашлась строчка с продуктом
            if (product != null)
            {
                // Установить новую цену
                product.Cost = newCost;
            }
            // Продукт в таблице Documents
            var productFromDocuments = localDb.Documents.Where(p => p.ProductId == idProduct).Select(u => u.Product).FirstOrDefault();
            // Если нашлась строчка с продуктом в таблице Documents
            if (productFromDocuments != null)
            {
                // установить новую цену
                productFromDocuments.Cost = newCost;
            }
        }
        // Удалить документ о покупке
        public static Document? DeleteDocument(int documentId)
        {
            using (var db = new AppDbContext())
            {
                // Найти документа в таблице
                var document = db.Documents
                    .Include(u => u.Client)
                    .Include(u => u.Product)
                    .Where(p => p.DocumentId == documentId)
                    .FirstOrDefault();
                // Если документа не нашлось
                if (document == null) 
                {
                    return null;
                }
                // Удалить документ из базы данных на сервере
                db.Documents.Remove(document);
                // Зафиксировать изменения
                db.SaveChanges();
                return document;
            } 
        }
        // Продать пользователю по Id товар по Id с определенным количеством
        public static void SellProduct(int clientId, int productId, int countProducts)
        {
            using (var db = new AppDbContext())
            {
                // Получить клиента из локальной базы данных
                var client = db.Clients.Where(p => p.ClientId == clientId).FirstOrDefault();
                // Если клиента не нашлось, то выйти из функции
                if (client == null)
                {
                    return;
                }
                // Получить продукт из локальной базы данных
                var product = db.Products.Where(p => p.ProductId == productId).FirstOrDefault();
                // Если продкта не нашлось, то выйти из функции
                if (product == null)
                {
                    return;
                }
                // Добавить в базу данных документ о продаже продукции
                db.Documents.Add(new Document { ClientId = clientId, ProductId = productId, Count = countProducts });
                // Зафиксировать изменения в базе данных
                db.SaveChanges();
            }
        }
        // Вывести документы о покупках клиента
        public static List<Document> GetDocuments(LocalDataBase localDb, int clientId)
        {
            // Найти в локальной базе данных заказы клиента по Id. Вернуть все в виде списка
            return localDb.Documents.Where(u => u.ClientId == clientId).ToList();
        }
        // Найти клиента по Id
        public static Client? GetClient(LocalDataBase localDb, int clientId)
        {
            // Найти в локальной базе данных клиента по Id. Если не нашлось, то вернуть null
            return localDb.Clients.Where(p => p.ClientId == clientId).FirstOrDefault();
        }
        // Загрузить базу данных
        public static LocalDataBase DownloadDb(int rank, int size)
        {
            using (var db = new AppDbContext())
            {
                // Число строк в таблицах
                int countClients = db.Clients.Count();
                int countProducts = db.Products.Count();
                int countDocuments = db.Documents.Count();

                // Число строк для взятия из таблицы каждым процессом
                int takeClients = (countClients / size + 1);
                int takeProducts = (countProducts / size + 1);
                int takeDocuments = (countDocuments / size + 1);

                // Число строк для пропуска в таблице каждым процессом
                int skipClients = rank * takeClients;
                int skipProducts = rank * takeProducts;
                int skipDocuments = rank * takeDocuments;

                return new LocalDataBase
                {
                    Clients = db.Clients.Skip(skipClients).Take(takeClients).ToList(),
                    Products = db.Products.Skip(skipProducts).Take(takeProducts).ToList(),
                    Documents = db.Documents.Include(p => p.Client)
                                .Include(p => p.Product).Skip(skipDocuments).Take(takeDocuments).ToList(),
                };
            }
        }
        // Сгенерировать данные в базе данных
        public static void GenerateData(int count)
        {
            // Подключение к базе данных
            using (var db = new AppDbContext())
            {
                // Найти максимальный Id, что продолжить список продуктов
                int maxId = 0;
                // Проверка на наличие хотя бы одного элемента в таблице
                if (db.Products.Any())
                    // Переприсвоить новое значение maxId
                    maxId = db.Products.Max(p => p.ProductId);
                
                // Локальный список клиентов
                List<Client> clients = new List<Client> { };
                // Локальный список продуктов
                List<Product> products = new List<Product> { };
                // Объект Random
                Random rand = new Random();
                // Цикл для генерации случайных пользователей и продуктов
                for (int i = 1; i < count + 1; i++)
                {
                    // Случайный клиент
                    Client client = new Client
                    {
                        // случайное ФИО
                        FullName = Faker.Name.FullName(),
                        // Случайный адрес
                        Adress = $"{Faker.Address.StreetName()} {Faker.Address.StreetAddress()}",
                        // Случайный номер телефона
                        PhoneNumber = Faker.Phone.Number()
                    };
                    // Добавить в базу данных случайного клиента
                    db.Clients.Add(client);
                    // Добавить в локальный список клиента
                    clients.Add(client);

                    // Случайный продукт
                    Product product = new Product
                    {
                        // Для 0 процесса -> Product{0 + 1 + 0 * count}
                        ProductName = $"Product{maxId + i}",
                        // Случайная стоимость товара
                        Cost = rand.Next(25, 256)
                    };
                    // Добавить в базу данных случайный продукт
                    db.Products.Add(product);
                    // Доабвить в локальный список продукт
                    products.Add(product);
                }
                // Зафиксировать изменения в базе данных
                db.SaveChanges();
                // Цикл для генерации случайных документов о покупке
                for (int i = 0;  i < count; i++)
                {
                    // Случайный клиент
                    var client = clients[rand.Next(0, clients.Count())];
                    // Случайный продукт
                    var product = products[rand.Next(0, products.Count())];
                    // Добавление в базу данных документов о покупке
                    db.Documents.Add(new Document
                    {
                        // Айди клиента
                        ClientId = client.ClientId,
                        // Айди продукта
                        ProductId = product.ProductId,
                        // Количество купленного товара
                        Count = rand.Next(0, 26)
                    });
                }
                // Сохранить изменения в базе данных
                db.SaveChanges();
            }
        }
    }
}

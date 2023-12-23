using Microsoft.EntityFrameworkCore;
using Vladlen.Entities;

namespace Vladlen
{
    public class Commands
    {
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
        public static void GenerateData(int count, int rank)
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
                
                // Цикл для генерации случайных пользователей и продуктов
                for (int i = 1; i < count + 1; i++)
                {
                    // Случайный клиент
                    Client client = new Client
                    {
                        // случайное ФИО
                        FullName = Faker.Name.FullName(),
                        // Случайный адрес
                        Adress = $"{Faker.Address.StreetName()} {Faker.Address.StreetAddress}",
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
                        // для 1 процесса -> Product{0 + 1 + 1 * count}
                        ProductName = $"Product{maxId + i + rank * count}"
                    };
                    // Добавить в базу данных случайный продукт
                    db.Products.Add(product);
                    // Доабвить в локальный список продукт
                    products.Add(product);
                }
                // Зафиксировать изменения в базе данных
                db.SaveChanges();
                // Объект Random
                Random rand = new Random();
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

using Vladlen.Entities;

namespace Vladlen
{
    public class Commands
    {
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

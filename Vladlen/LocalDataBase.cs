using Vladlen.Entities;

namespace Vladlen
{
    public class LocalDataBase
    {
        // Локальный список клиентов
        public List<Client> Clients {  get; set; }
        // Локальный список продуктов
        public List<Product> Products { get; set; }
        // Локальный список документов
        public List<Document> Documents { get; set; }
    }
}

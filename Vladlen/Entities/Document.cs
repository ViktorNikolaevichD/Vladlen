namespace Vladlen.Entities
{
    public class Document
    {
        // Айди документа
        public int DocumentId { get; set; }
        // Айди пользователя
        public int ClientId { get; set; }
        // Навигационное свойство на таблицу пользователей
        public Client Client { get; set; }
        // Айди продукта
        public int ProductId {  get; set; }
        // Навигационноу свойство на таблицу продуктов
        public Product Product { get; set; }
        // Количество проданного товара
        public int count { get; set; }

    }
}

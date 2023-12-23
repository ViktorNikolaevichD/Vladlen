namespace Vladlen.Entities
{
    public class Client
    {
        // Айди клиента в базе
        public int ClientId { get; set; }
        // ФИО клиента в базе
        public string FullName {  get; set; }
        // Адрес клиента
        public string Adress { get; set; }
        // Телефон клиента
        public string PhoneNumber { get; set; }
    }
}

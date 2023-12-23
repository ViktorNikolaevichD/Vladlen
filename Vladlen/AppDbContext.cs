using Microsoft.EntityFrameworkCore;
using Vladlen.Entities;

namespace Vladlen
{
    public class AppDbContext : DbContext
    {
        // Таблица клиентов
        public DbSet<Client> Clients { get; set; }
        // Таблица продуктов
        public DbSet<Product> Products { get; set; }
        // Таблица документов
        public DbSet<Document> Documents { get; set; }

        public AppDbContext()
        {
            // Существует ли база данных
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Строка подключения к базе данных
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=DbProductShop;Trusted_Connection=True;");
        }
    }
}

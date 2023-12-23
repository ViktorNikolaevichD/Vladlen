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
            });
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;


namespace Aegis.Data
{
    public class AppDbContext
    {
        private readonly string _connectionString;

        public AppDbContext()
        {
            _connectionString = "Host=localhost;Database=Aegis;Username=postgres;Password=Q4UMQ4XA";
        }

        public NpgsqlConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}

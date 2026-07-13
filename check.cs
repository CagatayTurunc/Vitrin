using System;
using Npgsql;

class Program
{
    static void Main()
    {
        string connStr = "Host=localhost;Database=vitrin_auth;Username=postgres;Password=123456";
        using var conn = new NpgsqlConnection(connStr);
        conn.Open();
        using var cmd = new NpgsqlCommand("SELECT column_name FROM information_schema.columns WHERE table_name = 'Users'", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            Console.WriteLine(reader.GetString(0));
        }
    }
}

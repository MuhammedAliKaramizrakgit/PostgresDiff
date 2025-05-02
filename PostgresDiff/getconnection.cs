using System;
using Npgsql;

public class GetDonnection
{
    private string _connectionString;

    public GetDonnection(string host, string database, string username, string password)
    {
        _connectionString = $"Host={host};Database={database};Username={username};Password={password};";
    }

    public bool CheckUserExists(string username)
    {
        bool userExists = false;
        try
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM pg_user WHERE usename = @username", conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    userExists = (long)cmd.ExecuteScalar() > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"User Check Error: {ex.Message}");
        }
        return userExists;
    }
}

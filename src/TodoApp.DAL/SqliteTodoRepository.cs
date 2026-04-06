using Microsoft.Data.Sqlite;
using TodoApp.Core.Interfaces;
using TodoApp.Core.Models;

namespace TodoApp.DAL;

public class SqliteTodoRepository : ITodoRepository
{
    private readonly string _connectionString;

    public SqliteTodoRepository(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
        Initialize();
    }

    private void Initialize()
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Todos (
                Id          TEXT NOT NULL PRIMARY KEY,
                Title       TEXT NOT NULL,
                IsCompleted INTEGER NOT NULL DEFAULT 0
            )
            """;
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<TodoItem> LoadAll()
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Title, IsCompleted FROM Todos ORDER BY rowid";

        var items = new List<TodoItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new TodoItem(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetInt32(2) != 0));
        }
        return items;
    }

    public void Add(TodoItem item)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO Todos (Id, Title, IsCompleted) VALUES ($id, $title, $completed)";
        cmd.Parameters.AddWithValue("$id", item.Id.ToString());
        cmd.Parameters.AddWithValue("$title", item.Title);
        cmd.Parameters.AddWithValue("$completed", item.IsCompleted ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void Update(TodoItem item)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Todos SET Title = $title, IsCompleted = $completed WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", item.Id.ToString());
        cmd.Parameters.AddWithValue("$title", item.Title);
        cmd.Parameters.AddWithValue("$completed", item.IsCompleted ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void Delete(Guid id)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Todos WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}

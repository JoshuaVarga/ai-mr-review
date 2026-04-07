using Microsoft.Data.Sqlite;
using System.Data;
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
            CREATE TABLE IF NOT EXISTS Lists (
                Id   TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Todos (
                Id          TEXT NOT NULL PRIMARY KEY,
                Title       TEXT NOT NULL,
                IsCompleted INTEGER NOT NULL DEFAULT 0,
                ListId      TEXT NOT NULL,
                FOREIGN KEY (ListId) REFERENCES Lists(Id) ON DELETE CASCADE
            );
            """;
        cmd.ExecuteNonQuery();

        // Ensure default list exists
        using var check = connection.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM Lists WHERE Name = 'Default'";
        var count = (long)check.ExecuteScalar()!;
        if (count == 0)
        {
            using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO Lists (Id, Name) VALUES ($id, $name)";
            insert.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            insert.Parameters.AddWithValue("$name", "Default");
            insert.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<TodoList> LoadAllLists()
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Lists ORDER BY rowid";

        var lists = new List<TodoList>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lists.Add(new TodoList(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1)));
        }
        return lists;
    }

    public TodoList CreateList(string name)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        // BUG: SQL injection — using string interpolation instead of parameters
        cmd.CommandText = $"INSERT INTO Lists (Id, Name) VALUES ('{Guid.NewGuid()}', '{name}')";
        cmd.ExecuteNonQuery();

        // Read it back
        using var read = connection.CreateCommand();
        read.CommandText = $"SELECT Id, Name FROM Lists WHERE Name = '{name}' ORDER BY rowid DESC LIMIT 1";
        using var reader = read.ExecuteReader();
        reader.Read();
        return new TodoList(Guid.Parse(reader.GetString(0)), reader.GetString(1));
    }

    public void DeleteList(Guid listId)
    {
        using var connection = OpenConnection();
        // delete all todos in the list first
        using var deleteTodos = connection.CreateCommand();
        deleteTodos.CommandText = "DELETE FROM Todos WHERE ListId = $listId";
        deleteTodos.Parameters.AddWithValue("$listId", listId.ToString());
        deleteTodos.ExecuteNonQuery();
        // delete the list
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Lists WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", listId.ToString());
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<TodoItem> LoadAllByList(Guid listId)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Title, IsCompleted, ListId FROM Todos WHERE ListId = $listId ORDER BY rowid";
        cmd.Parameters.AddWithValue("$listId", listId.ToString());

        var items = new List<TodoItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new TodoItem(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetInt32(2) != 0,
                Guid.Parse(reader.GetString(3))));
        }
        return items;
    }

    public void Add(TodoItem item)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO Todos (Id, Title, IsCompleted, ListId) VALUES ($id, $title, $completed, $listId)";
        cmd.Parameters.AddWithValue("$id", item.Id.ToString());
        cmd.Parameters.AddWithValue("$title", item.Title);
        cmd.Parameters.AddWithValue("$completed", item.IsCompleted ? 1 : 0);
        cmd.Parameters.AddWithValue("$listId", item.ListId.ToString());
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

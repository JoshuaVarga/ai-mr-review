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

        // Create tables
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Lists (
                    Id   TEXT NOT NULL PRIMARY KEY,
                    Name TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Todos (
                    Id          TEXT NOT NULL PRIMARY KEY,
                    Title       TEXT NOT NULL,
                    IsCompleted INTEGER NOT NULL DEFAULT 0,
                    ListId      TEXT NOT NULL DEFAULT '',
                    FOREIGN KEY (ListId) REFERENCES Lists(Id) ON DELETE CASCADE
                );
                """;
            cmd.ExecuteNonQuery();
        }

        // Ensure default list exists, capturing its ID for migration use.
        // ORDER BY rowid ensures the original row is returned deterministically
        // in the unlikely event a user-created list shares the name 'Default'.
        string defaultListId;
        using (var check = connection.CreateCommand())
        {
            check.CommandText = "SELECT Id FROM Lists WHERE Name = 'Default' ORDER BY rowid LIMIT 1";
            var existing = check.ExecuteScalar() as string;
            if (existing == null)
            {
                defaultListId = Guid.NewGuid().ToString();
                using var insert = connection.CreateCommand();
                insert.CommandText = "INSERT INTO Lists (Id, Name) VALUES ($id, $name)";
                insert.Parameters.AddWithValue("$id", defaultListId);
                insert.Parameters.AddWithValue("$name", "Default");
                insert.ExecuteNonQuery();
            }
            else
            {
                defaultListId = existing;
            }
        }

        // Migrate: add ListId column to Todos for pre-existing databases that predate this schema version.
        // On a fresh database the column already exists from CREATE TABLE above, so hasListId will be
        // true and the ALTER TABLE branch is never reached.
        // Note: the ALTER TABLE uses DEFAULT '' because SQLite requires a default for NOT NULL columns
        // added via ALTER TABLE. The empty string is safe here only because the backfill step below
        // immediately overwrites it with a valid list ID before any FK checks occur.
        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA table_info(Todos)";
            using var reader = pragma.ExecuteReader();
            var hasListId = false;
            while (reader.Read())
            {
                if (reader.GetString(1) == "ListId") { hasListId = true; break; }
            }

            if (!hasListId)
            {
                using var alter = connection.CreateCommand();
                alter.CommandText = "ALTER TABLE Todos ADD COLUMN ListId TEXT NOT NULL DEFAULT ''";
                alter.ExecuteNonQuery();

                using var backfill = connection.CreateCommand();
                backfill.CommandText = "UPDATE Todos SET ListId = $defaultListId WHERE ListId = ''";
                backfill.Parameters.AddWithValue("$defaultListId", defaultListId);
                backfill.ExecuteNonQuery();
            }
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
        var id = Guid.NewGuid();
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO Lists (Id, Name) VALUES ($id, $name)";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();

        return new TodoList(id, name);
    }

    public void DeleteList(Guid listId)
    {
        using var connection = OpenConnection();
        // Todos in this list are removed automatically via ON DELETE CASCADE;
        // foreign key enforcement is enabled in OpenConnection via PRAGMA foreign_keys = ON.
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
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON";
        pragma.ExecuteNonQuery();
        return connection;
    }
}

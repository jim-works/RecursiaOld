using System.Data.SQLite;
using System.IO;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Recursia;
public class SQLInterface : IDisposable
{
    private struct DataKey
    {
        public int Tid;
        public ChunkCoord Pos;

        public override int GetHashCode() => HashCode.Combine(Tid,Pos);
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is DataKey k && k.Tid == Tid && k.Pos == Pos;
        }
    }
    private readonly SQLiteConnection conn;
    private const string createWorldInfoTable = @"
        CREATE TABLE worldInfo (
            key TEXT NOT NULL,
            value TEXT NOT NULL,
            PRIMARY KEY (key)
        ) STRICT";
    private const string createPlayersTable = @"
        CREATE TABLE players (
            name TEXT NOT NULL,
            data BLOB NOT NULL,
            PRIMARY KEY (name)
        ) STRICT
    ";
    private const string createChunkDataTable = @"
        CREATE TABLE data (
            tid INTEGER NOT NULL,
            x INTEGER NOT NULL,
            y INTEGER NOT NULL,
            z INTEGER NOT NULL,
            data BLOB NOT NULL,
            PRIMARY KEY (tid,x,y,z)
        ) STRICT";
    private const string worldFormatVersion = "1";

    private readonly SQLiteCommand createDataTableCommand;
    private readonly SQLiteCommand saveDataCommand;
    private readonly SQLiteCommand loadDataCommand;
    private readonly SQLiteCommand savePlayerCommand;
    private readonly SQLiteCommand loadPlayerCommand;
    private readonly ConcurrentBag<(DataKey, TaskCompletionSource<ISerializable?>)> dataLoadQueue = new();
    private readonly ConcurrentDictionary<DataKey, ISerializable> dataSaveQueue = new();
    private readonly Dictionary<string, int> dataNameMap = new();
    private readonly Dictionary<int, Func<BinaryReader,ISerializable>> deserializers = new();
    private readonly List<(DataKey,ISerializable)> returnToSave = new();

    public SQLInterface(string dbpath)
    {
        Godot.GD.Print("Opening database at " + dbpath);
        bool init = false;
        if (!File.Exists(dbpath))
        {
            SQLiteConnection.CreateFile(dbpath);
            init = true;
        }

        var connectionString = new SQLiteConnectionStringBuilder()
        {
            DataSource = dbpath
        }.ToString();
        conn = new SQLiteConnection(connectionString);
        conn.Open();

        //prepare save/load commands
        createDataTableCommand = conn.CreateCommand();
        createDataTableCommand.CommandText = createChunkDataTable;

        saveDataCommand = conn.CreateCommand();
        saveDataCommand.CommandText = @"
            INSERT OR REPLACE INTO data (tid,x, y, z, data)
            VALUES (@name,@x, @y, @z, @data)";
        saveDataCommand.Parameters.Add("@name", System.Data.DbType.Int32);
        saveDataCommand.Parameters.Add("@x", System.Data.DbType.Int32);
        saveDataCommand.Parameters.Add("@y", System.Data.DbType.Int32);
        saveDataCommand.Parameters.Add("@z", System.Data.DbType.Int32);
        saveDataCommand.Parameters.Add("@data", System.Data.DbType.Binary);

        loadDataCommand = conn.CreateCommand();
        loadDataCommand.CommandText = @"
            SELECT data FROM data
            WHERE tid = @name AND x = @x AND y = @y AND z = @z";
        loadDataCommand.Parameters.Add("@name", System.Data.DbType.Int32);
        loadDataCommand.Parameters.Add("@x", System.Data.DbType.Int32);
        loadDataCommand.Parameters.Add("@y", System.Data.DbType.Int32);
        loadDataCommand.Parameters.Add("@z", System.Data.DbType.Int32);

        savePlayerCommand = conn.CreateCommand();
        savePlayerCommand.CommandText = @"
            INSERT OR REPLACE INTO players (name, data)
            VALUES (@name, @data)";
        savePlayerCommand.Parameters.Add("@name", System.Data.DbType.String);
        savePlayerCommand.Parameters.Add("@data", System.Data.DbType.Binary);

        loadPlayerCommand = conn.CreateCommand();
        loadPlayerCommand.CommandText = @"
            SELECT data FROM players WHERE name=@name";
        loadPlayerCommand.Parameters.Add("@name", System.Data.DbType.String);

        if (init)
        {
            initializeTables();
        }
        else if (!verifyDB())
        {
            Godot.GD.PushError("Database format version mismatch!!");
            conn = null!;
            return;
        }
        //print number of chunks in database
        using SQLiteCommand command = conn.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM data WHERE tid=0";
        using SQLiteDataReader reader = command.ExecuteReader();
        if (reader.Read())
        {
            Godot.GD.Print("Database contains " + reader.GetInt32(0) + " chunks");
        }
    }
    public void BeginPolling()
    {
        //run task to save/load on a single thread to avoid sqlite shenanigans
        //TODO: can close program in middle of this and dispose connection
        Task.Run(() =>
        {
            while (true)
            {
                Task.Delay(Settings.SaveLoadIntervalMs);
                emptySaveQueue();
                emptyLoadQueue();
            }
        });
    }
    public void RegisterDataTable(string dataTable, int id, Func<BinaryReader,ISerializable> deserializer)
    {
        if (!dataNameMap.TryAdd(dataTable, id))
        {
            throw new System.Data.DataException($"Table {dataTable} already exists!");
        }
        deserializers[id] = deserializer;
    }

    private bool verifyDB()
    {
        try
        {
            using SQLiteCommand command = conn.CreateCommand();
            command.CommandText = "SELECT value FROM worldInfo WHERE key = 'formatVersion'";
            using SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                return (string)reader["value"] == worldFormatVersion;
            }
            else
            {
                return false;
            }
        }
        catch (SQLiteException e)
        {
            Godot.GD.PushError("Error verifying database: " + e.Message);
            return false;
        }
    }
    private void initializeTables()
    {
        Godot.GD.Print("Initializing tables");
        using SQLiteCommand command = conn.CreateCommand();
        command.CommandText = createChunkDataTable;
        command.ExecuteNonQuery();
        command.CommandText = createWorldInfoTable;
        command.ExecuteNonQuery();
        command.CommandText = @"
                INSERT INTO worldInfo (key, value)
                VALUES ('formatVersion', @formatVersion)";
        command.Parameters.AddWithValue("@formatVersion", worldFormatVersion);
        command.ExecuteNonQuery();
        command.CommandText = createPlayersTable;
        command.ExecuteNonQuery();
    }

    public void Close()
    {
        saveDataCommand.Dispose();
        loadDataCommand.Dispose();
        savePlayerCommand.Dispose();
        loadPlayerCommand.Dispose();
        createDataTableCommand.Dispose();
        conn.Close();
    }
    public void Save(int dataTable, ChunkCoord pos, ISerializable obj)
    {
        dataSaveQueue[new DataKey{Tid=dataTable,Pos=pos}] = obj;
    }
    public async Task<ISerializable?> LoadData(int dataTable, ChunkCoord pos)
    {
        var waiting = new TaskCompletionSource<ISerializable?>();
        dataLoadQueue.Add((new DataKey{Tid=dataTable,Pos=pos}, waiting));
        return await waiting.Task;
    }
    private void emptySaveQueue()
    {
        int count = 0;
        using SQLiteTransaction transaction = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
        foreach (var kvp in dataSaveQueue.ToArray())
        {
            if (dataSaveQueue.TryRemove(kvp.Key, out ISerializable? obj))
            {
                if (obj.NoSerialize)
                {
                    //these chunks aren't ready to be saved yet as they're still generating
                    returnToSave.Add((kvp.Key,obj));
                    continue;
                }
                saveDataCommand.Parameters["@x"].Value = kvp.Key.Pos.X;
                saveDataCommand.Parameters["@y"].Value = kvp.Key.Pos.Y;
                saveDataCommand.Parameters["@z"].Value = kvp.Key.Pos.Z;
                saveDataCommand.Parameters["@name"].Value =kvp.Key.Tid;
                using (MemoryStream ms = new())
                using (BinaryWriter bw = new(ms))
                using (GZipStream gz = new(ms, CompressionLevel.Fastest))
                {
                    obj.Serialize(bw);
                    saveDataCommand.Parameters["@data"].Value = ms.ToArray();
                }
                saveDataCommand.ExecuteNonQuery();
                count++;
            }
        }
        transaction.Commit();
        foreach (var c in returnToSave) dataSaveQueue[c.Item1] = c.Item2;
        returnToSave.Clear();
        if (count > 0) Godot.GD.Print($"saved {count} objects!");
    }
    private void emptyLoadQueue()
    {
        while (dataLoadQueue.TryTake(out var item))
        {
            (DataKey k, TaskCompletionSource<ISerializable?> objTcs) = item;
            try
            {
                objTcs.SetResult(loadFromDB(k));
            }
            catch (Exception e)
            {
                Godot.GD.PushError(e);
                objTcs.TrySetResult(null);
            }
        }
    }
    private ISerializable? loadFromDB(DataKey key)
    {
        loadDataCommand.Parameters["@x"].Value = key.Pos.X;
        loadDataCommand.Parameters["@y"].Value = key.Pos.Y;
        loadDataCommand.Parameters["@z"].Value = key.Pos.Z;
        loadDataCommand.Parameters["@name"].Value = key.Tid;
        object? result = loadDataCommand.ExecuteScalar();
        if (result == null)
        {
            return null;
        }
        using MemoryStream ms = new((byte[])result);
        using GZipStream gz = new(ms, CompressionMode.Decompress);
        using BinaryReader br = new(ms);
        return deserializers[key.Tid](br);
    }
    //TODO: locks
    // public void SavePlayers(IEnumerable<Player> players)
    // {
    //     using SQLiteTransaction transaction = conn.BeginTransaction();
    //     foreach (var p in players)
    //     {
    //         savePlayerCommand.Parameters["@name"].Value = p.Name;
    //         using (MemoryStream ms = new())
    //         using (BinaryWriter bw = new(ms))
    //         using (GZipStream gz = new(ms, CompressionLevel.Fastest))
    //         {
    //             p.Serialize(bw);
    //             savePlayerCommand.Parameters["@data"].Value = ms.ToArray();
    //         }
    //         savePlayerCommand.ExecuteNonQuery();
    //     }
    //     transaction.Commit();
    // }
    // public async Task<Player?> LoadPlayer(World world, string name)
    // {
    //     loadPlayerCommand.Parameters["@name"].Value = name;
    //     object? result = await loadPlayerCommand.ExecuteScalarAsync();
    //     if (result == null)
    //     {
    //         return null;
    //     }
    //     using MemoryStream ms = new((byte[])result);
    //     using GZipStream gz = new(ms, CompressionMode.Decompress);
    //     using BinaryReader br = new(ms);
    //     Player? player = PhysicsObject.Deserialize<Player>(world, br);
    //     if (player == null)
    //     {
    //         throw new InvalidDataException($"Couldn't load data for player name: {name}! Data corrupted!");
    //     }
    //     return player;
    // }

    public void Dispose()
    {
        emptySaveQueue();
        conn.Dispose();
        GC.SuppressFinalize(this);
    }
}
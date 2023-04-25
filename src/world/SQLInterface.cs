using System.Data.SQLite;
using System.IO;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

//TODO: check gzip is actually doing anything
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
    private readonly ConcurrentBag<(DataKey, TaskCompletionSource<object?>)> dataLoadQueue = new();
    private readonly ConcurrentDictionary<DataKey, (ISerializable, TaskCompletionSource?)> dataSaveQueue = new();
    private readonly ConcurrentBag<(string, TaskCompletionSource<Player?>)> playerLoadQueue = new();
    private readonly ConcurrentDictionary<string, Player> playerSaveQueue = new();
    private readonly Dictionary<string, int> dataNameMap = new();
    private readonly Dictionary<int, Func<BinaryReader,object?>> dataDeserializers = new();
    private readonly Func<BinaryReader, string, Player> playerDeserializer;
    private readonly List<(DataKey,(ISerializable,TaskCompletionSource?))> returnToSave = new();

    public SQLInterface(string dbpath, Func<BinaryReader, string, Player> playerDeserializer)
    {
        Godot.GD.Print("Opening database at " + dbpath);
        this.playerDeserializer = playerDeserializer;
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
        //saving then loading provides benefits for things like moving back/forward between chunks very quickly
        Task.Run(() =>
        {
            while (true)
            {
                Task.Delay(Settings.SaveLoadIntervalMs);
                lock (conn)
                {
                    emptySaveQueue();
                    emptyLoadQueue();
                }
            }
        });
    }
    public void RegisterDataTable(string dataTable, int id, Func<BinaryReader,object?> deserializer)
    {
        if (!dataNameMap.TryAdd(dataTable, id))
        {
            throw new System.Data.DataException($"Table {dataTable} already exists!");
        }
        dataDeserializers[id] = deserializer;
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
        lock (conn)
        {
            emptySaveQueue();
            emptyLoadQueue();
        }
        saveDataCommand.Dispose();
        loadDataCommand.Dispose();
        savePlayerCommand.Dispose();
        loadPlayerCommand.Dispose();
        createDataTableCommand.Dispose();
        conn.Close();
    }
    public void Save(int dataTable, ChunkCoord pos, ISerializable obj)
    {
        dataSaveQueue[new DataKey{Tid=dataTable,Pos=pos}] = (obj,null);
    }
    public async Task SaveAsync(int dataTable, ChunkCoord pos, ISerializable obj)
    {
        TaskCompletionSource saveTcs = new();
        dataSaveQueue[new DataKey{Tid=dataTable,Pos=pos}] = (obj, saveTcs);
        await saveTcs.Task;
    }
    public void Save(Player p)
    {
        playerSaveQueue[p.Name] = p;
    }
    public async Task<object?> LoadData(int dataTable, ChunkCoord pos)
    {
        var waiting = new TaskCompletionSource<object?>();
        dataLoadQueue.Add((new DataKey{Tid=dataTable,Pos=pos}, waiting));
        return await waiting.Task;
    }
        public async Task<Player?> LoadPlayer(string name)
    {
        var waiting = new TaskCompletionSource<Player?>();
        playerLoadQueue.Add((name, waiting));
        return await waiting.Task;
    }
    private void emptySaveQueue()
    {
        using SQLiteTransaction transaction = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
        emptyDataSaveQueue();
        emptyPlayerSaveQueue();
        transaction.Commit();
        foreach (var c in returnToSave) dataSaveQueue[c.Item1] = c.Item2;
        returnToSave.Clear();
    }
    private void emptyPlayerSaveQueue()
    {
        foreach (var kvp in playerSaveQueue.ToArray())
        {
            if (playerSaveQueue.TryRemove(kvp.Key, out Player? p))
            {
                if (p.NoSerialize()) continue;
                savePlayerCommand.Parameters["@name"].Value = p.Name;
                using (MemoryStream ms = new())
                using (BinaryWriter bw = new(ms))
                using (GZipStream gz = new(ms, CompressionLevel.Fastest))
                {
                    p.Serialize(bw);
                    Godot.GD.Print($"memory stream size {ms.Length}");
                    savePlayerCommand.Parameters["@data"].Value = ms.ToArray();
                }
                savePlayerCommand.ExecuteNonQuery();
                Godot.GD.Print($"saved player {kvp.Key} in position {kvp.Value.Position}");
            }
        }
    }
    private void emptyDataSaveQueue()
    {
        foreach (var kvp in dataSaveQueue.ToArray())
        {
            if (dataSaveQueue.TryRemove(kvp.Key, out var v))
            {
                (ISerializable obj,TaskCompletionSource? tcs) = v;
                if (obj.NoSerialize())
                {
                    //these chunks aren't ready to be saved yet as they're still generating
                    returnToSave.Add((kvp.Key,(obj,tcs)));
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
                tcs?.SetResult();
            }
        }
    }
    private void emptyLoadQueue()
    {
        //chunk data
        while (dataLoadQueue.TryTake(out var item))
        {
            (DataKey k, TaskCompletionSource<object?> objTcs) = item;
            try
            {
                objTcs.SetResult(loadDataFromDB(k));
            }
            catch (Exception e)
            {
                Godot.GD.PushError(e);
                objTcs.TrySetResult(null);
            }
        }
        //players
        while (playerLoadQueue.TryTake(out var item))
        {
            (string name, TaskCompletionSource<Player?> pTcs) = item;
            try
            {
                pTcs.SetResult(loadPlayerFromDB(name));
            }
            catch (Exception e)
            {
                Godot.GD.PushError(e);
                pTcs.SetResult(null);
            }
        }
    }
    private object? loadDataFromDB(DataKey key)
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
        return dataDeserializers[key.Tid](br);
    }
    private Player? loadPlayerFromDB(string key)
    {
        loadPlayerCommand.Parameters["@name"].Value = key;
        object? result = loadPlayerCommand.ExecuteScalar();
        if (result == null)
        {
            Godot.GD.Print("result is null");
            return null;
        }
        Godot.GD.Print("result is not null");
        using MemoryStream ms = new((byte[])result);
        //using GZipStream gz = new(ms, CompressionMode.Decompress);
        using BinaryReader br = new(ms);
        return playerDeserializer(br, key);
    }

    public void Dispose()
    {
        emptySaveQueue();
        conn.Dispose();
        GC.SuppressFinalize(this);
    }
}
using System.Data.SQLite;
using System.IO;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;

namespace Recursia;
public class SQLInterface : IDisposable
{
    private readonly SQLiteConnection conn;
    private const string createChunkTable = @"
        CREATE TABLE chunks (
            x INTEGER NOT NULL,
            y INTEGER NOT NULL,
            z INTEGER NOT NULL,
            terrainData BLOB NOT NULL,
            PRIMARY KEY (x, y, z)
        ) STRICT
    ";
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
    private const string worldFormatVersion = "1";

    private readonly SQLiteCommand saveChunkCommand;
    private readonly SQLiteCommand loadChunkCommand;
    private readonly SQLiteCommand savePlayerCommand;
    private readonly SQLiteCommand loadPlayerCommand;
    private readonly ConcurrentBag<(ChunkCoord, TaskCompletionSource<Chunk?>)> loadQueue = new();
    private readonly ConcurrentDictionary<ChunkCoord, Chunk> saveQueue = new();
    private readonly List<Chunk> returnToSave = new();

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
        saveChunkCommand = conn.CreateCommand();
        saveChunkCommand.CommandText = @"
            INSERT OR REPLACE INTO chunks (x, y, z, terrainData)
            VALUES (@x, @y, @z, @terrainData)";
        saveChunkCommand.Parameters.Add("@x", System.Data.DbType.Int32);
        saveChunkCommand.Parameters.Add("@y", System.Data.DbType.Int32);
        saveChunkCommand.Parameters.Add("@z", System.Data.DbType.Int32);
        saveChunkCommand.Parameters.Add("@terrainData", System.Data.DbType.Binary);
        loadChunkCommand = conn.CreateCommand();
        loadChunkCommand.CommandText = @"
            SELECT terrainData FROM chunks
            WHERE x = @x AND y = @y AND z = @z";
        loadChunkCommand.Parameters.Add("@x", System.Data.DbType.Int32);
        loadChunkCommand.Parameters.Add("@y", System.Data.DbType.Int32);
        loadChunkCommand.Parameters.Add("@z", System.Data.DbType.Int32);
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

        //run task to save/load on a single thread to avoid sqlite shenanigans
        Task.Run(() =>
        {
            while (true)
            {
                Task.Delay(Settings.SaveLoadIntervalMs);
                emptySaveQueue();
                emptyLoadQueue();
            }
        });

        //print number of chunks in database
        using SQLiteCommand command = conn.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM chunks";
        using SQLiteDataReader reader = command.ExecuteReader();
        if (reader.Read())
        {
            Godot.GD.Print("Database contains " + reader.GetInt32(0) + " chunks");
        }
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
        command.CommandText = createChunkTable;
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
        saveChunkCommand.Dispose();
        loadChunkCommand.Dispose();
        conn.Close();
    }
    public void Save(Chunk chunk)
    {
        saveQueue[chunk.Position] = chunk;
    }
    public async Task<Chunk?> LoadChunk(ChunkCoord coord)
    {
        var waiting = new TaskCompletionSource<Chunk?>();
        loadQueue.Add((coord, waiting));
        return await waiting.Task;
    }
    private void emptySaveQueue()
    {
        int count = 0;
        using SQLiteTransaction transaction = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
        foreach (var kvp in saveQueue.ToArray())
        {
            if (saveQueue.TryRemove(kvp.Key, out Chunk? chunk))
            {
                if (chunk.State == ChunkState.Sticky || chunk.GenerationState < ChunkGenerationState.GENERATED)
                {
                    //these chunks aren't ready to be saved yet as they're still generating or being used by a another chunk which may spill over
                    returnToSave.Add(chunk);
                    continue;
                }
                saveChunkCommand.Parameters["@x"].Value = chunk.Position.X;
                saveChunkCommand.Parameters["@y"].Value = chunk.Position.Y;
                saveChunkCommand.Parameters["@z"].Value = chunk.Position.Z;
                using (MemoryStream ms = new())
                using (BinaryWriter bw = new(ms))
                using (GZipStream gz = new(ms, CompressionLevel.Fastest))
                {
                    chunk.Serialize(bw);
                    saveChunkCommand.Parameters["@terrainData"].Value = ms.ToArray();
                }
                saveChunkCommand.ExecuteNonQuery();
                count++;
            }
        }
        transaction.Commit();
        foreach(var c in returnToSave) saveQueue[c.Position] = c;
        returnToSave.Clear();
        if (count > 0) Godot.GD.Print($"saved {count} chunks");
    }
    private void emptyLoadQueue()
    {
        while (loadQueue.TryTake(out var item))
        {
            (ChunkCoord coord, TaskCompletionSource<Chunk?> chunkTcs) = item;
            try
            {
                chunkTcs.SetResult(loadFromDB(coord));
            }
            catch (Exception e)
            {
                Godot.GD.PushError(e);
                chunkTcs.TrySetResult(null);
            }
        }
    }
    private Chunk? loadFromDB(ChunkCoord coord)
    {
        loadChunkCommand.Parameters["@x"].Value = coord.X;
        loadChunkCommand.Parameters["@y"].Value = coord.Y;
        loadChunkCommand.Parameters["@z"].Value = coord.Z;
        object? result = loadChunkCommand.ExecuteScalar();
        if (result == null)
        {
            return null;
        }
        using MemoryStream ms = new((byte[])result);
        using GZipStream gz = new(ms, CompressionMode.Decompress);
        using BinaryReader br = new(ms);
        return Chunk.Deserialize(br);
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
        conn.Dispose();
        GC.SuppressFinalize(this);
    }
}
using System.Data.SQLite;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading.Tasks;
using System;

public class SQLInterface
{
    private SQLiteConnection conn;
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
    private const string worldFormatVersion = "1";

    private SQLiteCommand saveChunkCommand;
    private SQLiteCommand loadChunkCommand;

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

        if (init)
        {
            initializeTables();
        }
        else if (!verifyDB())
        {
            Godot.GD.PushError("Database format version mismatch!!");
            conn = null;
            return;
        }

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

        //print number of chunks in database
        using (SQLiteCommand command = conn.CreateCommand())
        {
            command.CommandText = "SELECT COUNT(*) FROM chunks";
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    Godot.GD.Print("Database contains " + reader.GetInt32(0) + " chunks");
                }
            }
        }
    }

    private bool verifyDB()
    {
        try
        {
            using (SQLiteCommand command = conn.CreateCommand())
            {
                command.CommandText = "SELECT value FROM worldInfo WHERE key = 'formatVersion'";
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (string)reader["value"] == worldFormatVersion;
                    }
                    else
                    {
                        return false;
                    }

                }
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
        using (SQLiteCommand command = conn.CreateCommand())
        {
            command.CommandText = createChunkTable;
            command.ExecuteNonQuery();
            command.CommandText = createWorldInfoTable;
            command.ExecuteNonQuery();
            command.CommandText = @"
                INSERT INTO worldInfo (key, value)
                VALUES ('formatVersion', @formatVersion)";
            command.Parameters.AddWithValue("@formatVersion", worldFormatVersion);
            command.ExecuteNonQuery();
        }
    }

    public void Close()
    {
        saveChunkCommand.Dispose();
        loadChunkCommand.Dispose();
        conn.Close();
    }

    public void SaveChunks(Func<Chunk> getChunk)
    {
        using (SQLiteTransaction transaction = conn.BeginTransaction())
        {
            while (getChunk() is Chunk chunk)
            {
                saveChunkCommand.Parameters["@x"].Value = chunk.Position.X;
                saveChunkCommand.Parameters["@y"].Value = chunk.Position.Y;
                saveChunkCommand.Parameters["@z"].Value = chunk.Position.Z;
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter bw = new BinaryWriter(ms))
                using (GZipStream gz = new GZipStream(ms, CompressionLevel.Fastest))
                {
                    chunk.Serialize(bw);
                    saveChunkCommand.Parameters["@terrainData"].Value = ms.ToArray();
                }
                saveChunkCommand.ExecuteNonQuery();
            }
            transaction.Commit();
        }
    }

    public void LoadChunks(IEnumerable<ChunkCoord> chunks, List<Chunk> placeInto)
    {
        foreach (ChunkCoord coord in chunks)
        {
            loadChunkCommand.Parameters["@x"].Value = coord.X;
            loadChunkCommand.Parameters["@y"].Value = coord.Y;
            loadChunkCommand.Parameters["@z"].Value = coord.Z;
            object result = loadChunkCommand.ExecuteScalar();
            if (result == null)
            {
                placeInto.Add(null);
                continue;
            }
            using (MemoryStream ms = new MemoryStream((byte[])result))
            using (GZipStream gz = new GZipStream(ms, CompressionMode.Decompress))
            using (BinaryReader br = new BinaryReader(ms))
            {
                Chunk chunk = Chunk.Deserialize(br);
                placeInto.Add(chunk);
            }
        }
    }
}
using System.Data.SQLite;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;

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
    public SQLInterface(string dbpath)
    {
        Godot.GD.Print("Opening database at " + dbpath);
        bool init = false;
        if (!File.Exists(dbpath))
        {
            SQLiteConnection.CreateFile(dbpath);
            init = true;
        }
        
        var connectionString = new SQLiteConnectionStringBuilder() {
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
        conn.Close();
    }

    public void SaveChunks(IEnumerable<Chunk> chunks)
    {
        using (SQLiteTransaction transaction = conn.BeginTransaction())
        {
            using (SQLiteCommand command = conn.CreateCommand())
            {
                command.CommandText = @"
                    INSERT OR REPLACE INTO chunks (x, y, z, terrainData)
                    VALUES (@x, @y, @z, @terrainData)
                ";
                foreach (Chunk chunk in chunks)
                {
                    command.Parameters.AddWithValue("@x", chunk.Position.X);
                    command.Parameters.AddWithValue("@y", chunk.Position.Y);
                    command.Parameters.AddWithValue("@z", chunk.Position.Z);
                    using (MemoryStream ms = new MemoryStream())
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    using (GZipStream gz = new GZipStream(ms, CompressionLevel.Fastest))
                    {
                        chunk.Serialize(bw);
                        command.Parameters.AddWithValue("@terrainData", ms.ToArray());
                    }
                    command.ExecuteNonQuery();
                }
            }
            transaction.Commit();
        }
    }

    public void LoadChunks(IEnumerable<ChunkCoord> chunks, List<Chunk> placeInto)
    {
        using (SQLiteCommand command = conn.CreateCommand())
        {
            command.CommandText = @"
                SELECT terrainData FROM chunks
                WHERE x = @x AND y = @y AND z = @z
            ";
            foreach (ChunkCoord coord in chunks)
            {
                command.Parameters.AddWithValue("@x", coord.X);
                command.Parameters.AddWithValue("@y", coord.Y);
                command.Parameters.AddWithValue("@z", coord.Z);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        using (MemoryStream ms = new MemoryStream((byte[])reader["terrainData"]))
                        using (GZipStream gz = new GZipStream(ms, CompressionMode.Decompress))
                        using (BinaryReader br = new BinaryReader(ms))
                        {
                            Chunk chunk = Chunk.Deserialize(br);
                            placeInto.Add(chunk);
                        }
                    }
                    else
                    {
                        placeInto.Add(null);
                    }
                }
            }
        }
    }
}
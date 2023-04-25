using Godot;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Recursia;
public class EntityCollection
{
    private readonly World world;
    private readonly ConcurrentDictionary<ChunkCoord, List<PhysicsObject>> physicsObjects = new();
    private readonly ConcurrentDictionary<ChunkCoord, List<Combatant>> combatants = new ();
    private readonly ConcurrentDictionary<string, Player> players = new();

    public IEnumerable<KeyValuePair<string,Player>> Players => players;
    public IEnumerable<KeyValuePair<ChunkCoord,List<PhysicsObject>>> PhysicsObjects => physicsObjects;
    public event System.Action<ChunkCoord, List<PhysicsObject>>? OnChunkUnload;

    public EntityCollection(World world)
    {
        this.world = world;
    }

    //Call on main thread
    public void Unload(ChunkCoord pos)
    {
        combatants.TryRemove(pos, out var _);
        if (physicsObjects.TryRemove(pos, out List<PhysicsObject>? objects))
        {
            //in unloaded chunk, so remove all from scene
            foreach (var obj in objects)
            {
                obj.OnCrossChunkBoundary -= physicsObjectCrossChunkBoundary;
                obj.OnExitTree -= RemoveObject;
                obj.GetParent().RemoveChild(obj);
            }
            OnChunkUnload?.Invoke(pos, objects);
        }
    }

    private void physicsObjectCrossChunkBoundary(PhysicsObject p, ChunkCoord oldChunk)
    {
        removePhysicsObject(p,oldChunk);
        addPhysicsObject(p);
    }
    private void removePhysicsObject(PhysicsObject p, ChunkCoord from)
    {
        if (physicsObjects.TryGetValue(from, out List<PhysicsObject>? physics))
        {
            lock(physics) physics.Remove(p);
            if (physics.Count == 0) physicsObjects.TryRemove(from, out var _);
        }
        if (world.Chunks.TryGetChunk(from, out Chunk? chunk))
        {
            chunk.PhysicsObjects.Remove(p);
        }
        if (p is Combatant c && combatants.TryGetValue(from, out List<Combatant>? comb))
        {
            lock (comb) comb.Remove(c);
            if (combatants.IsEmpty) combatants.TryRemove(from, out var _);
        }
        if (p is Player player)
        {
            players.TryRemove(player.Name, out var _);
        }
    }
    private void addPhysicsObject(PhysicsObject p)
    {
        ChunkCoord to = p.IsInsideTree() ? (ChunkCoord)p.GlobalPosition : (ChunkCoord)p.InitialPosition;
        if (world.Chunks.TryGetChunk(to, out Chunk? chunk))
        {
            chunk.PhysicsObjects.Add(p);
        }
        if (physicsObjects.TryGetValue(to, out List<PhysicsObject>? list))
        {
            list.Add(p);
        }
        else
        {
            physicsObjects[to] = new List<PhysicsObject> { p };
        }
        if (p is Combatant c)
        {
            if (combatants.TryGetValue(to, out List<Combatant>? list2))
            {
                list2.Add(c);
            }
            else
            {
                combatants[to] = new List<Combatant> { c };
            }
            if (p is Player player && !players.TryAdd(player.Name, player))
            {
                GD.PushError($"Duplicate player {p.Name}");
            }
        }
    }

    //init runs before object is added to scene tree
    //if will handle registering if T : PhysicsObject/Combatant
    public T SpawnObject<T>(PackedScene prefab, Vector3 position, System.Action<T>? init=null) where T : Node3D
    {
        T obj = prefab.Instantiate<T>();
        var c = obj as PhysicsObject;
        if (c != null)
        {
            c.OldCoord = (ChunkCoord)position; //this way, if we spawn outside of (0,0,0), we won't add the object twice.
            c.InitialPosition = position;
            c.World = world;
        }
        init?.Invoke(obj);
        world.CallDeferred("add_child", obj);
        if (c != null) RegisterObject(c);
        return obj;
    }

    public void RemoveObject(PhysicsObject p)
    {
        removePhysicsObject(p, (ChunkCoord)p.GlobalPosition);
        p.OnCrossChunkBoundary -= physicsObjectCrossChunkBoundary;
        p.OnExitTree -= RemoveObject;
    }

    //will not register an object twice
    public void RegisterObject(PhysicsObject obj)
    {
        if (obj.Registered) return;
        obj.Registered = true;
        obj.World = world;
        GD.Print("Registering object " + obj.Name);
        obj.OnCrossChunkBoundary += physicsObjectCrossChunkBoundary;
        obj.OnExitTree += RemoveObject;
        addPhysicsObject(obj);
    }

    public bool ClosestEnemy(Vector3 pos, Team? team, float maxDist, [MaybeNullWhen(false)] out Combatant enemy)
    {
        float minSqrDist = float.PositiveInfinity;
        enemy = null;
        //TODO: only check chunks in range
        foreach (var l in combatants.Values)
        {
            lock (l)
            {
            foreach (var c in l)
            {
                if (!c.IsInsideTree()) continue;
                float sqrDist = (pos - c.GlobalPosition).LengthSquared();
                if (sqrDist < minSqrDist && sqrDist < maxDist * maxDist && c.Team != team)
                {
                    minSqrDist = sqrDist;
                    enemy = c;
                }
            }
            }
        }
        return enemy != null;
    }
    public IEnumerable<Combatant> GetEnemiesInRange(Vector3 pos, float range, Team? team)
    {
        //TODO: only check chunks in range
        foreach (var l in combatants.Values)
        {
            lock (l)
            {
            foreach (var c in l)
            {
                if (!c.IsInsideTree()) continue;
                if (c.Team != team && (c.GlobalPosition - pos).LengthSquared() < range * range) yield return c;
            }
            }
        }
    }
    public IEnumerable<PhysicsObject> GetPhysicsObjectsInRange(Vector3 pos, float range)
    {
        //TODO: only check chunks in range
        foreach (var kvp in physicsObjects)
        {
            lock (kvp.Value)
            {
            foreach (var obj in kvp.Value)
            {
                if (!obj.IsInsideTree()) continue;
                if ((obj.GlobalPosition - pos).LengthSquared() < range * range) yield return obj;
            }
            }
        }
    }
    public Combatant? CollidesWithEnemy(Box box, Team? team)
    {
        //TODO: only check chunks in range
        foreach (var l in combatants.Values)
        {
            lock (l)
            {
            foreach (var c in l)
            {
                if (!c.IsInsideTree()) continue;
                if (c.Team == team) continue;
                if (c.GetBox().IntersectsBox(box)) return c;
            }
            }
        }
        return null;
    }
}
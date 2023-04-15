using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
namespace Recursia;
public static class ObjectTypes
{
    private static readonly Dictionary<string, PackedScene> objects = new();
    public static bool TryGetInstance<T>(World world, string type, Vector3 position, [MaybeNullWhen(false)] out T instance, System.Action<T>? init = null) where T : Node3D {
        if (objects.TryGetValue(type, out var b)) {
            T obj = world.Entities.SpawnObject(b, position, init);
            if (obj is PhysicsObject p)
            {
                p.ObjectType = type;
            }
            instance = obj;
            return true;
        }
        instance = null;
        return false;
    }

    public static void CreateType(string name, PackedScene obj) {
        if (objects.ContainsKey(name)) {
            GD.PushWarning($"Object {name} already exists, replacing!");
        }
        GD.Print($"Created object type {name}");
        objects[name] = obj;
    }
}
using System.Collections.Generic;
using Godot;
public static class ObjectTypes
{
    private static Dictionary<string, PackedScene> objects = new Dictionary<string, PackedScene>();
    public static T GetInstance<T>(string type, Vector3 position, System.Action<T> init = null) where T : Node3D {
        if (objects.TryGetValue(type, out var b)) {
            T obj = World.Singleton.SpawnObject<T>(b, position, init);
            if (obj is PhysicsObject p)
            {
                p.ObjectType = type;
            }
        }
        Godot.GD.PushWarning($"Object type {type} not found");
        return null;
    }

    public static void CreateType(string name, PackedScene obj) {
        if (objects.ContainsKey(name)) {
            Godot.GD.PushWarning($"Object {name} already exists, replacing!");
        }
        Godot.GD.Print($"Created object type {name}");
        objects[name] = obj;
    }
}
using Godot;

public class Item
{
    public string Name;
    public int MaxStack = 999;
    public Texture Texture;
    
    public virtual void OnUse(Combatant user, Vector3 position, Vector3 dir)
    {
        
    }
}
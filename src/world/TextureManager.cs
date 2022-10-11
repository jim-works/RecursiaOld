using System.Collections.Generic;
using Godot;

public class TextureManager
{
    private Dictionary<string, TextureInfo> Textures = new Dictionary<string, TextureInfo>();

    public TextureManager()
    {
        Textures["dirt"] = CreateInfo(256,256,8,0,0);
        Textures["grass"] = CreateInfo(256,256,8,1,0);
        Textures["stone"] = CreateInfo(256,256,8,0,1);
        Textures["sand"] = CreateInfo(256,256,8,1,1);

        foreach (var kvp in Textures)
        {
            Godot.GD.Print($"{kvp.Key}: ({kvp.Value.Min}, {kvp.Value.Max})");
        }
    }


    public TextureInfo CreateInfo(int texWidth, int texHeight, int cellSize, int x, int y, int texId=0)
    {
        float cellWidthRatio = (float)cellSize/texWidth;
        float cellHeightRatio = (float)cellSize/texHeight;
        return new TextureInfo {
            Min = new Vector2((float)x*cellWidthRatio, (float)y*cellHeightRatio),
            Max = new Vector2((float)(x+1)*cellWidthRatio, (float)(y+1)*cellHeightRatio),
            Tex = texId
        };
    }

    public TextureInfo GetTexture(string blockName) {
        if (Textures.TryGetValue(blockName, out var t)) return t;
        return null;
    }
}
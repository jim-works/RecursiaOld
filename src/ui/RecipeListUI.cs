using Godot;
using System.Collections.Generic;

public class RecipeListUI : Control
{
    [Export]
    public PackedScene CraftingRecipeUI;
    [Export]
    public int Padding;
    
    private List<CraftingRecipeUI> recipeUIs = new List<CraftingRecipeUI>();
    private Control Parent; //scrollbox

    public override void _Ready()
    {
        Parent = GetParentControl();
        base._Ready();
    }
    //TODO: pooling
    public void DisplayList(IEnumerable<Recipe> recipes)
    {
        foreach (var recipe in recipeUIs)
        {
            recipe.QueueFree();
        }
        recipeUIs.Clear();
        float startHeight = 0;
        foreach (var recipe in recipes)
        {
            var ui = CraftingRecipeUI.Instance<CraftingRecipeUI>();
            recipeUIs.Add(ui);
            AddChild(ui);
            ui.RectPosition = new Vector2(0, startHeight);
            startHeight += ui.DisplayRecipe(recipe)+Padding;
        }
        RectMinSize = new Vector2(RectMinSize.x, startHeight);
    }


    //signal
    public void OnPause()
    {
        Parent.Visible = true;
    }

    //signal
    public void OnUnpause()
    {
        Parent.Visible = false;
    }
}
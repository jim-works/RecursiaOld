using Godot;
using System.Collections.Generic;

public partial class RecipeListUI : Control
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
            var ui = CraftingRecipeUI.Instantiate<CraftingRecipeUI>();
            recipeUIs.Add(ui);
            AddChild(ui);
            ui.Position = new Vector2(0, startHeight);
            startHeight += ui.DisplayRecipe(recipe)+Padding;
        }
        CustomMinimumSize = new Vector2(CustomMinimumSize.X, startHeight);
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
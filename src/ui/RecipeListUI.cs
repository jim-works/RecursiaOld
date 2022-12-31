using Godot;
using System.Collections.Generic;

public class RecipeListUI : Control
{
    [Export]
    public PackedScene CraftingRecipeUI;
    [Export]
    public int Padding;
    
    private List<CraftingRecipeUI> recipeUIs = new List<CraftingRecipeUI>();

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
    }


    //signal
    public void OnPause()
    {
        Visible = true;
    }

    //signal
    public void OnUnpause()
    {
        Visible = false;
    }
}
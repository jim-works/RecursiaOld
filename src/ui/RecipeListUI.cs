using Godot;
using System.Collections.Generic;

namespace Recursia;
public partial class RecipeListUI : Control
{
    [Export]
    public PackedScene CraftingRecipeUI = null!;
    [Export]
    public int Padding;

    private readonly List<CraftingRecipeUI> recipeUIs = new();
    private Control Parent = null!; //scrollbox

    public override void _Ready()
    {
        Parent = GetParentControl();
        if (CraftingRecipeUI == null)
        {
            GD.PushError($"Null CraftingRecipeUI on RecipeListUI {Name}");
        }
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
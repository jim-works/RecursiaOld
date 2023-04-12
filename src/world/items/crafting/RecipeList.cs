using System.Collections.Generic;
using System.Configuration;

//TODO:
//make this more efficient : (generalized) suffix tree?
namespace Recursia;
public static class RecpieList
{
    public static readonly List<Recipe> Recipes = new();

    public static void AddRecipe(Recipe r)
    {
        Recipes.Add(r);
        Godot.GD.Print("Added recipe");
    }

    //finds all recipes with ingredient or product that have a substring of query
    public static List<Recipe> Search(string query)
    {
        query = query.Trim();
        List<Recipe> result = new();
        foreach (var recipe in Recipes)
        {
            if (recipe.Station.Contains(query))
            {
                result.Add(recipe);
                continue;
            }
            bool add = false;
            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient.Item.DisplayName.Contains(query)) {
                    add = true;
                    result.Add(recipe);
                    break;
                }
            }
            if (add) continue;
            foreach (var product in recipe.Product)
            {
                if (product.Item.DisplayName.Contains(query)) {
                    add = true;
                    result.Add(recipe);
                    break;
                }
            }
        }
        return result;
    }
}
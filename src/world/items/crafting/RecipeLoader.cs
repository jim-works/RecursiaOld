using System.Collections.Generic;

public static class RecipeLoader
{

    public static void Load()
    {
        RecpieList.AddRecipe(new Recipe(
            new List<ItemStack> {new ItemStack {Size=5,Item=ItemTypes.GetBlockItem("dirt")}},
            new List<ItemStack> {new ItemStack {Size=1,Item=ItemTypes.Get("gun")}},
            "hand"
        ));

        RecpieList.AddRecipe(new Recipe(
            new List<ItemStack> {new ItemStack {Size=6,Item=ItemTypes.GetBlockItem("dirt")}},
            new List<ItemStack> {new ItemStack {Size=2,Item=ItemTypes.Get("gun")}},
            "hand"
        ));
    }
}
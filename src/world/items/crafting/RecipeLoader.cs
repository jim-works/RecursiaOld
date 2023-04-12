using System.Collections.Generic;

namespace Recursia;
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
            new List<ItemStack> {new ItemStack {Size=10,Item=ItemTypes.GetBlockItem("stone")}},
            new List<ItemStack> {new ItemStack {Size=10,Item=ItemTypes.Get("explosive_bullet")}},
            "hand"
        ));

        RecpieList.AddRecipe(new Recipe(
            new List<ItemStack> {new ItemStack {Size=10,Item=ItemTypes.GetBlockItem("dirt")}},
            new List<ItemStack> {new ItemStack {Size=1,Item=ItemTypes.Get("shotgun")}},
            "hand"
        ));

        RecpieList.AddRecipe(new Recipe(
            new List<ItemStack> {new ItemStack {Size=10,Item=ItemTypes.GetBlockItem("dirt")},new ItemStack {Size=10,Item=ItemTypes.GetBlockItem("stone")}},
            new List<ItemStack> {new ItemStack {Size=20,Item=ItemTypes.Get("tracking_bullet")}},
            "hand"
        ));

        RecpieList.AddRecipe(new Recipe(
            new List<ItemStack> {new ItemStack {Size=1,Item=ItemTypes.Get("gun")},new ItemStack {Size=1,Item=ItemTypes.Get("marp_rod")}},
            new List<ItemStack> {new ItemStack {Size=5,Item=ItemTypes.Get("cursed_idol")}},
            "hand"
        ));
    }
}
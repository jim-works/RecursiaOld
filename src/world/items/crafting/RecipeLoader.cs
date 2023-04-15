using System.Collections.Generic;

namespace Recursia;
public static class RecipeLoader
{
    public static void Load()
    {
        //temporary, idc about null
        ItemTypes.TryGetBlockItem("dirt", out var dirt);
        ItemTypes.TryGetBlockItem("stone", out var stone);
        ItemTypes.TryGet("gun", out var gun);
        ItemTypes.TryGet("explosive_bullet", out var explosive_bullet);
        ItemTypes.TryGet("shotgun", out var shotgun);
        ItemTypes.TryGet("tracking_bullet", out var tracking_bullet);
        ItemTypes.TryGet("marp_rod", out var marp_rod);

        RecpieList.AddRecipe(new Recipe(
            new List<ItemStack> {new ItemStack {Size=5,Item=dirt!}},
            new List<ItemStack> {new ItemStack {Size=1,Item=gun!}},
            "hand"
        ));

        RecpieList.AddRecipe(new Recipe(
            new List<ItemStack> {new ItemStack {Size=10,Item=stone!}},
            new List<ItemStack> {new ItemStack {Size=10,Item=explosive_bullet!}},
            "hand"
        ));

        RecpieList.AddRecipe(new Recipe(
            new List<ItemStack> {new ItemStack {Size=10,Item=dirt!}},
            new List<ItemStack> {new ItemStack {Size=1,Item=shotgun!}},
            "hand"
        ));

        RecpieList.AddRecipe(new Recipe(
            new List<ItemStack> {new ItemStack {Size=10,Item=dirt!},new ItemStack {Size=10,Item=stone!}},
            new List<ItemStack> {new ItemStack {Size=20,Item=tracking_bullet!}},
            "hand"
        ));

        RecpieList.AddRecipe(new Recipe(
            new List<ItemStack> {new ItemStack {Size=1,Item=gun!},new ItemStack {Size=1,Item=marp_rod!}},
            new List<ItemStack> {new ItemStack {Size=5,Item=marp_rod!}},
            "hand"
        ));
    }
}
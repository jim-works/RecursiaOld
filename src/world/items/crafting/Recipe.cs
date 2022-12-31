using System.Collections.Generic;

public class Recipe
{
    public List<ItemStack> Ingredients {get; private set;}
    public List<ItemStack> Product {get; private set;}
    public string Station {get; private set;}

    //copies lists into corresponding properties
    public Recipe(List<ItemStack> ingredients, List<ItemStack> products, string station)
    {
        Ingredients = new List<ItemStack>(ingredients);
        Product = new List<ItemStack>(products);
        Station = station;
    }
}
using System.Collections.Generic;

public partial class Recipe
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

    public bool CraftableBy(Inventory inv)
    {
        foreach (var ingredient in Ingredients)
        {
            if (inv.Count(ingredient.Item) < ingredient.Size) return false;
        }
        return true;
    }

    public bool Craft(Inventory from, Inventory to)
    {
        if (!CraftableBy(from)) return false;
        foreach (var ingredient in Ingredients)
        {
            from.DeleteItems(ingredient.Item, ingredient.Size);
        }
        foreach (var product in Product)
        {
            ItemStack prod = product; //copy
            to.AddItem(ref prod);
        }
        return true;
    }
}
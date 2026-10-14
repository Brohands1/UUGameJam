using UnityEngine;
namespace Crossroads.Diner
{
    [CreateAssetMenu(menuName = "Crossroads/Recipe")]
    public sealed class RecipeDefinition : DinerDefinition
    {
        public IngredientDefinition first;
        public IngredientDefinition second;
        [Min(1)] public int price = 12;
        [Min(0.1f)] public float cookSeconds = 4;
        public bool Matches(IngredientDefinition a, IngredientDefinition b)
        { return (first == a && second == b) || (first == b && second == a); }
    }
}

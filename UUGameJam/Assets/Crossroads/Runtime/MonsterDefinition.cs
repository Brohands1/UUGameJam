using UnityEngine;
namespace Crossroads.Diner
{
    [CreateAssetMenu(menuName = "Crossroads/Monster")]
    public sealed class MonsterDefinition : DinerDefinition
    {
        public ToolDefinition weakness;
        public IngredientDefinition ingredient;
        [Min(1)] public int ingredientYield = 2;
    }
}

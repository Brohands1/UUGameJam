using System;
using System.Collections.Generic;
using UnityEngine;
namespace Crossroads.Diner
{
    [Serializable] public class ArrivalPhase
    {
        public float startsAt;
        [Min(.1f)] public float interval = 10;
        [Min(.1f)] public float patience = 50;
        public ArrivalPhase(float start, float every, float wait) { startsAt=start; interval=every; patience=wait; }
    }
    [Serializable] public class OpeningGuest
    { public MonsterDefinition species; public RecipeDefinition recipe; }

    [CreateAssetMenu(menuName = "Crossroads/Game configuration")]
    public sealed class DinerConfig : ScriptableObject
    {
        public DinerTheme theme;
        public IngredientDefinition[] ingredients;
        public ToolDefinition[] tools;
        public MonsterDefinition[] monsters;
        public RecipeDefinition[] recipes;
        public OpeningGuest[] openingGuests;
        [Min(1)] public int waitingSlots = 4, capturedSlots = 2, traySlots = 2;
        [Min(0)] public int startingStock = 2;
        [Min(1)] public float shiftDuration = 300;
        [Min(1)] public int revenueTarget = 100;
        [Min(0)] public float capturePatiencePenalty = 5;
        [Min(.1f)] public float processingSeconds = 3;
        [Min(0)] public float captureCooldown = .6f;
        [Min(.1f)] public float fullAreaRefillDelay = 1;
        [Min(.1f)] public float turnSeconds = .35f, pushSeconds = .2f;
        public int randomSeed = 137;
        public ArrivalPhase[] phases = { new ArrivalPhase(0,10,50),new ArrivalPhase(60,8,45),new ArrivalPhase(180,6,40) };

        public ArrivalPhase Phase(float elapsed)
        {
            var value = phases[0];
            foreach(var phase in phases) if(elapsed >= phase.startsAt) value = phase;
            return value;
        }
        public string ValidateConfiguration()
        {
            if(revenueTarget < 1 || capturePatiencePenalty < 0 || float.IsNaN(capturePatiencePenalty) || float.IsInfinity(capturePatiencePenalty)) return "Invalid revenue target or capture patience penalty.";
            if(theme == null || ingredients == null || ingredients.Length == 0 || monsters == null || monsters.Length == 0 || recipes == null || recipes.Length == 0 || tools == null || tools.Length == 0) return "Missing catalog or theme.";
            if(waitingSlots < 1 || capturedSlots < 1 || traySlots < 1 || shiftDuration <= 0 || processingSeconds <= 0 || startingStock < 0 || turnSeconds <= 0 || pushSeconds <= 0) return "Invalid capacity or timing.";
            if(phases == null || phases.Length == 0 || phases[0].startsAt != 0) return "Arrival phases must begin at zero.";
            float previous=-1;
            foreach(var p in phases) { if(p.startsAt <= previous || p.interval <= 0 || p.patience <= 0) return "Invalid arrival phase."; previous=p.startsAt; }
            var ids = new HashSet<string>();
            foreach(var list in new DinerDefinition[][] { ingredients, tools, monsters, recipes }) foreach(var d in list)
            { if(d == null || string.IsNullOrWhiteSpace(d.id) || !ids.Add(d.id)) return "Catalog IDs must be nonempty and unique."; }
            foreach(var m in monsters) if(Array.IndexOf(tools,m.weakness)<0 || Array.IndexOf(ingredients,m.ingredient)<0 || m.ingredientYield<1) return "Monster references invalid catalog assets.";
            foreach(var r in recipes) if(Array.IndexOf(ingredients,r.first)<0 || Array.IndexOf(ingredients,r.second)<0 || r.price<1 || r.cookSeconds<=0) return "Invalid recipe.";
            for(int i=0;i<recipes.Length;i++) for(int j=i+1;j<recipes.Length;j++) if(recipes[i].Matches(recipes[j].first,recipes[j].second)) return "Two recipes use the same ingredient pair.";
            if(openingGuests != null) foreach(var g in openingGuests) if(Array.IndexOf(monsters,g.species)<0 || Array.IndexOf(recipes,g.recipe)<0) return "Invalid opening guest.";
            return null;
        }
    }
}

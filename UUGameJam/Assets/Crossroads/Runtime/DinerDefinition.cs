using UnityEngine;

namespace Crossroads.Diner
{
    public enum ArtKind { Vampire, Werewolf, Ghost, Garlic, Chains, Salt, Blood, Meat, Ecto, Stew, Soup, Pot, Cleaver, Lantern, Crossroads, SpectralStew }

    // Assets are immutable during a shift. IDs, never artwork names, identify gameplay data.
    public abstract class DinerDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        [Tooltip("Optional RectTransform prefab. Takes priority over icon; may contain an Animator.")]
        public GameObject visualPrefab;
        public ArtKind placeholder;
        public Color tint = Color.white;
    }
}

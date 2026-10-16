using UnityEngine;
namespace Crossroads.Diner
{
    [CreateAssetMenu(menuName = "Crossroads/Presentation theme")]
    public sealed class DinerTheme : ScriptableObject
    {
        public Font bodyFont;
        public Font titleFont;
        public Sprite frontBackground, kitchenBackground, slotSprite, panelSprite;
        public GameObject frontDecorationPrefab, kitchenDecorationPrefab;
        public Sprite processorIcon, potIcon, trashIcon;
        public AudioClip ambience, arrival, capture, invalid, processed, cooked, paid, walkout;
        [Range(0, 1)] public float defaultVolume = .55f;
        public Color ink = new Color(.06f,.1f,.12f);
        public Color cream = new Color(.94f,.88f,.73f);
        public Color gold = new Color(.87f,.65f,.32f);
        public Color mint = new Color(.49f,.76f,.68f);
        public Color danger = new Color(.85f,.36f,.32f);
    }
}

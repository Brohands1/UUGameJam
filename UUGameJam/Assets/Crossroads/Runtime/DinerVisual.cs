using UnityEngine;
using UnityEngine.UI;
namespace Crossroads.Diner
{
    public sealed class DinerVisual : MonoBehaviour
    {
        public DinerArtGraphic fallback;
        public Image spriteImage;
        DinerDefinition current;
        GameObject instance;
        bool initialized;
        public void Show(DinerDefinition definition)
        {
            if(initialized && current==definition && (definition==null || gameObject.activeSelf)) return;
            initialized=true;
            current=definition;
            if(instance!=null) { Destroy(instance); instance=null; }
            if(definition==null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            fallback.gameObject.SetActive(definition.icon==null && definition.visualPrefab==null);
            spriteImage.gameObject.SetActive(definition.icon!=null && definition.visualPrefab==null);
            if(definition.visualPrefab!=null)
            {
                instance=Instantiate(definition.visualPrefab,transform,false);
                var rect=instance.GetComponent<RectTransform>();
                if(rect!=null) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero; }
            }
            else if(definition.icon!=null) { spriteImage.sprite=definition.icon; spriteImage.preserveAspect=true; }
            else { fallback.kind=definition.placeholder; fallback.color=definition.tint; fallback.SetVerticesDirty(); }
        }
    }
}

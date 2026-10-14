using UnityEngine;
using UnityEngine.UI;
namespace Crossroads.Diner
{
    public sealed class DinerHitArea : MonoBehaviour
    {
        public bool isSource, isTarget;
        public PayloadType payloadType;
        public TargetType targetType;
        public int index;
        public string command;
        public string tooltip;
        public Image frame;
        public Color restingColor;
        public RectTransform Rect => (RectTransform)transform;
        public DropTarget Target(DinerSession session)
        {
            int id=targetType==TargetType.Guest && index<session.Guests.Length && session.Guests[index]!=null ? session.Guests[index].id : 0;
            return new DropTarget(targetType,index,id);
        }
        public bool Contains(Vector2 point) => gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(Rect,point,null);
        public void Highlight(bool active,bool valid)
        { if(frame!=null) frame.color=active ? (valid ? new Color(.3f,.57f,.47f) : new Color(.58f,.25f,.22f)) : restingColor; }
    }
}

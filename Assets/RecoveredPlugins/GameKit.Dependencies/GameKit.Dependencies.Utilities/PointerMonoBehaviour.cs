using UnityEngine;
using UnityEngine.EventSystems;

namespace GameKit.Dependencies.Utilities {
public abstract class PointerMonoBehaviour : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
	public void OnPointerEnter(PointerEventData eventData)
	{
		OnHovered(hovered: true, eventData);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		OnHovered(hovered: false, eventData);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		OnPressed(pressed: true, eventData);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		OnPressed(pressed: false, eventData);
	}

	public virtual void OnHovered(bool hovered, PointerEventData eventData)
	{
	}

	public virtual void OnPressed(bool pressed, PointerEventData eventData)
	{
	}
}

}
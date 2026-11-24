
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonDebug : MonoBehaviour,
    IPointerEnterHandler,
    IPointerDownHandler,
    IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("RETRY UI: Pointer entered button.");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("RETRY UI: Pointer pressed button.");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("RETRY UI: Button click received.");
    }
}

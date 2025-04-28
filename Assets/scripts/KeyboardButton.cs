using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class KeyboardButton : MonoBehaviour, IPointerClickHandler
{
    public KeyCode assignedKey; // Inspector 창에서 할당할 키 코드

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("가상 키보드 버튼 클릭 - 키: " + assignedKey);
    }

}
using UnityEngine;
using UnityEngine.EventSystems;

public class UIFirstSelection : MonoBehaviour
{
    [SerializeField] private GameObject firstSelected;

    private void OnEnable()
    {
        if (EventSystem.current == null || firstSelected == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);

        
    }
}
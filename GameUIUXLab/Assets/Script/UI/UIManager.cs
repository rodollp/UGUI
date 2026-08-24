using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Transform toastRoot;
    [SerializeField] private  ToastMessge toastPrefab;

    public void ShowToast(string message)
    {
        ToastMessge toast = Instantiate(toastPrefab, toastRoot);
        toast.Show(message);
    }

    public void ShowItemToast()
    {
        ShowToast("아이템을 획득했습니다.");
    }
}
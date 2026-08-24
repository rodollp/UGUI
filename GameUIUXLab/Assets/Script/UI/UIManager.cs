using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Transform toastRoot;
    [SerializeField] private ToastMessage toastPrefab;
    [SerializeField] private int poolSize = 5;

    private List<ToastMessage> toastPool = new();

    private void Awake()
    {
        CreatePool();
    }

    private void CreatePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            ToastMessage toast = Instantiate(toastPrefab, toastRoot);
            toast.gameObject.SetActive(false);

            toastPool.Add(toast);
        }
    }

    public void ShowToast(string message)
    {
        foreach (ToastMessage toast in toastPool)
        {
            if (!toast.gameObject.activeSelf)
            {
                toast.Show(message);
                return;
            }
        }
    }

    public void ShowItemToast()
    {
        ShowToast("아이템을 획득했습니다.");
    }
}
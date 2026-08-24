using System.Collections;
using TMPro;
using UnityEngine;

public class ToastMessage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float lifeTime = 2f;

    public void Show(string message)
    {
        messageText.text = message;
        gameObject.SetActive(true);

        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(lifeTime);

        gameObject.SetActive(false);
    }
}
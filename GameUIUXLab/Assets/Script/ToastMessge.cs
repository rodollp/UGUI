using System.Collections;
using TMPro;
using UnityEngine;

public class ToastMessge : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float lifeTime = 2.0f;

    public void Show(string message)
    {
        messageText.text = message;
        StartCoroutine(HideAfterDelay());

    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }

}

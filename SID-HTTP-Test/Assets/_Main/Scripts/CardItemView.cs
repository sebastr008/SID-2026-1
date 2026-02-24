using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CardItemView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RawImage avatar;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text speciesText;
    [SerializeField] private Image background;

    private int index;
    private Action<int> onClick;

    private Color normal = new Color(1, 1, 1, 0.2f);
    private Color selected = new Color(1, 1, 1, 0.6f);

    public void Bind(int idx, string title, string species, string imageUrl, Action<int> clickCb)
    {
        index = idx;
        onClick = clickCb;

        titleText.text = title;
        if (speciesText != null) speciesText.text = species ?? "";

        SetSelected(false);

        if (!string.IsNullOrEmpty(imageUrl))
            StartCoroutine(LoadImage(imageUrl));
        else
            avatar.texture = null;
    }

    public void SetSelected(bool isSelected)
    {
        if (background != null) background.color = isSelected ? selected : normal;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke(index);
    }

    private IEnumerator LoadImage(string url)
    {
        using (var req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;

            avatar.texture = DownloadHandlerTexture.GetContent(req);
        }
    }
}
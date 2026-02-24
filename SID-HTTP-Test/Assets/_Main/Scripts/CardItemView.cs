using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class CardItemView : MonoBehaviour
{
    [SerializeField] private RawImage avatar;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text speciesText;

    public void SetData(int id, string name, string species, string imageUrl)
    {
        titleText.text = $"#{id} - {name}";
        if (speciesText != null) speciesText.text = species;

        if (!string.IsNullOrEmpty(imageUrl))
            {
                StartCoroutine(LoadImage(imageUrl));
            }
            else
            {
                // opcional: dejar la RawImage apagada para que no se vea estirada
                 avatar.enabled = false;
            }

        StartCoroutine(LoadImage(imageUrl));
    }

    private IEnumerator LoadImage(string url)
    {
        Debug.Log("Cargando imagen: " + url);

        using (var req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Image error: {req.error} | code: {req.responseCode} | url: {url}");
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(req);

            if (tex == null)
            {
                Debug.LogError("Texture llegó null: " + url);
                yield break;
            }

            avatar.texture = tex;
            avatar.color = Color.white;   // por si estaba transparente
            avatar.enabled = true;
        }
    }
}
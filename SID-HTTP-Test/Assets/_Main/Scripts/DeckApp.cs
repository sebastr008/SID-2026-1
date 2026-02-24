using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class DeckApp : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string baseUrl = "https://rickandmortyapi.com/api/character";
    [SerializeField] private string fakeApiBase = "https://my-json-server.typicode.com/sebastr008/SID-2026-1";

    [Header("Jugadores")]
    [SerializeField] private string[] playerNames = { "Arnoldo", "Pepito", "Arnold Schwarznegger", "Jugador API Falsa" };
    [SerializeField] private int[] playerPages = { 1, 2, 3, -1 }; // -1 = deck desde db.json
    [SerializeField] private int customDeckId = 1; // /customDecks/1

    [Header("Tamaño de baraja")]
    [SerializeField] private int deckSize = 6;

    [Header("UI")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private ScrollRect scrollRect;

    [SerializeField] private Transform cardsParent;   // Content del ScrollView
    [SerializeField] private CardItemView cardPrefab; // Prefab del Project

    private int currentPlayerIndex = 0;
    private bool isLoading = false;
    private Coroutine currentLoad;

    private void Start()
    {
        prevButton.onClick.AddListener(() => ChangePlayer(-1));
        nextButton.onClick.AddListener(() => ChangePlayer(1));
        LoadCurrentPlayer();
    }

    private void ChangePlayer(int dir)
    {
        if (isLoading) return;

        currentPlayerIndex = (currentPlayerIndex + dir + playerNames.Length) % playerNames.Length;
        LoadCurrentPlayer();
    }

    private void LoadCurrentPlayer()
    {
        if (currentLoad != null) StopCoroutine(currentLoad);

        ClearCards();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;

        currentLoad = StartCoroutine(LoadDeckForCurrentPlayer());
    }

    private IEnumerator LoadDeckForCurrentPlayer()
    {
        isLoading = true;
        prevButton.interactable = false;
        nextButton.interactable = false;

        try
        {
            int page = playerPages[currentPlayerIndex];

            if (page == -1)
            {
                // ESTE jugador usa el deck de tu db.json
                playerNameText.text = "Cargando...";
                yield return LoadDeckFromDbCards(customDeckId);
            }
            else
            {
                // Estos jugadores usan Rick & Morty por page
                playerNameText.text = playerNames[currentPlayerIndex];
                yield return LoadDeckFromPage(page);
            }
        }
        finally
        {
            isLoading = false;
            prevButton.interactable = true;
            nextButton.interactable = true;
        }
    }

    private IEnumerator LoadDeckFromPage(int page)
    {
        string url = $"{baseUrl}?page={page}";

        string json = null;
        yield return GetJsonWithRetry(url, t => json = t);
        if (string.IsNullOrEmpty(json)) yield break;

        CharacterPageResponse response = JsonUtility.FromJson<CharacterPageResponse>(json);
        if (response == null || response.results == null)
        {
            Debug.LogError("No pude parsear el JSON (response/results null).");
            yield break;
        }

        int count = Mathf.Min(deckSize, response.results.Length);
        for (int i = 0; i < count; i++)
        {
            Character c = response.results[i];

            var item = Instantiate(cardPrefab, cardsParent);
            item.SetData(c.id, c.name, c.species, c.image);

            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator LoadDeckFromDbCards(int deckId)
    {
        // Pide el deck (objeto) desde db.json
        string deckUrl = $"{fakeApiBase}/customDecks/{deckId}";

        string deckJson = null;
        yield return GetJsonWithRetry(deckUrl, t => deckJson = t);
        if (string.IsNullOrEmpty(deckJson)) yield break;

        DbDeckDto deck = JsonUtility.FromJson<DbDeckDto>(deckJson);
        if (deck == null || deck.cards == null)
        {
            Debug.LogError("DbDeckDto vino null o sin cards.");
            yield break;
        }

        // Nombre del jugador DB viene del db.json (ownerName)
        playerNameText.text = deck.ownerName;

        int count = Mathf.Min(deckSize, deck.cards.Length);
        for (int i = 0; i < count; i++)
        {
            DbCardDto c = deck.cards[i];

            var item = Instantiate(cardPrefab, cardsParent);

            // Aquí las cartas SON tus nombres del db.json
            // species lo dejamos vacío
            item.SetData(c.id, c.name, "", c.image);

            yield return new WaitForSeconds(0.03f);
        }
    }

    private IEnumerator GetJsonWithRetry(string url, Action<string> onOk)
    {
        int attempts = 0;

        while (attempts < 3)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();

                if (req.responseCode == 429)
                {
                    attempts++;
                    yield return new WaitForSeconds(1.0f * attempts);
                    continue;
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"API error: {req.error} code:{req.responseCode} url:{url}");
                    yield break;
                }

                onOk?.Invoke(req.downloadHandler.text);
                yield break;
            }
        }

        Debug.LogError($"429 persistente. url:{url}");
    }

    private void ClearCards()
    {
        for (int i = cardsParent.childCount - 1; i >= 0; i--)
            Destroy(cardsParent.GetChild(i).gameObject);
    }
}
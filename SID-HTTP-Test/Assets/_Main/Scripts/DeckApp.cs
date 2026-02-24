using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private string[] playerNames = { "Arnoldo", "Pepito", "Arnold Schwarznegger", "Jugador DB" };
    [SerializeField] private int[] playerPages = { 1, 2, 3, -1 }; // -1 => db.json customDecks
    [SerializeField] private int customDeckId = 1;

    [Header("Deck")]
    [SerializeField] private int deckSize = 6;

    [Header("UI")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private ScrollRect scrollRect;

    [SerializeField] private Transform cardsParent;   // Content del ScrollView
    [SerializeField] private CardItemView cardPrefab;

    [Header("Swap UI")]
    [SerializeField] private TMP_Dropdown targetPlayerDropdown;
    [SerializeField] private Button swapButton;

    // ====== Estado interno ======
    private class CardData
    {
        public int id;
        public string name;
        public string species;
        public string status;
        public string imageUrl;
    }

    private class PlayerDeck
    {
        public string displayName;
        public bool loaded;
        public List<CardData> cards = new List<CardData>();
    }

    private PlayerDeck[] decks;
    private int currentPlayerIndex = 0;
    private int selectedCardIndex = -1;

    private bool isLoading = false;
    private Coroutine currentLoad;

    private void Start()
    {
        decks = new PlayerDeck[playerNames.Length];
        for (int i = 0; i < decks.Length; i++)
            decks[i] = new PlayerDeck { displayName = playerNames[i], loaded = false };

        prevButton.onClick.AddListener(() => ChangePlayer(-1));
        nextButton.onClick.AddListener(() => ChangePlayer(1));

        // Dropdown targets
        targetPlayerDropdown.ClearOptions();
        targetPlayerDropdown.AddOptions(new List<string>(playerNames));

        swapButton.onClick.AddListener(SwapSelectedWithTarget);
        swapButton.interactable = false;

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

        selectedCardIndex = -1;
        swapButton.interactable = false;

        currentLoad = StartCoroutine(EnsureDeckLoadedThenRender(currentPlayerIndex));
    }

    private IEnumerator EnsureDeckLoadedThenRender(int playerIndex)
    {
        isLoading = true;
        prevButton.interactable = false;
        nextButton.interactable = false;
        swapButton.interactable = false;

        try
        {
            if (!decks[playerIndex].loaded)
            {
                int page = playerPages[playerIndex];
                if (page == -1) yield return BuildDeckFromDb(playerIndex);
                else yield return BuildDeckFromPage(playerIndex, page);
            }

            RenderDeck(playerIndex);
            playerNameText.text = decks[playerIndex].displayName;
        }
        finally
        {
            isLoading = false;
            prevButton.interactable = true;
            nextButton.interactable = true;
        }
    }

    private void RenderDeck(int playerIndex)
    {
        ClearCards();

        var deck = decks[playerIndex];
        int count = Mathf.Min(deckSize, deck.cards.Count);

        for (int i = 0; i < count; i++)
        {
            var c = deck.cards[i];
            var item = Instantiate(cardPrefab, cardsParent);

            string title = $"#{c.id} - {c.name}";
            item.Bind(i, title, c.species, c.status, c.imageUrl, OnCardClicked);
        }
    }

    private void OnCardClicked(int idx)
    {
        selectedCardIndex = idx;
        swapButton.interactable = true;

        // actualiza highlight
        for (int i = 0; i < cardsParent.childCount; i++)
        {
            var view = cardsParent.GetChild(i).GetComponent<CardItemView>();
            if (view != null) view.SetSelected(i == selectedCardIndex);
        }
    }

    private void SwapSelectedWithTarget()
    {
        if (isLoading) return;
        if (selectedCardIndex < 0) return;

        int targetIndex = targetPlayerDropdown.value;
        if (targetIndex == currentPlayerIndex) return;

        StartCoroutine(SwapCoroutine(targetIndex, selectedCardIndex));
    }

    private IEnumerator SwapCoroutine(int targetPlayerIndex, int slotIndex)
    {
        // asegura que el target tenga deck cargado (si nunca lo has visitado)
        if (!decks[targetPlayerIndex].loaded)
        {
            isLoading = true;
            prevButton.interactable = false;
            nextButton.interactable = false;
            swapButton.interactable = false;

            int page = playerPages[targetPlayerIndex];
            if (page == -1) yield return BuildDeckFromDb(targetPlayerIndex);
            else yield return BuildDeckFromPage(targetPlayerIndex, page);

            isLoading = false;
            prevButton.interactable = true;
            nextButton.interactable = true;
        }

        var a = decks[currentPlayerIndex];
        var b = decks[targetPlayerIndex];

        if (slotIndex >= a.cards.Count || slotIndex >= b.cards.Count) yield break;

        // swap por slot (misma posición)
        CardData tmp = a.cards[slotIndex];
        a.cards[slotIndex] = b.cards[slotIndex];
        b.cards[slotIndex] = tmp;

        // refresca el deck actual en pantalla
        selectedCardIndex = -1;
        swapButton.interactable = false;
        RenderDeck(currentPlayerIndex);
    }

    private IEnumerator BuildDeckFromPage(int playerIndex, int page)
    {
        string url = $"{baseUrl}?page={page}";

        string json = null;
        yield return GetJsonWithRetry(url, t => json = t);
        if (string.IsNullOrEmpty(json)) yield break;

        CharacterPageResponse response = JsonUtility.FromJson<CharacterPageResponse>(json);
        if (response == null || response.results == null) yield break;

        var deck = decks[playerIndex];
        deck.cards.Clear();

        int count = Mathf.Min(deckSize, response.results.Length);
        for (int i = 0; i < count; i++)
        {
            Character c = response.results[i];
            deck.cards.Add(new CardData
            {
                id = c.id,
                name = c.name,
                species = c.species,
                status = c.status,
                imageUrl = c.image
            });
        }

        deck.displayName = playerNames[playerIndex];
        deck.loaded = true;
    }

    private IEnumerator BuildDeckFromDb(int playerIndex)
    {
        // trae tu deck de db.json: /customDecks/1
        string deckUrl = $"{fakeApiBase}/customDecks/{customDeckId}";

        string deckJson = null;
        yield return GetJsonWithRetry(deckUrl, t => deckJson = t);
        if (string.IsNullOrEmpty(deckJson)) yield break;

        DbDeckDto deckDto = JsonUtility.FromJson<DbDeckDto>(deckJson);
        if (deckDto == null || deckDto.cards == null) yield break;

        var deck = decks[playerIndex];
        deck.cards.Clear();

        int count = Mathf.Min(deckSize, deckDto.cards.Length);
        for (int i = 0; i < count; i++)
        {
            DbCardDto c = deckDto.cards[i];
            deck.cards.Add(new CardData
            {
                id = c.id,
                name = c.name,
                species = c.species,
                status = c.status,
                imageUrl = c.image // puede ser "" y CardItemView lo ignora
            });
        }

        deck.displayName = deckDto.ownerName; // nombre del “Jugador DB” desde db.json
        deck.loaded = true;
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
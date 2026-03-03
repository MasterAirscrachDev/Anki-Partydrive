using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static OverdriveServer.NetStructures;

public class ChargeSuspendUIController : MonoBehaviour
{
    //elements of the stat card
    //Parent (Theme to player color but desaturated)
    //0 - carIconBackground (theme to player color)
    //1 - carIcon set to car sprite
    //2 - playerName text set to player name
    //3 - carName text set to car name
    //4 - Set to players best stat (eg, top damage would be "The Destroyer")
    //5 - Stats text "DMG DEALT: X\nDMG TAKEN: X\nBOOST TIME: x:xx\nITEMS COLLECTED: X\nDISABLES DEALT: X"
    [SerializeField] GameObject playerStatsCardPrefab;
    //positions of the player cards in child UI space, set in inspector
    //0 = 1 player
    //1,2 = P1 and P2 (2 players)
    //3,4,5 = P1, P2 and P3 (3 players)
    //6,7,8,9 = P1, P2, P3 and P4 (4 players)
    //10,11,12,13,14 = P1, P2, P3, P4 and P5 (5 players)
    //15,16,17,18,19,20 = P1, P2, P3, P4, P5 and P6 (6 players)
    [SerializeField] Vector2[] playerCardPositions;
    [SerializeField] CarSprite[] carSprites;

    readonly List<GameObject> spawnedCards = new List<GameObject>();

    void OnEnable()
    {
        CreateStatCards();
        SR.cms.SetConnectionSuspended(true);
    }

    void OnDisable()
    {
        ClearCards();
    }

    void CreateStatCards()
    {
        ClearCards();
        CMS cms = SR.cms;
        List<CarController> controllers = cms.controllers;
        int count = controllers.Count;
        if(count == 0) return;

        // Position offset: for n players the block starts at index n*(n-1)/2
        int posOffset = count * (count - 1) / 2;

        // First pass: find the best value for each stat across all players
        float bestDmgDealt = 0f, bestDmgTaken = 0f, bestBoost = 0f;
        int bestItems = 0, bestDisables = 0;
        foreach(CarController cc in controllers)
        {
            if(cc == null) continue;
            PlayerStats s = cc.GetPlayerStats();
            if(s.TotalDamageDealt > bestDmgDealt) bestDmgDealt = s.TotalDamageDealt;
            if(s.TotalDamageTaken > bestDmgTaken) bestDmgTaken = s.TotalDamageTaken;
            if(s.TotalBoostTime > bestBoost) bestBoost = s.TotalBoostTime;
            if(s.TotalAbilityPickups > bestItems) bestItems = s.TotalAbilityPickups;
            if(s.TotalDisables > bestDisables) bestDisables = s.TotalDisables;
        }

        for(int i = 0; i < count; i++)
        {
            CarController cc = controllers[i];
            if(cc == null) continue;

            GameObject card = Instantiate(playerStatsCardPrefab, transform);
            spawnedCards.Add(card);

            // Position the card
            RectTransform rt = card.GetComponent<RectTransform>();
            int posIndex = posOffset + i;
            if(posIndex < playerCardPositions.Length)
                rt.anchoredPosition = playerCardPositions[posIndex];

            Color playerColor = cc.GetPlayerColor();

            // Parent background — desaturated player color
            Image parentImage = card.GetComponent<Image>();
            if(parentImage != null)
            {
                Color.RGBToHSV(playerColor, out float h, out float s, out float v);
                parentImage.color = Color.HSVToRGB(h, s * 0.3f, v * 0.6f);
            }

            Transform cardT = card.transform;

            // 0 - carIconBackground (player color)
            Image iconBg = cardT.GetChild(0).GetComponent<Image>();
            if(iconBg != null) iconBg.color = playerColor;

            // 1 - carIcon (car sprite)
            Image carIcon = cardT.GetChild(1).GetComponent<Image>();
            if(carIcon != null)
            {
                string carID = cc.GetDesiredCarID();
                ModelName model = cms.CarModelFromId(carID);
                carIcon.sprite = GetCarSprite(model);
            }

            // 2 - playerName
            TMP_Text playerNameText = cardT.GetChild(2).GetComponent<TMP_Text>();
            if(playerNameText != null) playerNameText.text = cc.GetPlayerName();

            // 3 - carName
            TMP_Text carNameText = cardT.GetChild(3).GetComponent<TMP_Text>();
            if(carNameText != null)
            {
                string carID = cc.GetDesiredCarID();
                carNameText.text = cms.CarNameFromId(carID);
            }

            // 4 - best stat title
            PlayerStats stats = cc.GetPlayerStats();
            TMP_Text bestStatText = cardT.GetChild(4).GetComponent<TMP_Text>();
            if(bestStatText != null) bestStatText.text = GetBestStatTitle(stats);

            // 5 - stats text (gold highlight lines where this player leads)
            TMP_Text statsText = cardT.GetChild(5).GetComponent<TMP_Text>();
            if(statsText != null)
            {
                int boostMin = Mathf.FloorToInt(stats.TotalBoostTime / 60f);
                int boostSec = Mathf.FloorToInt(stats.TotalBoostTime % 60f);

                string dmgDealtLine = $"DMG DEALT: {Mathf.RoundToInt(stats.TotalDamageDealt)}";
                string dmgTakenLine = $"DMG TAKEN: {Mathf.RoundToInt(stats.TotalDamageTaken)}";
                string boostLine = $"BOOST TIME: {boostMin}:{boostSec:D2}";
                string itemsLine = $"ITEMS COLLECTED: {stats.TotalAbilityPickups}";
                string disablesLine = $"DISABLES DEALT: {stats.TotalDisables}";

                if(stats.TotalDamageDealt > 0 && stats.TotalDamageDealt >= bestDmgDealt)
                    dmgDealtLine = $"<color=yellow>{dmgDealtLine}</color>";
                if(stats.TotalDamageTaken > 0 && stats.TotalDamageTaken >= bestDmgTaken)
                    dmgTakenLine = $"<color=yellow>{dmgTakenLine}</color>";
                if(stats.TotalBoostTime > 0 && stats.TotalBoostTime >= bestBoost)
                    boostLine = $"<color=yellow>{boostLine}</color>";
                if(stats.TotalAbilityPickups > 0 && stats.TotalAbilityPickups >= bestItems)
                    itemsLine = $"<color=yellow>{itemsLine}</color>";
                if(stats.TotalDisables > 0 && stats.TotalDisables >= bestDisables)
                    disablesLine = $"<color=yellow>{disablesLine}</color>";

                statsText.text = $"{dmgDealtLine}\n{dmgTakenLine}\n{boostLine}\n{itemsLine}\n{disablesLine}";
            }
        }
    }

    static string GetBestStatTitle(PlayerStats stats)
    {
        // Map each stat to a title; pick the highest relative contribution
        float dmgDealt = stats.TotalDamageDealt;
        float dmgTaken = stats.TotalDamageTaken;
        float boostTime = stats.TotalBoostTime;
        int items = stats.TotalAbilityPickups;
        int disables = stats.TotalDisables;

        // Use a simple comparison — the stat with the greatest "weight" wins
        string title = "The Racer"; // default fallback
        float best = 0f;

        if(dmgDealt > best){ best = dmgDealt; title = "The Destroyer"; }
        if(dmgTaken > best){ best = dmgTaken; title = "The Tank"; }
        if(boostTime * 10f > best){ best = boostTime * 10f; title = "Speed Demon"; }
        if(items * 20f > best){ best = items * 20f; title = "The Collector"; }
        if(disables * 30f > best){ best = disables * 30f; title = "The Disabler"; }

        return title;
    }

    Sprite GetCarSprite(ModelName model)
    {
        if(carSprites == null) return null;
        // Index 0 is the fallback/unknown sprite
        Sprite fallback = carSprites.Length > 0 ? carSprites[0].sprite : null;
        foreach(CarSprite cs in carSprites)
        {
            if(cs.id == (int)model) return cs.sprite;
        }
        return fallback;
    }

    void ClearCards()
    {
        foreach(GameObject card in spawnedCards)
        {
            if(card != null) Destroy(card);
        }
        spawnedCards.Clear();
    }

    void Resume()
    {
        SR.cms.SetConnectionSuspended(false);
    }

    //called from UI
    public void ReturnToMenu()
    {
        Resume();
        SR.cms.OnBackToMenuCallback();
        SR.ui.SetUILayer("Menu");
    }

    public void BackToGamemode()
    {
        Resume();
        SR.cms.LoadGamemode();
    }

    void OnDrawGizmosSelected() {
        Color[] colors = new Color[]{ Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };
        for(int i = 0; i < playerCardPositions.Length; i++){
            if(i == 0){ Gizmos.color = colors[0]; }
            else if(i == 1){ Gizmos.color = colors[1]; }
            else if(i == 3){ Gizmos.color = colors[2]; }
            else if(i == 6){ Gizmos.color = colors[3]; }
            else if(i == 10){ Gizmos.color = colors[4]; }
            else if(i == 15){ Gizmos.color = colors[5]; }

            Gizmos.DrawSphere(gameObject.GetComponent<RectTransform>().TransformPoint(playerCardPositions[i]), 10);
        }
    }

    [System.Serializable]
    struct CarSprite
    {
        public int id;
        public Sprite sprite;
    }
}

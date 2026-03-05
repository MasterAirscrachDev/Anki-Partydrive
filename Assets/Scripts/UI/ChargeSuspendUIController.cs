using System.Collections.Generic;
using System.Collections;
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
        //check if any players are sitting out/disconnected, and if so we can skip them in the positioning and stats
        for(int i = 0; i < controllers.Count; i++)
        {
            if(!controllers[i].IsCarConnected())
            { count--; }
        }

        // Position offset: for n players the block starts at index n*(n-1)/2
        int posOffset = count * (count - 1) / 2;

        // First pass: find the best value for each stat across all connected players
        float bestDmgDealt = 0f, bestDmgTaken = 0f, bestBoost = 0f;
        int bestItems = 0, bestDisables = 0;
        foreach(CarController cc in controllers)
        {
            if(cc == null || !cc.IsCarConnected()) continue;
            PlayerStats s = cc.GetPlayerStats();
            if(s.TotalDamageDealt > bestDmgDealt) bestDmgDealt = s.TotalDamageDealt;
            if(s.TotalDamageTaken > bestDmgTaken) bestDmgTaken = s.TotalDamageTaken;
            if(s.TotalBoostTime > bestBoost) bestBoost = s.TotalBoostTime;
            if(s.TotalAbilityPickups > bestItems) bestItems = s.TotalAbilityPickups;
            if(s.TotalDisables > bestDisables) bestDisables = s.TotalDisables;
        }

        // Second pass: count how many players share each best value.
        // A title is only awarded if exactly one player leads that stat.
        int dmgDealtLeaders = 0, dmgTakenLeaders = 0, boostLeaders = 0, itemLeaders = 0, disableLeaders = 0;
        foreach(CarController cc in controllers)
        {
            if(cc == null || !cc.IsCarConnected()) continue;
            PlayerStats s = cc.GetPlayerStats();
            if(bestDmgDealt > 0 && s.TotalDamageDealt >= bestDmgDealt) dmgDealtLeaders++;
            if(bestDmgTaken > 0 && s.TotalDamageTaken >= bestDmgTaken) dmgTakenLeaders++;
            if(bestBoost > 0 && s.TotalBoostTime >= bestBoost) boostLeaders++;
            if(bestItems > 0 && s.TotalAbilityPickups >= bestItems) itemLeaders++;
            if(bestDisables > 0 && s.TotalDisables >= bestDisables) disableLeaders++;
        }
        bool uniqueDmgDealt   = dmgDealtLeaders  == 1;
        bool uniqueDmgTaken   = dmgTakenLeaders  == 1;
        bool uniqueBoost      = boostLeaders     == 1;
        bool uniqueItems      = itemLeaders      == 1;
        bool uniqueDisables   = disableLeaders   == 1;

        int cardIndex = 0;
        for(int i = 0; i < controllers.Count; i++)
        {
            CarController cc = controllers[i];
            if(cc == null || !cc.IsCarConnected()) continue;

            GameObject card = Instantiate(playerStatsCardPrefab, transform);
            spawnedCards.Add(card);

            // Position the card
            RectTransform rt = card.GetComponent<RectTransform>();
            int posIndex = posOffset + cardIndex;
            cardIndex++;
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
            RawImage carIcon = cardT.GetChild(1).GetComponent<RawImage>();
            if(carIcon != null)
            {
                string carID = cc.GetDesiredCarID();
                ModelName model = cms.CarModelFromId(carID);
                carIcon.texture = GetCarSprite(model).texture;
                //Debug.Log($"Set car icon for player {cc.GetPlayerName()} to model {model}");
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

            // 4 - best stat title (awarded to the sole leader of each stat)
            PlayerStats stats = cc.GetPlayerStats();
            TMP_Text bestStatText = cardT.GetChild(4).GetComponent<TMP_Text>();
            if(bestStatText != null) bestStatText.text = GetBestStatTitle(stats,
                bestDmgDealt, uniqueDmgDealt,
                bestDmgTaken, uniqueDmgTaken,
                bestBoost,    uniqueBoost,
                bestItems,    uniqueItems,
                bestDisables, uniqueDisables);

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
        StartCoroutine(DisconnectDelayed());
    }
    IEnumerator DisconnectDelayed()
    {
        yield return new WaitForSeconds(1.5f);
        
        SR.cms.SetConnectionSuspended(true);
    }

    // Awards a title to a player if they are the sole leader of a stat.
    // Priority order is used when a player leads multiple stats.
    // Returns "The Racer" if the player leads nothing outright.
    static string GetBestStatTitle(PlayerStats stats,
        float bestDmgDealt,   bool uniqueDmgDealt,
        float bestDmgTaken,   bool uniqueDmgTaken,
        float bestBoost,      bool uniqueBoost,
        int   bestItems,      bool uniqueItems,
        int   bestDisables,   bool uniqueDisables)
    {
        if(uniqueDmgDealt   && stats.TotalDamageDealt    >= bestDmgDealt)   return "The Destroyer";
        if(uniqueDisables   && stats.TotalDisables       >= bestDisables)   return "The Disabler";
        if(uniqueDmgTaken   && stats.TotalDamageTaken    >= bestDmgTaken)   return "The Tank";
        if(uniqueBoost      && stats.TotalBoostTime      >= bestBoost)      return "Speed Demon";
        if(uniqueItems      && stats.TotalAbilityPickups >= bestItems)      return "The Collector";
        return "The Racer";
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
        Debug.LogWarning($"Car sprite for model {model} not found, using fallback.");
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
        Debug.Log("Resuming game and returning to gamemode UI");
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

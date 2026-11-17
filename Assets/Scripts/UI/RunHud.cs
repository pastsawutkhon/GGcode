using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ATM.Gameplay
{
    public sealed class RunHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text waveLabel;
        [SerializeField] private TMP_Text gameGoldLabel;
        [SerializeField] private TMP_Text cardGoldLabel;
        [SerializeField] private TMP_Text rerollLabel;
        [SerializeField] private GameObject inGamePanel;
        [SerializeField] private GameObject cardPanel;
        [SerializeField] private UpgradeCardView[] cardViews;
        [SerializeField] private Button rerollButton;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private Button restartButton;

        private static readonly Key[] CardKeys = { Key.Digit1, Key.Digit2, Key.Digit3 };
        private GameRunManager run;

        internal void Initialize(GameRunManager runManager)
        {
            run = runManager;
            for (int i = 0; i < cardViews.Length; i++)
            {
                int cardIndex = i;
                cardViews[i].Bind(() => run.ChooseCard(cardIndex));
            }
            rerollButton.onClick.AddListener(run.RerollCards);
            restartButton.onClick.AddListener(run.Restart);
        }

        internal void ShowWave(int number)
        {
            waveLabel.text = $"Wave {number}";
            inGamePanel.SetActive(true);
            cardPanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        internal void ShowGold(long gold, long cost)
        {
            gameGoldLabel.text = $"Gold {gold}";
            cardGoldLabel.text = gameGoldLabel.text;
            rerollLabel.text = cost == 0 ? "Reroll (Free)" : $"Reroll ({cost})";
            rerollButton.interactable = gold >= cost;
        }

        internal void ShowCards(UpgradeCard[] cards, CharacterStats stats)
        {
            for (int i = 0; i < cards.Length; i++)
                cardViews[i].Show(cards[i], stats);
            inGamePanel.SetActive(false);
            cardPanel.SetActive(true);
        }

        internal void ShowGameOver(int wave)
        {
            cardPanel.SetActive(false);
            inGamePanel.SetActive(true);
            resultLabel.text = $"Reached Wave {wave}";
            gameOverPanel.SetActive(true);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (run == null || keyboard == null)
                return;

            if (run.Phase == RunPhase.GameOver)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                    run.Restart();
                return;
            }

            if (run.Phase != RunPhase.ChoosingCard)
                return;

            if (keyboard.rKey.wasPressedThisFrame)
            {
                run.RerollCards();
                return;
            }

            for (int i = 0; i < CardKeys.Length; i++)
            {
                if (keyboard[CardKeys[i]].wasPressedThisFrame)
                {
                    run.ChooseCard(i);
                    return;
                }
            }
        }
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ATM.Gameplay
{
    public sealed class UpgradeCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image artwork;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text tier;
        [SerializeField] private TMP_Text effect;
        [SerializeField] private TMP_Text preview;
        [SerializeField] private Sprite commonArt;
        [SerializeField] private Sprite uncommonArt;
        [SerializeField] private Sprite rareArt;
        [SerializeField] private Sprite epicArt;
        [SerializeField] private Sprite legendaryArt;

        internal void Bind(Action onSelected) => button.onClick.AddListener(() => onSelected());

        internal void Show(UpgradeCard card, CharacterStats stats)
        {
            artwork.sprite = card.Rarity switch
            {
                UpgradeRarity.Uncommon => uncommonArt,
                UpgradeRarity.Rare => rareArt,
                UpgradeRarity.Epic => epicArt,
                UpgradeRarity.Legendary => legendaryArt,
                _ => commonArt
            };
            title.text = card.Title;
            tier.text = card.Numeral;
            effect.text = card.EffectText;
            preview.text = card.BuildPreview(stats);
        }
    }
}

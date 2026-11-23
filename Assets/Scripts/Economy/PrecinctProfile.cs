using UnityEngine;

namespace ATM.Gameplay
{
    internal enum Training { Vitality, Power, Defense, Bounty }

    internal sealed class PrecinctProfile
    {
        [System.Serializable]
        private class Save
        {
            public long credits;
            public int vitality, power, defense, bounty;
            public string lastRun;
        }

        private const string Key = "ATM.Profile.1";
        private readonly Save data = JsonUtility.FromJson<Save>(PlayerPrefs.GetString(Key, "{}")) ?? new Save();
        public long Credits => data.credits;
        public int Level(Training track) => track switch
        {
            Training.Vitality => data.vitality,
            Training.Power => data.power,
            Training.Defense => data.defense,
            _ => data.bounty
        };
        public System.Numerics.BigInteger Price(Training track)
        {
            System.Numerics.BigInteger next = (long)Level(track) + 1;
            return 100 * next * next;
        }
        public bool CanBuy(Training track) => Credits >= Price(track);
        public void Buy(Training track)
        {
            if (!CanBuy(track)) return;
            data.credits -= (long)Price(track);
            switch (track)
            {
                case Training.Vitality: data.vitality++; break;
                case Training.Power: data.power++; break;
                case Training.Defense: data.defense++; break;
                case Training.Bounty: data.bounty++; break;
            }
            Store();
        }
        public void Bank(string runId, long amount)
        {
            if (runId == data.lastRun) return;
            data.credits += System.Math.Min(amount, long.MaxValue - data.credits);
            data.lastRun = runId;
            Store();
        }
        public void Apply(CharacterStats stats) =>
            stats.ApplyBaseTraining(data.vitality, data.power, data.defense);

        private void Store()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}

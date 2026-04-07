// =============================================================================
// CurrencyManager.cs
// Manages all currency balances for all players.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Tracks and mutates currency balances for every registered currency type.
    /// Supports multiple players (split-screen) via player index.
    /// </summary>
    public class CurrencyManager : MonoBehaviour, ISaveable
    {
        #region Inspector Fields

        [Header("Currencies")]
        [Tooltip("All CurrencySO assets used in the game.")]
        [SerializeField] private List<CurrencySO> _currencies = new List<CurrencySO>();

        [Header("Multi-Player")]
        [SerializeField] [Range(1, 4)] private int _playerCount = 1;

        #endregion

        #region Data

        // [playerIndex][currencyID] = balance
        private Dictionary<int, Dictionary<string, long>> _balances
            = new Dictionary<int, Dictionary<string, long>>();

        // Fast lookup: currencyID → CurrencySO
        private Dictionary<string, CurrencySO> _currencyMap = new Dictionary<string, CurrencySO>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ServiceLocator.Register<CurrencyManager>(this);
            InitialiseCurrencies();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<CurrencyManager>();
        }

        #endregion

        #region Initialisation

        private void InitialiseCurrencies()
        {
            _currencyMap.Clear();
            foreach (var c in _currencies)
            {
                if (c == null || string.IsNullOrEmpty(c.CurrencyID)) continue;
                _currencyMap[c.CurrencyID] = c;
            }

            _balances.Clear();
            for (int i = 0; i < _playerCount; i++)
            {
                var playerBalances = new Dictionary<string, long>();
                foreach (var c in _currencies)
                {
                    if (c == null) continue;
                    playerBalances[c.CurrencyID] = c.StartingAmount;
                }
                _balances[i] = playerBalances;
            }
        }

        #endregion

        #region Public API

        /// <summary>Returns the current balance of <paramref name="currencyID"/> for the player.</summary>
        public long GetBalance(string currencyID, int playerIndex = 0)
        {
            if (!_balances.TryGetValue(playerIndex, out var pb)) return 0;
            pb.TryGetValue(currencyID, out long bal);
            return bal;
        }

        /// <summary>Returns the CurrencySO for the given ID, or null.</summary>
        public CurrencySO GetCurrency(string currencyID)
        {
            _currencyMap.TryGetValue(currencyID, out var c);
            return c;
        }

        /// <summary>All registered currencies.</summary>
        public IReadOnlyList<CurrencySO> AllCurrencies => _currencies;

        /// <summary>
        /// Attempts to deduct <paramref name="amount"/> of <paramref name="currencyID"/>.
        /// Returns true on success. Returns false if balance would go negative.
        /// </summary>
        public bool Spend(string currencyID, long amount, int playerIndex = 0)
        {
            if (amount <= 0) return true;
            var bal = GetBalance(currencyID, playerIndex);
            if (bal < amount)
            {
                Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Player {playerIndex} cannot afford {amount} {currencyID} (has {bal}).");
                return false;
            }
            SetBalance(currencyID, bal - amount, playerIndex);
            return true;
        }

        /// <summary>Adds <paramref name="amount"/> of <paramref name="currencyID"/> to the player's balance.</summary>
        public void Add(string currencyID, long amount, int playerIndex = 0)
        {
            if (amount <= 0) return;
            var currency = GetCurrency(currencyID);
            var current  = GetBalance(currencyID, playerIndex);
            long newBal  = current + amount;

            if (currency != null && currency.MaxAmount > 0)
                newBal = Math.Min(newBal, currency.MaxAmount);

            SetBalance(currencyID, newBal, playerIndex);
        }

        /// <summary>Directly sets the balance (use carefully — prefers Add/Spend for gameplay).</summary>
        public void SetBalance(string currencyID, long amount, int playerIndex = 0)
        {
            if (!_balances.TryGetValue(playerIndex, out var pb))
            {
                pb = new Dictionary<string, long>();
                _balances[playerIndex] = pb;
            }

            var currency = GetCurrency(currencyID);
            if (currency != null && currency.MaxAmount > 0)
                amount = Math.Min(amount, currency.MaxAmount);
            amount = Math.Max(0, amount);

            long oldBal = GetBalance(currencyID, playerIndex);
            pb[currencyID] = amount;

            InventoryEventBus.Publish(new CurrencyChangedEvent
            {
                CurrencyID  = currencyID,
                OldBalance  = oldBal,
                NewBalance  = amount,
                PlayerIndex = playerIndex
            });
        }

        /// <summary>True if the player can afford the given amount.</summary>
        public bool CanAfford(string currencyID, long amount, int playerIndex = 0)
            => GetBalance(currencyID, playerIndex) >= amount;

        #endregion

        #region ISaveable

        public string SaveKey => "CurrencyManager";

        public object CaptureState()
        {
            var data = new CurrencySaveData
            {
                PlayerBalances = new List<PlayerCurrencySaveData>()
            };

            foreach (var kvp in _balances)
            {
                var playerData = new PlayerCurrencySaveData
                {
                    PlayerIndex = kvp.Key,
                    Entries     = new List<CurrencyEntry>()
                };
                foreach (var bal in kvp.Value)
                    playerData.Entries.Add(new CurrencyEntry { CurrencyID = bal.Key, Balance = bal.Value });
                data.PlayerBalances.Add(playerData);
            }
            return data;
        }

        public void RestoreState(object data)
        {
            if (data is not CurrencySaveData saveData) return;
            _balances.Clear();

            foreach (var playerData in saveData.PlayerBalances)
            {
                var pb = new Dictionary<string, long>();
                foreach (var entry in playerData.Entries)
                    pb[entry.CurrencyID] = entry.Balance;
                _balances[playerData.PlayerIndex] = pb;
            }
        }

        #endregion
    }

    // ── Save data POCOs ───────────────────────────────────────────────────────

    [Serializable]
    public class CurrencyEntry
    {
        public string CurrencyID;
        public long   Balance;
    }

    [Serializable]
    public class PlayerCurrencySaveData
    {
        public int               PlayerIndex;
        public List<CurrencyEntry> Entries;
    }

    [Serializable]
    public class CurrencySaveData
    {
        public List<PlayerCurrencySaveData> PlayerBalances;
    }
}

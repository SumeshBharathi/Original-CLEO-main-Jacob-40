using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using System.Text;
using UnityEngine.SceneManagement;

namespace CSFramework {
	/// <summary>
	/// A class that contains information of a slot.
	/// </summary>
	[Serializable]
	public class GameInfo {
		private CustomSlot slot;
		public int roundsCompleted = 0;
		public int freeSpins = 0;
		public int bonuses = 0;
		public int balance = 1000;
        public decimal dollars = 10;
		public int bet = 1;
		public int roundBalance;
        public int freeRoundBalance;
		public int roundHits;
		public int totalHits;
		public List<HitInfo> scatterHitInfos;
		public virtual int roundCost { get { return slot.currentMode.costPerLine*slot.gameInfo.bet*slot.lineManager.activeLines; } }
		public GameInfo(CustomSlot slot) { this.slot = slot; }
        public LoadOnClick LoadOnClick;

        ///for tracking time and state
        public static string idString;
        public static string sessionIdString;
        public static string dataLine;
        public List<string> dataLog = new List<string>();
        public StringBuilder payoutsSequence = new StringBuilder();
        public StringBuilder addBalanceSequence = new StringBuilder();
        public StringBuilder hitSymbolSequence = new StringBuilder();
        public string sceneVersion = SceneManager.GetActiveScene().name;
        bool firstSpin = true;
        int startFreeSpins;
        int startBalance;
        public int totalFreeSpins = 0;
        public Stopwatch stopWatch = new Stopwatch();
        public TimeSpan buttonTimeElapsed;
        String spinTimeStamp;
        TimeSpan spinTimeElapsed;
        TimeSpan _spinTimeElapsed;
        TimeSpan roundEndTimeElapsed;
        TimeSpan _roundDuration;
        TimeSpan idleStartTime;
        TimeSpan cumulativeIdle;
        TimeSpan cumulativeIdle2;

        public static void DisplayTimerProperties() {
            // Display the timer frequency and resolution.
            if (Stopwatch.IsHighResolution) {
                UnityEngine.Debug.Log("Operations timed using the system's high-resolution performance counter.");
            }
            else {
                UnityEngine.Debug.Log("Operations timed using the DateTime class.");
            }
            long frequency = Stopwatch.Frequency;
            UnityEngine.Debug.Log("  Timer frequency in ticks per second = " + frequency.ToString());
            long nanosecPerTick = (1000L * 1000L * 1000L) / frequency;
            UnityEngine.Debug.Log("  Timer is accurate within " + nanosecPerTick.ToString() + " nanoseconds");  
        }

        internal void OnStartSpin() {
            roundHits = 0;
			roundBalance = 0;
            if(slot.currentMode != slot.modes.freeSpinMode) { freeRoundBalance = 0; }
            AddBalance(-roundCost);
            payoutsSequence.Remove(0, payoutsSequence.Length);
            addBalanceSequence.Remove(0, addBalanceSequence.Length);
            hitSymbolSequence.Remove(0, hitSymbolSequence.Length);

            //Debug.Log("Spinning: " + bet + ", " + balance + ", " + roundCost);

            //Player tracking
            //values recorded at the start of a spin are dealt with here
            if (freeSpins > 0) {
                startFreeSpins = 1;
                totalFreeSpins += 1;
            }
            else startFreeSpins = 0;

            startBalance = balance + roundCost;
            spinTimeStamp = DateTime.Now.ToString("yyyy:MM:dd-HH:mm:ss.fff");
            if (firstSpin) {
                firstSpin = false;
                stopWatch.Start();
            }
            else { spinTimeElapsed = stopWatch.Elapsed; }
            cumulativeIdle += (spinTimeElapsed - _spinTimeElapsed - _roundDuration);
            cumulativeIdle2 += (spinTimeElapsed - idleStartTime);
        }

        internal void OnStartRound() {
            slot.lineManager.allHitHolders.Clear();
            scatterHitInfos = new List<HitInfo>();
            foreach (Symbol symbol in slot.symbolManager.symbols) if (symbol.matchType == Symbol.MatchType.Scatter) scatterHitInfos.Add(new HitInfo(slot, null, symbol));
            idleStartTime = stopWatch.Elapsed;
            if (firstSpin && slot.debug.useDebugKeys) { DisplayTimerProperties(); }
        }

		internal void OnRoundComplete() {
			roundsCompleted++;
			if (bonuses > 0 && slot.currentMode == slot.modes.bonusMode) slot.AddBonus(-1);
			    else if (freeSpins > 0 && slot.currentMode == slot.modes.freeSpinMode) slot.AddFreeSpin(-1);

            //more player tracking
            //values recorded at the end of a spin are dealth with here
            //log line gets written when the round completes
            roundEndTimeElapsed = stopWatch.Elapsed;
            dataLine = (
                idString + ", " //participant ID
                + sessionIdString + ", " //session / condition ID
                + spinTimeStamp + ", " //time stamp
                + sceneVersion + ", "
                + roundsCompleted + ", " //trial number
                + spinTimeElapsed.TotalSeconds + ", " //time elapsed since first spin was made (seconds)
                + (spinTimeElapsed.TotalSeconds - _spinTimeElapsed.TotalSeconds) + ", " //interval between spin starts
                + roundEndTimeElapsed.TotalSeconds + ", " //time when the round ended with respect to the first spin
                + (roundEndTimeElapsed.TotalSeconds - spinTimeElapsed.TotalSeconds) + ", " //total temporal length of the round
                + (spinTimeElapsed.TotalSeconds - _spinTimeElapsed.TotalSeconds - _roundDuration.TotalSeconds) + ", " //idle time before player initiated the round
                + cumulativeIdle.TotalSeconds + ", " //cumulative idle time
                + (spinTimeElapsed.TotalSeconds - idleStartTime.TotalSeconds) + ", " //another perhaps more accurate way to measure idle time
                + cumulativeIdle2.TotalSeconds + ", " //another perhaps more accurate cumulative idle time
                + bet + ", " //bet denomination
                + (roundCost / bet) + ", " // number of lines played
                + roundCost + ", " //cost of the round (bet x lines)
                + roundBalance + ", " //net cost of the round
                + startBalance + ", " //balance at beginning of the round
                + (startBalance - roundCost) + ", " //balance after bet placed
                + balance + ", " //balance at the end of the round
                + roundHits + ", " //number of hits in the round
                + totalHits + ", " //cumulative number of hits
                + startFreeSpins + ", " //bool to indicate whether the round is a free spin
                + totalFreeSpins + ", " //total free spins played
                + payoutsSequence + ", " //symbol length of each hit, ordered and separated by ";"
                + hitSymbolSequence + ", " //symbol type in each hit, ordered and separated by ";"
                + addBalanceSequence //win amount for each hit, ordered and separated by ";"
                );

				UnityEngine.Debug.Log(roundsCompleted + " " + (startBalance-40-balance) + " " + balance);

            //write the data to a file or list depending on platform
            if (Application.platform != RuntimePlatform.WebGLPlayer) {
                using (StreamWriter writer = new StreamWriter((Application.persistentDataPath + @"/" + idString + "_" + sessionIdString + "_tracking_log.csv"), true)) {
                    writer.WriteLine(dataLine);
                }
				//always add data to the list for convenience
				dataLog.Add(dataLine);
            }
            else { dataLog.Add(dataLine); }
            
            _spinTimeElapsed = spinTimeElapsed;
            _roundDuration = (roundEndTimeElapsed - spinTimeElapsed);
        }

		public void AddBalance(int amount, HitInfo info = null) {
			roundBalance += amount;
			balance += amount;
            dollars += decimal.Divide(amount, 100);
            if (slot.currentMode == slot.modes.freeSpinMode) { freeRoundBalance += amount; }
			slot.callbacks.onAddBalance.Invoke(new BalanceInfo(amount, info));
		}

		public void AddHit() {
			roundHits++;
			totalHits++;
		}
	}

	[Serializable]
	public class ReelInfo : UnityEvent<ReelInfo> {
		public Reel reel;
		public bool isFirstReel { get { return reel.index == 0; } }
		public bool isLastReel { get { return reel.index == reel.slot.reels.Length - 1; } }
		public ReelInfo() { }
		public ReelInfo(Reel reel) { this.reel = reel; }
	}

	[Serializable]
	public class SlotModeInfo : UnityEvent<SlotModeInfo> {
		public SlotMode lastMode;
		public SlotModeInfo() { }

		public SlotModeInfo(SlotMode lastMode) { this.lastMode = lastMode; }
	}

	[Serializable]
	public class BalanceInfo : UnityEvent<BalanceInfo> {
		public HitInfo hitInfo;
		public int amount;
		public BalanceInfo() { }

		public BalanceInfo(int amount, HitInfo hitInfo = null) {
			this.hitInfo = hitInfo;
			this.amount = amount;
		}
	}

	[Serializable]
	public class LineInfo : UnityEvent<LineInfo> {
		public Line line;
		public bool isLineEnabled { get { return line && line.isLineEnabled; } }

		public LineInfo() { }

		public LineInfo(Line line) { this.line = line; }
	}

	/// <summary>
	/// A class that contains line's information.
	/// Will be rest once Hit Check starts.
	/// </summary>
	[Serializable]
	public class HitInfo : UnityEvent<HitInfo> {
		/// <summary>
		/// A list of all SymbolHolders Hit Check traced.
		/// </summary>
		public SymbolHolder[] holders;

		/// <summary>
		/// A list of SymbolHolders which were actually Hit.
		/// </summary>
		public List<SymbolHolder> hitHolders = new List<SymbolHolder>();

		/// <summary>
		/// DOTween sequence that will be played if the line was a hit.
		/// You can Join/Append your own Tween to how the sequence is played. 
		/// </summary>
		public Sequence sequence;
		public bool isSequencePlayed;

		public Line line;
		public Symbol hitSymbol;
		public int hitChains;
		public int payout;

		private CustomSlot slot;
		private bool isProcessed;
		public bool isLineEnabled { get { return line && line.isLineEnabled; } }

		private bool _isHit;
		public bool isHit { get { return _isHit && (hitSymbol.matchType == Symbol.MatchType.Scatter || (line && line.isLineEnabled)); } }
		public HitInfo() { }

		public HitInfo(CustomSlot slot, Line line = null, Symbol symbol = null) {
			this.slot = slot;
			this.line = line;
			this.hitSymbol = symbol;
		}

		public bool ProcessHitCheck() {
			if (isProcessed) return false;
			isProcessed = true;

			if (line) {
				holders = line.GetHoldersOnPath();
				if (holders == null || holders.Length != slot.reels.Length) return false;
				ParseChains(holders);
			} else {
				List<SymbolHolder> list = slot.GetVisibleHolders();
				foreach (SymbolHolder holder in list) {
					if (holder.symbol == hitSymbol && hitSymbol.matchType == Symbol.MatchType.Scatter) {
						hitChains++;
						hitHolders.Add(holder);
					}
				}
				holders = hitHolders.ToArray();
				_isHit = hitChains >= hitSymbol.minChains;
			}

			if (isHit) slot.ProcessHit(this);
			return isHit;
		}

		internal void Reset() {
			_isHit = false;
			hitChains = 0;
		}

		internal void ParseChains(SymbolHolder[] refHolders) {
			Symbol[] symbols = new Symbol[slot.reels.Length];
			for (int x = 0; x < slot.reels.Length; x++) symbols[x] = holders[x].symbol;
			ParseChains(symbols, refHolders);
		}

		internal void ParseChains(Symbol[] symbols, SymbolHolder[] refHolders = null) {
			bool chainStopped = false;
			for (int i = 0; i < symbols.Length; i++) {
				Symbol symbol = symbols[i];
				if (i == 0) hitSymbol = symbol;
				if (!chainStopped && symbol.CanMatch(hitSymbol)) {
					if (hitSymbol.matchType == Symbol.MatchType.Wild && symbol.matchType != Symbol.MatchType.Wild) hitSymbol = symbol;
					hitChains++;
					if (refHolders != null) hitHolders.Add(refHolders[i]);
				} else chainStopped = true;
			}

			if (!hitSymbol || hitSymbol.minChains == -1 || hitChains < hitSymbol.minChains) return;
			if (slot.config.advanced.alternativeLineCheck && !ValidateAlternativeLine()) return;

			_isHit = true;
			if (slot.config.advanced.alternativeLineCheck) slot.lineManager.allHitHolders.Add(hitHolders);
		}

		internal bool ValidateAlternativeLine() {
			if (hitHolders.Count < slot.config.reelLength) {
				foreach (Row row in slot.rows) {
					if (row.isHiddenRow) continue;
					if (row.holders[hitHolders.Count].symbol.CanMatch(hitSymbol)) return false;
				}
			}
			foreach (List<SymbolHolder> holders in slot.lineManager.allHitHolders) {
				if (holders != hitHolders && holders.Count >= hitHolders.Count) {
					bool samePath = true;
					for (int i = 0; i < hitHolders.Count; i++)
						if (hitHolders[i] != holders[i]) {
							samePath = false;
							break;
						}
					if (samePath) return false;
				}
			}
			return true;
		}
	}
}
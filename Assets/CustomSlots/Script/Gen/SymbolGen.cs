using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CSFramework
{
	/// <summary>
	/// A class that handles generating and simulating symbol setups/loadout.
	/// </summary>
	[Serializable]
	public class SymbolGen
	{
		public enum SortMode
		{
			None,
			Profit,
			Hits,
			Count
		}

		[Serializable]
		public class LoadoutSaver
		{
			public List<List<String>> symbolLoadout = new List<List<String>>();
		}

		[Serializable]
		public class Setting
		{
			public int spinsPerTry = 10000;
			public SortMode sortMode;
			public bool showSymbolCounts = true;
			public bool confirmGeneration = true;
		}

		public CustomSlot slot;
		public Setting setting;

		public Dictionary<int, string[]> dict;

		[NonSerialized] public SymbolGenLog log;
		internal GameInfo gameInfo;
		private Symbol[] symbolsOnPath;
		private Dictionary<SlotMode, List<Symbol[]>> reelMap = new Dictionary<SlotMode, List<Symbol[]>>();
		private List<Symbol[]> reels = new List<Symbol[]>();
		private List<Symbol[]> scores = new List<Symbol[]>();
		private List<Symbol[]> recalledLoadout = new List<Symbol[]>();
		public LoadoutSaver loadoutSaver;
		public LoadoutSaver loadoutLoader;

		public LineManager lineManager { get { return slot.lineManager; } }
		public SymbolManager symbolManager { get { return slot.symbolManager; } }
		public int reelLength { get { return slot.config.reelLength; } }
		public int rows { get { return slot.config.rows; } }
		public int symbolsPerReel { get { return slot.config.symbolsPerReel; } }
		public Line[] lines { get { return lineManager.lines; } }
		public int CostPerSpin(SlotMode mode) { return mode.costPerLine * lines.Length; }
		public int NewCostPerSpin(SlotMode mode) { return (defaultMode.costPerLine - mode.costPerLine) * lines.Length; }
		public SlotMode defaultMode { get { return slot.modes.defaultMode; } }
		public SlotMode freeSpinMode { get { return slot.modes.freeSpinMode; } }
		public SlotMode bonusMode { get { return slot.modes.bonusMode; } }

		private void Init()
		{
			slot.layout.Refresh();
			slot.Validate();
			reels.Clear();
			for (int x = 0; x < reelLength; x++)
			{
				reels.Add(new Symbol[symbolsPerReel]);
				scores.Add(new Symbol[rows]);
			}
			foreach (Line line in lines) line.OnGenInit();
			log = new SymbolGenLog(this);
		}

		/// <summary>
		/// Generates a random set of symbols and apply it to the slot.
		/// </summary>
		public void Generate()
		{
			Init();
			MakeNewReels();
			ApplyLoadout();
			slot.layout.Refresh();
		}

		/// <summary>
		/// Simulate rounds and shows the result.
		/// </summary>
		public void Simulate()
		{
			if (Application.isPlaying) return;
			Init();
			gameInfo = new GameInfo(slot);
			gameInfo.OnStartRound();
			symbolsOnPath = new Symbol[slot.reels.Length];
			ParseReels();
			for (int i = 0; i < setting.spinsPerTry; i++) Spin();
			log.ProcessResult();
			slot.Validate();
		}

		private void MakeNewReels()
		{
			int lastIndex = 0;
			foreach (Symbol symbol in symbolManager.symbols)
				if (symbol.minCountPerReel > 0)
					for (int i = 0; i < symbol.minCountPerReel; i++)
					{
						if (lastIndex >= symbolsPerReel) break;
						for (int x = 0; x < reelLength; x++) reels[x][lastIndex] = symbol;
						lastIndex++;
					}

			Debug.Log(reelLength);
			Debug.Log(symbolsPerReel);
			for (int x = 0; x < reelLength; x++) for (int y = lastIndex; y < symbolsPerReel; y++) reels[x][y] = symbolManager.GetRandomSymbol();

			// reels[4][98]

			string[,] imap = new string[150, 15];

			dict = new Dictionary<int, string[]>    {
	{ 1, new string[] { "J", "A", "K", "Wild", "Q", "Stone", "Mask", "Sfnx", "K", "A", "J", "Scarab", "A", "J", "A" } },
{ 2, new string[] { "Scarab","Stone","Ankh","A","Scarab","Ankh","J","J","K","Ankh","K","K","K","A","A" } },
{ 3, new string[] { "Stone","Scarab","Wild","Scarab","Q","A","Scarab","Q","Ankh","Q","Sfnx","Q","Stone","Wild","Sfnx" } },
{ 4, new string[] { "J","K","Scarab","Sfnx","Scarab","Mask","Q","Sfnx","Stone","Stone","Q","K","Scarab","Sfnx","Sfnx" } },
{ 5, new string[] { "Scarab","Sfnx","Q","Sfnx","A","Sfnx","Ankh","Stone","Ankh","Ankh","A","K","Q","Stone","Scarab" } },
{ 6, new string[] { "K","Stone","Q","Stone","Sfnx","Sfnx","Stone","K","J","A","K","Stone","Scarab","Sfnx","J" } },
{ 7, new string[] { "Sfnx","Scarab","Ankh","Stone","K","Q","J","J","Q","Ankh","Sfnx","Stone","Scarab","Wild","Ankh" } },
{ 8, new string[] { "Sfnx","J","Q","Q","Scarab","Scarab","Mask","Stone","Scarab","Scarab","K","J","Stone","Scarab","Scarab" } },
{ 9, new string[] { "Ankh","Wild","Sfnx","Scarab","Ankh","K","Q","Sfnx","Scarab","K","A","A","Stone","Scarab","J" } },
{ 10, new string[] { "Sfnx","K","Scarab","Wild","Sfnx","Sfnx","Scarab","Mask","Scarab","Sfnx","Wild","Ankh","A","Ankh","Wild" } },
{ 11, new string[] { "Q","Scarab","Ankh","J","Q","Wild","Scarab","Ankh","Mask","Scarab","A","Scarab","Sfnx","A","K" } },
{ 12, new string[] { "J","Stone","Stone","Scarab","Sfnx","A","Ankh","J","Wild","K","Sfnx","Scarab","Stone","Wild","Scarab" } },
{ 13, new string[] { "Sfnx","Sfnx","Mask","Q","Scarab","Scarab","K","Scarab","J","Scarab","Sfnx","Wild","Scarab","J","Sfnx" } },
{ 14, new string[] { "Scarab","Scarab","Ankh","K","Sfnx","A","Scarab","J","K","Sfnx","Wild","Ankh","J","Scarab","Q" } },
{ 15, new string[] { "Q","Scarab","K","J","Wild","A","Stone","Sfnx","Scarab","A","Sfnx","Wild","A","Scarab","J" } },
{ 16, new string[] { "K","Sfnx","Stone","J","J","Mask","Stone","Ankh","Ankh","J","K","Scarab","Scarab","K","Q" } },
{ 17, new string[] { "Stone","J","Q","A","Q","Ankh","A","Ankh","Ankh","Q","Scarab","Stone","Ankh","A","Scarab" } },
{ 18, new string[] { "Q","J","K","Scarab","A","Stone","A","Scarab","J","Sfnx","Sfnx","Sfnx","K","Wild","Wild" } },
{ 19, new string[] { "K","Scarab","K","Ankh","Sfnx","J","Stone","K","Ankh","J","Mask","Q","Scarab","Mask","Scarab" } },
{ 20, new string[] { "Sfnx","Scarab","Scarab","Q","Scarab","Stone","J","Stone","A","Sfnx","Scarab","Sfnx","Wild","Stone","Sfnx" } },
{ 21, new string[] { "Ankh","Ankh","Sfnx","Mask","J","Sfnx","Sfnx","Sfnx","A","Stone","Stone","Q","Scarab","Sfnx","Wild" } },
{ 22, new string[] { "Sfnx","J","K","Q","K","Q","Mask","Stone","Stone","Sfnx","Wild","Ankh","Wild","Sfnx","Stone" } },
{ 23, new string[] { "Stone","A","Ankh","Stone","A","J","A","Scarab","Sfnx","Stone","A","Sfnx","Stone","Ankh","Scarab" } },
{ 24, new string[] { "Ankh","A","J","Wild","Ankh","A","Scarab","Ankh","J","Sfnx","Ankh","Q","Sfnx","K","Q" } },
{ 25, new string[] { "Q","Ankh","Stone","A","Wild","A","A","Ankh","Q","Scarab","A","A","J","K","A" } },
{ 26, new string[] { "Sfnx","J","Scarab","Wild","Sfnx","Sfnx","Q","J","K","Sfnx","Sfnx","A","Sfnx","Ankh","Stone" } },
{ 27, new string[] { "Ankh","Sfnx","K","K","Sfnx","K","J","J","A","K","Sfnx","Sfnx","K","Wild","Scarab" } },
{ 28, new string[] { "Ankh","Stone","Ankh","A","Q","Wild","K","K","A","Wild","A","Stone","Ankh","Q","Stone" } },
{ 29, new string[] { "A","Scarab","K","A","Stone","Mask","Q","Ankh","Ankh","Sfnx","Q","A","Ankh","A","Scarab" } },
{ 30, new string[] { "Sfnx","J","K","Ankh","Wild","A","Sfnx","Q","Q","Sfnx","Sfnx","Stone","Stone","Q","K" } },
{ 31, new string[] { "Ankh","K","Q","Ankh","J","Wild","Stone","J","Scarab","Mask","J","A","Wild","Sfnx","A" } },
{ 32, new string[] { "J","Ankh","K","Stone","Sfnx","J","Mask","J","Q","Stone","Stone","Scarab","Stone","Scarab","Sfnx" } },
{ 33, new string[] { "K","Ankh","Stone","Ankh","K","K","Scarab","Scarab","A","Sfnx","A","Stone","A","Sfnx","Ankh" } },
{ 34, new string[] { "Scarab","K","Stone","A","A","A","J","Ankh","J","Scarab","Ankh","Stone","Sfnx","J","Scarab" } },
{ 35, new string[] { "Q","Wild","K","Ankh","K","Sfnx","Mask","Stone","K","Scarab","J","Scarab","A","A","Sfnx" } },
{ 36, new string[] { "Stone","Ankh","Q","K","Stone","K","Wild","Wild","J","A","Sfnx","Scarab","K","Sfnx","Sfnx" } },
{ 37, new string[] { "K","J","K","Scarab","Q","A","Scarab","Stone","J","Stone","Sfnx","Sfnx","K","A","Stone" } },
{ 38, new string[] { "Q","Ankh","Scarab","J","J","A","Wild","Scarab","Q","K","Ankh","Mask","Q","Ankh","J" } },
{ 39, new string[] { "Ankh","Sfnx","K","Sfnx","Scarab","J","Stone","K","Ankh","A","Ankh","Scarab","Sfnx","Scarab","K" } },
{ 40, new string[] { "Wild","Sfnx","K","A","A","J","Stone","Wild","Stone","K","Mask","Ankh","Wild","J","K" } },
{ 41, new string[] { "Sfnx","Stone","Stone","Stone","Sfnx","Wild","A","Stone","Wild","J","Mask","Stone","Scarab","J","Scarab" } },
{ 42, new string[] { "K","K","Q","Scarab","A","J","Scarab","Q","Stone","J","Scarab","Scarab","Scarab","A","Q" } },
{ 43, new string[] { "K","Scarab","Ankh","Wild","Sfnx","K","A","Sfnx","Wild","A","Scarab","Q","Mask","Scarab","Scarab" } },
{ 44, new string[] { "Stone","Stone","Scarab","Scarab","Ankh","Wild","Mask","Scarab","J","Sfnx","K","K","Sfnx","Ankh","A" } },
{ 45, new string[] { "Q","Stone","Mask","Stone","J","J","J","J","Ankh","Stone","Scarab","K","Q","Stone","Wild" } },
{ 46, new string[] { "Stone","Scarab","Ankh","Sfnx","Scarab","Scarab","Ankh","Q","Ankh","Stone","Stone","Wild","K","Scarab","Stone" } },
{ 47, new string[] { "Q","Ankh","K","Stone","Wild","K","J","Scarab","Ankh","A","J","Mask","Stone","Scarab","Scarab" } },
{ 48, new string[] { "Scarab","Sfnx","Q","Ankh","K","Wild","Sfnx","Stone","J","Sfnx","Scarab","Wild","Stone","Scarab","K" } },
{ 49, new string[] { "Scarab","K","J","Q","Scarab","Q","Wild","Ankh","J","A","Ankh","Wild","Ankh","Ankh","Ankh" } },
{ 50, new string[] { "K","K","Scarab","Q","A","Sfnx","Sfnx","Ankh","Stone","K","Wild","K","Ankh","Scarab","A" } },
{ 51, new string[] { "Ankh","Scarab","Stone","A","Ankh","J","Wild","J","Stone","Stone","Mask","Ankh","Scarab","Sfnx","A" } },
{ 52, new string[] { "Scarab","K","Ankh","Wild","J","Scarab","K","Scarab","Sfnx","Stone","K","A","Ankh","Q","Scarab" } },
{ 53, new string[] { "Ankh","Wild","A","Mask","Sfnx","A","K","Scarab","Stone","K","K","Sfnx","A","Wild","A" } },
{ 54, new string[] { "Sfnx","Sfnx","Ankh","Sfnx","Sfnx","Stone","Sfnx","Sfnx","J","J","Ankh","A","Scarab","Scarab","Q" } },
{ 55, new string[] { "Mask","Ankh","Ankh","J","Stone","J","Q","A","K","Stone","Ankh","Wild","Stone","Ankh","Sfnx" } },
{ 56, new string[] { "J","Scarab","Ankh","A","Q","Sfnx","Scarab","Sfnx","Stone","K","J","Wild","J","Ankh","J" } },
{ 57, new string[] { "A","Stone","Q","Sfnx","Sfnx","A","J","A","Scarab","K","K","J","Scarab","Scarab","Sfnx" } },
{ 58, new string[] { "J","A","Stone","K","Scarab","Ankh","Q","Q","K","Sfnx","Ankh","Stone","Sfnx","Sfnx","Mask" } },
{ 59, new string[] { "Stone","Sfnx","Ankh","A","Ankh","Scarab","Sfnx","Stone","Sfnx","J","A","K","Sfnx","Q","Scarab" } },
{ 60, new string[] { "Ankh","A","Scarab","Stone","Scarab","Q","Mask","Sfnx","Ankh","Mask","J","K","Sfnx","Scarab","Scarab" } },
{ 61, new string[] { "Scarab","J","Scarab","Ankh","Ankh","J","Q","Scarab","K","Q","Q","Scarab","Scarab","Q","Scarab" } },
{ 62, new string[] { "Scarab","Wild","Mask","Sfnx","Q","Scarab","A","Ankh","J","Stone","Q","Stone","A","Ankh","Stone" } },
{ 63, new string[] { "Wild","Sfnx","Stone","J","Stone","Scarab","Ankh","Mask","A","Sfnx","Sfnx","K","Sfnx","J","J" } },
{ 64, new string[] { "K","J","Ankh","Wild","Ankh","Scarab","Scarab","Scarab","J","A","Scarab","Stone","Stone","A","A" } },
{ 65, new string[] { "Ankh","A","Scarab","Wild","Q","Ankh","Mask","Stone","Q","Scarab","Q","Mask","A","Sfnx","Scarab" } },
{ 66, new string[] { "Sfnx","J","A","Q","Sfnx","Sfnx","Q","Ankh","Ankh","A","A","Sfnx","Sfnx","A","A" } },
{ 67, new string[] { "Stone","K","Scarab","A","Scarab","Stone","Stone","Wild","Ankh","Sfnx","J","Wild","Sfnx","A","Scarab" } },
{ 68, new string[] { "Ankh","Q","K","Sfnx","J","A","Ankh","J","A","Sfnx","A","Sfnx","Sfnx","Sfnx","Ankh" } },
{ 69, new string[] { "Q","A","Stone","Ankh","Stone","Stone","Scarab","A","Stone","Ankh","Scarab","Sfnx","A","Sfnx","Scarab" } },
{ 70, new string[] { "Q","Sfnx","Stone","Wild","Sfnx","Q","A","Ankh","K","Q","Ankh","Sfnx","A","Sfnx","Scarab" } },
{ 71, new string[] { "Sfnx","K","Scarab","Sfnx","Wild","Stone","K","Sfnx","Mask","Stone","Stone","J","Q","A","Scarab" } },
{ 72, new string[] { "Ankh","Wild","Ankh","Sfnx","Scarab","Wild","Stone","Scarab","Mask","Q","Sfnx","Stone","A","Scarab","K" } },
{ 73, new string[] { "Mask","J","Stone","A","Stone","Stone","Sfnx","Stone","Sfnx","J","Wild","Ankh","Scarab","Q","A" } },
{ 74, new string[] { "Scarab","Q","K","Sfnx","K","Sfnx","Stone","Mask","A","Sfnx","Sfnx","Sfnx","J","Q","Stone" } },
{ 75, new string[] { "Scarab","Q","Wild","Scarab","Q","Ankh","Sfnx","Stone","A","A","Wild","Ankh","Ankh","J","Q" } },
{ 76, new string[] { "Q","K","Ankh","Wild","A","Q","A","Sfnx","Scarab","Scarab","K","Scarab","J","Ankh","Stone" } },
{ 77, new string[] { "Ankh","Stone","Ankh","Ankh","Wild","A","J","Ankh","Ankh","Wild","Wild","Scarab","J","K","Sfnx" } },
{ 78, new string[] { "Wild","A","Wild","Sfnx","Sfnx","Stone","Wild","A","Stone","Stone","Ankh","Stone","Scarab","Q","Stone" } },
{ 79, new string[] { "Scarab","K","K","J","A","Ankh","Ankh","Wild","Stone","Q","A","Scarab","Stone","J","Scarab" } },
{ 80, new string[] { "Mask","Ankh","Ankh","Stone","Stone","J","Q","Mask","Sfnx","Sfnx","J","Ankh","K","J","K" } },
{ 81, new string[] { "Ankh","A","Scarab","J","Stone","Scarab","Mask","A","K","J","Scarab","A","Ankh","Stone","Stone" } },
{ 82, new string[] { "Ankh","K","Scarab","Ankh","Ankh","A","Ankh","Mask","A","Wild","A","Scarab","Q","J","Q" } },
{ 83, new string[] { "Stone","Scarab","J","A","A","Stone","A","A","Scarab","Sfnx","Scarab","A","Ankh","A","Stone" } },
{ 84, new string[] { "K","Stone","Sfnx","Ankh","Q","Sfnx","J","Stone","Q","Ankh","J","K","K","Scarab","Scarab" } },
{ 85, new string[] { "Stone","Stone","Ankh","Scarab","Q","Sfnx","K","Ankh","J","Sfnx","Mask","Wild","Scarab","J","A" } },
{ 86, new string[] { "Scarab","J","J","J","Stone","J","Stone","Mask","Mask","A","J","K","Scarab","Sfnx","K" } },
{ 87, new string[] { "Sfnx","Ankh","Stone","Sfnx","A","J","J","Scarab","K","Ankh","K","Wild","Stone","Scarab","K" } },
{ 88, new string[] { "Wild","Ankh","Stone","A","Scarab","Q","Q","J","Mask","Q","Q","Sfnx","J","Sfnx","J" } },
{ 89, new string[] { "K","Wild","K","A","Scarab","Q","K","J","Wild","Stone","A","Stone","Q","Q","Ankh" } },
{ 90, new string[] { "K","Ankh","Scarab","Stone","A","Q","K","Sfnx","J","Q","J","Sfnx","Sfnx","Scarab","Sfnx" } },
{ 91, new string[] { "Sfnx","K","J","Ankh","Stone","J","A","Mask","Q","J","K","A","Stone","Wild","Stone" } },
{ 92, new string[] { "Wild","Scarab","Ankh","A","Q","A","Ankh","Mask","Stone","A","Stone","Ankh","K","Stone","Sfnx" } },
{ 93, new string[] { "K","J","A","J","Stone","Ankh","Stone","J","Mask","A","Stone","Stone","A","Stone","Stone" } },
{ 94, new string[] { "Ankh","Ankh","Ankh","Sfnx","Q","Scarab","Scarab","Mask","J","J","Ankh","A","Sfnx","Scarab","Q" } },
{ 95, new string[] { "K","Q","Stone","A","Scarab","Q","Ankh","Mask","Sfnx","Sfnx","Q","J","J","Ankh","Scarab" } },
{ 96, new string[] { "Stone","K","Scarab","Sfnx","Stone","Sfnx","Stone","Wild","A","Sfnx","Scarab","A","Sfnx","Scarab","Sfnx" } },
{ 97, new string[] { "Mask","Ankh","Stone","Wild","A","Q","A","Ankh","A","A","Scarab","A","Ankh","Wild","Scarab" } },
{ 98, new string[] { "Scarab","Wild","Scarab","Stone","Scarab","Ankh","K","Q","Ankh","Scarab","Stone","Sfnx","Stone","Ankh","Sfnx" } },
{ 99, new string[] { "Q","A","Ankh","Stone","Scarab","Stone","K","Q","K","Wild","Sfnx","Wild","Mask","Sfnx","K" } },
{ 100, new string[] { "Stone","Ankh","Scarab","Sfnx","Scarab","Q","Sfnx","Sfnx","Ankh","Scarab","Wild","Mask","Ankh","Ankh","Ankh" } },
{ 101, new string[] { "Ankh","Wild","Ankh","A","J","Sfnx","Ankh","Sfnx","J","Scarab","Stone","Stone","Scarab","Scarab","Q" } },
{ 102, new string[] { "Q","Scarab","J","Scarab","Ankh","A","Sfnx","Scarab","J","Stone","Stone","K","Sfnx","Stone","K" } },
{ 103, new string[] { "Ankh","Sfnx","Stone","J","J","Ankh","K","Scarab","Sfnx","Sfnx","A","Wild","Sfnx","K","K" } },
{ 104, new string[] { "K","Q","Ankh","A","Ankh","J","Scarab","A","Wild","Stone","K","Scarab","Ankh","K","A" } },
{ 105, new string[] { "Q","Q","K","Scarab","Scarab","J","Q","Scarab","Scarab","K","J","Wild","J","Scarab","Sfnx" } },
{ 106, new string[] { "K","Scarab","A","Scarab","Stone","Scarab","Sfnx","Ankh","Sfnx","A","K","Stone","Stone","Ankh","Ankh" } },
{ 107, new string[] { "Stone","A","Scarab","Ankh","K","A","Sfnx","Ankh","Sfnx","Ankh","Sfnx","Ankh","Sfnx","J","Ankh" } },
{ 108, new string[] { "Scarab","Sfnx","Scarab","Stone","Q","J","J","Stone","Wild","Stone","Scarab","Stone","A","A","K" } },
{ 109, new string[] { "Sfnx","J","Q","Wild","Sfnx","Sfnx","Sfnx","Stone","Sfnx","Ankh","Stone","A","Mask","Ankh","Scarab" } },
{ 110, new string[] { "K","Scarab","Q","Ankh","Sfnx","Stone","J","Q","Ankh","Scarab","Scarab","Scarab","Sfnx","J","Scarab" } },
{ 111, new string[] { "Q","Ankh","Ankh","Wild","A","A","J","Mask","Q","Stone","Stone","A","A","J","Scarab" } },
{ 112, new string[] { "Scarab","Q","K","J","J","K","Stone","J","Scarab","Stone","Sfnx","Stone","Scarab","Ankh","Mask" } },
{ 113, new string[] { "K","Ankh","Stone","Sfnx","Q","Scarab","Ankh","Ankh","J","Sfnx","Stone","Scarab","Scarab","Q","Q" } },
{ 114, new string[] { "Q","Sfnx","Ankh","Stone","J","Scarab","Q","Q","Q","Wild","Sfnx","Sfnx","Scarab","A","Scarab" } },
{ 115, new string[] { "Sfnx","Stone","Stone","A","Ankh","Ankh","K","K","Scarab","K","Sfnx","Q","Sfnx","K","Ankh" } },
{ 116, new string[] { "Sfnx","Ankh","Ankh","Sfnx","Scarab","A","Mask","Stone","J","Ankh","Stone","Scarab","Stone","Mask","Scarab" } },
{ 117, new string[] { "Stone","Scarab","K","A","Wild","Wild","Mask","Scarab","K","Ankh","Scarab","Sfnx","Mask","J","Scarab" } },
{ 118, new string[] { "Q","Sfnx","A","J","Sfnx","Scarab","Ankh","Q","J","Sfnx","Stone","Mask","Scarab","Scarab","Mask" } },
{ 119, new string[] { "K","Scarab","K","Stone","J","Mask","A","J","Q","Scarab","Ankh","K","Ankh","Stone","Stone" } },
{ 120, new string[] { "Ankh","Ankh","A","K","Ankh","A","J","Q","Stone","Wild","Scarab","Sfnx","K","Stone","J" } },
{ 121, new string[] { "Scarab","Ankh","Q","Mask","Ankh","J","Ankh","Q","Mask","A","Q","Sfnx","Scarab","Sfnx","Wild" } },
{ 122, new string[] { "Ankh","Sfnx","Scarab","Q","Q","K","Stone","Q","J","Q","Sfnx","Sfnx","Scarab","Stone","K" } },
{ 123, new string[] { "Stone","J","Q","Sfnx","A","Ankh","Scarab","Stone","Stone","Scarab","Q","A","J","Stone","K" } },
{ 124, new string[] { "Ankh","Q","K","Q","Ankh","Wild","Q","Sfnx","Ankh","Scarab","Ankh","Sfnx","Scarab","Sfnx","K" } },
{ 125, new string[] { "Sfnx","Scarab","Sfnx","J","K","Ankh","K","K","Sfnx","Stone","Sfnx","Scarab","Scarab","Q","A" } },
{ 126, new string[] { "Q","K","Q","K","A","Scarab","J","Scarab","Scarab","J","Q","Stone","Q","Scarab","Mask" } },
{ 127, new string[] { "J","K","Q","Stone","Wild","Stone","K","Stone","A","Stone","Q","Wild","Q","K","A" } },
{ 128, new string[] { "Q","K","Ankh","A","Ankh","Sfnx","Sfnx","J","Sfnx","Sfnx","Scarab","Sfnx","Scarab","Scarab","A" } },
{ 129, new string[] { "J","K","Ankh","Sfnx","Ankh","Sfnx","Scarab","Sfnx","Wild","Scarab","Sfnx","A","K","Scarab","Mask" } },
{ 130, new string[] { "Scarab","Ankh","Wild","Mask","Sfnx","Sfnx","J","J","J","J","Sfnx","Stone","Scarab","Scarab","Stone" } },
{ 131, new string[] { "Ankh","Stone","Ankh","J","Q","Scarab","Ankh","Wild","Sfnx","Sfnx","Wild","Sfnx","Scarab","Ankh","Ankh" } },
{ 132, new string[] { "Scarab","Stone","K","K","Sfnx","Q","K","Q","J","Ankh","J","Scarab","Scarab","J","Stone" } },
{ 133, new string[] { "Q","Mask","Stone","J","A","Stone","Ankh","K","J","Stone","Ankh","A","Sfnx","Scarab","J" } },
{ 134, new string[] { "K","Scarab","K","Wild","Scarab","K","Sfnx","J","J","Stone","Q","Sfnx","A","K","Q" } },
{ 135, new string[] { "Q","Stone","Wild","K","J","Sfnx","K","Q","Stone","J","J","Sfnx","Scarab","Scarab","Stone" } },
{ 136, new string[] { "Q","Scarab","Scarab","Wild","Ankh","Ankh","A","Ankh","Sfnx","K","Sfnx","Ankh","K","Sfnx","Wild" } },
{ 137, new string[] { "Scarab","Ankh","K","A","K","Wild","Sfnx","Sfnx","Ankh","A","Sfnx","Mask","Stone","Q","Scarab" } },
{ 138, new string[] { "Q","Wild","J","Sfnx","A","J","Scarab","Ankh","Q","Sfnx","Stone","K","Scarab","Q","K" } },
{ 139, new string[] { "Scarab","Scarab","Sfnx","Scarab","Sfnx","A","K","Q","Ankh","Sfnx","Sfnx","Ankh","Scarab","Stone","Wild" } },
{ 140, new string[] { "Scarab","Stone","Ankh","Scarab","Scarab","K","Stone","A","Q","Q","A","Q","Q","Scarab","K" } },
{ 141, new string[] { "Q","Ankh","Ankh","J","Stone","Scarab","A","Q","Sfnx","A","Sfnx","Ankh","Ankh","Sfnx","Q" } },
{ 142, new string[] { "Q","K","Scarab","Sfnx","Sfnx","K","Stone","Q","Ankh","Scarab","Q","Stone","Scarab","Q","Q" } },
{ 143, new string[] { "Scarab","J","Q","Q","J","Ankh","Sfnx","K","Wild","Ankh","Stone","K","J","A","Ankh" } },
{ 144, new string[] { "Stone","Ankh","A","Wild","K","A","Sfnx","Q","Mask","Ankh","K","Ankh","Ankh","Scarab","Wild" } },
{ 145, new string[] { "Sfnx","Ankh","Sfnx","Wild","Sfnx","K","Wild","A","K","K","Q","Sfnx","Stone","Q","Scarab" } },
{ 146, new string[] { "Ankh","Ankh","Ankh","Q","A","Stone","Wild","Sfnx","Scarab","J","J","J","Sfnx","Ankh","Q" } },
{ 147, new string[] { "Ankh","Ankh","K","J","Stone","Wild","Q","Q","J","Sfnx","K","Q","Sfnx","Wild","Sfnx" } },
{ 148, new string[] { "Q","Q","Q","J","Stone","Sfnx","J","Q","Scarab","Ankh","J","A","Stone","K","Stone" } },
{ 149, new string[] { "Scarab","Scarab","Scarab","A","Scarab","A","Wild","Ankh","Scarab","Mask","K","A","Scarab","Ankh","Q" } },
{ 150, new string[] { "Wild","K","Ankh","Sfnx","Stone","Q","Ankh","Q","Scarab","Sfnx","Ankh","Scarab","J","Stone","Sfnx" } },
{ 151, new string[] { "Sfnx","Ankh","K","Sfnx","K","Wild","Ankh","Q","J","Sfnx","Sfnx","Q","Scarab","Q","Sfnx" } }, 
	};









			int a = 2;
			int b = 3;
			int c = 4;




			int x1 = 0;

			int spin1 = 1;



			for (int j = 0; j < 10000; j += 11)
			{

				Debug.Log("hi");



				if (x1 + 1 >= 152)
				{
					Debug.Log("breaked at x1" + x1);
					break;
				}

				if (!dict.ContainsKey((x1 + 1)))
				{
					Debug.Log("no key found " + x1);
					break;
				}

				string[] symbolNames = dict.ContainsKey((x1 + 1)) ? dict[(x1 + 1)] : dict[(1)];




				reels[0][j + a] = symbolManager.symbols[findSymbolByName(symbolNames[2])];
				reels[0][j + b] = symbolManager.symbols[findSymbolByName(symbolNames[1])];
				reels[0][j + c] = symbolManager.symbols[findSymbolByName(symbolNames[0])];


				Debug.Log(x1);
				Debug.Log(reels[0][j + c]);
				Debug.Log(reels[0][j + b]);
				Debug.Log(reels[0][j + a]);


				a = a + 1;
				b = b + 1;
				c = c + 1;

				x1 = x1 + 1;

				if (spin1 + 1 < 151)
				{
					spin1 = spin1 + 1;
				}
				else
				{
					spin1 = 0;
				}


			}


			a = 2;
			b = 3;
			c = 4;

			int x2 = 0;

			int spin2 = 1;



			for (int j = 0; j < 10000; j += 15)
			{

				if (x2 + 1 >= 152)
				{
					Debug.Log("breaked at x2" + x2);
					break;
				}

				if (!dict.ContainsKey((x2 + 1)))
				{
					Debug.Log("no key found " + x2);
					break;
				}


				
				string[] symbolNames = dict.ContainsKey((x2 + 1)) ? dict[(x2 + 1)] : dict[(1)];

				reels[1][j + a] = symbolManager.symbols[findSymbolByName(symbolNames[5])];
				reels[1][j + b] = symbolManager.symbols[findSymbolByName(symbolNames[4])];
				reels[1][j + c] = symbolManager.symbols[findSymbolByName(symbolNames[3])];

				a = a + 1;
				b = b + 1;
				c = c + 1;

				x2 = x2 + 1;

				if (spin2 + 1 < 151)
				{
					spin2 = spin2 + 1;
				}
				else
				{
					spin2 = 0;
				}


			}

			a = 2;
			b = 3;
			c = 4;

			int x3 = 0;

			int spin3 = 1;


			for (int j = 0; j < 10000; j += 19)
			{

				if (x3 + 1 >= 152)
				{
					Debug.Log("breaked at x3" + x3);
					break;
				}

				if (!dict.ContainsKey((x3 + 1)))
				{
					Debug.Log("no key found " + x3);
					break;
				}

				string[] symbolNames = dict.ContainsKey((x3 + 1)) ? dict[(x3 + 1)] : dict[(1)];


				reels[2][j + a] = symbolManager.symbols[findSymbolByName(symbolNames[8])];
				reels[2][j + b] = symbolManager.symbols[findSymbolByName(symbolNames[7])];
				reels[2][j + c] = symbolManager.symbols[findSymbolByName(symbolNames[6])];


				a = a + 1;
				b = b + 1;
				c = c + 1;


				x3 = x3 + 1;

				if (spin3 + 1 < 151)
				{
					spin3 = spin3 + 1;
				}
				else
				{
					spin3 = 0;
				}


			}

			a = 2;
			b = 3;
			c = 4;


			int x4 = 0;

			int spin4 = 1;


			for (int j = 0; j < 10000; j += 23)
			{
				if (x4 + 1 >= 152)
				{
					Debug.Log("breaked at x4" + x4);
					break;
				}

				if (!dict.ContainsKey((x4 + 1)))
				{
					Debug.Log("no key found " + x4);
					break;
				}

				string[] symbolNames = dict.ContainsKey((x4 + 1)) ? dict[(x4 + 1)] : dict[(1)];


				reels[3][j + a] = symbolManager.symbols[findSymbolByName(symbolNames[11])];
				reels[3][j + b] = symbolManager.symbols[findSymbolByName(symbolNames[10])];
				reels[3][j + c] = symbolManager.symbols[findSymbolByName(symbolNames[9])];

				a = a + 1;
				b = b + 1;
				c = c + 1;

				x4 = x4 + 1;

				if (spin4 + 1 < 151)
				{
					spin4 = spin4 + 1;
				}
				else
				{
					spin4 = 0;
				}


			}

			a = 2;
			b = 3;
			c = 4;


			int x5 = 0;

			int spin5 = 1;


			for (int j = 0; j < 10000; j += 27)
			{
				if (x5 + 1 >= 152)
				{
					Debug.Log("breaked at x5" + x4);
					break;
				}

				if (!dict.ContainsKey((x5 + 1)))
				{
					Debug.Log("no key found " + x5);
					break;
				}

				
				string[] symbolNames = dict.ContainsKey((x5 + 1)) ? dict[(x5 + 1)] : dict[(1)];


				

				reels[4][j + a] = symbolManager.symbols[findSymbolByName(symbolNames[14])];
				reels[4][j + b] = symbolManager.symbols[findSymbolByName(symbolNames[13])];
				reels[4][j + c] = symbolManager.symbols[findSymbolByName(symbolNames[12])];


				a = a + 1;
				b = b + 1;
				c = c + 1;

				x5 = x5 + 1;

				if (spin5 + 1 < 151)
				{
					spin5 = spin5 + 1;
				}
				else
				{
					spin5 = 0;
				}


			}




		}

		public int findSymbolByName(string name)
		{
			for (int i = 0; i < symbolManager.symbols.Length; i++)
			{
				if (symbolManager.symbols[i].name == name)
				{
					return i;
				}
			}
			return -1;
		}

		private void ParseReels()
		{
			reelMap.Clear();
			SlotMode cleanMode = new SlotMode();
			foreach (SlotMode mode in new SlotMode[] { cleanMode, defaultMode, freeSpinMode, bonusMode })
			{
				List<Symbol[]> r = new List<Symbol[]>();
				for (int j = 0; j < reelLength; j++) r.Add(new Symbol[symbolsPerReel]);
				for (int x = 0; x < reelLength; x++)
					for (int y = 0; y < symbolsPerReel; y++)
					{
						Symbol symbol = slot.reels[x].symbols[y];
						if (mode.symbolSwaps != null) for (int i = 0; i < mode.symbolSwaps.Count; i++) if (symbol == mode.symbolSwaps[i].from) symbol = mode.symbolSwaps[i].to;
						r[x][y] = symbol;
						if (mode == cleanMode) r[x][y].log.count++;
					}
				reelMap.Add(mode, r);
			}
		}

		private void Spin()
		{
			slot.lineManager.allHitHolders.Clear();

			if (gameInfo.bonuses > 0)
			{
				log.totalCost += CostPerSpin(bonusMode);
				gameInfo.bonuses--;
				reels = reelMap[bonusMode];
			}
			else if (gameInfo.freeSpins > 0)
			{
				log.totalCost += CostPerSpin(freeSpinMode);
				gameInfo.freeSpins--;
				reels = reelMap[freeSpinMode];
			}
			else
			{
				log.totalCost += CostPerSpin(defaultMode);
				reels = reelMap[defaultMode];
			}

			// Draw symbols
			for (int x = 0; x < reelLength; x++)
			{
				int index = Random.Range(0, symbolsPerReel);
				for (int y = 0; y < rows; y++)
				{
					Symbol symbol = reels[x][index];

					// Processing scatter hit info.
					for (int j = 0; j < gameInfo.scatterHitInfos.Count; j++)
					{
						HitInfo info = gameInfo.scatterHitInfos[j];
						if (symbol == info.hitSymbol) info.hitChains++;
					}
					scores[x][y] = symbol;
					index++;
					if (index >= reels[x].Length) index = 0;
				}
			}

			for (int j = 0; j < gameInfo.scatterHitInfos.Count; j++)
			{
				var info = gameInfo.scatterHitInfos[j];
				if (info.hitChains >= info.hitSymbol.minChains)
				{
					log.LogHit(info);
					info.hitSymbol.log.LogHit(info);
				}
				info.Reset();
			}

			// Hit Check
			for (int i = 0; i < lines.Length; i++)
			{
				Line line = lines[i];
				if (line.paths == null) continue;
				for (int x = 0; x < reelLength; x++) symbolsOnPath[x] = scores[x][line.paths[x]];
				line.hitInfo.ParseChains(symbolsOnPath);
				ProcessHit(line.hitInfo);
			}
		}

		private void ProcessHit(HitInfo info)
		{
			if (info.isHit)
			{
				log.LogHit(info);
				info.hitSymbol.log.LogHit(info);
			}
			info.Reset();
		}

		private void ApplyLoadout() { for (int x = 0; x < reelLength; x++) reels[x].CopyTo(slot.reels[x].symbols, 0); }

		public void SaveLoadout()
		{
#if UNITY_EDITOR
            if (!Application.isEditor) return;
            var path = EditorUtility.SaveFilePanel("Save symbol loadout as file", "", "loadout.dat","dat");
            if (path.Length != 0) {
                loadoutSaver.symbolLoadout.Clear();
                for (int i = 0; i < reelLength; i++) {
                    loadoutSaver.symbolLoadout.Add(new List<String>());
                    foreach (Symbol symbol in slot.reels[i].symbols) loadoutSaver.symbolLoadout[i].Add(symbol.name);
                }
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Create(path);
                bf.Serialize(file, loadoutSaver);
                file.Close();
                Debug.Log("symbol loadout saved");
            }
#endif
		}

		public void LoadLoadout()
		{
#if UNITY_EDITOR
            if (!Application.isEditor) return;
            string path = EditorUtility.OpenFilePanel("Load symbol loadout from file", "", "dat");
            if (path.Length != 0) {
                loadoutLoader.symbolLoadout.Clear();
                recalledLoadout.Clear();
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(path, FileMode.Open);
                loadoutLoader = (LoadoutSaver)bf.Deserialize(file);
                file.Close();
                for (int i = 0; i < reelLength; i++) {
                    recalledLoadout.Add(new Symbol[symbolsPerReel]);
					
                    for (int k = 0; k < symbolsPerReel; k++) {
                        recalledLoadout[i][k] = symbolManager.GetSymbol(loadoutLoader.symbolLoadout[i][k]);
						
                    }
                }
                for (int x = 0; x < reelLength; x++) recalledLoadout[x].CopyTo(slot.reels[x].symbols, 0);
                slot.layout.Refresh();


			int a = 2;
			int b = 3;
			int c = 4;

			int x1 = 1;

			int spin1 = 1;

			for (int j = 0; j < 10000; j += 11)
			{

				

				if (x1 >= 150)
				{
					Debug.Log("---END 1---");
					break;
				}



				

				Debug.Log(recalledLoadout[0][j + c]);
				Debug.Log(recalledLoadout[0][j + b]);
				Debug.Log(recalledLoadout[0][j + a]);
				

				Debug.Log("-----" + (x1));


				a = a + 1;
				b = b + 1;
				c = c + 1;

				x1 = x1+1;

				if (spin1 + 1 < 150)
				{
					spin1 = spin1 + 1;
				}
				else
				{
					spin1 = 0;
				}


			}



			a = 2;
			b = 3;
			c = 4;




			int x2 = 1;

			int spin2 = 1;

			for (int j = 0; j < 10000; j += 15)
			{

				

				if (x2 >= 151)
				{
					Debug.Log("---END 2---");
					break;
				}



				

				Debug.Log(recalledLoadout[1][j + c]);
				Debug.Log(recalledLoadout[1][j + b]);
				Debug.Log(recalledLoadout[1][j + a]);
				

				Debug.Log("-----" + (x2));


				a = a + 1;
				b = b + 1;
				c = c + 1;

				x2 = x2+1;

				if (spin2 + 1 < 150)
				{
					spin2 = spin2 + 1;
				}
				else
				{
					spin2 = 0;
				}


			}


			a = 2;
			b = 3;
			c = 4;




			int x3 = 1;

			int spin3 = 1;

			for (int j = 0; j < 10000; j += 19)
			{

				

				if (x3 >= 151)
				{
					Debug.Log("---END 3---");
					break;
				}



				

				Debug.Log(recalledLoadout[2][j + c]);
				Debug.Log(recalledLoadout[2][j + b]);
				Debug.Log(recalledLoadout[2][j + a]);
				

				Debug.Log("-----" + (x3));


				a = a + 1;
				b = b + 1;
				c = c + 1;

				x3 = x3+1;

				if (spin3 + 1 < 150)
				{
					spin3 = spin3 + 1;
				}
				else
				{
					spin3 = 0;
				}


			}

			a = 2;
			b = 3;
			c = 4;




			int x4 = 1;

			int spin4 = 1;

			for (int j = 0; j < 10000; j += 23)
			{

				

				if (x4 >= 151)
				{
					Debug.Log("---END 4---");
					break;
				}



				

				Debug.Log(recalledLoadout[3][j + c]);
				Debug.Log(recalledLoadout[3][j + b]);
				Debug.Log(recalledLoadout[3][j + a]);
				

				Debug.Log("-----" + (x4));


				a = a + 1;
				b = b + 1;
				c = c + 1;

				x4 = x4+1;

				if (spin4 + 1 < 150)
				{
					spin4 = spin4 + 1;
				}
				else
				{
					spin4 = 0;
				}


			}



			a = 2;
			b = 3;
			c = 4;




			int x5 = 1;

			int spin5 = 1;

			for (int j = 0; j < 10000; j += 27)
			{

				

				if (x5 >= 151)
				{
					Debug.Log("---END 5---");
					break;
				}



				

				Debug.Log(recalledLoadout[4][j + c]);
				Debug.Log(recalledLoadout[4][j + b]);
				Debug.Log(recalledLoadout[4][j + a]);
				

				Debug.Log("-----" + (x5));


				a = a + 1;
				b = b + 1;
				c = c + 1;

				x5 = x5+1;

				if (spin5 + 1 < 150)
				{
					spin5 = spin5 + 1;
				}
				else
				{
					spin5 = 0;
				}


			}

			


                Debug.Log("symbol loadout loaded");
				
            }
#endif
		}
	}
}

using System;
using CSFramework;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Net.Http.Headers;
using System.Collections;
using System.Net.Http;
using System;
using System.Threading.Tasks;


namespace Elona.Slot
{
	/// <summary>
	/// A main class for Elona Slot(Demo) derived from BaseSlotGame.
	/// For the most part, it's overriding the base methods to add visual/audio effects.
	/// </summary>
	public class Elos : MonoBehaviour
	{
		public CustomSlot slot;
		private int spinCounter;//
		private int maxSpin;//

		public GameObject immersionPrompt;

		[Hide] public bool gameOver = false;
		[Hide] public bool noSpinsLeft = false;
		[Hide] public int extraSpins = 0;



		[Serializable]
		public class Assets
		{
			[Serializable]
			public class Tweens
			{
				public TweenSprite tsBonus, tsIntro1, tsIntro2, tsWin, tsWinSpecial;
			}

			public Tweens tweens;
			public AudioSource bgm, audioDemo, audioEarnSmall, audioEarnBig, audioPay, audioSpin, audioSpinLoop, audioReelStop, audioClick;
			public AudioSource audioWinSmall, audioWinMedium, audioWinBig, audioLose, audioBet, audioImpact, audioBeep;
			public AudioSource audioBonus, audioWinSpecial, audioSpinBonus;
			public ParticleSystem particlePay, particlePrize, particleFreeSpin;
			public ElosEffectMoney effectMoney;
		}

		[Serializable]
		public class ElonaSlotData
		{
		}

		[Serializable]
		public class ElonaSlotSetting
		{
			public bool allowDebt = false;
			public int startingCredits = 1000;
			[Tooltip("0 = unlimited")] public int spinLimit = 0;
			public bool SpinLimitExcludesFreeSpins = false;
			public int immersionPromptInterval = 5; //
		}

		public Assets assets;
		public ElonaSlotData data;
		public ElonaSlotSetting setting;
		public ElosUI ui;
		public ElosBonusGame bonusGame;
		public float transitionTime = 3f;
		public CanvasGroup cg;
		public GameObject mold;


		protected void Awake()
		{
			spinCounter = 0;
			maxSpin = setting.immersionPromptInterval;
			// maxSpin = 30;
			immersionPrompt = GameObject.Find("Canvas Elos")//
				?.transform.Find("Elos Game")
				?.transform.Find("UI")
				?.transform.Find("Immersion prompt")
				?.gameObject;


			slot.callbacks.onProcessHit.AddListener(OnProcessHit);
			Initialize();
		}

		public void Initialize()
		{
			mold.gameObject.SetActive(false);
			GameObject immersionPrompt = GameObject.Find("Immersion prompt");
			if (!slot.debug.skipIntro)
			{
				cg.alpha = 0;
				cg.DOFade(1f, transitionTime * 0.5f).SetDelay(transitionTime * 0.5f);
			}
		}

		private void Update()
		{
			if (slot.debug.useDebugKeys)
			{
				if (Input.GetKeyDown(KeyCode.Alpha1)) assets.tweens.tsBonus.Play(0);
				if (Input.GetKeyDown(KeyCode.Alpha2)) assets.tweens.tsWinSpecial.Play(0);
				if (Input.GetKeyDown(KeyCode.Alpha3)) assets.tweens.tsIntro1.Play(0);
				if (Input.GetKeyDown(KeyCode.Alpha4)) assets.tweens.tsIntro2.Play(0);
				if (Input.GetKeyDown(KeyCode.F10)) slot.AddEvent(new SlotEvent(bonusGame.Activate));
			}
			if (Application.platform == RuntimePlatform.Android)
			{
				if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
			}
		}
		IEnumerator DelayAction(float delayTime)
		{
			//Wait for the specified delay time before continuing.
			yield return new WaitForSeconds(delayTime);

			//Do the action after the delay time has finished.
			immersionPrompt.SetActive(true);
			GameObject.Find("Button Manual Spin").SetActive(false);
		}

		public void Play()
		{

			Debug.Log(spinCounter);
			// Debug.Log(maxSpin);

			if (setting.SpinLimitExcludesFreeSpins) extraSpins = slot.gameInfo.totalFreeSpins;
			if (slot.state == CustomSlot.State.Idle && !setting.allowDebt && slot.gameInfo.balance < slot.gameInfo.roundCost)
			{
				assets.audioBeep.Play();
				return;
			}
			else if (setting.spinLimit > 0 && slot.gameInfo.roundsCompleted >= (setting.spinLimit + extraSpins))
			{
				noSpinsLeft = true;
				ui.ToggleCollectWin();

				return;
			}
			else if (gameOver)
			{
				return;
			}


			// sumesh - fixed the spin button state for accurate immersion prompt interval
			if (slot.state == CustomSlot.State.Idle)
			{
				spinCounter++;
			}




			slot.Play();

			// Debug.Log("Spin Counter Value");
			// Debug.Log(spinCounter);

			if (spinCounter > maxSpin - 1)
			{
				Debug.Log("reset");
				Debug.Log("test1");

				spinCounter = 0;

				//bring up immersion prompt
				StartCoroutine(DelayAction(3.2f));
			}





		}

		public void SpinLogButton()
		{
			if (slot.state == CustomSlot.State.Idle)
			{
				Debug.Log("Logging spin btn");
				ui.LogButton("SpinKeyUsedForSpin", 1, 1);
			}
		}
		public void OnProcessHit(HitInfo info)
		{
			if (info.hitSymbol.payType == Symbol.PayType.Custom) slot.AddEvent(new SlotEvent(bonusGame.Activate));
		}

		public void Save() { Save("game1"); }

		public void Save(string id)
		{
			PlayerPrefs.SetInt(id + "_balance", slot.gameInfo.balance);
			PlayerPrefs.Save();
		}

		public void Load() { Load("game1"); }

		public void Load(string id)
		{
			slot.gameInfo.balance = setting.startingCredits;    //PlayerPrefs.GetInt(id + "_balance", slot.gameInfo.balance);  //toggle to load saved score
			slot.gameInfo.dollars = decimal.Divide(slot.gameInfo.balance, 100);
		}

		public void DeleteSave(string id)
		{
			PlayerPrefs.DeleteKey(id + "_balance");
		}
	}
}
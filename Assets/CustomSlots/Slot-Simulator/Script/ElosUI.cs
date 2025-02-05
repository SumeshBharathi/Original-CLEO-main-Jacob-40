using System;
using System.Collections;
using CSFramework;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Elona.Slot
{

    public class QualtricsBridge : MonoBehaviour
    {
        [DllImport("__Internal")]
        public static extern void TransferData(string str1, string str2, string str3);
    }

    public class ElosUI : BaseSlotGameUI
    {
        [Serializable]
        public class Colors
        {
            public Gradient freeSpinBG;
            public Gradient freeSpinBGSlot;
        }

        [Header("Elos")] public Elos elos;

        public bool EnableCreditAnimations = true;
        public Colors colors;
        public Image background, highlightFreeSpin, backgroundSlot;
        public Button buttonPlay;
        public GameObject payTable, payLineTable, soundMenu, collectWin, diagnosticsDisplay, creditsObject, dollarsObject, immersionPrompt;
        public AudioMixer mixer;
        public Text textHandPay;

        public float volumeMaster { set { mixer.SetFloat("VolumeMaster", Mathf.Lerp(-80, 0, value)); } }
        public float volumeBGM { set { mixer.SetFloat("VolumeBGM", Mathf.Lerp(-80, 0, value)); } }
        public float volumeSE { set { mixer.SetFloat("VolumeSE", Mathf.Lerp(-80, 0, value)); } }

        private Tweener _moneyTween, _dollarsTween, _winningsTween;

        private int lastCredits, lastWinningsBalance, lastFreeRoundBalance;
        private int _lastCredits, _lastWinningsBalance, _lastFreeRoundBalance;
        private string lastDollars, _lastDollars;

        private Elos.Assets assets { get { return elos.assets; } }
        private Elos.ElonaSlotData data { get { return elos.data; } }
        //private void OnEnable() { assets.bgm.Play(); }
        public static string uiDataLine;
        public List<string> uiLog = new List<string>();
        private StreamWriter writerButton;
        private bool lockButtons = false;


        //fps display
        public Text textFPS;
        private int FramesPerSec;
        private string fps;
        private float frequency = 1.0f;
        private bool fpsActive;

        private IEnumerator FPS()
        {
            for (; ; )
            {
                int lastFrameCount = Time.frameCount;
                float lastTime = Time.realtimeSinceStartup;
                yield return new WaitForSeconds(frequency);
                float timeSpan = Time.realtimeSinceStartup - lastTime;
                int frameCount = Time.frameCount - lastFrameCount;
                textFPS.text = string.Format("FPS: {0}", Mathf.RoundToInt(frameCount / timeSpan));
            }
        }

        public override void Initialize()
        {
            elos.Load();
            base.Initialize();
            slot.callbacks.onAddBalance.AddListener(OnAddBalance);
            elos.bonusGame.gameObject.SetActive(false);
            textIncome.text = "0";
            textCredits.text = "" + slot.gameInfo.balance;
        }

        public override void OnActivated()
        {
            base.OnActivated();
            if (!slot.debug.skipIntro)
            {
                assets.audioDemo.Play();
                assets.tweens.tsIntro1.Play();
                assets.tweens.tsIntro2.Play();
            }
        }

        public override void OnRoundStart()
        {
            base.OnRoundStart();
            lastCredits = slot.gameInfo.balance;
            lastDollars = string.Format("$ {0:0.00}", slot.gameInfo.dollars);
            lastWinningsBalance = 0;
            if (slot.currentMode != slot.modes.freeSpinMode) buttonPlay.interactable = true;

        }

        public override void OnReelStart(ReelInfo info)
        {
            base.OnReelStart(info);
            if (info.isFirstReel)
            {
                assets.audioSpin.Play();
                assets.audioSpinLoop.Play();
            }
        }

        public override void OnReelStop(ReelInfo info)
        {
            base.OnReelStop(info);
            assets.audioReelStop.Play();
            if (info.isFirstReel && slot.currentMode.spinMode == SlotMode.SpinMode.ManualStopAll) buttonPlay.interactable = false;
            if (info.isLastReel)
            {
                buttonPlay.interactable = false;
                assets.audioSpinLoop.Stop();
            }
        }

        public override void OnRoundComplete()
        {
            base.OnRoundComplete();
            //if (slot.gameInfo.roundHits == 0) assets.audioLose.Play();    //disabled lose sfx
        }

        public override void EnableNextLine()
        {
            if (!slot.lineManager.EnableNextLine()) assets.audioBeep.Play();
            else assets.audioBet.Play();
        }

        public override void DisableCurrentLine()
        {
            if (!slot.lineManager.DisableCurrentLine()) assets.audioBeep.Play();
            else
            {
                assets.audioBet.pitch = 0.6f;
                assets.audioBet.Play();
            }
        }

        public override void RaiseBet()
        {
            if (!slot.isIdle) { LogButton("raise", betIndex + 1, 0); }
            else
            {
                LogButton("raise", betIndex + 1, 1);
                assets.audioBet.Play();
            }
            base.RaiseBet();
        }

        public override void LowerBet()
        {
            if (!slot.isIdle) { LogButton("lower", betIndex + 1, 0); }
            else
            {
                LogButton("lower", betIndex + 1, 1);
                assets.audioBet.Play();
            }
            base.LowerBet();
        }

        public override bool SetBet(int index)
        {
            if (!base.SetBet(index))
            {
                assets.audioBeep.Play();
                return false;
            }
            return true;
        }

        public void TogglePayTable()
        {
            if (!slot.isIdle)
            {
                LogButton("paytable", 0, 0);
                assets.audioBeep.Play();
            }
            else
            {
                LogButton("paytable", 0, 1);
                assets.audioClick.Play();
                payTable.SetActive(!payTable.activeSelf);
            }
        }

        public void TogglePayLineTable()
        {
            Debug.Log("Pay Line Table");
            if (!slot.isIdle)
            {
                LogButton("payLinetable", 0, 0);
                assets.audioBeep.Play();
            }
            else
            {
                LogButton("payLinetable", 0, 1);
                assets.audioClick.Play();
                payLineTable.SetActive(!payLineTable.activeSelf);
            }
        }
        public void ToggleSoundMenu()
        {
            LogButton("soundmenu", 0, 1);
            assets.audioClick.Play();
            soundMenu.SetActive(!soundMenu.activeSelf);
        }

        public void ToggleCredits()
        {
            if (creditsObject.activeSelf)
            {
                LogButton("credits", 0, 1);
                assets.audioClick.Play();
                creditsObject.SetActive(!creditsObject.activeSelf);
                dollarsObject.SetActive(!dollarsObject.activeSelf);
            }
            else
            {
                LogButton("credits", 1, 1);
                assets.audioClick.Play();
                creditsObject.SetActive(!creditsObject.activeSelf);
                dollarsObject.SetActive(!dollarsObject.activeSelf);
            }
        }

        public void ToggleCollectWin()
        {
            if (!slot.isIdle || (lockButtons && !elos.noSpinsLeft))
            {
                LogButton("collect", 0, 0);
                assets.audioBeep.Play();
            }
            else
            {
                if (!elos.noSpinsLeft) LogButton("collect", 0, 1);
                // the line to update the end screen text
                if ((slot.gameInfo.dollars - 40) > 0)
                {
                    textHandPay.text = string.Format("You won:\n\n${0:0.00}\n\nThis earns you a $5 bonus\n\n\n", slot.gameInfo.dollars - 40);
                }
                else
                {
                    textHandPay.text = string.Format("You lost:\n\n${0:0.00}\n\n This earns you no bonus\n\n\n", slot.gameInfo.dollars - 40);
                }

                if (!collectWin.activeSelf) assets.audioWinSpecial.Play();
                collectWin.SetActive(true);
                elos.gameOver = true;
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    string dataLogString = string.Join("|", slot.gameInfo.dataLog);
                    QualtricsBridge.TransferData("cleo_data", dataLogString, slot.debug.dataDestinationURL);
                    string uiLogString = string.Join("|", uiLog) + " Immersion_1 - " + ImmersionPromptScript.immersion_val_1.ToString() +
                    " | Immersion_2 - " + ImmersionPromptScript.immersion_val_2.ToString() + " | Immersion_3 - " + ImmersionPromptScript.immersion_val_3.ToString();
                    QualtricsBridge.TransferData("cleo_ui", uiLogString, slot.debug.dataDestinationURL);
                    Debug.Log(dataLogString);
                    Debug.Log(uiLogString);

                }
                else
                {
                    string dataLogString = string.Join("|", slot.gameInfo.dataLog);
                    // QualtricsBridge.TransferData("cleo_data", dataLogString, slot.debug.dataDestinationURL);
                    string uiLogString = string.Join("|", uiLog) + " Immersion_1 - " + ImmersionPromptScript.immersion_val_1.ToString() +
                    " | Immersion_2 - " + ImmersionPromptScript.immersion_val_2.ToString() + " | Immersion_3 - " + ImmersionPromptScript.immersion_val_3.ToString();
                    // QualtricsBridge.TransferData("cleo_ui", uiLogString, slot.debug.dataDestinationURL);
                    Debug.Log(dataLogString);
                    Debug.Log(uiLogString);
                }
            }
        }

        public void ToggleDiagnostics()
        {
            diagnosticsDisplay.SetActive(!diagnosticsDisplay.activeSelf);
            if (!fpsActive)
            {
                StartCoroutine(FPS());
                fpsActive = true;
            }
            else
            {
                StopCoroutine(FPS());
                fpsActive = false;
            }
        }

        public void ToggleButtonsLock()
        {
            if (!lockButtons) { lockButtons = true; }
            else { lockButtons = false; }
        }

        public override void ToggleFreeSpin(bool enable)
        {
            base.ToggleFreeSpin(enable);
            if (enable)
            {
                assets.bgm.volume = 1f;
                assets.bgm.Play();
                assets.particleFreeSpin.Play();
                backgroundSlot.DOGradientColor(colors.freeSpinBGSlot, 0.6f);
                background.DOGradientColor(colors.freeSpinBG, 0.6f);
            }
            else
            {
                assets.particleFreeSpin.Stop();
                backgroundSlot.DOColor(Color.white, 2f);
                background.DOColor(Color.white, 2f);
                assets.bgm.DOFade(0, 5);
                if (assets.bgm.volume == 0) assets.bgm.Stop();
            }
        }

        public override void OnProcessHit(HitInfo info)
        {
            base.OnProcessHit(info);
            SymbolHolder randomHolder = info.hitHolders[Random.Range(0, info.hitHolders.Count)];
            ///ElosSymbol symbol = randomHolder.symbol as ElosSymbol;
            foreach (SymbolHolder holder in info.hitHolders) info.sequence.Join(ShowWinAnimation(info, holder));
        }

        // Winning particle and audio effect when a line is a "hit"
        public Tweener ShowWinAnimation(HitInfo info, SymbolHolder holder)
        {
            return Util.Tween(() =>
            {
                int coins = (info.hitChains - 2) * (info.hitChains - 2) * (info.hitChains - 2) + 1;

                if (info.hitSymbol.payType == Symbol.PayType.Normal)
                {
                    assets.particlePrize.transform.position = holder.transform.position;
                    Util.Emit(assets.particlePrize, coins);
                    if (info.hitChains <= 3) assets.audioWinSmall.Play();
                    else if (info.hitChains == 4) assets.audioWinMedium.Play();
                    else assets.audioWinBig.Play();
                    //if (info.hitChains >= 4) assets.tweens.tsWin.SetText(info.hitChains + "-IN-A-ROW!", info.hitChains*40).Play(); //disabled the tsWin animation here eg '4-IN-A-ROW!'
                }
                else
                {
                    assets.audioWinSpecial.Play();
                    if (info.hitSymbol.payType == Symbol.PayType.FreesSpin) assets.tweens.tsWinSpecial.SetText("Free Spin!").Play();
                    else assets.tweens.tsWinSpecial.SetText("BONUS!").Play();
                }
            });
        }

        //function to streamline button logging
        public void LogButton(string type, int value, int valid)
        {

            if (slot.state == CustomSlot.State.Idle)
            {
                Debug.Log("Logging button");
                slot.gameInfo.buttonTimeElapsed = slot.gameInfo.stopWatch.Elapsed;
                uiDataLine = (GameInfo.idString + ", "
                    + GameInfo.sessionIdString + ", "
                    + DateTime.Now.ToString("yyyy:MM:dd-HH:mm:ss.fff") + ", "
                    + slot.gameInfo.roundsCompleted + ", "
                    + slot.gameInfo.buttonTimeElapsed.TotalSeconds + ", "
                    + slot.gameInfo.dollars + ","
                    + type + ", "
                    + value + ", "
                    + valid);


                // sumesh - logging UI data at any point
                //write the data to a file or list depending on platform
                // if (Application.platform == RuntimePlatform.WebGLPlayer) {
                //     using (StreamWriter writerButton = new StreamWriter((Application.persistentDataPath + @"/" + GameInfo.idString + "_" + GameInfo.sessionIdString + "_button_log.csv"), true)){
                //         writerButton.WriteLine(uiDataLine);
                //         uiLog.Add(uiDataLine);
                //     }
                // }
                // else { 

                uiLog.Add(uiDataLine);
            }
            // }

        }

        //function for pressing a button
        private void PressButton(string buttonType, int buttonValue)
        {
            if (buttonType == "bet")
            {
                if (!slot.isIdle || elos.gameOver || lockButtons) { LogButton(buttonType, buttonValue, 0); assets.audioBeep.Play(); }
                else { LogButton(buttonType, buttonValue, 1); SetBet(buttonValue); elos.Play(); }
            }
            else if (buttonType == "lines")
            {
                if (!slot.isIdle || elos.gameOver || lockButtons) { LogButton(buttonType, buttonValue, 0); assets.audioBeep.Play(); }
                else { LogButton(buttonType, buttonValue, 1); slot.lineManager.EnableLines(buttonValue); assets.audioBet.Play(); }
            }
        }

        private void Update()
        {
            //keyboard mappings for physical buttons
            //repeat bet button
            if (Input.GetKeyDown(KeyCode.Space) && immersionPrompt.activeSelf == false && immersionPrompt.activeInHierarchy == false && !((!slot.isIdle || elos.gameOver || lockButtons)))
            {
                LogButton("SpaceKeyUsedForSpin", 1, 1);
                // PressButton("bet", 0);
                elos.Play();
            }

            if (slot.debug.useKeyboardInput)
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    if (!slot.isIdle || elos.gameOver) { LogButton("repeat", 0, 0); }
                    else { elos.Play(); LogButton("repeat", 0, 1); }
                }

                //collect button
                if (Input.GetKeyDown(KeyCode.Z)) { ToggleCollectWin(); }

                //bet buttons
                if (Input.GetKeyDown(KeyCode.C)) { PressButton("bet", 0); }
                if (Input.GetKeyDown(KeyCode.V)) { PressButton("bet", 1); }
                if (Input.GetKeyDown(KeyCode.B)) { PressButton("bet", 2); }
                if (Input.GetKeyDown(KeyCode.N)) { PressButton("bet", 3); }
                if (Input.GetKeyDown(KeyCode.M)) { PressButton("bet", 4); }

                //line buttons
                if (Input.GetKeyDown(KeyCode.F)) { PressButton("lines", 1); }
                if (Input.GetKeyDown(KeyCode.G)) { PressButton("lines", 5); }
                if (Input.GetKeyDown(KeyCode.H)) { PressButton("lines", 9); }
                if (Input.GetKeyDown(KeyCode.J)) { PressButton("lines", 20); }
                if (Input.GetKeyDown(KeyCode.K)) { PressButton("lines", 40); }

                //diagnostics, reset, and lock buttons
                if (slot.debug.useDebugKeys)
                {
                    if (Input.GetKeyDown(KeyCode.T)) { ToggleDiagnostics(); }
                    if (Input.GetKeyDown(KeyCode.E)) { collectWin.SetActive(false); elos.gameOver = false; }
                    if (Input.GetKeyDown(KeyCode.R)) { ToggleButtonsLock(); }
                    if (Input.GetKeyDown(KeyCode.Y))
                    {
                        string dataLogString = string.Join("|", slot.gameInfo.dataLog);
                        QualtricsBridge.TransferData("cleo_data", dataLogString, slot.debug.dataDestinationURL);
                        string uiLogString = string.Join("|", uiLog);
                        QualtricsBridge.TransferData("cleo_ui", uiLogString, slot.debug.dataDestinationURL);
                    }
                }
            }

            //values used for balance and win tweens
            if (_lastCredits != lastCredits)
            {
                textCredits.text = "" + lastCredits;
                _lastCredits = lastCredits;
            }

            if (_lastDollars != lastDollars)
            {
                textDollars.text = "" + lastDollars;
                _lastDollars = lastDollars;
            }

            if (_lastWinningsBalance != lastWinningsBalance)
            {
                textIncome.text = "" + lastWinningsBalance;
                _lastWinningsBalance = 0;
            }

            if (_lastFreeRoundBalance != lastFreeRoundBalance)
            {
                textIncome.text = "" + lastFreeRoundBalance;
                _lastFreeRoundBalance = lastFreeRoundBalance;
            }
        }

        public override void RefreshMoney() { }

        public void OnAddBalance(BalanceInfo info)
        {
            if (info.amount == 0) return;

            float duration = 1f;
            if (info.amount < 0)
            {
                assets.audioPay.Play();
                Util.Emit(assets.particlePay, 3);
            }
            else
            {
                if (info.hitInfo != null)
                {
                    if (info.hitInfo.hitChains <= 4) assets.audioEarnSmall.Play();
                    else assets.audioEarnBig.Play();
                    duration = slot.effects.GetHitEffect(info.hitInfo).duration * 0.8f;
                }
                else
                {
                    assets.audioEarnSmall.Play();
                }
            }

            Util.InstantiateAt<ElosEffectMoney>(assets.effectMoney, transform).SetText(info.amount, info.hitInfo == null ? "" : info.hitInfo.hitChains + " in a row!").Play(100, 3f);

            if (_moneyTween != null && _moneyTween.IsPlaying()) _moneyTween.Complete();
            if (_dollarsTween != null && _dollarsTween.IsPlaying()) _dollarsTween.Complete();
            if (_winningsTween != null && _winningsTween.IsPlaying()) _winningsTween.Complete();

            if (EnableCreditAnimations && slot.state != CustomSlot.State.SpinStarting)
            {
                _moneyTween = DOTween.To(() => lastCredits, x => lastCredits = x, slot.gameInfo.balance, duration).OnComplete(() => { _moneyTween = null; });
                _dollarsTween = DOTween.To(() => lastDollars, x => lastDollars = x, string.Format("$ {0:0.00}", slot.gameInfo.dollars), duration).OnComplete(() => { _dollarsTween = null; });
            }
            else
            {
                lastCredits = slot.gameInfo.balance;
                lastDollars = string.Format("$ {0:0.00}", slot.gameInfo.dollars);
            }

            if ((slot.gameInfo.roundBalance + slot.gameInfo.roundCost) > 0 && slot.currentMode == slot.modes.freeSpinMode)
            {
                if (EnableCreditAnimations) _winningsTween = DOTween.To(() => lastFreeRoundBalance, x => lastFreeRoundBalance = x, slot.gameInfo.freeRoundBalance, duration).OnComplete(() => { _winningsTween = null; });
                else lastFreeRoundBalance = slot.gameInfo.freeRoundBalance;
            }
            else if ((slot.gameInfo.roundBalance + slot.gameInfo.roundCost) > 0)
            {
                if (EnableCreditAnimations) _winningsTween = DOTween.To(() => lastWinningsBalance, x => lastWinningsBalance = x, (slot.gameInfo.roundBalance + slot.gameInfo.roundCost), duration).OnComplete(() => { _winningsTween = null; });
                else lastWinningsBalance = (slot.gameInfo.roundBalance + slot.gameInfo.roundCost);
            }
        }
    }
}
﻿using BeatSaberMarkupLanguage.Attributes;
using BLPPCounter.Helpfuls;
using BLPPCounter.Settings.Configs;
using BLPPCounter.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Newtonsoft.Json.Linq;
using BLPPCounter.Counters;
using BLPPCounter.CalculatorStuffs;
using System.Threading.Tasks;
using System.Threading;
using HMUI;
using BeatSaberMarkupLanguage.Tags;
using BeatSaberMarkupLanguage.Components;
using BLPPCounter.Patches;
using UnityEngine.PlayerLoop;
using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;

namespace BLPPCounter.Settings.SettingHandlers
{
    public class PpInfoTabHandler
    {
#pragma warning disable IDE0051, IDE0044
        #region Misc Static Variables & Injects
        internal StandardLevelDetailViewController Sldvc;
        internal GameplayModifiersPanelController Gmpc;
        public static PpInfoTabHandler Instance { get; }
        private static PluginConfig PC => PluginConfig.Instance;
        #endregion
        #region Variables
        private string CurrentMods = "", UnformattedCurrentMods = "", CurrentTab = "Info";
        private float CurrentModMultiplier = 1f;
        private BeatmapKey CurrentMap;
        private JToken CurrentDiff;
        private object RefreshLock = new object();
        private readonly Dictionary<string, (BeatmapKey, Action<PpInfoTabHandler>)> Updates = new Dictionary<string, (BeatmapKey, Action<PpInfoTabHandler>)>()
        {
            {"Info", (default, new Action<PpInfoTabHandler>(pith => pith.UpdateInfo())) },
            {"Capture Values", (default, new Action<PpInfoTabHandler>(pith => pith.UpdateCaptureValues())) },
            {"Relative Values", (default, new Action<PpInfoTabHandler>(pith => pith.UpdateRelativeValues())) },
            {"Custom Accuracy", (default, new Action<PpInfoTabHandler>(pith => pith.UpdateCustomAccuracy())) }
        };
        #region Relative Counter
        private static Func<string> GetTarget, GetNoScoreTarget;
        private float TargetPP = 0;
        private bool TargetHasScore = false;
        #endregion
        #region Clan Counter
        private float PPToCapture = 0;
        #endregion
        #region Custom Accuracy
        private bool IsPPMode = true;
        #endregion
        #endregion
        #region UI Variables & components
        [UIValue(nameof(TestAcc))] private float TestAcc = 95.0f;
        [UIValue(nameof(TestPp))] private int TestPp = 450;
        [UIComponent(nameof(PrecentTable))] private TextMeshProUGUI PrecentTable;
        [UIComponent(nameof(PPTable))] private TextMeshProUGUI PPTable;
        [UIComponent("ModeButton")] private TextMeshProUGUI ModeButtonText;
        [UIObject(nameof(CA_PrecentSlider))] private GameObject CA_PrecentSlider;
        [UIObject(nameof(CA_PPSlider))] private GameObject CA_PPSlider;
        [UIObject(nameof(PrecentTable_BG))] private GameObject PrecentTable_BG;
        [UIObject(nameof(PPTable_BG))] private GameObject PPTable_BG;

        [UIComponent(nameof(RelativeText))] private TextMeshProUGUI RelativeText;
        [UIComponent(nameof(RelativeTable))] private TextMeshProUGUI RelativeTable;

        [UIComponent(nameof(ClanTable))] private TextMeshProUGUI ClanTable;
        [UIComponent(nameof(ClanTarget))] private TextMeshProUGUI ClanTarget;

        [UIComponent(nameof(AccStarText))] private TextMeshProUGUI AccStarText;
        [UIComponent(nameof(TechStarText))] private TextMeshProUGUI TechStarText;
        [UIComponent(nameof(PassStarText))] private TextMeshProUGUI PassStarText;
        [UIComponent(nameof(StarText))] private TextMeshProUGUI StarText;
        [UIComponent(nameof(SpeedModText))] private TextMeshProUGUI SpeedModText;
        [UIComponent(nameof(ModMultText))] private TextMeshProUGUI ModMultText;
        
        [UIComponent(nameof(MapName))] private TextMeshProUGUI MapName;
        [UIComponent(nameof(MapID))] private TextMeshProUGUI MapID;
        [UIValue(nameof(BeatmapName))] private string BeatmapName
        {
            get => _BeatmapName;
            set { if (MapName is null) return; MapName.text = $"<color=#777777>Map Name: <color=#aaaaaa>{value}</color>"; _BeatmapName = value; }
        }
        private string _BeatmapName = "";
        [UIValue(nameof(BeatmapID))] private string BeatmapID
        {
            get => _BeatmapID;
            set { if (MapID is null) return; MapID.text = $"<color=#777777>Map ID: <color=#aaaaaa>{value}</color>"; _BeatmapID = value; }
        }
        private string _BeatmapID = "";
        #endregion
        #region UI Functions
        [UIAction(nameof(Refresh))]
        private void ForceRefresh() { if (Sldvc != null && Gmpc != null) Refresh(true); }
        [UIAction(nameof(RefreshMods))]
        private void RefreshMods() { if (Sldvc != null && Gmpc != null) { UpdateMods(); UpdateTabDisplay(); } }
        [UIAction(nameof(RefreshClanTable))]
        private void RefreshClanTable() => UpdateCaptureValues();
        [UIAction(nameof(ChangeTab))]
        private void ChangeTab(SegmentedControl sc, int index)
        {
            if (sc.cells[index] is TextSegmentedControlCell tscc)
            {
                CurrentTab = tscc.text;
                if (Sldvc != null && Gmpc != null)
                    UpdateTabDisplay();
            }
        }
        [UIAction(nameof(PercentFormat))]
        private string PercentFormat(float toFormat) => $"{toFormat:N2}%";
        [UIAction(nameof(PPFormat))]
        private string PPFormat(int toFormat) => $"{toFormat:N0} pp";
        [UIAction(nameof(UpdateCA))]
        private void UpdateCA() => UpdateCustomAccuracy();
        [UIAction(nameof(ToggleCAMode))]
        private void ToggleCAMode()
        {
            IsPPMode = !IsPPMode;
            CA_PPSlider.SetActive(IsPPMode);
            CA_PrecentSlider.SetActive(!IsPPMode);
            PPTable_BG.SetActive(!IsPPMode);
            PrecentTable_BG.SetActive(IsPPMode);
            ModeButtonText.text = IsPPMode ? "<color=#A020F0>Input PP" : "<color=#FFD700>Input Precentage";
            UpdateCustomAccuracy();
        }
        [UIAction(nameof(RefreshRelativeTable))]
        private void RefreshRelativeTable() => UpdateRelativeValues();
        #endregion
        #region Inits
        static PpInfoTabHandler()
        {
            InitFormatters();
            Instance = new PpInfoTabHandler();
            TabSelectionPatch.ReferenceTabSelected += () =>
            {
                if (Instance.Sldvc != null && Instance.Gmpc != null)
                    Instance.Refresh();
            };
        }
        internal void SldvcInit() { Sldvc.didChangeContentEvent += (a, b) => RefreshAsync(); Sldvc.didChangeDifficultyBeatmapEvent += a => RefreshAsync(); }
        //internal void GmpcInit() { Gmpc.didChangeGameplayModifiersEvent += UpdateMods; UpdateMods(); }
        #endregion
        #region Formatting
        #region Relative Counter
        private static void InitFormatters()
        {
            var simple = HelpfulFormatter.GetBasicTokenParser(PC.MessageSettings.TargetHasNoScoreMessage,
                new Dictionary<string, char>() { { "Target", 't' } }, "TargetNoScoreMessage", null, (tokens, tokensCopy, priority, vals) =>
                {
                    foreach (char key in vals.Keys) if (vals[key] is null || vals[key].ToString().Length == 0) HelpfulFormatter.SetText(tokensCopy, key);
                }, out _, false).Invoke();
            GetNoScoreTarget = () => simple.Invoke(new Dictionary<char, object>() { { 't', Targeter.TargetName } });
            GetTarget = () => TheCounter.TargetFormatter?.Invoke(Targeter.TargetName, "") ?? "Target formatter is null";
        }
        private float GetAccToBeatTarget() =>
            CurrentDiff == null ? 0.0f : BLCalc.GetAcc(TargetPP, CurrentDiff, Gmpc.gameplayModifiers.songSpeed, CurrentModMultiplier, PC.DecimalPrecision);
        private float UpdateTargetPP()
        {
            //Plugin.Log.Info("Content Changed");
            CurrentMap = Sldvc.beatmapKey;
            CurrentDiff = null;
            if (!Sldvc.beatmapLevel.levelID.Substring(0, 6).Equals("custom")) return 0.0f; //means the level selected is not custom
            string apiOutput = RelativeCounter.RequestScore(
                Targeter.TargetID,
                Sldvc.beatmapLevel.levelID.Split('_')[2].ToLower(),
                Sldvc.beatmapKey.difficulty.ToString().Replace("+", "Plus"),
                Sldvc.beatmapKey.beatmapCharacteristic.serializedName,
                true
                );
            TargetHasScore = !(apiOutput is null || apiOutput.Length == 0);
            if (!TargetHasScore) { UpdateDiff(); return 0.0f; } //target doesn't have a score on this diff.
            JToken targetScore = JToken.Parse(apiOutput);
            BeatmapID = targetScore["leaderboardId"].ToString();
            BeatmapName = targetScore["song"]["name"].ToString();
            float outp = (float)targetScore["pp"];
            if (outp == 0.0f) //If the score set doesn't have a pp value, calculate one manually
            {
                int maxScore = (int)targetScore["difficulty"]["maxScore"], playerScore = (int)targetScore["modifiedScore"];
                targetScore = targetScore["difficulty"];
                outp = BLCalc.Inflate(BLCalc.GetPpSum((float)playerScore / maxScore, (float)targetScore["accRating"], (float)targetScore["passRating"], (float)targetScore["techRating"]));
            } else targetScore = targetScore["difficulty"];
            CurrentDiff = targetScore;
            if (UnformattedCurrentMods.Length > 0) CurrentModMultiplier = HelpfulPaths.GetMultiAmounts(CurrentDiff, UnformattedCurrentMods.Split(' '));
            return outp;
        }
        #endregion
        #region Clan Table
        private void BuildClanTable() => BuildTable((acc, pass, tech) => BLCalc.GetAcc(acc, pass, tech, PPToCapture, PC.DecimalPrecision) + "", ClanTable);
        #endregion
        #region Custom Accuracy
        private void BuildPPTable() => BuildTable((acc, pass, tech) => BLCalc.GetSummedPp(TestAcc / 100.0f, acc, pass, tech, PC.DecimalPrecision).Total + "", PPTable, " pp", accLbl: "<color=#0D0>PP</color>");
        private void BuildPrecentTable() => BuildTable((acc, pass, tech) => BLCalc.GetAcc(acc, pass, tech, TestPp, PC.DecimalPrecision) + "", PrecentTable, accLbl: "<color=#0D0>Acc</color>");
        #endregion
        private void BuildTable(Func<float, float, float, string> valueCalc, TextMeshProUGUI table,
            string suffix = "%",
            string speedLbl = "<color=blue>Speed</color>",
            string accLbl = "<color=#0D0>Acc</color> to Cap",
            string gnLbl = "With <color=#666>GN</color>")
        {
            string[][] arr = new string[] { "<color=red>Slower</color>", "<color=#aaa>Normal</color>", "<color=#0F0>Faster</color>", "<color=#FFD700>Super Fast</color>" }.RowToColumn(3);
            float[] ratings = HelpfulPaths.GetAllRatings(CurrentDiff); //ss-sf, [acc, pass, tech, star]
            float gnMult = (float)CurrentDiff["modifierValues"]["gn"] + 1.0f;
            for (int i = 0; i < arr.Length; i++)
                arr[i][1] = "<color=#0c0>" + valueCalc(ratings[i * 4], ratings[i * 4 + 1], ratings[i * 4 + 2]) + "</color>" + suffix;
            if (gnMult > 1.0f) for (int i = 0; i < arr.Length; i++)
                    arr[i][2] = "<color=#77aa77cc>" + valueCalc(ratings[i * 4] * gnMult, ratings[i * 4 + 1] * gnMult, ratings[i * 4 + 2] * gnMult) + "</color>" + suffix;
            else for (int i = 0; i < arr.Length; i++) arr[i][2] = "N/A";
            IEnumerator DelayRoutine()
            {
                yield return new WaitForEndOfFrame();
                HelpfulMisc.SetupTable(table, 0, arr, true, true, speedLbl, accLbl, gnLbl);
            };
            Task.Run(() => Sldvc.StartCoroutine(DelayRoutine())); 
            //This is done so that the game object is shown before the table is built. Otherwise, the game object gives wrong measurements, which messes up the table.
        }
        private static string Grammarize(string mods) //this is very needed :)
        {
            int commaCount = mods.Count(c => c == ',');
            if (commaCount == 0) return mods;
            if (commaCount == 1) return mods.Replace(", ", " and ");
            return mods.Substring(0, mods.LastIndexOf(',')) + " and" + mods.Substring(mods.LastIndexOf(','));
        }
        private void UpdateMods() //this is why you use bitmasks instead of a billion bools vars
        {
            string newMods = "";
            UnformattedCurrentMods = "";
            GameplayModifiers mods = Gmpc.gameplayModifiers;
            switch (mods.songSpeed)
            {//Speed mods are not added to UnformattedCurrentMods because they are handled in a different way.
                case GameplayModifiers.SongSpeed.Slower: newMods += "Slower Song, "; break;
                case GameplayModifiers.SongSpeed.Faster: newMods += "Faster Song, "; break;
                case GameplayModifiers.SongSpeed.SuperFast: newMods += "Super Fast Song, "; break;
            }
            if (mods.ghostNotes) { UnformattedCurrentMods += "gn "; newMods += "Ghost Notes, "; }
            if (mods.disappearingArrows) { UnformattedCurrentMods += "da "; newMods += "Disappearing Arrows, "; }
            if (mods.energyType == GameplayModifiers.EnergyType.Battery) newMods += "Four Lifes, ";
            if (mods.noArrows) { UnformattedCurrentMods += "na "; newMods += "No Arrows, "; }
            if (mods.noFailOn0Energy) { UnformattedCurrentMods += "nf "; newMods += "No Fail, "; }
            if (mods.zenMode) newMods += "Zen Mode (why are you using zen mode), ";
            if (mods.instaFail) newMods += "One Life, ";
            if (mods.noBombs) { UnformattedCurrentMods += "nb "; newMods += "No Bombs, "; }
            if (mods.proMode) { UnformattedCurrentMods += "pm "; newMods += "Pro Mode, "; }
            if (mods.smallCubes) { UnformattedCurrentMods += "sc "; newMods += "Small Cubes, "; }
            if (mods.strictAngles) { UnformattedCurrentMods += "sa "; newMods += "Strict Angles, "; }
            if (mods.enabledObstacleType == GameplayModifiers.EnabledObstacleType.NoObstacles) { UnformattedCurrentMods += "no "; newMods += "No Walls, "; }
            CurrentMods = newMods.Length > 1 ? Grammarize(newMods.Substring(0, newMods.Length - 2)) : null;
            if (UnformattedCurrentMods.Length > 0) UnformattedCurrentMods = UnformattedCurrentMods.Trim();
            if (CurrentDiff != null) CurrentModMultiplier = HelpfulPaths.GetMultiAmounts(CurrentDiff, UnformattedCurrentMods.Split(' '));
        }
        #endregion
        #region Misc Functions
        public void UpdateTabDisplay(bool forceUpdate = false) 
        {
            if (CurrentTab.Length <= 0 || (!forceUpdate && Updates[CurrentTab].Item1 == CurrentMap)) return;
            Updates[CurrentTab].Item2.Invoke(this);
            Updates[CurrentTab] = (CurrentMap, Updates[CurrentTab].Item2);
        }
        private void UpdateInfo()
        {
            if (CurrentDiff != null)
            {
                const char Star = (char)9733;
                var (accRating, passRating, techRating, starRating) = HelpfulMisc.GetRatingsAndStar(CurrentDiff, Gmpc.gameplayModifiers.songSpeed, CurrentModMultiplier);
                AccStarText.text = Math.Round(accRating, PC.DecimalPrecision) + " " + Star;
                PassStarText.text = Math.Round(passRating, PC.DecimalPrecision) + " " + Star;
                TechStarText.text = Math.Round(techRating, PC.DecimalPrecision) + " " + Star;
                StarText.text = Math.Round(starRating, PC.DecimalPrecision) + " " + Star;
            }
            SpeedModText.text = "<color=green>" + HelpfulMisc.AddSpaces(Gmpc.gameplayModifiers.songSpeed.ToString());
            ModMultText.text = $"x{Math.Round(CurrentModMultiplier, 2):N2}";
        }
        private void UpdateCaptureValues() 
        {
            if (CurrentDiff is null) return;
            ClanTarget.text = TargetHasScore ? GetTarget() : GetNoScoreTarget();
            Task.Run(() =>
            {
                PPToCapture = ClanCounter.LoadNeededPp(BeatmapID, out _)[0];
                BuildClanTable();
            });
        }
        private void UpdateRelativeValues()
        {
            RelativeText.text = TargetHasScore ? GetTarget() : GetNoScoreTarget();
            BuildTable((acc, tech, pass) => BLCalc.GetAcc(acc, pass, tech, TargetPP, PC.DecimalPrecision) + "", RelativeTable);
        }
        private void UpdateCustomAccuracy()
        {
            if (CurrentDiff is null) return;
            if (IsPPMode) BuildPrecentTable(); else BuildPPTable();
        }
        private void UpdateDiff()
        {
            if (!TheCounter.CallAPI("leaderboards/hash/" + Sldvc.beatmapLevel.levelID.Split('_')[2].ToUpper(), out string dataStr)) return;
            int val = Map.FromDiff(Sldvc.beatmapKey.difficulty);
            CurrentDiff = JToken.Parse(dataStr);
            BeatmapName = CurrentDiff["song"]["name"].ToString();
            CurrentDiff = CurrentDiff["leaderboards"].Children().First(t => ((int)t["difficulty"]["value"]) == val);
            BeatmapID = CurrentDiff["id"].ToString();
            CurrentDiff = CurrentDiff["difficulty"];
        }
        private void RefreshAsync() => Task.Run(() => Refresh());
        public void Refresh(bool forceRefresh = false)
        {
            if (!TabSelectionPatch.IsReferenceTabSelected || (!forceRefresh && Sldvc.beatmapKey.Equals(CurrentMap))) return;
            if (Monitor.TryEnter(RefreshLock))
            {
                try
                {
                    UpdateMods();
                    TargetPP = UpdateTargetPP();
                    UpdateTabDisplay(forceRefresh);
                }
                catch (Exception e) { Plugin.Log.Error($"There was an error!\n{e.Message}"); }
                finally { Monitor.Exit(RefreshLock); }
            }
        }
        #endregion
    }
}

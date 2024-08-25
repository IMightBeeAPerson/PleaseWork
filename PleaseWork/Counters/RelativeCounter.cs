﻿using System;
using TMPro;
using BeatLeader.Models.Replay;
using System.Net.Http;
using PleaseWork.Settings;
using PleaseWork.CalculatorStuffs;
using PleaseWork.Utils;
using PleaseWork.Helpfuls;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace PleaseWork.Counters
{
    public class RelativeCounter: IMyCounters
    {
        private static readonly HttpClient client = new HttpClient();
        public string Name { get => "Relative"; }
        private static Func<bool, string, string, float, string, string, float, string, string> displayFormatter;

        private TMP_Text display;
        private float accRating, passRating, techRating;
        private float[] best; //pass, acc, tech, total, replay pass rating, replay acc rating, replay tech rating, current score, current combo
        private Replay bestReplay;
        private string replayMods;
        private NoteEvent[] noteArray;
        private int precision;
        private NormalCounter backup;
        private bool failed;
        #region Init
        public RelativeCounter(TMP_Text display, float accRating, float passRating, float techRating)
        {
            this.accRating = accRating;
            this.passRating = passRating;
            this.techRating = techRating;
            this.display = display;
            failed = false;
            precision = PluginConfig.Instance.DecimalPrecision;
            if (displayFormatter == null) FormatTheFormat(PluginConfig.Instance.FormatSettings.RelativeTextFormat);
        }
        public RelativeCounter(TMP_Text display, MapSelection map) : this(display, map.AccRating, map.PassRating, map.TechRating) { SetupData(map); }
        private void SetupReplayData(JToken data)
        {
            Plugin.Log.Debug(data.ToString());
            string replay = (string)data["replay"];
            ReplayDecoder.TryDecodeReplay(RequestByteData(replay), out bestReplay);
            noteArray = bestReplay.notes.ToArray();
            replayMods = bestReplay.info.modifiers.ToLower();
            Modifier mod = replayMods.Contains("fs") ? Modifier.FasterSong : replayMods.Contains("sf") ? Modifier.SuperFastSong : replayMods.Contains("ss") ? Modifier.SlowerSong : Modifier.None;
            replayMods = replayMods.ToUpper();
            data = data["difficulty"];
            best[4] = HelpfulPaths.GetRating(data, PPType.Pass, mod);
            best[5] = HelpfulPaths.GetRating(data, PPType.Acc, mod);
            best[6] = HelpfulPaths.GetRating(data, PPType.Tech, mod);
        }
        #endregion
        #region Overrides
        public void SetupData(MapSelection map)
        {
            try
            {
                string check = RequestScore(Targeter.TargetID, map.Map.Hash, map.Difficulty, map.Mode);
                if (check != default && check.Length > 0)
                {
                    JToken playerData = JObject.Parse(check);
                    best = new float[9];
                    SetupReplayData(playerData);
                    /*playerData = new Regex(@"(?<=contextExtensions..\[)[^\[\]]+").Match(playerData).Value;
                    playerData = new Regex(@"{.+?(?=..scoreImprovement)").Matches(playerData)[0].Value;*/
                }
            }
            catch (Exception e)
            {
                Plugin.Log.Warn("There was an error loading the replay of the player, most likely they have never played the map before.");
                if (PluginConfig.Instance.Debug) Plugin.Log.Debug(e);
                failed = true;
                backup = new NormalCounter(display, map);
                return;
            }
            if (!failed && best != null && best.Length >= 9)
                best[7] = best[8] = 0;
        }
        public void ReinitCounter(TMP_Text display)
        {
            this.display = display;
            if (failed) backup.ReinitCounter(display);
            if (!failed && best != null && best.Length >= 9)
                best[7] = best[8] = 0;
        }
        public void ReinitCounter(TMP_Text display, float passRating, float accRating, float techRating)
        {
            this.display = display;
            this.passRating = passRating;
            this.accRating = accRating;
            this.techRating = techRating;
            precision = PluginConfig.Instance.DecimalPrecision;
            if (failed) backup.ReinitCounter(display, passRating, accRating, techRating);
            if (!failed && best != null && best.Length >= 9)
                best[7] = best[8] = 0;
        }
        public void ReinitCounter(TMP_Text display, MapSelection map)
        { this.display = display; passRating = map.PassRating; accRating = map.AccRating; techRating = map.TechRating; failed = false; SetupData(map); }
        #endregion
        #region API Calls
        private byte[] RequestByteData(string path)
        {
            try
            {
                HttpResponseMessage hrm = client.GetAsync(new Uri(path)).Result;
                hrm.EnsureSuccessStatusCode();
                return hrm.Content.ReadAsByteArrayAsync().Result;
            }
            catch (HttpRequestException e)
            {
                Plugin.Log.Warn($"Beat Leader API request failed!\nPath: {path}\nError: {e.Message}");
                Plugin.Log.Debug(e);
                return null;
            }
        }
        private string RequestScore(string id, string hash, string diff, string mode)
        {
            return RequestData($"https://api.beatleader.xyz/score/8/{id}/{hash}/{diff}/{mode}");
        }
        private string RequestData(string path)
        {
            try
            {
                return client.GetStringAsync(new Uri(path)).Result;
            }
            catch (HttpRequestException e)
            {
                Plugin.Log.Warn($"Beat Leader API request failed!\nPath: {path}\nError: {e.Message}");
                Plugin.Log.Debug(e);
                return "";
            }
        }
        #endregion
        #region Helper Functions
        public static void FormatTheFormat(string format)
        {
            var simple = HelpfulFormatter.GetBasicTokenParser(format,
                tokens =>
                {
                    if (!PluginConfig.Instance.ShowLbl) HelpfulFormatter.SetText(tokens, 'l');
                    if (!PluginConfig.Instance.RelativeWithNormal) { HelpfulFormatter.SetText(tokens, 'p'); HelpfulFormatter.SetText(tokens, 'o'); }
                },
                (tokens, tokensCopy, priority, vals) =>
                {
                    HelpfulFormatter.SurroundText(tokensCopy, 'c', $"{vals['c']}", "</color>");
                    HelpfulFormatter.SurroundText(tokensCopy, 'f', $"{vals['f']}", "</color>");
                    if (!(bool)vals['q']) HelpfulFormatter.SetText(tokensCopy, '1');
                });
            displayFormatter = (fc, color, modPp, regPp, fcCol, fcModPp, fcRegPp, label) =>
            {
                Dictionary<char, object> vals = new Dictionary<char, object>()
                {
                    { 'q', fc }, { 'c', color }, {'x',  modPp }, {'p', regPp }, {'l', label }, { 'f', fcCol }, { 'y', fcModPp }, { 'o', fcRegPp }
                };
                return simple.Invoke(vals);
            };
        }
        #endregion
        #region Updates
        private void UpdateBest(int notes)
        {
            if (notes < 1) return;
            NoteEvent note = noteArray[notes-1];
            if (note.eventType == NoteEventType.good)
            {
                best[8]++;
                best[7] += BLCalc.GetCutScore(note.noteCutInfo) * HelpfulMath.MultiplierForNote((int)Math.Round(best[8]));
            }
            else
            {
                best[8] = HelpfulMath.DecreaseMultiplier((int)Math.Round(best[8]));
            }

            (best[0], best[1], best[2]) = BLCalc.GetPp(best[7] / HelpfulMath.MaxScoreForNotes(notes), best[5], best[4], best[6]);
            best[3] = BLCalc.Inflate(best[0] + best[1] + best[2]);
        }
        public void UpdateCounter(float acc, int notes, int badNotes, float fcPercent)
        {
            if (failed)
            {
                backup.UpdateCounter(acc, notes, badNotes, fcPercent);
                return;
            }
            bool displayFc = PluginConfig.Instance.PPFC && badNotes > 0, showLbl = PluginConfig.Instance.ShowLbl;
            UpdateBest(notes);
            float[] ppVals = new float[16];
            (ppVals[0], ppVals[1], ppVals[2]) = BLCalc.GetPp(acc, accRating, passRating, techRating);
            ppVals[3] = BLCalc.Inflate(ppVals[0] + ppVals[1] + ppVals[2]);
            for (int i = 0; i < 4; i++)
                ppVals[i + 4] = ppVals[i] - best[i];
            if (displayFc)
            {
                (ppVals[8], ppVals[9], ppVals[10]) = BLCalc.GetPp(fcPercent, accRating, passRating, techRating);
                ppVals[11] = BLCalc.Inflate(ppVals[8] + ppVals[9] + ppVals[10]);
                for (int i = 8; i < 12; i++)
                    ppVals[i + 4] = ppVals[i] - best[i - 8];
            }
            for (int i = 0; i < ppVals.Length; i++)
                ppVals[i] = (float)Math.Round(ppVals[i], precision);
            string[] labels = new string[] { " Pass PP", " Acc PP", " Tech PP", " PP" };
            string target = PluginConfig.Instance.ShowEnemy ? PluginConfig.Instance.Target : "None";
            string color(float num) => PluginConfig.Instance.UseGrad ? HelpfulFormatter.NumberToGradient(num) : HelpfulFormatter.NumberToColor(num);
            if (PluginConfig.Instance.SplitPPVals)
            {
                string text = "";
                for (int i = 0; i < 4; i++)
                    text += displayFormatter.Invoke(displayFc, color(ppVals[i + 4]), ppVals[i + 4].ToString(HelpfulFormatter.NUMBER_TOSTRING_FORMAT), ppVals[i],
                        color(ppVals[i + 12]), ppVals[i + 12].ToString(HelpfulFormatter.NUMBER_TOSTRING_FORMAT), ppVals[i + 8], labels[i]) + "\n";
                display.text = text;
            }
            else
                display.text = displayFormatter.Invoke(displayFc, color(ppVals[7]), ppVals[7].ToString(HelpfulFormatter.NUMBER_TOSTRING_FORMAT), ppVals[3],
                    color(ppVals[15]), ppVals[15].ToString(HelpfulFormatter.NUMBER_TOSTRING_FORMAT), ppVals[11], labels[3]) + "\n";
            if (!target.Equals("None"))
                display.text += TheCounter.TargetFormatter.Invoke(target, replayMods);

        }
        #endregion
    }
}

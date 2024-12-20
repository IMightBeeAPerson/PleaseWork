﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using BLPPCounter.Utils;
//using UnityEngine;
using System.Drawing;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace BLPPCounter.Settings.Configs
{
    internal class PluginConfig
    {
        #region Non-Settings Variables
        public static PluginConfig Instance { get; set; }
        public virtual TokenFormatSettings TokenSettings { get; set; } = new TokenFormatSettings();
        public virtual MessageSettings MessageSettings { get; set; } = new MessageSettings();
        public virtual TextFormatSettings FormatSettings { get; set; } = new TextFormatSettings();
        #endregion
        #region General Settings
        public virtual int DecimalPrecision { get; set; } = 2;
        public virtual double FontSize { get; set; } = 3;
        public virtual bool ShowLbl { get; set; } = true;
        public virtual bool PPFC { get; set; } = false;
        public virtual bool SplitPPVals { get; set; } = false;
        public virtual bool ExtraInfo { get; set; } = false;
        public virtual bool UseGrad { get; set; } = true;
        public virtual int GradVal { get; set; } = 100;
        public virtual string PPType { get; set; } = "Normal";
        #endregion
        #region Clan Counter Settings
        public virtual bool ShowClanMessage { get; set; } = false;
        public virtual int MapCache { get; set; } = 10;
        public virtual double ClanPercentCeil { get; set; } = 99;
        public virtual bool CeilEnabled { get; set; } = true;
        #endregion
        #region Relative Counter Settings
        public virtual bool ShowRank { get; set; } = true;
        public virtual string RelativeDefault { get; set; } = "Normal";
        #endregion
        #region Rank Counter Settings
        public virtual int MinRank { get; set; } = 100;
        public virtual int MaxRank { get; set; } = 0;
        #endregion
        #region Target Settings
        public virtual bool ShowEnemy { get; set; } = true;
        public virtual string Target { get; set; } = Targeter.NO_TARGET;

        //The below list is not in order so that in the config file there is nothing below this that gets obstructed.
        [UseConverter(typeof(ListConverter<CustomTarget>))]
        public virtual List<CustomTarget> CustomTargets { get; set; } = new List<CustomTarget>();
        #endregion
        #region Unused Code
        //public virtual bool LocalReplay { get; set; } = false;
        //public virtual string ChosenPlaylist { get; set; } = "";
        #endregion
        #region Menu Settings
        #region Simple Settings
        public virtual bool SimpleUI { get; set; } = true;
        public virtual int SimpleMenuConfig { get; set; } = 0; //Don't worry about this, nothing janky at all going on here :)
        public virtual int SimpleMenuConfigLength { get; set; } = 0; //Nothing janky at all
        #endregion
        #region Format Settings
        public virtual bool UpdatePreview { get; set; } = true;

        #region Colors
        [UseConverter(typeof(SystemColorConverter))] public virtual Color EscapeCharacterColor { get; set; } = Color.FromArgb(235, 33, 235); //#eb21eb
        [UseConverter(typeof(SystemColorConverter))] public virtual Color SpecialCharacterColor { get; set; } = Color.Goldenrod;
        [UseConverter(typeof(SystemColorConverter))] public virtual Color AliasColor { get; set; } = Color.FromArgb(187, 242, 46); //#bbf22e
        [UseConverter(typeof(SystemColorConverter))] public virtual Color AliasQuoteColor { get; set; } = Color.FromArgb(32, 171, 51); //#20ab33
        [UseConverter(typeof(SystemColorConverter))] public virtual Color ParamColor { get; set; } = Color.DarkCyan;
        [UseConverter(typeof(SystemColorConverter))] public virtual Color ParamVarColor { get; set; } = Color.Brown;
        [UseConverter(typeof(SystemColorConverter))] public virtual Color DelimeterColor { get; set; } = Color.DarkSlateGray;
        [UseConverter(typeof(SystemColorConverter))] public virtual Color CaptureColor { get; set; } = Color.FromArgb(44, 241, 245); //#2cf1f5
        [UseConverter(typeof(SystemColorConverter))] public virtual Color CaptureIdColor { get; set; } = Color.LightBlue;
        [UseConverter(typeof(SystemColorConverter))] public virtual Color GroupColor { get; set; } = Color.FromArgb(27, 40, 224); //#1b28e0
        [UseConverter(typeof(SystemColorConverter))] public virtual Color GroupReplaceColor { get; set; } = Color.FromArgb(255, 75, 43); //#ff4b2b
        [UseConverter(typeof(SystemColorConverter))] public virtual Color ShorthandColor { get; set; } = Color.DarkMagenta; //#ff4b2b
        [UseConverter(typeof(SystemColorConverter))] public virtual Color HighlightColor { get; set; } = Color.FromArgb(136, 255, 255, 0); //#ff4b2b
        #endregion
        #endregion
        #endregion
    }
}

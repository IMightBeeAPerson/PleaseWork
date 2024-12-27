﻿using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BLPPCounter.Helpfuls;
using IPA.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BLPPCounter.Utils.List_Settings
{
    internal class ValueListInfo : INotifyPropertyChanged
    {
        //IDE find and replace pattern
        // \)\s*\n?\s*{\s*\n?\s*([^;]+;)\s*\n?\s*}
        //) => $+
#pragma warning disable IDE0044
        #region Static Variables
        internal static Action UpdatePreview;
        #endregion
        #region Variables
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly Type ActualClass;
        private Func<object, bool, object> ValFormatter;
        private object _GivenValue;
        private char GivenToken;
        private object GivenValue {
            get => HasWrapper ? new Func<object>(() => _GivenValue) : _GivenValue;
            set { _GivenValue = value; PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(GivenValue))); }
        }
        private object FormattedGivenValue => ValFormatter?.Invoke(GivenValue, false) ?? GivenValue;
        public bool HasWrapper;
        #endregion
        #region UI Variables
        private static readonly Dictionary<string, MemberInfo> UIVals = 
            HelpfulMisc.GetAllVariablesUsingAttribute(typeof(ValueListInfo), typeof(UIValue), BindingFlags.Instance | BindingFlags.NonPublic).ToDictionary(mi => mi.Name);
        
        [UIValue(nameof(ShowToggle))] private bool ShowToggle = false;
        [UIValue(nameof(ShowTextBox))] private bool ShowTextBox = false;
        [UIValue(nameof(ShowIncrement))] private bool ShowIncrement = false;

        [UIValue(nameof(ValueName))] private string ValueName;
        [UIValue(nameof(GivenValueBool))] private bool GivenValueBool
        {
            get => _GivenValue is bool outp && outp;
            set { if (ActualClass == typeof(bool)) GivenValue = value; }
        }
        [UIValue(nameof(GivenValueString))] private string GivenValueString
        {
            get => _GivenValue is string outp ? outp : _GivenValue.ToString();
            set { if (ActualClass == typeof(string)) GivenValue = value; }
        }
        [UIValue(nameof(GivenValueNumber))] private float GivenValueNumber
        {
            get => IsInteger ? _GivenValue is int i ? i : default : _GivenValue is float outp ? outp : default;
            set { if (HelpfulMisc.IsNumber(ActualClass)) GivenValue = value.GetType() == ActualClass ? value : Convert.ChangeType(value, ActualClass); }
        }
        [UIValue(nameof(IsInteger))] private bool IsInteger;
        [UIValue(nameof(MinVal))] private float MinVal;
        [UIValue(nameof(MaxVal))] private float MaxVal;
        [UIValue(nameof(IncrementVal))] private float IncrementVal;
        #endregion
        #region UI Components
        [UIComponent(nameof(TextBox))] private StringSetting TextBox;
        [UIComponent(nameof(Increment))] private IncrementSetting Increment;

        #endregion
        #region Init
        internal ValueListInfo(object givenValue, char token, string name, bool hasWrapper = false, Func<object, bool, object> valFormat = null,
            IEnumerable<(string, object)> extraParams = null)
        {
            HasWrapper = hasWrapper;
            _GivenValue = givenValue;
            GivenToken = token;
            ValueName = name;
            ValFormatter = valFormat;
            //if (valFormat == null) Plugin.Log.Info($"{name} has no formatter!");
            ActualClass = givenValue.GetType();
            switch (ActualClass)
            {
                case Type v when v == typeof(bool): ShowToggle = true; break;
                case Type v when HelpfulMisc.IsNumber(v): ShowIncrement = true; break;
                default: ShowTextBox = true; break;
            }
            if (extraParams != null) foreach ((string, object) newVal in extraParams)
                {
                    if (UIVals.TryGetValue(newVal.Item1, out MemberInfo mi))
                    {
                        if (mi is FieldInfo fi) fi.SetValue(this, newVal.Item2);
                        else if (mi is PropertyInfo pi) pi.SetValue(this, newVal.Item2);
                        else continue;
                    }
                }
            else
            {
                IsInteger = true;
                MinVal = 0;
                MaxVal = 10;
                IncrementVal = 1;
            }
            PropertyChanged += OnPropertyChanged;
        }
        #endregion
        #region UI Functions
        [UIAction(nameof(Formatterer))] 
        private string Formatterer(object input) => $"<align=\"center\">{ValFormatter?.Invoke(input, true) ?? input.ToString()}";
        #endregion
        #region Functions
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args) => UpdatePreview?.Invoke();
        public override string ToString() => (HasWrapper ?
            $"Raw: () => {_GivenValue} || Formatted: () => {(ValFormatter?.Invoke(GivenValue, false) as Func<object>)?.Invoke() ?? "null"}" :
            $"Raw: {_GivenValue} || Formatted: {ValFormatter?.Invoke(GivenValue, false) ?? "null"}") + 
            " || Val Formatted: " + Formatterer(_GivenValue);
        #endregion
        #region Static Functions
        internal static Dictionary<char, object> GetNewTestVals(IEnumerable<ValueListInfo> arr, bool formatted = true, Dictionary<char, object> oldVals = null)
        {
            Dictionary<char, object> outp = oldVals ?? new Dictionary<char, object>();
            foreach (ValueListInfo val in arr)
                outp[val.GivenToken] = formatted ? val.FormattedGivenValue : val.GivenValue;
            return outp;
        }
        #endregion
    }
}

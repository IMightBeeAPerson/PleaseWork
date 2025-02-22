﻿using BeatSaberMarkupLanguage.Attributes;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static BLPPCounter.Helpfuls.HelpfulFormatter;
using System;
using System.Linq;
using BeatSaberMarkupLanguage.Components.Settings;
using UnityEngine;
using TMPro;
using System.ComponentModel;
using static BLPPCounter.Utils.FormatListInfo.ChunkType;
using static BLPPCounter.Helpfuls.HelpfulMisc;
using BLPPCounter.Settings.Configs;
using BLPPCounter.Helpfuls;
using System.Reflection;
using BLPPCounter.Utils.Special_Utils;

namespace BLPPCounter.Utils
{
    public class FormatListInfo : INotifyPropertyChanged
    {
#pragma warning disable IDE0044, CS0649, CS0414
        #region Static Variables
        public static Dictionary<string, char> AliasConverter { get; internal set; }
        private static List<object> ParentList;
        private static Action UpdateTable, UpdatePreview;

        private static readonly string AliasRegex = string.Format("(?<Token>{0}.|{0}{1}[^{1}]+{1}){2}(?<Params>[^{3}]+){3}|(?<Token>{0}{1}[^{1}]+{1}|{0}.)", Regex.Escape($"{ESCAPE_CHAR}"), Regex.Escape($"{ALIAS}"), Regex.Escape($"{PARAM_OPEN}"), Regex.Escape($"{PARAM_CLOSE}"));
        //(?<Token>&.|&'[^']+')\((?<Params>[^\)]+)\)|(?<Token>&'[^']+'|&.)
        private static readonly string RegularTextRegex = "[^" + INSERT_SELF + RegexSpecialChars.Substring(1) + "+"; //[^$&*[\]<>]+
        private static readonly string EscapedCharRegex = $"{Regex.Escape("" + ESCAPE_CHAR)}{RegexSpecialChars}"; //&[&*[\]<>]
        internal static readonly Regex CollectiveRegex = GetRegexForAllChunks();

        public static FormatListInfo DefaultVal => new FormatListInfo("Default Text", false);

        #endregion
        #region UI Variables
        [UIValue(nameof(TypesOfChunks))] private List<object> TypesOfChunks => Enum.GetNames(typeof(ChunkType)).Select(s => s.Replace('_', ' ')).Cast<object>().ToList();
        [UIValue(nameof(ChoiceOptions))] private List<object> ChoiceOptions = new List<object>();

        [UIValue(nameof(ChunkStr))] private string ChunkStr
        {
            get => Chunk.ToString().Replace('_', ' ');
            set
            {
                Chunk = (ChunkType)Enum.Parse(typeof(ChunkType), value.Replace(' ', '_'));
                UpdateView();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Chunk)));
            }
        }
        public ChunkType Chunk { get; private set; }
        [UIValue(nameof(IncrementVal))] private int IncrementVal
        {
            get { if (int.TryParse(Text2, out int outp)) return outp; else return 50; }
            set => Text2 = "" + value;
        }
        [UIValue(nameof(Text))] private string Text { get => _Text; set { _Text = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text))); } }
        [UIValue(nameof(Text2))] private string Text2 { get => _Text2; set { _Text2 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text2))); } }

        [UIValue(nameof(ShowTextComp))] private bool ShowTextComp;
        [UIValue(nameof(ShowText2Comp))] private bool ShowText2Comp;
        [UIValue(nameof(ShowIncrement))] private bool ShowIncrement;
        [UIValue(nameof(ShowChoice))] private bool ShowChoice;
        [UIValue(nameof(TextCompLabel))] private string TextCompLabel = "Input Text";
        [UIValue(nameof(ChoiceText))] private string ChoiceText = "Choose Token";
        [UIValue(nameof(IncrementText))] private string IncrementText = "Capture ID";

        [UIComponent(nameof(TextComp))] private TextMeshProUGUI TextCompLabelObj;
        [UIComponent(nameof(TextComp))] private StringSetting TextComp;
        [UIComponent(nameof(Text2Comp))] private StringSetting Text2Comp;
        [UIComponent(nameof(Incrementer))] private IncrementSetting Incrementer;
        [UIComponent(nameof(Incrementer))] private TextMeshProUGUI IncrementerText;
        [UIObject(nameof(ChoiceContainer))] private GameObject ChoiceContainer;
        [UIComponent(nameof(Choicer))] private DropDownListSetting Choicer;

        #endregion
        #region Variables
        public event PropertyChangedEventHandler PropertyChanged;
        public FormatListInfo AboveInfo { get => _AboveInfo; set { _AboveInfo = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AboveInfo))); } }
        public FormatListInfo _AboveInfo = null; //This is so that parameters can find their parent.
        private string[] TokenParams; //This will be accessed by other instances other this class.
        private string _Text, _Text2;
        public FormatListInfo Child { get; private set; } = null;
        public bool HasChild => Child != null;
        #endregion
        #region Inits
        private FormatListInfo(ChunkType ct = default, string text = "", string text2 = "", string[] tokenParams = null,
            bool textComp = false, bool text2Comp = false, bool increment = false, bool choice = false)
        {
            Chunk = ct;
            _Text = text;
            _Text2 = text2;
            TokenParams = tokenParams;
            PropertyChanged += DoSomethingOnPropertyChange;

            ShowTextComp = textComp;
            ShowText2Comp = text2Comp;
            ShowIncrement = increment;
            ShowChoice = choice;
        }
        private FormatListInfo(bool isTokenValue, string name, string[] tokenParams) :
            this(
                  ct: isTokenValue ? Escaped_Token : Escaped_Character,
                  text: isTokenValue ? ConvertFromAlias(name) : name,
                  tokenParams: tokenParams,
                  choice: true
                  )
        {
            ChoiceOptions = isTokenValue ? AliasConverter.Keys.Cast<object>().ToList() : SPECIAL_CHARS.Select(c => "" + c).Cast<object>().ToList();
            if (!isTokenValue) ChoiceText = "Choose Escaped Character";
        }
        private FormatListInfo(bool isOpen, string token, ChunkType ct) :
            this(
                ct: ct,
                text: ct == Group_Open ? ConvertFromAlias(token) : token,
                text2: token,
                increment: ct == Capture_Open,
                choice: ct == Group_Open
                )
        {
            if (ct == Group_Open) ChoiceOptions = AliasConverter.Keys.Cast<object>().ToList();
        }
        private FormatListInfo(bool isOpen, string richTextKey, string richTextValue) :
            this(
                ct: isOpen ? Rich_Text_Open : Rich_Text_Close,
                text: RICH_SHORTHANDS.TryGetValue(richTextKey, out string val) ? val : richTextKey,
                text2: richTextValue,
                textComp: isOpen,
                text2Comp: isOpen
                )
        {
            if (isOpen) TextCompLabel = "Enter Key";
        }
        private FormatListInfo(string text, bool isInsertSelf) :
            this(
                ct: isInsertSelf ? Insert_Group_Value : Regular_Text,
                text: text,
                textComp: !isInsertSelf
                )
        {}
        private FormatListInfo(string name, int index) :
            this(
                ct: Parameter,
                text: name, //Don't need to worry about the alias here because the function using this initializer takes care of it.
                text2: $"{index + 1}", //plus one because the average person doesn't use zero indexing and this number will be displayed.
                increment: true,
                choice: true
                )
        {
            ChoiceOptions = AliasConverter.Keys.Cast<object>().ToList();
            IncrementText = "Parameter Index";
        }
        #endregion
        #region Static Functions
        #region Inits
        public static List<FormatListInfo> InitAllFromChunks((Match, ChunkType)[] chunks)
        {
            List<FormatListInfo> outp = new List<FormatListInfo>();
            foreach ((Match, ChunkType) chunk in chunks)
            {
                outp.Add(InitFromGivenChunk(chunk, out FormatListInfo[] extras));
                if (extras != null) outp.AddRange(extras);
            }
            for (int i = 0; i < outp.Count; i++) if (i != 0) outp[i].AboveInfo = outp[i - 1];
            return outp;
        }
        public static FormatListInfo InitFromGivenChunk((Match, ChunkType) chunk, out FormatListInfo[] extraInfo)
        {
            extraInfo = null;
            //Plugin.Log.Info(chunk.ToString());
            switch (chunk.Item2)
            {
                case Regular_Text: return new FormatListInfo(chunk.Item1.Value, false);
                case Escaped_Character: return new FormatListInfo(false, chunk.Item1.Value[1]+"", null as string[]);
                case Escaped_Token:
                    if (!chunk.Item1.Groups["Params"].Success) return new FormatListInfo(true, chunk.Item1.Groups["Token"].Value.Substring(1), null as string[]);
                    string[] theParams = chunk.Item1.Groups["Params"].Value.Split(DELIMITER).Select(s => ConvertFromAlias(s)).ToArray();
                    extraInfo = new FormatListInfo[theParams.Length];
                    for (int i = 0; i < theParams.Length; i++)
                        extraInfo[i] = new FormatListInfo(theParams[i], i);
                    return new FormatListInfo(true, chunk.Item1.Groups["Token"].Value.Substring(1), theParams);
                case Capture_Open:
                case Capture_Close:
                    return new FormatListInfo(chunk.Item2 == Capture_Open, chunk.Item1.Value.Substring(1), chunk.Item2);
                case Group_Open:
                case Group_Close:
                    return new FormatListInfo(chunk.Item2 == Group_Open, chunk.Item1.Value.Substring(1), chunk.Item2);
                case Rich_Text_Open:
                    return new FormatListInfo(true, chunk.Item1.Groups["Key"].Value, chunk.Item1.Groups["Value"].Value);
                case Rich_Text_Close: return new FormatListInfo(false, "", "");
                case Insert_Group_Value: return new FormatListInfo("" + INSERT_SELF, true);
                default: return null;
            }
        }
        internal static void InitStaticActions(List<object> parentList, Action updateTable, Action updatePreview)
        {
            ParentList = parentList;
            UpdateTable = updateTable;
            UpdatePreview = updatePreview;
        }
        #endregion
        #region Misc
        private static string ConvertFromAlias(string str)
        {
            if (str[0] == ALIAS) return str.Substring(1, str.Length - 2);
            if (str.Length > 1) return str;
            return GetKeyFromDictionary(AliasConverter, str[0]);
        }
        public static (Match, ChunkType)[] ChunkItAll(string format)
        {
            MatchCollection mc = CollectiveRegex.Matches(format);
            (Match, ChunkType)[] outp = new (Match, ChunkType)[mc.Count];
            for (int i = 0; i < outp.Length; i++)
                //outp[i] = (mc[i], (ChunkType)Enum.Parse(typeof(ChunkType), mc[i].Groups.First(g => g.Success && g.Name.Contains("_")).Name)); // 1.37.0 and above
                outp[i] = (mc[i], (ChunkType)Enum.Parse(typeof(ChunkType), mc[i].Groups.OfType<Group>().First(g => g.Success && g.Name.Contains("_")).Name)); // 1.34.2 and below
            return outp;
        }
        private static Regex GetRegexForAllChunks()
        {
            string outp = "\\G(?:";
            List<ChunkType> arr = new List<ChunkType>() { Insert_Group_Value, Group_Open };
            arr.AddRange((Enum.GetValues(typeof(ChunkType)) as IEnumerable<ChunkType>).Where(ct => !(arr.Contains(ct) || ct.Equals(Parameter))));
            foreach (ChunkType ct in arr)
                outp += $"(?<{ct}>{GetRegexForChunk(ct)})|";
            //Plugin.Log.Info(outp.Substring(0, outp.Length - 1) + ")");
            return new Regex(outp.Substring(0, outp.Length - 1) + ")");
            // \G(?:(?<Insert_Group_Value>\$)|(?<Group_Open>(?<Alias>\['[^']+')|(?<Token>\[[^']))|(?<Regular_Text>[^$&*[\]<>]+)|(?<Escaped_Character>&[&*[\]<>])|(?<Escaped_Token>(?<Token>&.|&'[^']+')\((?<Params>[^\)]+)\)|(?<Token>&'[^']+'|&.))|(?<Capture_Open><\d+)|(?<Capture_Close>>)|(?<Group_Close>])|(?<Rich_Text_Open>\*(?<Key>[^,\*]+),(?<Value>[^\*]+)\*|<(?<Key>[^=]+)=(?<Value>[^>]+)>)|(?<Rich_Text_Close>\*|<[^>]+>))
        }
        internal static string GetRegexForChunk(ChunkType ct)
        {
            switch (ct)
            {
                case Regular_Text: return RegularTextRegex;//[^$&*[\]<>]+
                case Escaped_Character: return EscapedCharRegex;//&[&*[\]<>]
                case Escaped_Token: return AliasRegex;//(?<Token>&.|&'[^']+')\((?<Params>[^\)]+)\)|(?<Token>&'[^']+'|&.)
                case Capture_Open: return $"{Regex.Escape(CAPTURE_OPEN+"")}\\d+"; //<\d+
                case Capture_Close: return Regex.Escape(CAPTURE_CLOSE + ""); //>
                case Group_Open: return string.Format("(?<Alias>{0}{1}[^{1}]+{1})|(?<Token>{0}[^{1}])", Regex.Escape($"{GROUP_OPEN}"), Regex.Escape($"{ALIAS}")); //(?<Alias>\['[^']+')|(?<Token>\[[^'])
                case Group_Close: return Regex.Escape(GROUP_CLOSE + ""); //\]
                case Rich_Text_Open: return string.Format("{0}(?<Key>[^{1}{0}]+){1}(?<Value>[^{0}]+){0}|<(?<Key>[^=]+)=(?<Value>[^>]+)>", Regex.Escape(RICH_SHORT + ""), Regex.Escape(DELIMITER + "")); //\*(?<Key>[^,]+),(?<Value>[^\*]+)\*|<(?<Key>[^=]+)=(?<Value>[^>]+)>
                case Rich_Text_Close: return $"{Regex.Escape(RICH_SHORT+"")}|<[^>]+>"; //\*|<[^>]+>
                case Insert_Group_Value: return Regex.Escape(INSERT_SELF+""); //$
                default: return "";
            }
        }
        public static string ColorFormat(string format)
        {
            var arr = ChunkItAll(format);
            format = "";
            foreach (var chunk in arr)
                    format += ColorFormatChunk(chunk);
            return format;
        }
        public static string ColorFormatChunk((Match, ChunkType) chunk) => ColorFormatChunk(chunk.Item1.Value, chunk.Item2);
        public static string ColorFormatChunk(string Text, ChunkType ct)
        {
            PluginConfig pc = PluginConfig.Instance;
            string outp;
            string ColorEscapeToken(string token) => token[0] == ALIAS ? 
                string.Format(ColorDefaultFormatToColor("'cAlias0'"), ConvertFromAlias(token)) :
                string.Format(ColorFormatToColor("'cAlias0'"), GetKeyFromDictionary(AliasConverter, token[0]));
            switch (ct)
            {
                case Regular_Text:
                    return "<color=white>" + Text.Replace("\\n", $"{ConvertColorToMarkup(pc.SpecialCharacterColor)}\\n</color>");
                case Escaped_Character:
                    return $"{ColorSpecialChar(ESCAPE_CHAR)}{ConvertColorToMarkup(pc.AliasColor)}{Text[1]}";
                case Escaped_Token:
                    string[] hold1 = null;
                    Text = Text.Substring(1);
                    if (Text.Contains(PARAM_OPEN))
                    {
                        hold1 = Text.Split(PARAM_OPEN);
                        Text = hold1[0];
                    }
                    else return ColorSpecialChar(ESCAPE_CHAR) + ColorEscapeToken(Text);
                    outp = ColorSpecialChar(ESCAPE_CHAR) + ColorEscapeToken(Text) + ColorSpecialChar(PARAM_OPEN);
                    hold1 = hold1[1].Substring(0, hold1[1].Length - 1).Split(DELIMITER);
                    for (int i = 0; i < hold1.Length; i++)
                        outp += $"{(i != 0 ? ColorSpecialChar(DELIMITER) : "")}{ColorEscapeToken(hold1[i])}";
                    outp += ColorSpecialChar(PARAM_CLOSE);
                    return outp;
                case Capture_Open:
                    return $"{ColorSpecialChar(CAPTURE_OPEN)}{ConvertColorToMarkup(pc.CaptureIdColor)}{Text.Substring(1)}";
                case Capture_Close:
                    return ColorSpecialChar(CAPTURE_CLOSE);
                case Group_Open:
                    return $"{ColorSpecialChar(GROUP_OPEN)}{ColorEscapeToken(Text.Substring(1))}";
                case Group_Close:
                    return ColorSpecialChar(GROUP_CLOSE);
                case Rich_Text_Open:
                    int index;
                    if (Text[0] == '<')
                    {
                        index = Text.IndexOf('=');
                        return $"{ConvertColorToMarkup(pc.ShorthandColor)}<{ConvertColorToMarkup(pc.SpecialCharacterColor)}{Text.Substring(1, index - 1)}" + 
                            $"{ConvertColorToMarkup(pc.DelimeterColor)}={ConvertColorToMarkup(pc.ParamVarColor)}{Text.Substring(index)}{ConvertColorToMarkup(pc.ShorthandColor)}>";
                    }
                    index = Text.IndexOf(DELIMITER);
                    outp = $"{ColorSpecialChar(RICH_SHORT)}{{0}}{ColorSpecialChar(DELIMITER)}{ConvertColorToMarkup(pc.ParamVarColor)}{Text.Substring(index + 1, Text.Length - index - 2)}{ColorSpecialChar(RICH_SHORT)}";
                    Text = Text.Substring(1, index - 1);
                    return RICH_SHORTHANDS.ContainsValue(Text) ? string.Format(outp, RICH_SHORTHANDS.First(p => p.Value.Equals(Text)).Key) : string.Format(outp, Text);
                case Rich_Text_Close:
                    return ColorSpecialChar(RICH_SHORT);
                case Insert_Group_Value:
                    return ColorSpecialChar(INSERT_SELF);
                default: return Text;
            }
        }
        #endregion
        #endregion
        #region UI Functions
        [UIAction(nameof(Centerer))] private string Centerer(string strIn) => $"<align=\"center\">{strIn}";
        [UIAction(nameof(MoveChunkUp))] private void MoveChunkUp()
        {
            int index = ParentList.IndexOf(this);
            if (index > 0)
            {
                FormatListInfo other = ParentList[index - 1] as FormatListInfo;
                ParentList[index] = other;
                ParentList[index - 1] = this;
                AboveInfo = other.AboveInfo;
                other.AboveInfo = this;
                UpdateTable();
            }
        }
        [UIAction(nameof(MoveChunkDown))] private void MoveChunkDown()
        {
            int index = ParentList.IndexOf(this);
            if (index < ParentList.Count - 1)
            {
                FormatListInfo other = ParentList[index + 1] as FormatListInfo;
                ParentList[index] = other;
                ParentList[index + 1] = this;
                other.AboveInfo = AboveInfo;
                AboveInfo = other;
                UpdateTable();
            }
        }
        [UIAction(nameof(RemoveChunk))] private void RemoveChunk()
        {
            int index = ParentList.IndexOf(this);
            if (index < ParentList.Count - 1)
                (ParentList[index + 1] as FormatListInfo).AboveInfo = index > 0 ? (ParentList[index - 1] as FormatListInfo) : null;
            TellParentTheyHaveAChild(true);
            ParentList.Remove(this);
            UpdateTable();
        }
        #endregion
        #region Functions
        public void SetParentToken() //For ChunkType.Parameter
        {
            if (Chunk != Parameter) return;
            if (!int.TryParse(_Text2, out int index)) { index = 1; _Text2 = "1"; }
            FormatListInfo parent = this;
            while (parent != null && parent.Chunk == Parameter) parent = parent.AboveInfo;
            index--;
            if (parent == null || parent.TokenParams == null || parent.TokenParams.Length >= index) return;
            parent.TokenParams[index] = _Text;
        }
        public void TellParentTheyHaveAChild(bool childIsDead = false)
        {
            if (((Capture_Close | Group_Close | Rich_Text_Close | Parameter) & Chunk) == 0) return;
            FormatListInfo parent = AboveInfo;
            ChunkType open = Chunk == Parameter ? Escaped_Token : (ChunkType)((int)Chunk / 2), close = Chunk == Parameter ? Regular_Text : Chunk;
            while (parent != null && ((open | close) & parent.Chunk) == 0) parent = parent.AboveInfo;
            if (parent != null && parent.Chunk == open) parent.Child = childIsDead ? null : this;
        }
        private void DoSomethingOnPropertyChange(object sender, PropertyChangedEventArgs e) 
        {
            if (e.PropertyName.Equals(nameof(Text))) SetParentToken();
            if (e.PropertyName.Equals(nameof(AboveInfo))) TellParentTheyHaveAChild();
            else UpdatePreview?.Invoke(); //All other properties, when changed, affect the preview.
        }
        private void UpdateView()
        {
            switch (Chunk)
            {
                case Rich_Text_Close:
                case Rich_Text_Open:
                    TextCompLabelObj.text = "Enter Key";
                    break;
                case Regular_Text:
                    TextCompLabelObj.text = "Input Text";
                    break;
                case Capture_Open:
                    TextCompLabelObj.text = "Enter Capture ID";
                    break;
                case Parameter:
                    IncrementerText.text = "Parameter Index";
                    ChoiceOptions = AliasConverter.Keys.Cast<object>().ToList();
                    break;
                case Group_Open:
                case Escaped_Token:
                    ChoiceOptions = AliasConverter.Keys.Cast<object>().ToList();
                    break;
                case Escaped_Character:
                    ChoiceOptions = SPECIAL_CHARS.Select(c => "" + c).Cast<object>().ToList();
                    break;
            }
            if (((Escaped_Token | Escaped_Character | Group_Open | Parameter) & Chunk) > 0)
            {
#if NEW_VERSION
                Choicer.Values = ChoiceOptions;
                if (!Choicer.Values.Contains(Text)) { Choicer.Value = Choicer.Values[0]; Text = Choicer.Values[0] as string; }  //1.37.0 and above */
#else
                Choicer.values = ChoiceOptions;
                if (!Choicer.values.Contains(Text)) { Choicer.Value = Choicer.values[0]; Text = Choicer.values[0] as string; }  //1.34.2 and below */
#endif
                else Choicer.Value = Text;
                Choicer.UpdateChoices();
                ChoiceContainer.SetActive(true);
            } else ChoiceContainer.SetActive(false);
            TextComp.gameObject.SetActive(Chunk == Regular_Text || Chunk == Rich_Text_Open);
            Text2Comp.gameObject.SetActive(Rich_Text_Open == Chunk);
            Incrementer.gameObject.SetActive(((Capture_Open | Parameter) & Chunk) > 0);
            if (((Capture_Close | Group_Close | Rich_Text_Close | Parameter) & Chunk) != 0)
                TellParentTheyHaveAChild();
        }
        public bool Updatable()
        {
            FormatListInfo parent = AboveInfo;
            if (((Capture_Open | Group_Open | Rich_Text_Open | Escaped_Token) & Chunk) != 0) return HasChild || (Chunk == Escaped_Token && TokenParams == null);
            if (((Capture_Close | Group_Close | Rich_Text_Close | Parameter) & Chunk) == 0) return true;
            ChunkType open = (ChunkType)((int)Chunk / 2);
            if (Chunk == Group_Close) open |= Capture_Open | Capture_Close;
            ChunkType close = Chunk == Parameter ? Regular_Text : Chunk;
            while (parent != null && ((open | close) & parent.Chunk) == 0) parent = parent.AboveInfo;
            return parent != null && (parent.Chunk & open) != 0;
        }
        public string GetDisplay()
        {
            string outp;
            switch (Chunk)
            {
                case Regular_Text:
                    return Text;
                case Escaped_Character:
                    return $"{ESCAPE_CHAR}{Text}";
                case Escaped_Token:
                    outp = $"{ESCAPE_CHAR}{ALIAS}{Text}{ALIAS}";
                    if (TokenParams == null) return outp;
                    outp += PARAM_OPEN;
                    for (int i = 0; i < TokenParams.Length; i++) outp += (i != 0 ? "," : "") + $"{ALIAS}{TokenParams[i]}{ALIAS}";
                    return outp + PARAM_CLOSE;
                case Capture_Open:
                    return $"{CAPTURE_OPEN}{Text}";
                case Capture_Close:
                    return "" + CAPTURE_CLOSE;
                case Group_Open:
                    return $"{GROUP_OPEN}{ALIAS}{Text}{ALIAS}";
                case Group_Close:
                    return "" + GROUP_CLOSE;
                case Rich_Text_Open:
                    outp = $"{RICH_SHORT}{{0}}{DELIMITER}{Text2}{RICH_SHORT}";
                    return RICH_SHORTHANDS.ContainsValue(Text) ? string.Format(outp, RICH_SHORTHANDS.First(p => p.Value.Equals(Text)).Key) : string.Format(outp, Text);
                case Rich_Text_Close:
                    return "" + RICH_SHORT;
                case Insert_Group_Value:
                    return "" + INSERT_SELF;
                default: return "";
            }
        }
        public string GetColorDisplay() => ColorFormatChunk(GetDisplay(), Chunk);
        /*{
            PluginConfig pc = PluginConfig.Instance;
            string outp;
            switch (Chunk)
            {
                case Regular_Text:
                    return "<color=white>" + Text.Replace("\\n", $"{ConvertColorToMarkup(pc.SpecialCharacterColor)}\\n</color>");
                case Escaped_Character:
                    return $"{ColorSpecialChar(ESCAPE_CHAR)}{ConvertColorToMarkup(pc.AliasColor)}{Text}";
                case Escaped_Token:
                    outp = $"{ColorSpecialChar(ESCAPE_CHAR)}{ColorSpecialChar(ALIAS)}{ConvertColorToMarkup(pc.AliasColor)}{Text}{ColorSpecialChar(ALIAS)}";
                    if (TokenParams != null)
                    {
                        outp += ColorSpecialChar(PARAM_OPEN);
                        for (int i=0;i<TokenParams.Length;i++) 
                            outp += $"{(i != 0 ? ColorSpecialChar(DELIMITER) : "")}{ColorSpecialChar(ALIAS)}{ConvertColorToMarkup(pc.AliasColor)}{TokenParams[i]}{ColorSpecialChar(ALIAS)}"; 
                        outp += ColorSpecialChar(PARAM_CLOSE);
                    }
                    return outp;
                case Capture_Open:
                    return $"{ColorSpecialChar(CAPTURE_OPEN)}{ConvertColorToMarkup(pc.CaptureIdColor)}{Text2}";
                case Capture_Close:
                    return ColorSpecialChar(CAPTURE_CLOSE);
                case Group_Open:
                    return $"{ColorSpecialChar(GROUP_OPEN)}{ColorSpecialChar(ALIAS)}{ConvertColorToMarkup(pc.AliasColor)}{Text}{ColorSpecialChar(ALIAS)}";
                case Group_Close:
                    return ColorSpecialChar(GROUP_CLOSE);
                case Rich_Text_Open:
                    outp = $"{ColorSpecialChar(RICH_SHORT)}{ConvertColorToMarkup(pc.SpecialCharacterColor)}{{0}}{ColorSpecialChar(DELIMITER)}{ConvertColorToMarkup(pc.ParamVarColor)}{Text2}{ColorSpecialChar(RICH_SHORT)}";
                    return RICH_SHORTHANDS.ContainsValue(Text) ? string.Format(outp, RICH_SHORTHANDS.First(p => p.Value.Equals(Text)).Key) : string.Format(outp, Text);
                case Rich_Text_Close:
                    return ColorSpecialChar(RICH_SHORT);
                case Insert_Group_Value:
                    return ColorSpecialChar(INSERT_SELF);
                default: return GetDisplay();
            }
        }//*/
#endregion
        #region Overrides
        public override string ToString()
        {
            return $"{{Chunk: {Chunk}, Text: {Text}, Secondary Text: {Text2}, Token Params: [{(TokenParams != null ? string.Join(", ", TokenParams) : "")}]}}";
        }
        #endregion
        #region Inner Classes
        public enum ChunkType //Not using Flags attribute because this isn't a real bitmask. This is done simply to parse it easier.
        {
            Regular_Text = 0,
            Escaped_Character = 1, 
            Escaped_Token = 2,
            Parameter = 4,
            Capture_Open = 8, 
            Capture_Close = 16, 
            Group_Open = 32,
            Group_Close = 64,
            Rich_Text_Open = 128,
            Rich_Text_Close = 256,
            Insert_Group_Value = 512
        }
        #endregion
    }
}

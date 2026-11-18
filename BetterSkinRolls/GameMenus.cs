using System;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BetterSkinRolls
{
    /// <summary>
    /// The mod's settings inside the game's Options, built by cloning the game's widgets so they look the same:
    /// - Options gets a fifth tab, "Skin Rolls", next to Gameplay/Graphics/Audio/Language: a copy of the Gameplay panel
    ///   with three toggle rows (the personal settings), each with a hint line under it.
    /// Layout facts (game 2026-11): OptionsScreen/UpperButtonHolder (HorizontalLayoutGroup, 4 × 250 px tabs in 1030 px)
    /// holds the tab buttons, whose persistent onClick calls SetActive on every "...Display (To toggle)" panel;
    /// panels are 1030 × 700 with a Layout (VerticalLayoutGroup) of 1030 × 60 rows (title TMP 412 px + Toggle 40 px).
    /// Built on the first PauseManager.OnPause (it runs at scene start too) and rebuilt if the scene reloads.
    /// </summary>
    [HarmonyPatch(typeof(PauseManager), "OnPause")]
    internal static class GameMenus
    {
        private const string TabLabel = "Skin Rolls";
        private const string PanelTitle = "Better Skin Rolls";

        private static readonly AccessTools.FieldRef<PauseManager, GameObject> OptionsScreen =
            AccessTools.FieldRefAccess<PauseManager, GameObject>("_optionsScreen");
        private static readonly System.Reflection.FieldInfo Instance = AccessTools.Field(typeof(PauseManager), "_instance");

        private static GameObject _panel;
        private static Toggle _self, _fallback, _strike;
        private static TextMeshProUGUI _selfTitle, _selfHint, _fallbackTitle, _fallbackHint, _strikeTitle, _strikeHint;
        private static bool _optionsFailed, _languageHooked;

        private static void Postfix()
        {
            var manager = Instance?.GetValue(null) as PauseManager;
            if (!manager)
                return;
            if (!_panel && !_optionsFailed)
            {
                try { BuildOptionsTab(manager); }
                catch (Exception e) { _optionsFailed = true; Plugin.Log.LogError($"Could not add the Options tab: {e}"); }
            }
            if (!_languageHooked)
            {
                _languageHooked = true;
                Strings.LanguageChanged += Refresh;
            }
            Refresh();
        }

        // ---------- Options tab ----------

        private static void BuildOptionsTab(PauseManager manager)
        {
            var options = OptionsScreen(manager)?.transform;
            if (!options)
                throw new InvalidOperationException("PauseManager._optionsScreen not found, the game was probably updated");
            var tabs = options.Find("UpperButtonHolder");
            var gameplay = FindChild(options, "GameplayDisplay");
            var gameplayTab = tabs ? FindChild(tabs, "GameplayButton") : null;
            if (!tabs || !gameplay || !gameplayTab)
                throw new InvalidOperationException("Options tabs or Gameplay panel not found, the game was probably updated");

            // Panel: a copy of Gameplay with only its header and one toggle row kept as the row template.
            var panel = CloneInactive(gameplay.gameObject, "BetterSkinRollsDisplay (To toggle)");
            var layout = panel.transform.Find("Layout");
            var header = layout ? layout.GetChild(0).GetComponent<TextMeshProUGUI>() : null;
            Transform rowTemplate = null;
            if (layout)
                foreach (Transform child in layout)
                    if (child.GetComponentInChildren<Toggle>(true) && (!rowTemplate || child.name.StartsWith("Nametags")))
                        rowTemplate = child;
            if (!header || !rowTemplate)
                throw new InvalidOperationException("Gameplay panel layout not as expected");
            for (int i = layout.childCount - 1; i >= 0; i--)
            {
                var child = layout.GetChild(i);
                if (child != header.transform && child != rowTemplate)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            header.text = PanelTitle;

            _self = AddRow(layout, rowTemplate, Plugin.Enabled, out _selfTitle);
            _selfHint = AddHint(layout, rowTemplate, layout.childCount);
            _fallback = AddRow(layout, rowTemplate, Plugin.LocalFallback, out _fallbackTitle);
            _fallbackHint = AddHint(layout, rowTemplate, layout.childCount);
            _strike = AddRow(layout, rowTemplate, Plugin.StrikeCompletedPrizes, out _strikeTitle);
            _strikeHint = AddHint(layout, rowTemplate, layout.childCount);
            UnityEngine.Object.DestroyImmediate(rowTemplate.gameObject);

            Place(panel, options, gameplay.GetSiblingIndex() + 1);
            panel.SetActive(false);
            _panel = panel;

            // Tab: a copy of the Gameplay tab that shows our panel and hides every other one.
            var tab = CloneInactive(gameplayTab.gameObject, "BetterSkinRollsButton");
            var button = tab.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => ShowPanel(options));
            var label = tab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label)
                label.text = TabLabel;
            Place(tab, tabs, tabs.childCount);
            tab.SetActive(true);

            // The game's own tab buttons (upper and lower rows) switch panels through persistent calls that don't know
            // ours: hide it whenever another one is clicked.
            foreach (var other in options.GetComponentsInChildren<Button>(true))
                if (other != button && other.transform.parent && other.transform.parent.name.EndsWith("ButtonHolder")
                    && other.name != "BackButton")
                    other.onClick.AddListener(HidePanel);

            FitTabs(tabs);
        }

        private static void ShowPanel(Transform options)
        {
            if (!_panel)
                return;
            foreach (Transform child in options)
                if (child.gameObject != _panel && child.name.Contains("Display"))
                    child.gameObject.SetActive(false);
            _panel.SetActive(true);
            Refresh();
        }

        private static void HidePanel()
        {
            if (_panel)
                _panel.SetActive(false);
        }

        /// <summary>Five tabs in the width four used to take; labels shrink to fit instead of overflowing.</summary>
        private static void FitTabs(Transform tabs)
        {
            var rect = (RectTransform)tabs;
            var group = tabs.GetComponent<HorizontalLayoutGroup>();
            int count = 0;
            foreach (Transform child in tabs)
                if (child.gameObject.activeSelf)
                    count++;
            if (count == 0)
                return;
            float spacing = group ? group.spacing : 10f;
            float width = (rect.rect.width - spacing * (count - 1)) / count;
            foreach (Transform child in tabs)
            {
                var r = (RectTransform)child;
                r.sizeDelta = new Vector2(width, r.sizeDelta.y);
                var element = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = width;
                element.minWidth = width;
                foreach (var text in child.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    var tr = text.rectTransform;
                    if (tr.anchorMin.x == tr.anchorMax.x && tr.sizeDelta.x > width - 16f)
                        tr.sizeDelta = new Vector2(width - 16f, tr.sizeDelta.y);
                    AutoSize(text, 0.55f);
                }
            }
        }

        // ---------- Rows ----------

        private static Toggle AddRow(Transform layout, Transform template, ConfigEntry<bool> entry, out TextMeshProUGUI title)
        {
            var row = UnityEngine.Object.Instantiate(template.gameObject, layout, false);
            row.name = "BetterSkinRolls_" + entry.Definition.Key.Replace(" ", "");
            row.SetActive(true);
            return Wire(row, entry, out title);
        }

        private static Toggle Wire(GameObject row, ConfigEntry<bool> entry, out TextMeshProUGUI title)
        {
            var toggle = row.GetComponentInChildren<Toggle>(true);
            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.SetIsOnWithoutNotify(entry.Value);
            toggle.onValueChanged.AddListener(new UnityAction<bool>(value => entry.Value = value));
            title = row.GetComponentInChildren<TextMeshProUGUI>(true);
            if (title)
                AutoSize(title, 0.6f);
            return toggle;
        }

        /// <summary>
        /// A description line under a setting, built from a copy of a toggle row so it follows the game's margins:
        /// the row's title becomes the hint and is stretched from the title's left edge to the toggle's right edge,
        /// left-aligned, smaller, in the title's own color (white) slightly faded.
        /// </summary>
        private static TextMeshProUGUI AddHint(Transform layout, Transform rowTemplate, int index)
        {
            var row = CloneInactive(rowTemplate.gameObject, "BetterSkinRolls_Hint");
            var rowRect = (RectTransform)row.transform;
            var toggle = row.GetComponentInChildren<Toggle>(true);
            TextMeshProUGUI text = null;
            foreach (Transform child in row.transform)
            {
                var tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp && !text)
                    text = tmp;
            }
            if (!text || !toggle)
                throw new InvalidOperationException("toggle row layout not as expected");

            float left = EdgeX((RectTransform)text.transform, false);
            float right = EdgeX((RectTransform)toggle.transform, true);
            foreach (Transform child in row.transform)
                if (child != text.transform)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);

            var tr = text.rectTransform;
            tr.anchorMin = new Vector2(0f, 0f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(0.5f, 0.5f);
            tr.offsetMin = new Vector2(left - rowRect.rect.xMin, 0f);
            tr.offsetMax = new Vector2(right - rowRect.rect.xMax, 0f);

            text.fontSize = 24;
            text.enableAutoSizing = true;
            text.fontSizeMax = 24;
            text.fontSizeMin = 16;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.TopLeft;
            var color = text.color;
            color.a = 0.8f;
            text.color = color;

            var element = row.GetComponent<LayoutElement>() ?? row.AddComponent<LayoutElement>();
            element.preferredHeight = 56;
            element.minHeight = 56;
            rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, 56f);

            Place(row, layout, index);
            row.SetActive(true);
            return text;
        }

        /// <summary>A direct child's left or right edge in its parent's local space (no rotation or scale).</summary>
        private static float EdgeX(RectTransform child, bool right)
        {
            var r = child.rect;
            return child.localPosition.x + (right ? r.xMax : r.xMin) * child.localScale.x;
        }

        public static void Refresh()
        {
            try
            {
                SetTexts();
                if (_self) _self.SetIsOnWithoutNotify(Plugin.Enabled.Value);
                if (_fallback) _fallback.SetIsOnWithoutNotify(Plugin.LocalFallback.Value);
                if (_strike) _strike.SetIsOnWithoutNotify(Plugin.StrikeCompletedPrizes.Value);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not update the menus: {e.Message}");
            }
        }

        private static void SetTexts()
        {
            if (_selfTitle) _selfTitle.text = Strings.Get(Strings.SelfToggle);
            if (_selfHint) _selfHint.text = Strings.Get(Strings.SelfHint);
            if (_fallbackTitle) _fallbackTitle.text = Strings.Get(Strings.FallbackToggle);
            if (_fallbackHint) _fallbackHint.text = Strings.Get(Strings.FallbackHint);
            if (_strikeTitle) _strikeTitle.text = Strings.Get(Strings.StrikeToggle);
            if (_strikeHint) _strikeHint.text = Strings.Get(Strings.StrikeHint);
        }

        // ---------- Helpers ----------

        private static Transform FindChild(Transform parent, string prefix)
        {
            foreach (Transform child in parent)
                if (child.name.StartsWith(prefix))
                    return child;
            return null;
        }

        /// <summary>
        /// Instantiate under an inactive holder so the copy's OnEnable never runs, then strip the game's
        /// localization components (they would put the original text back) before it's ever enabled.
        /// </summary>
        private static GameObject CloneInactive(GameObject template, string name)
        {
            var holder = new GameObject("BetterSkinRolls_Holder");
            holder.SetActive(false);
            var clone = UnityEngine.Object.Instantiate(template, holder.transform, false);
            clone.name = name;
            foreach (var c in clone.GetComponentsInChildren<Component>(true))
                if (c != null && c.GetType().Name.Contains("Localize"))
                    UnityEngine.Object.DestroyImmediate(c);
            clone.SetActive(false);
            clone.transform.SetParent(null, false);
            UnityEngine.Object.Destroy(holder);
            return clone;
        }

        private static void Place(GameObject go, Transform parent, int index)
        {
            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(Mathf.Clamp(index, 0, parent.childCount - 1));
        }

        private static void AutoSize(TextMeshProUGUI text, float minRatio)
        {
            float size = text.enableAutoSizing ? text.fontSizeMax : text.fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = Mathf.Max(12f, size * minRatio);
        }
    }
}

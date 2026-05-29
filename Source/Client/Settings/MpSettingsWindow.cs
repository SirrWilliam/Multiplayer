using Multiplayer.Client.Util;
using Multiplayer.Common;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using Random = System.Random;

namespace Multiplayer.Client;

public static class MpSettingsWindow
{
    //
    private static Vector2 _scrollPosition = Vector2.zero;
    private static SettingsTabs _currentTab = SettingsTabs.General;
    private static string _selectedTitle = null;
    private static string _selectedDesc = null;
    private static Texture2D _selectedPreview = null;   // null = placeholder
    private static float _lastHoveredRow = -1f;

    //Scroll
    private static Vector2 _scrollPos = Vector2.zero;
    private static float _totalContentH = 0f;

    //Layout Constants
    private const float TitleHeight = 36f;
    private const float ButtonHeight = 36f;
    private const float RowHeight = 32f;
    private const float SectionGap = 2f;
    private const float SectionLabelHeight = 20f;

    //
    const string UsernameField = "UsernameField";

    //Colors
    private static Color Gray(float t, float a = 1f) => new Color(t, t, t, a);

    private static readonly Color ColorSection = Gray(0.55f);
    private static readonly Color ColorHover = Gray(1f, 0.07f);
    private static readonly Color ColorSelected = new Color(0.9f, 0.7f, 0.2f, 0.18f);
    private static readonly Color ColorPanelBg = Gray(0.13f, 0.95f);
    private static readonly Color ColorDivider = Gray(0.35f, 0.60f);

    private enum SettingsTabs
    {
        General,
        Color,
    }

    public static void DoSettingsWindowContents(MpSettings settings, Rect inRect)
    {
        using var _ = MpStyle.Set(GameFont.Small);

        var tabs = new List<TabRecord>
        {
            new($"MpSettingsPage{SettingsTabs.General}".Translate(), () => _currentTab = SettingsTabs.General, _currentTab == SettingsTabs.General),
            new($"MpSettingsPage{SettingsTabs.Color}".Translate(), () => _currentTab = SettingsTabs.Color, _currentTab == SettingsTabs.Color),
        };
        inRect.yMin += 30f;

        TabDrawer.DrawTabs(inRect, tabs);

        GUI.BeginGroup(new Rect(0, inRect.yMin, inRect.width, inRect.height));
        {
            Rect groupRect = inRect.AtZero();
            switch (_currentTab)
            {
                case SettingsTabs.General:
                    DrawGeneralSettings(settings, groupRect);
                    break;
                case SettingsTabs.Color:
                    //DrawFactionChooser(groupRect);
                    break;
            }
        }
        GUI.EndGroup();
    }

    #region Panels

    public static void DrawGeneralSettings(MpSettings settings, Rect inRect)
    {
        //Body
        float bodyY = inRect.y;
        float bodyH = inRect.height - TitleHeight - ButtonHeight - 8f;
        Rect bodyRect = new Rect(inRect.x, bodyY, inRect.width, bodyH);

        float leftW = bodyRect.width * 0.52f - 4f;
        float rightW = bodyRect.width - leftW - 8f;

        Rect leftRect = new Rect(bodyRect.x, bodyRect.y, leftW, bodyRect.height);
        Rect rightRect = new Rect(bodyRect.x + leftW + 8f, bodyRect.y, rightW, bodyRect.height);

        DrawLeftPanel(settings, leftRect);
        DrawRightPanel(rightRect);

        /*  
          const string buttonOff = "Off";

          using (MpStyle.Set(TextAnchor.MiddleCenter))
              if (listing.ButtonTextLabeled("MpPingLocButtonSetting".Translate(),
                      settings.sendPingButton != null ? $"Mouse {settings.sendPingButton - (int)KeyCode.Mouse0 + 1}" : buttonOff))
                  Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>(ButtonChooser(b => settings.sendPingButton = b))));

          using (MpStyle.Set(TextAnchor.MiddleCenter))
              if (listing.ButtonTextLabeled("MpJumpToPingButtonSetting".Translate(),
                      settings.jumpToPingButton != null ? $"Mouse {settings.jumpToPingButton - (int)KeyCode.Mouse0 + 1}" : buttonOff))
                  Find.WindowStack.Add(
                      new FloatMenu(new List<FloatMenuOption>(ButtonChooser(b => settings.jumpToPingButton = b))));

          listing.End();

          IEnumerable<FloatMenuOption> ButtonChooser(Action<KeyCode?> setter)
          {
              yield return new FloatMenuOption(buttonOff, () => { setter(null); });

              for (var btn = 0; btn < 5; btn++)
              {
                  var b = btn;
                  yield return new FloatMenuOption($"Mouse {b + 3}", () => { setter(KeyCode.Mouse2 + b); });
              }
          }*/
    }


    private static void DrawLeftPanel(MpSettings settings, Rect rect)
    {
        Widgets.DrawBoxSolidWithOutline(rect, ColorPanelBg, ColorLibrary.Grey, 1);
        Rect inner = rect.ContractedBy(6f);

        // Scroll view
        Rect viewRect = new Rect(0f, 0f, inner.width - 16f, _totalContentH > 0 ? _totalContentH : 9999f);
        Widgets.BeginScrollView(inner, ref _scrollPos, viewRect);

        float y = 0f;

        y = DrawSectionHeader(y, viewRect.width, "PLAYER");

        y = DrawTextRow(y, viewRect.width,
         "MpUsernameSetting".Translate(),
         ref settings.username,
         "MpUsernameSetting".Translate(),
         null,
         null,
         onChange: val =>
         {
             Multiplayer.username = val;
         },
         locked: true,
         validate: val => val.Length <= 15 && MultiplayerServer.UsernamePattern.IsMatch(val),
         controlName: UsernameField);

        y += SectionGap;
        y = DrawSectionHeader(y, viewRect.width, "SERVER");

        y = DrawIntRow(y, viewRect.width,
  "MpAutosaveSlots".Translate(),
  ref settings.autosaveSlots,
  "MpAutosaveSlots".Translate(), null, null,
  onChange: val =>
  {
      settings.autosaveSlots = val;
  },
  locked: false);

        y = DrawToggleRow(y, viewRect.width, "MpAutoAcceptSteam".Translate(),
          ref settings.autoAcceptSteam,
          "MpAutoAcceptSteam".Translate(),
          "MpAutoAcceptSteamDesc".Translate(),
          null, true);

        y = DrawToggleRow(y, viewRect.width, "MpAppendNameToAutosave".Translate(),
          ref settings.appendNameToAutosave,
          "MpAppendNameToAutosave".Translate(),
          null,
          null);

        y += SectionGap;
        y = DrawSectionHeader(y, viewRect.width, "USER INTERFACE");

        y = DrawToggleRow(y, viewRect.width, "MpShowPlayerCursors".Translate(),
          ref settings.showCursors,
          "MpShowPlayerCursors".Translate(),
          null,
          null);

        y = DrawToggleRow(y, viewRect.width, "MpPlayerCursorTransparency".Translate(),
          ref settings.transparentPlayerCursors,
          "MpPlayerCursorTransparency".Translate(),
          null,
          null);

        y = DrawToggleRow(y, viewRect.width, "MpTransparentChat".Translate(),
          ref settings.transparentChat,
          "MpTransparentChat".Translate(),
          "MpTransparentChatDesc".Translate(),
          MultiplayerStatic.MpTransparentChat);

        y = DrawToggleRow(y, viewRect.width, "MpHideOtherPlayersInColonistBar".Translate(),
          ref settings.hideOtherPlayersInColonistBar,
          "MpHideOtherPlayersInColonistBar".Translate(),
          null,
          null,
          onChange: newVal =>
          {
              if (Multiplayer.Client != null)
              {
                  Log.Warning("TEST");
                  Find.ColonistBar.MarkColonistsDirty();
                  Find.ColonistBar.CheckRecacheEntries();
              }
          });

        y = DrawToggleRow(y, viewRect.width, "MpShowModCompat".Translate(),
          ref settings.showModCompatibility,
          "MpShowModCompat".Translate(),
          "MpShowModCompatDesc".Translate(),
          null);

        y = DrawToggleRow(y, viewRect.width, "MpShowMainMenuAnimation".Translate(),
          ref settings.showMainMenuAnim,
          "MpShowMainMenuAnimation".Translate(),
          null,
          null);

        y += SectionGap;
        y = DrawSectionHeader(y, viewRect.width, "PING");

        y = DrawToggleRow(y, viewRect.width, "MpEnablePingsSetting".Translate(),
   ref settings.enablePings,
   "MpEnablePingsSetting".Translate(),
   null,
   null);

        y = DrawToggleRow(y, viewRect.width, "MpEnableCrossPlanetLayerPings".Translate(),
   ref settings.enableCrossPlanetLayerPings,
   "MpEnableCrossPlanetLayerPings".Translate(),
   "MpEnableCrossPlanetLayerPingsDesc".Translate(),
   null);

        y += SectionGap;
        y = DrawSectionHeader(y, viewRect.width, "DEBUG");

        y = DrawToggleRow(y, viewRect.width, "Show Debug Window",
ref settings.showDevInfo,
"Show Debug Window",
null,
null,
locked: !Prefs.DevMode);


        if (MpVersion.IsDebug && FileAssoc.IsSupported())
        {
            if (FileAssoc.IsRegistered())
            {
                y = DrawButtonRow(y, viewRect.width, "Remove File Associations", "REMOVE", "Remove File Associations", null, null,
locked: !Prefs.DevMode, onClick: () =>
{
    FileAssoc.Remove();
});
            }
            else
            {
                y = DrawButtonRow(y, viewRect.width, "Register File Associations", "REGISTER", "Register File Associations", null, null,
locked: !Prefs.DevMode, onClick: () =>
{
    FileAssoc.Register();
});
            }
        }

        y = DrawButtonRow(y, viewRect.width, "Generate Debug Info", "GENERATE", "Generate Debug Info", null, null,
        locked: false, onClick: () =>
        {
            try
            {
                DebugInfoFile.Generate();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to generate debug info: {e}");
            }
        });

        if (VersionChecker.IsContinuousRelease || VersionChecker.IsLocalBuild)
        {
            y = DrawToggleRow(y, viewRect.width, "MpIncludeReplayInDesync".Translate(),
   ref settings.includeReplayInDesync,
  "MpIncludeReplayInDesync".Translate(),
  null,
  null);
        }

#if DEBUG
        y = DrawButtonRow(y, viewRect.width, "Desync Tracing Mode", settings.desyncTracingMode.ToString().ToUpper(), "Desync Tracing Mode", null, null,
locked: !Prefs.DevMode, onClick: () =>
{
    settings.desyncTracingMode = settings.desyncTracingMode.Cycle();
});


#endif


        y = DrawIntRow(y, viewRect.width,
  "Desync Radius",
  ref settings.desyncTracesRadius,
  "Desync Radius", null, null,
  onChange: val =>
  {
      settings.desyncTracesRadius = val;
  },
locked: !Prefs.DevMode);

        y = DrawIntRow(y, viewRect.width,
  "Jitted Methods",
  ref settings.jittedMethodsInDesync,
    "Jitted Methods", null, null,
  onChange: val =>
  {
      settings.jittedMethodsInDesync = val;
  },
locked: !Prefs.DevMode);

        y += 8f;
        _totalContentH = y;

        Widgets.EndScrollView();
    }

    private static void DrawRightPanel(Rect rect)
    {
        Widgets.DrawBoxSolidWithOutline(rect, ColorPanelBg, ColorLibrary.Grey, 1);

        Rect inner = rect.ContractedBy(10f);

        if (_selectedTitle == null)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(1f, 1f, 1f, 0.3f);
            Widgets.Label(inner, "Önizleme için\nbir ayar seçin");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            return;
        }

        float y = inner.y;

        // Title
        Text.Font = GameFont.Small;
        GUI.color = new Color(0.85f, 0.85f, 0.85f);
        Widgets.Label(new Rect(inner.x, y, inner.width, 22f),
                      _selectedTitle.ToUpperInvariant());
        GUI.color = Color.white;
        y += 26f;

        // Line
        Widgets.DrawLineHorizontal(inner.x, y, inner.width);
        y += 8f;

        // Preview Image
        if (_selectedPreview != null)
        {
            float previewH = inner.width * (720f / 1280f);
            Rect previewRect = new Rect(inner.x, y, inner.width, previewH);
            GUI.DrawTexture(previewRect, _selectedPreview, ScaleMode.ScaleAndCrop);
            Widgets.DrawBox(previewRect, 1);
            y += previewH + 6f;
            // Görsel ile açıklama arasına ince çizgi
            Widgets.DrawLineHorizontal(inner.x, y, inner.width);
            y += 6f;
        }

        // Description
        if (!string.IsNullOrEmpty(_selectedDesc))
        {
            Rect descRect = new Rect(inner.x, y, inner.width, inner.yMax - y);
            Widgets.Label(descRect, _selectedDesc);
        }
    }
    #endregion

    #region UI Utils
    /// <summary>
    /// Draws a tiny, colored section header label and returns the updated Y position for the next UI element.
    /// </summary>
    /// <param name="yPosition">The current vertical position on the canvas where the header starts.</param>
    /// <param name="width">The total available width for the header constraint.</param>
    /// <param name="headerText">The text string to display as the section title.</param>
    /// <returns>The next available Y position, factoring in the header height and bottom spacing.</returns>
    private static float DrawSectionHeader(float yPosition, float width, string headerText)
    {
        Text.Font = GameFont.Tiny;
        GUI.color = ColorSection;
        Widgets.Label(new Rect(4f, yPosition, width - 8f, SectionLabelHeight), headerText);
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        return yPosition + SectionLabelHeight + 6f;
    }

    /// <summary>
    /// Draws a custom interactive toggle (ON/OFF) row with hover-based preview handling.
    /// Supports disabling interactions via the locked state and triggers a callback upon value changes.
    /// </summary>
    /// <param name="yPosition">The current vertical layout position where the row starts.</param>
    /// <param name="width">The total available width for the row layout.</param>
    /// <param name="labelText">The display label text for the toggle setting.</param>
    /// <param name="value">A reference to the boolean variable being toggled.</param>
    /// <param name="previewTitle">The title passed to the preview panel when the row is hovered.</param>
    /// <param name="description">The descriptive text passed to the preview panel when the row is hovered.</param>
    /// <param name="previewImage">The texture/image passed to the preview panel when the row is hovered.</param>
    /// <param name="locked">If set to true, interaction is disabled (Useful for gating settings behind research or conditions).</param>
    /// <param name="onChange">An optional callback action triggered immediately after the value changes, passing the new state.</param>
    /// <returns>The next available Y position, factoring in the row height and spacing for sequential layout building.</returns>
    private static float DrawToggleRow(float yPosition, float width, string labelText, ref bool value, string previewTitle, string description, Texture2D previewImage, bool locked = false, Action<bool> onChange = null)
    {
        Rect row = new Rect(0f, yPosition, width, RowHeight);
        Widgets.DrawBoxSolidWithOutline(row, Gray(0.18f, 0.85f), locked ? Gray(1f, 0.4f) : Color.white, 1);

        HandleRowHover(row, previewTitle, description, previewImage);

        // Hover Sound
        bool isHovered = Mouse.IsOver(row);
        if (isHovered && _lastHoveredRow != yPosition)
        {
            SoundDefOf.Mouseover_Standard.PlayOneShotOnCamera();
            _lastHoveredRow = yPosition;
        }
        else if (!isHovered && _lastHoveredRow == yPosition)
            _lastHoveredRow = -1f;

        if (isHovered)
            Widgets.DrawBoxSolid(row, ColorHover);

        // Toggle Button Width
        Text.Font = GameFont.Tiny;
        string onLabelText = "MpSettingOn".Translate();
        string offLabelText = "MpSettingOff".Translate();
        float toggleButtonWidth = Mathf.Max(Text.CalcSize(onLabelText).x, Text.CalcSize(offLabelText).x) + 16f;
        toggleButtonWidth = Mathf.Max(toggleButtonWidth, 44f);
        Text.Font = GameFont.Small;

        float labelWidth = width - toggleButtonWidth - 36f - 16f;

        // Label
        GUI.color = locked ? Gray(1f, 0.4f) : Color.white;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(8f, yPosition + 2f, labelWidth, RowHeight - 4f), "■  " + labelText);
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        // Toggle Button Background
        Rect btnRect = new Rect(width - toggleButtonWidth - 4f, yPosition + 5f, toggleButtonWidth, RowHeight - 10f);
        Color btnBg = value
            ? (locked ? Gray(0.7f, 0.5f) : Color.white)
            : (locked ? Gray(0.15f, 0.5f) : Gray(0.25f));
        Widgets.DrawBoxSolidWithOutline(btnRect, btnBg, locked ? Gray(1f, 0.4f) : Color.white, 1);

        // Toggle Button Text
        Text.Font = GameFont.Tiny;
        GUI.color = locked ? (value ? Color.black : Gray(0.6f)) : Gray(0.5f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(btnRect.x, btnRect.y + 1f, btnRect.width, btnRect.height),
            value ? offLabelText : onLabelText);
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        GUI.color = Color.white;

        if (locked)
        {
            if (isHovered)
                TooltipHandler.TipRegion(row, "MpSettingLocked".Translate());

            if (Widgets.ButtonInvisible(btnRect, false))
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
        }
        else if (Widgets.ButtonInvisible(btnRect, false))
        {
            value = !value;
            (value ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff).PlayOneShotOnCamera();
            onChange?.Invoke(value);
        }

        return yPosition + RowHeight + 4f;
    }

    private static float DrawButtonRow(float yPosition, float width, string labelText, string buttonText, string previewTitle, string description, Texture2D previewImage, bool locked = false, Action onClick = null)
    {
        Rect row = new Rect(0f, yPosition, width, RowHeight);
        Widgets.DrawBoxSolidWithOutline(row, Gray(0.18f, 0.85f), locked ? Gray(1f, 0.4f) : Color.white, 1);

        HandleRowHover(row, previewTitle, description, previewImage);

        bool isHovered = Mouse.IsOver(row);
        if (isHovered && _lastHoveredRow != yPosition)
        {
            SoundDefOf.Mouseover_Standard.PlayOneShotOnCamera();
            _lastHoveredRow = yPosition;
        }
        else if (!isHovered && _lastHoveredRow == yPosition)
            _lastHoveredRow = -1f;

        if (isHovered)
            Widgets.DrawBoxSolid(row, ColorHover);

        // Button Width
        Text.Font = GameFont.Tiny;
        float buttonWidth = Text.CalcSize(buttonText).x + 16f;
        buttonWidth = Mathf.Max(buttonWidth, 44f);
        Text.Font = GameFont.Small;

        float labelWidth = width - buttonWidth - 36f - 16f;

        // Label
        GUI.color = locked ? Gray(1f, 0.4f) : Color.white;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(8f, yPosition + 2f, labelWidth, RowHeight - 4f), "■  " + labelText);
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        // Button
        Rect btnRect = new Rect(width - buttonWidth - 4f, yPosition + 5f, buttonWidth, RowHeight - 10f);
        Widgets.DrawBoxSolidWithOutline(btnRect, locked ? Gray(0.15f, 0.5f) : Gray(0.25f), locked ? Gray(1f, 0.4f) : Color.white, 1);

        Text.Font = GameFont.Tiny;
        GUI.color = locked ? Gray(0.6f) : Color.white;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(btnRect.x, btnRect.y + 1f, btnRect.width, btnRect.height), buttonText);
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        GUI.color = Color.white;

        if (locked)
        {
            if (isHovered)
                TooltipHandler.TipRegion(row, "MpSettingLocked".Translate());

            if (Widgets.ButtonInvisible(btnRect, false))
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
        }
        else if (Widgets.ButtonInvisible(btnRect, false))
        {
            SoundDefOf.Click.PlayOneShotOnCamera();
            onClick?.Invoke();
        }

        return yPosition + RowHeight + 4f;
    }

    private static float DrawTextRow(float yPosition, float width, string labelText, ref string value, string previewTitle, string description, Texture2D previewImage, bool locked = false, Func<string, bool> validate = null, string controlName = null, Action<string> onChange = null)
    {
        Rect row = new Rect(0f, yPosition, width, RowHeight);
        Widgets.DrawBoxSolidWithOutline(row, Gray(0.18f, 0.85f), locked ? Gray(1f, 0.4f) : Color.white, 1);

        HandleRowHover(row, previewTitle, description, previewImage);

        // Hover Sound
        bool isHovered = Mouse.IsOver(row);
        if (isHovered && _lastHoveredRow != yPosition)
        {
            SoundDefOf.Mouseover_Standard.PlayOneShotOnCamera();
            _lastHoveredRow = yPosition;
        }
        else if (!isHovered && _lastHoveredRow == yPosition)
            _lastHoveredRow = -1f;

        if (isHovered)
            Widgets.DrawBoxSolid(row, ColorHover);

        float fieldWidth = 120f;
        float labelWidth = width - fieldWidth - 16f;

        // Label
        GUI.color = locked ? Gray(1f, 0.4f) : Color.white;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(8f, yPosition + 2f, labelWidth, RowHeight - 4f), "■  " + labelText);
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        Rect fieldRect = new Rect(width - fieldWidth - 4f, yPosition + 5f, fieldWidth, RowHeight - 10f);

        if (controlName != null)
            GUI.SetNextControlName(controlName);

        string oldValue = value;
        GUI.enabled = !locked;
        string typed = Widgets.TextField(fieldRect, value);
        GUI.enabled = true;

        if (locked)
        {
            if (isHovered)
                TooltipHandler.TipRegion(row, "MpSettingLocked".Translate());

            if (Widgets.ButtonInvisible(fieldRect, false))
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
        }
        else if (oldValue != typed && (validate == null || validate(typed)))
        {
            value = typed;
            onChange?.Invoke(value);
        }

        return yPosition + RowHeight + 4f;
    }

    private static float DrawSliderRow(float y, float w, string label, ref float value, float min, float max, string previewTitle, string desc, Texture2D preview)
    {
        Rect row = new Rect(0f, y, w, RowHeight);
        HandleRowHover(row, previewTitle, desc, preview);

        float labelW = w * 0.52f;
        float valW = 32f;
        float sliderW = w - labelW - valW - 12f;

        // Label
        Widgets.Label(new Rect(8f, y + 2f, labelW - 8f, RowHeight - 4f), label);

        //
        string valStr = Mathf.RoundToInt(value).ToString();
        Widgets.Label(new Rect(labelW, y + 2f, valW, RowHeight - 4f), valStr);

        // Slider
        Rect sliderRect = new Rect(labelW + valW, y + RowHeight / 2f - 8f, sliderW - 4f, 16f);
        value = Widgets.HorizontalSlider(sliderRect, value, min, max);

        // Line
        Widgets.DrawLineHorizontal(4f, y + RowHeight - 1f, w - 8f);

        return y + RowHeight;
    }

    private static float DrawIntRow(float yPosition, float width, string labelText, ref int value, string previewTitle, string description, Texture2D previewImage, bool locked = false, Func<int, bool> validate = null, string controlName = null, Action<int> onChange = null)
    {
        Rect row = new Rect(0f, yPosition, width, RowHeight);
        Widgets.DrawBoxSolidWithOutline(row, Gray(0.18f, 0.85f), locked ? Gray(1f, 0.4f) : Color.white, 1);

        HandleRowHover(row, previewTitle, description, previewImage);

        bool isHovered = Mouse.IsOver(row);
        if (isHovered && _lastHoveredRow != yPosition)
        {
            SoundDefOf.Mouseover_Standard.PlayOneShotOnCamera();
            _lastHoveredRow = yPosition;
        }
        else if (!isHovered && _lastHoveredRow == yPosition)
            _lastHoveredRow = -1f;

        if (isHovered)
            Widgets.DrawBoxSolid(row, ColorHover);

        float fieldWidth = 120f;
        float labelWidth = width - fieldWidth - 16f;

        GUI.color = locked ? Gray(1f, 0.4f) : Color.white;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(8f, yPosition + 2f, labelWidth, RowHeight - 4f), "■  " + labelText);
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        Rect fieldRect = new Rect(width - fieldWidth - 4f, yPosition + 5f, fieldWidth, RowHeight - 10f);

        if (controlName != null)
            GUI.SetNextControlName(controlName);

        string oldStr = value.ToString();
        GUI.enabled = !locked;
        string typed = Widgets.TextField(fieldRect, oldStr);
        GUI.enabled = true;

        if (locked)
        {
            if (isHovered)
                TooltipHandler.TipRegion(row, "MpSettingLocked".Translate());

            if (Widgets.ButtonInvisible(fieldRect, false))
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
        }
        else if (typed != oldStr && int.TryParse(typed, out int parsed) && (validate == null || validate(parsed)))
        {
            value = parsed;
            onChange?.Invoke(value);
        }

        return yPosition + RowHeight + 4f;
    }

    private static void HandleRowHover(Rect row, string title, string desc, Texture2D preview)
    {
        if (Mouse.IsOver(row))
        {
            _selectedTitle = title;
            _selectedDesc = desc;
            _selectedPreview = preview;

            Widgets.DrawBoxSolid(row, ColorHover);
        }
    }
    #endregion

    private static (string r, string g, string b)[] colorsBuffer = { };

    private static void DoColorContents(MpSettings settings, Rect inRect, Rect pageButtonPos)
    {
        var viewRect = new Rect(inRect)
        {
            height = (settings.playerColors.Count + 1) * 32f,
            width = inRect.width - 20f,
        };

        var rect = new Rect(pageButtonPos.xMin - 150, pageButtonPos.yMin, 125, 32);
        if (Widgets.ButtonText(rect, "MpResetColors".Translate()))
        {
            settings.playerColors = new List<ColorRGBClient>(MpSettings.DefaultPlayerColors);
            PlayerManager.PlayerColors = settings.playerColors.Select(c => (ColorRGB)c).ToArray();
        }

        if (settings.playerColors.Count != colorsBuffer.Length)
        {
            colorsBuffer = new (string r, string g, string b)[settings.playerColors.Count];
        }

        Widgets.BeginScrollView(inRect, ref _scrollPosition, viewRect);

        var toRemove = -1;
        for (var i = 0; i < settings.playerColors.Count; i++)
        {
            var colors = settings.playerColors[i];
            if (DrawColorRow(settings, i * 32 + 120, ref colors, ref colorsBuffer[i], out var edited))
                toRemove = i;
            if (edited)
            {
                settings.playerColors[i] = colors;
                PlayerManager.PlayerColors = settings.playerColors.Select(c => (ColorRGB)c).ToArray();
            }
        }

        rect = new Rect(402, settings.playerColors.Count * 32 + 118, 32, 32);
        if (Widgets.ButtonText(rect, "+"))
        {
            var rand = new Random();
            settings.playerColors.Add(new ColorRGBClient((byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256)));
            PlayerManager.PlayerColors = settings.playerColors.Select(c => (ColorRGB)c).ToArray();
        }

        Widgets.EndScrollView();

        if (toRemove >= 0)
        {
            settings.playerColors.RemoveAt(toRemove);
            PlayerManager.PlayerColors = settings.playerColors.Select(c => (ColorRGB)c).ToArray();
        }
    }

    private static bool DrawColorRow(MpSettings settings, int pos, ref ColorRGBClient color, ref (string r, string g, string b) buffer, out bool edited)
    {
        var (r, g, b) = ((int)color.r, (int)color.g, (int)color.b);
        var rect = new Rect(10, pos, 100, 28);
        Widgets.TextFieldNumericLabeled(rect, "R", ref r, ref buffer.r, 0, 255);
        rect = new Rect(120, pos, 100, 28);
        Widgets.TextFieldNumericLabeled(rect, "G", ref g, ref buffer.g, 0, 255);
        rect = new Rect(230, pos, 100, 28);
        Widgets.TextFieldNumericLabeled(rect, "B", ref b, ref buffer.b, 0, 255);

        rect = new Rect(350, pos - 2, 32, 32);
        Widgets.DrawBoxSolid(rect, color);

        if (color.r != r || color.g != g || color.b != b)
        {
            color = new ColorRGBClient((byte)r, (byte)g, (byte)b);
            edited = true;
        }
        else edited = false;

        if (settings.playerColors.Count > 1)
        {
            rect = new Rect(402, pos - 2, 32, 32);
            return Widgets.ButtonText(rect, "-");
        }

        return false;
    }
}



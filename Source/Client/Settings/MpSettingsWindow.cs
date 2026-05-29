using Multiplayer.Client.Util;
using Multiplayer.Common;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Verse;
using Verse.Sound;
using Random = System.Random;

namespace Multiplayer.Client;

public static class MpSettingsWindow
{
    private static string slotsBuffer;
    private static string desyncRadiusBuffer;
    private static string jittedMethodsBuffer;

    private static Vector2 scrollPosition = Vector2.zero;
    private static SettingsTabs currentTab = SettingsTabs.General;

    // ── Pencere boyutu ──────────────────────────────────────────────────────

    // ── Seçili ayarın meta verisi (sağ panel + alt açıklama için) ──────────
    private static string _selectedTitle = null;
    private static string _selectedDesc = null;
    private static Texture2D _selectedPreview = null;   // null = placeholder çizilir

    // ── Scroll pozisyonu (sol panel) ───────────────────────────────────────
    private static Vector2 _scrollPos = Vector2.zero;
    private static float _totalContentH = 0f;          // dinamik hesap

    // ── Layout sabitleri ───────────────────────────────────────────────────
    private const float TitleH = 36f;
    private const float ButtonH = 36f;
    private const float RowH = 32f;
    private const float SectionGap = 2f;
    private const float SectionLabelH = 20f;

    // ── Renkler ────────────────────────────────────────────────────────────
    private static readonly Color ColorSection = new Color(0.55f, 0.55f, 0.55f);
    private static readonly Color ColorHover = new Color(1f, 1f, 1f, 0.07f);
    private static readonly Color ColorSelected = new Color(0.9f, 0.7f, 0.2f, 0.18f);
    private static readonly Color ColorPanelBg = new Color(0.13f, 0.13f, 0.13f, 0.95f);
    private static readonly Color ColorDivider = new Color(0.35f, 0.35f, 0.35f, 0.6f);

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
            new($"MpSettingsPage{SettingsTabs.General}".Translate(), () => currentTab = SettingsTabs.General, currentTab == SettingsTabs.General),
            new($"MpSettingsPage{SettingsTabs.Color}".Translate(), () => currentTab = SettingsTabs.Color, currentTab == SettingsTabs.Color),
            };
        inRect.yMin += 30f;

        TabDrawer.DrawTabs(inRect, tabs);

        GUI.BeginGroup(new Rect(0, inRect.yMin, inRect.width, inRect.height));
        {
            Rect groupRect = inRect.AtZero();
            switch (currentTab)
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

    public static void DrawGeneralSettings(MpSettings settings, Rect inRect)
    {
        // ── Ana gövde alanı ──────────────────────────────────────────────
        float bodyY = inRect.y;
        float bodyH = inRect.height - TitleH - ButtonH - 8f;
        Rect bodyRect = new Rect(inRect.x, bodyY, inRect.width, bodyH);

        float leftW = bodyRect.width * 0.52f - 4f;
        float rightW = bodyRect.width - leftW - 8f;

        Rect leftRect = new Rect(bodyRect.x, bodyRect.y, leftW, bodyRect.height);
        Rect rightRect = new Rect(bodyRect.x + leftW + 8f, bodyRect.y, rightW, bodyRect.height);

        DrawLeftPanel(settings, leftRect);
        DrawRightPanel(rightRect);

        // ── Butonlar ──────────────────────────────────────────────────────
        Rect btnRect = new Rect(inRect.x,
                                inRect.y + inRect.height - ButtonH,
                                inRect.width, ButtonH);


        /*  var listing = new Listing_Standard();
          listing.Begin(inRect);
          listing.ColumnWidth = 270f;

          DoUsernameField(settings, listing);
          listing.TextFieldNumericLabeled("MpAutosaveSlots".Translate() + ":  ", ref settings.autosaveSlots, ref slotsBuffer, 1f,
              99f);

          listing.CheckboxLabeled("MpShowPlayerCursors".Translate(), ref settings.showCursors);
          DoHideOtherPlayersInColonistBarField(settings, listing);
          listing.CheckboxLabeled("MpPlayerCursorTransparency".Translate(), ref settings.transparentPlayerCursors);
          listing.CheckboxLabeled("MpAutoAcceptSteam".Translate(), ref settings.autoAcceptSteam,
              "MpAutoAcceptSteamDesc".Translate());
          listing.CheckboxLabeled("MpTransparentChat".Translate(), ref settings.transparentChat);
          listing.CheckboxLabeled("MpAppendNameToAutosave".Translate(), ref settings.appendNameToAutosave);
          listing.CheckboxLabeled("MpShowModCompat".Translate(), ref settings.showModCompatibility,
              "MpShowModCompatDesc".Translate());
          listing.CheckboxLabeled("MpEnablePingsSetting".Translate(), ref settings.enablePings);
          listing.CheckboxLabeled("MpEnableCrossPlanetLayerPings".Translate(), ref settings.enableCrossPlanetLayerPings,
              "MpEnableCrossPlanetLayerPingsDesc".Translate());
          listing.CheckboxLabeled("MpShowMainMenuAnimation".Translate(), ref settings.showMainMenuAnim);

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

          if (listing.ButtonText("Generate debug info"))
          {
              try
              {
                  Find.WindowStack.Add(new Dialog_AdvancedSettings());
                  DebugInfoFile.Generate();
              }
              catch(Exception e)
              {
                  Log.Error($"Failed to generate debug info {e}");
              }
          }

          if (VersionChecker.IsContinuousRelease || VersionChecker.IsLocalBuild)
              listing.CheckboxLabeled("MpIncludeReplayInDesync".Translate(), ref settings.includeReplayInDesync);

          if (Prefs.DevMode)
          {
              listing.CheckboxLabeled("Show debug info", ref settings.showDevInfo);
              listing.TextFieldNumericLabeled("Desync radius:  ", ref settings.desyncTracesRadius, ref desyncRadiusBuffer, 1f,
                  200f);
              listing.TextFieldNumericLabeled("Jitted methods:  ", ref settings.jittedMethodsInDesync, ref jittedMethodsBuffer);

              if (MpVersion.IsDebug && FileAssoc.IsSupported())
              {
                  if (FileAssoc.IsRegistered())
                  {
                      if (listing.ButtonText("Remove file associations")) FileAssoc.Remove();
                  }
                  else
                  {
                      if (listing.ButtonText("Register file associations")) FileAssoc.Register();
                  }
              }
  #if DEBUG
              using (MpStyle.Set(TextAnchor.MiddleCenter))
                  if (listing.ButtonTextLabeled("Desync tracing mode", settings.desyncTracingMode.ToString()))
                      settings.desyncTracingMode = settings.desyncTracingMode.Cycle();
  #endif
          }

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


    // ═══════════════════ SOL PANEL ════════════════════════════════════════
    private static void DrawLeftPanel(MpSettings settings, Rect rect)
    {
        Widgets.DrawBoxSolid(rect, ColorPanelBg);
        Widgets.DrawBox(rect, 1);

        Rect inner = rect.ContractedBy(6f);

        // Scroll view
        Rect viewRect = new Rect(0f, 0f, inner.width - 16f, _totalContentH > 0 ? _totalContentH : 9999f);
        Widgets.BeginScrollView(inner, ref _scrollPos, viewRect);

        float y = 0f;

        //   DoHideOtherPlayersInColonistBarField(settings, listing);


        //   listing.CheckboxLabeled("MpEnablePingsSetting".Translate(), ref settings.enablePings);
        //    listing.CheckboxLabeled("MpEnableCrossPlanetLayerPings".Translate(), ref settings.enableCrossPlanetLayerPings,
        //       "MpEnableCrossPlanetLayerPingsDesc".Translate());

        // ── GENEL ────────────────────────────────────────────────────────
        y = DrawSectionHeader(y, viewRect.width, "SERVER");

        y = DrawToggleRow(y, viewRect.width, "MpAutoAcceptSteam".Translate(),
        ref settings.autoAcceptSteam,
        "MpAutoAcceptSteam".Translate(),
        "MpAutoAcceptSteamDesc".Translate(),
        null);

        y = DrawToggleRow(y, viewRect.width, "MpAppendNameToAutosave".Translate(),
    ref settings.appendNameToAutosave,
    "MpAppendNameToAutosave".Translate(),
    null,
    null);

        y += SectionGap;
        y = DrawSectionHeader(y, viewRect.width, "USER INTERFACE");

        y = DrawTextRow(y, viewRect.width,
    "MpUsernameSetting".Translate(),
    ref settings.username,
     "MpUsernameSetting".Translate(), null, null,
    onChange: val => Multiplayer.username = val,
    validate: val => val.Length <= 15 && MultiplayerServer.UsernamePattern.IsMatch(val),
    controlName: UsernameField);

        // Oyundayken odaklanmayı engelle — metod dışında kalması mantıklı
        // çünkü bu davranış sadece bu alana özel
        if (Multiplayer.Client != null && GUI.GetNameOfFocusedControl() == UsernameField)
            UI.UnfocusCurrentControl();


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

        y += 8f;
        _totalContentH = y;  // bir sonraki frame'de scroll yüksekliği doğru hesaplanır

        Widgets.EndScrollView();
    }

    // ═══════════════════ SAĞ PANEL (ÖNİZLEME + AÇIKLAMA) ════════════════
    private static void DrawRightPanel(Rect rect)
    {
        Widgets.DrawBoxSolid(rect, ColorPanelBg);
        Widgets.DrawBox(rect, 1);

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

        // ── Başlık ────────────────────────────────────────────────────────
        Text.Font = GameFont.Small;
        GUI.color = new Color(0.85f, 0.85f, 0.85f);
        Widgets.Label(new Rect(inner.x, y, inner.width, 22f),
                      _selectedTitle.ToUpperInvariant());
        GUI.color = Color.white;
        y += 26f;

        // ── Ayırıcı çizgi ─────────────────────────────────────────────────
        Widgets.DrawLineHorizontal(inner.x, y, inner.width);
        y += 8f;

        // ── Görsel (varsa) ─────────────────────────────────────────────────
        // Resim panelin %60'ını alır; yoksa hiç yer kaplamaz → açıklama üste kayar
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

        // ── Açıklama — her zaman görselin hemen altında ────────────────────
        if (!string.IsNullOrEmpty(_selectedDesc))
        {
            Rect descRect = new Rect(inner.x, y, inner.width, inner.yMax - y);
            Widgets.Label(descRect, _selectedDesc);
        }
    }

    // ═══════════════════ YARDIMCI ÇİZİM METODLARİ ═════════════════════════

    /// Bölüm başlığı (GENEL, MİNİHARİTA vb.)
    private static float DrawSectionHeader(float y, float w, string label)
    {
        Text.Font = GameFont.Tiny;
        GUI.color = ColorSection;
        Widgets.Label(new Rect(4f, y, w - 8f, SectionLabelH), label);
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        return y + SectionLabelH + 6f;
    }

    /// Slider satırı — hover'da sağ panel + açıklama güncellenir
    private static float DrawSliderRow(float y, float w, string label, ref float value, float min, float max, string previewTitle, string desc, Texture2D preview)
    {
        Rect row = new Rect(0f, y, w, RowH);
        HandleRowHover(row, previewTitle, desc, preview);

        float labelW = w * 0.52f;
        float valW = 32f;
        float sliderW = w - labelW - valW - 12f;

        // Label
        Widgets.Label(new Rect(8f, y + 2f, labelW - 8f, RowH - 4f), label);

        // Değer göstergesi
        string valStr = Mathf.RoundToInt(value).ToString();
        Widgets.Label(new Rect(labelW, y + 2f, valW, RowH - 4f), valStr);

        // Slider
        Rect sliderRect = new Rect(labelW + valW, y + RowH / 2f - 8f, sliderW - 4f, 16f);
        value = Widgets.HorizontalSlider(sliderRect, value, min, max);

        // Alt çizgi
        Widgets.DrawLineHorizontal(4f, y + RowH - 1f, w - 8f);

        return y + RowH;
    }

    private static float DrawToggleRow(float y, float w, string label, ref bool value, string previewTitle, string desc, Texture2D preview, Action<bool> onChange = null)
    {
        Rect row = new Rect(0f, y, w, RowH);

        Widgets.DrawBoxSolid(row, new Color(0.18f, 0.18f, 0.18f, 0.85f));
        Widgets.DrawBox(row, 1);

        HandleRowHover(row, previewTitle, desc, preview);

        if (Mouse.IsOver(row))
            Widgets.DrawBoxSolid(row, new Color(0.30f, 0.30f, 0.30f, 0.6f));


        float btnW = 52f;
        float valW = 36f;
        float labelW = w - btnW - valW - 16f;

        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(8f, y + 2f, labelW, RowH - 4f), "■  " + label);
        Text.Anchor = TextAnchor.UpperLeft;

        Rect btnRect = new Rect(w - btnW - 4f, y + 5f, btnW, RowH - 10f);
        Widgets.DrawBoxSolid(btnRect, value ? Color.white : new Color(0.25f, 0.25f, 0.25f));
        Widgets.DrawBox(btnRect, 1);

        Text.Font = GameFont.Tiny;
        GUI.color = value ? Color.black : new Color(0.6f, 0.6f, 0.6f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(btnRect.x, btnRect.y + 1f, btnRect.width, btnRect.height), value ? "ON" : "OFF");
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        GUI.color = Color.white;

        if (Widgets.ButtonInvisible(btnRect, false))
        {
            value = !value;
            onChange?.Invoke(value);
        }

        return y + RowH + 4f;
    }

    private static float DrawTextRow(float y, float w,
        string label, ref string value,
        string previewTitle, string desc, Texture2D preview,
        Action<string> onChange = null,
        Func<string, bool> validate = null,   
        string controlName = null)             
    {
        Rect row = new Rect(0f, y, w, RowH);

        Widgets.DrawBoxSolid(row, new Color(0.18f, 0.18f, 0.18f, 0.85f));
        Widgets.DrawBox(row, 1);

        HandleRowHover(row, previewTitle, desc, preview);

        if (Mouse.IsOver(row))
            Widgets.DrawBoxSolid(row, new Color(0.30f, 0.30f, 0.30f, 0.6f));

        float fieldW = 120f;
        float labelW = w - fieldW - 16f;

        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(8f, y + 2f, labelW, RowH - 4f), "■  " + label);
        Text.Anchor = TextAnchor.UpperLeft;

        Rect fieldRect = new Rect(w - fieldW - 4f, y + 5f, fieldW, RowH - 10f);

        if (controlName != null)
            GUI.SetNextControlName(controlName);

        string oldValue = value;
        string typed = Widgets.TextField(fieldRect, value);

        if (oldValue != typed && (validate == null || validate(typed)))
        {
            value = typed;
            onChange?.Invoke(value);
        }

        return y + RowH + 4f;
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

        Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);

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

    const string UsernameField = "UsernameField";

    private static void DoUsernameField(MpSettings settings, Listing_Standard listing)
    {
        GUI.SetNextControlName(UsernameField);

        var prevField = settings.username;
        var fieldStr = listing.TextEntryLabeled("MpUsernameSetting".Translate() + ":  ", settings.username);

        if (prevField != fieldStr && fieldStr.Length <= 15 && MultiplayerServer.UsernamePattern.IsMatch(fieldStr))
        {
            settings.username = fieldStr;
            Multiplayer.username = fieldStr;
        }

        // Don't allow changing the username while playing
        if (Multiplayer.Client != null && GUI.GetNameOfFocusedControl() == UsernameField)
            UI.UnfocusCurrentControl();
    }

    private static void DoHideOtherPlayersInColonistBarField(MpSettings settings, Listing_Standard listing)
    {

        bool oldValue = settings.hideOtherPlayersInColonistBar;
        listing.CheckboxLabeled("MpHideOtherPlayersInColonistBar".Translate(), ref settings.hideOtherPlayersInColonistBar);
        if (oldValue != settings.hideOtherPlayersInColonistBar && Multiplayer.Client != null)
        {
            //Force update ColonistBar
            Find.ColonistBar.MarkColonistsDirty();
            Find.ColonistBar.CheckRecacheEntries();
        }

    }


}

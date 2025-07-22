using HarmonyLib;
using Multiplayer.Common.Networking.Chat;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace Multiplayer.Client
{
    public static class HoverChat
    {
        private const float MaxFadeTime = 15f;
        private const float ScrollbarWidth = 6f;
        private const float TextBoxHeight = 30f;

        public static float CurrentAlpha { get; private set; }
        public static bool IsVisible => CurrentAlpha > 0f;
        public static bool IsTyping = false;


        private static string currentInput = string.Empty;
        private static float fadeTimer = 0f;
        private static Vector2 scrollPosition = Vector2.zero;
        private static bool scrollJumpToBottom = false;
        private static bool hasBeenFocused = false;
        private static int lastMessageCount = 0;

        public static void Update()
        {
            if ((fadeTimer > 0f) && !IsTyping){
                fadeTimer -= Time.deltaTime;
            }

            if (Event.current.type is EventType.KeyUp && Event.current.keyCode == KeyCode.Escape) {
                IsTyping = false;
                Show();
            }

            if (Event.current.type is EventType.KeyUp && Event.current.keyCode == KeyCode.Return)
            {
                Event.current.Use();
                Show();
                IsTyping = !IsTyping;
                if (IsTyping)
                  hasBeenFocused = false;
            }

            /*if (IsTyping && GUI.GetNameOfFocusedControl() != "CustomChatInput")
              {
                currentInput = "";
                IsTyping = false;
                Show();
              }*/

            int MessageCount = Multiplayer.session.messages.Count;
            if (lastMessageCount != MessageCount)
            {
                lastMessageCount = MessageCount;
                Show();
            }

            CurrentAlpha = Mathf.Clamp01(fadeTimer / 2f);
        }

        public static void DoHoverChatContents(Rect inRect)
        {
            float chatAlpha = CurrentAlpha;
            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, chatAlpha);
            DrawHoverChat(inRect);
            GUI.color = old;
        }

        private static void DrawHoverChat(Rect inRect)
        {
            float contentWidth = inRect.width - ScrollbarWidth - 4f;
            float scrollAreaHeight = inRect.height - TextBoxHeight;  // mesaj alanı yüksekliği
            float viewHeight = 0f;

            foreach (var msg in Multiplayer.session.messages)
                viewHeight += DrawMessage(msg, 0f, contentWidth, draw: false);

            Rect scrollRect = new Rect(0, 0, inRect.width, scrollAreaHeight);
            Rect viewRect = new Rect(0, 0, contentWidth, viewHeight);

            if (scrollJumpToBottom)
            {
                scrollPosition.y = Mathf.Max(0f, viewHeight - scrollAreaHeight);
                scrollJumpToBottom = false;
            }

            scrollPosition = GUI.BeginScrollView(
                scrollRect,
                scrollPosition,
                viewRect,
                GUIStyle.none,
                GUIStyle.none
            );

            float y = 0f;
            foreach (var msg in Multiplayer.session.messages)
                y += DrawMessage(msg, y, contentWidth, draw: true);

            GUI.EndScrollView();

            if (IsTyping)
            {
                DrawThinScrollbar(scrollRect, viewHeight);
            }

            Rect inputRect = new Rect(
                0f,
                scrollAreaHeight + 2f,
                inRect.width - ScrollbarWidth - 4f,
                TextBoxHeight - 4f
            );

            if (IsTyping)
            {
                GUI.SetNextControlName("CustomChatInput");
                currentInput = Widgets.TextField(inputRect, currentInput);
                currentInput = currentInput.Substring(0, Math.Min(currentInput.Length, ChatManager.MaxChatMsgLength));
                Widgets.DrawBoxSolid(inputRect, new Color(0f, 0f, 0f, 0.3f));
                Widgets.DrawBox(inputRect, 1);

                if (Event.current.type is EventType.KeyUp
                    && Event.current.keyCode == KeyCode.Return
                    && GUI.GetNameOfFocusedControl() == "CustomChatInput")
                {
                    SendMessage();
                    currentInput = "";
                    IsTyping = false;
                    Show();
                    Event.current.Use();
                }
                if (Event.current.type is EventType.KeyUp
               && Event.current.keyCode == KeyCode.Escape
               && GUI.GetNameOfFocusedControl() == "CustomChatInput")
                {
                    IsTyping = false;
                    Show();
                    Event.current.Use();
                }

            }

            if (!hasBeenFocused && IsTyping && Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl("CustomChatInput");
                TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                editor.OnFocus();
                editor.MoveTextEnd();
                hasBeenFocused = true;
            }
        }

        public static void Show()
        {
            fadeTimer = MaxFadeTime;
            scrollJumpToBottom = true;
        }

        public static void SendMessage()
        {
            currentInput = currentInput.Trim();

            if (currentInput.NullOrEmpty()) return;

            else if (Multiplayer.Client == null)
                Multiplayer.session.AddMsg(currentInput);
            else
            {
               ClientUtil.SendMessage(currentInput);
            }

            currentInput = "";
        }

        private static void DrawThinScrollbar(Rect outRect, float viewHeight)
        {
            if (viewHeight <= outRect.height) return;

            float visibleRatio = outRect.height / viewHeight;
            float scrollHeight = visibleRatio * outRect.height;
            float scrollY = scrollPosition.y / viewHeight * outRect.height;

            Rect barBack = new Rect(outRect.width - ScrollbarWidth - 2f, 0, ScrollbarWidth, outRect.height);
            Rect barRect = new Rect(barBack.x, scrollY, ScrollbarWidth, scrollHeight);

            Widgets.DrawBoxSolid(barBack, new Color(1f, 1f, 1f, 0.05f));
            Widgets.DrawBoxSolid(barRect, new Color(1f, 1f, 1f, 0.25f * CurrentAlpha));
        }

        private static float DrawMessage(ChatMessageData message, float y, float width, bool draw = true)
        {
            float padding = 4f;
            float colorboxLeftMargin = 8f;
            float iconWidth = 2f;

            Color textColorWithAlpha = new Color(
                message.PlayerColor.r,
                message.PlayerColor.g,
                message.PlayerColor.b,
                CurrentAlpha
            );
            string formattedText = $"<color=#{ColorUtility.ToHtmlStringRGBA(textColorWithAlpha)}>"
                                 + $"{message.PlayerName}</color>: {message.Message}";

            float textHeight = Text.CalcHeight(
                formattedText,
                width - colorboxLeftMargin - padding
            );

            float boxHeight = textHeight + padding * 2f;

            if (!draw)
                return boxHeight + 2f;

            Rect backgroundRect = new Rect(
                0f,
                y,
                width,
                boxHeight
            );
            Widgets.DrawBoxSolid(
                backgroundRect,
                new Color(0f, 0f, 0f, 0.3f * CurrentAlpha)
            );

            Rect iconRect = new Rect(
                padding,
                y + padding,
                iconWidth,
                textHeight
            );
            Widgets.DrawBoxSolid(iconRect, textColorWithAlpha);

            Rect labelRect = new Rect(
                padding + colorboxLeftMargin,
                y + padding,
                width - (padding + colorboxLeftMargin) * 2f,
                textHeight
            );
            Widgets.Label(labelRect, formattedText);

            return boxHeight + 2f;
        }


    }

    [HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
    [HarmonyPriority(Priority.Last)]
    static class NewChatUIRootPlayPatch
    {
        private const float MarginX = 100f;
        private const float MarginY = 100f;
        private const float WindowWidth = 400f;
        private const float WindowHeight = 190f;

        static void Postfix()
        {
            if (Multiplayer.Client == null)
                return;

            HoverChat.Update();

            if (!HoverChat.IsVisible) {
                return;
            }

            Rect winRect = new Rect(MarginX, MarginY, WindowWidth, WindowHeight);
            Find.WindowStack.ImmediateWindow(
                 "MpChatWindow".GetHashCode(),
                 winRect,
                 WindowLayer.Super,
                 () =>
                 {
                     HoverChat.DoHoverChatContents(winRect);
                 },
                 doBackground: false,
                 shadowAlpha: 0

             );
        }
    }
}

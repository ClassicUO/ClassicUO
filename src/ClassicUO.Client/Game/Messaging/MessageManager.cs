// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Messaging;
using ClassicUO.Network;

namespace ClassicUO.Game.Messaging
{
    //enum MessageFont : byte
    //{
    //    INVALID = 0xFF,
    //    Bold = 0,
    //    Shadow = 1,
    //    BoldShadow = 2,
    //    Normal = 3,
    //    Gothic = 4,
    //    Italic = 5,
    //    SmallDark = 6,
    //    Colorful = 7,
    //    Rune = 8,
    //    SmallLight = 9
    //}

    internal enum AffixType : byte
    {
        Append = 0x00,
        Prepend = 0x01,
        System = 0x02,
        None = 0xFF
    }


    /// <summary>
    /// Facade over the two Messaging collaborators: keeps the existing public
    /// surface (<c>_world.MessageManager.X</c>) and owns the EventSink
    /// subscriptions. Prompt state and the central message-routing path live
    /// in dedicated cohesive classes under <see cref="Game.Messaging"/>.
    /// </summary>
    internal sealed class MessageManager : IEventListener
    {
        private readonly World _world;
        private readonly IPromptCoordinator _prompts;
        private readonly IMessageRouter _router;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public MessageManager(World world)
            : this(world, new PromptCoordinator(), new MessageRouter(world))
        {
        }

        /// <summary>Full DI seam — inject both collaborators.</summary>
        internal MessageManager(World world, IPromptCoordinator prompts, IMessageRouter router)
        {
            _world = world;
            _prompts = prompts;
            _router = router;

            _prompts.ServerPromptChanged += ForwardPromptChanged;
            _router.MessageReceived += ForwardMessageReceived;
            _router.LocalizedMessageReceived += ForwardLocalizedMessageReceived;
        }

        public void Subscribe()
        {
            EventSink.ChatMessage += _router.OnChatMessage;
            EventSink.UnicodeChatMessage += _router.OnUnicodeChatMessage;
            EventSink.ClilocMessage += _router.OnClilocMessage;
            EventSink.AsciiPrompt += _prompts.OnAsciiPrompt;
            EventSink.UnicodePrompt += _prompts.OnUnicodePrompt;
        }

        public void Unsubscribe()
        {
            EventSink.ChatMessage -= _router.OnChatMessage;
            EventSink.UnicodeChatMessage -= _router.OnUnicodeChatMessage;
            EventSink.ClilocMessage -= _router.OnClilocMessage;
            EventSink.AsciiPrompt -= _prompts.OnAsciiPrompt;
            EventSink.UnicodePrompt -= _prompts.OnUnicodePrompt;
        }

        // ---- Prompt facade ----
        public PromptData PromptData
        {
            get => _prompts.PromptData;
            set => _prompts.PromptData = value;
        }

        public event EventHandler<PromptData> ServerPromptChanged;
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<MessageEventArgs> LocalizedMessageReceived;

        public void CancelServerPrompt()
        {
            SendServerPromptResponse(string.Empty);
        }

        public void SendServerPromptResponse(string text)
        {
            if (PromptData.Prompt == ConsolePrompt.ASCII)
            {
                NetClient.Socket.Send_ASCIIPromptResponse(_world, text, text.Length < 1);
            }
            else if (PromptData.Prompt == ConsolePrompt.Unicode)
            {
                NetClient.Socket.Send_UnicodePromptResponse(_world, text, Settings.GlobalSettings.Language, text.Length < 1);
            }

            PromptData = default;
        }

        // ---- Router facade ----
        public void HandleMessage
        (
            Entity parent,
            string text,
            string name,
            ushort hue,
            MessageType type,
            byte font,
            TextType textType,
            bool unicode = false,
            string lang = null
        ) => _router.HandleMessage(parent, text, name, hue, type, font, textType, unicode, lang);

        public void OnLocalizedMessage(Entity entity, MessageEventArgs args) => _router.OnLocalizedMessage(entity, args);

        public TextObject CreateMessage
        (
            string msg,
            ushort hue,
            byte font,
            bool isunicode,
            MessageType type,
            TextType textType
        ) => _router.CreateMessage(msg, hue, font, isunicode, type, textType);

        private void ForwardPromptChanged(object sender, PromptData e) => ServerPromptChanged?.Invoke(this, e);
        private void ForwardMessageReceived(object sender, MessageEventArgs e) => MessageReceived?.Invoke(sender, e);
        private void ForwardLocalizedMessageReceived(object sender, MessageEventArgs e) => LocalizedMessageReceived?.Invoke(sender, e);
    }
}

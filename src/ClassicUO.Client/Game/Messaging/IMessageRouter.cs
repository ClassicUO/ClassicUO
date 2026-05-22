// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Messaging
{
    /// <summary>
    /// Owns the central <c>HandleMessage</c> path that dispatches incoming
    /// chat / cliloc / system / spell text to overhead bubbles, gump text
    /// surfaces and the <see cref="MessageReceived"/> /
    /// <see cref="LocalizedMessageReceived"/> notification fan-out. Also
    /// owns the <c>ChatMessage</c> / <c>UnicodeChatMessage</c> /
    /// <c>ClilocMessage</c> EventSink handlers and the
    /// <see cref="CreateMessage"/> rendering factory.
    /// </summary>
    internal interface IMessageRouter
    {
        event EventHandler<MessageEventArgs> MessageReceived;
        event EventHandler<MessageEventArgs> LocalizedMessageReceived;

        void OnChatMessage(ChatMessageArgs e);
        void OnUnicodeChatMessage(UnicodeChatMessageArgs e);
        void OnClilocMessage(ClilocMessageArgs e);

        void HandleMessage(
            Entity parent,
            string text,
            string name,
            ushort hue,
            MessageType type,
            byte font,
            TextType textType,
            bool unicode = false,
            string lang = null
        );

        void OnLocalizedMessage(Entity entity, MessageEventArgs args);

        TextObject CreateMessage(
            string msg,
            ushort hue,
            byte font,
            bool isunicode,
            MessageType type,
            TextType textType
        );
    }
}

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    internal interface IMessageManager
    {
        PromptData PromptData { get; set; }

        event EventHandler<PromptData> ServerPromptChanged;
        event EventHandler<MessageEventArgs> MessageReceived;
        event EventHandler<MessageEventArgs> LocalizedMessageReceived;

        void CancelServerPrompt();
        void SendServerPromptResponse(string text);

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

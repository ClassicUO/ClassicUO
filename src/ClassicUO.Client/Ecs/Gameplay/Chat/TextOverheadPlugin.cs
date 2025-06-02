using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using World = TinyEcs.World;

namespace ClassicUO.Ecs;

internal struct TextOverheadEvent
{
    public uint Serial;
    public string Text;
    public string Name;
    public ushort Hue;
    public byte Font;
    public MessageType MessageType;
    public float Time;
}

internal readonly struct TextOverheadPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddEvent<TextOverheadEvent>();
        scheduler.AddResource(new TextOverHeadManager());

        var readTextOverHeadFn = ReadTextOverhead;
        scheduler.OnUpdate(readTextOverHeadFn, ThreadingMode.Single)
                 .RunIf((EventReader<TextOverheadEvent> texts) => !texts.IsEmpty);
    }

    private static void ReadTextOverhead(
        TinyEcs.World world,
        Time time,
        EventReader<TextOverheadEvent> texts,
        Res<TextOverHeadManager> textOverHeadManager
    )
    {
        foreach (var text in texts)
        {
            switch (text.MessageType)
            {
                case MessageType.Regular:
                case MessageType.Spell:
                case MessageType.Whisper:
                case MessageType.Yell:
                case MessageType.Label:
                case MessageType.Limit3Spell:
                    var copyText = text;
                    copyText.Time = time.Total + 5000f;

                    textOverHeadManager.Value.Append(copyText);
                    break;
            }
        }
    }


}

internal sealed class TextOverHeadManager
{
    const int MAX_LENGTH = 200;

    private readonly StringBuilder _sb = new();

    private readonly List<uint> _toRemove = new();
    private readonly List<(int, int)> _cuttedTextIndices = new();
    private readonly Dictionary<uint, LinkedList<TextOverheadEvent>> _textOverHeadMap = new();
    private readonly LinkedList<LinkedList<TextOverheadEvent>> _mainLinkedList = new();

    public void Append(TextOverheadEvent text)
    {
        if (!_textOverHeadMap.TryGetValue(text.Serial, out var list))
        {
            list = new();
            _textOverHeadMap[text.Serial] = list;
        }

        if (list.Count >= 5)
            list.RemoveFirst();
        list.AddLast(text);

        _mainLinkedList.Remove(list);
        _mainLinkedList.AddLast(list);
    }

    public void Update(TinyEcs.World world, Time time, NetworkEntitiesMap networkEntities)
    {
        foreach ((var serial, var list) in _textOverHeadMap)
        {
            var ent = networkEntities.Get(world, serial);

            if (!ent.ID.IsValid() || list.Count == 0)
            {
                _toRemove.Add(serial);
                continue;
            }

            var first = list.First;
            while (first != null)
            {
                var next = first.Next;

                if (first.Value.Time <= time.Total)
                    list.Remove(first);
                first = next;
            }
        }

        if (_toRemove.Count > 0)
        {
            foreach (var serial in _toRemove)
            {
                if (_textOverHeadMap.Remove(serial, out var list))
                {
                    if (!_mainLinkedList.Remove(list))
                    {

                    }
                }
            }
            _toRemove.Clear();
        }
    }

    private static Texture2D _texture;


    private void CopyToStringBuilder(ReadOnlySpan<char> span)
    {
        _sb.Clear();
        foreach (ref readonly var c in span)
        {
            _sb.Append(c);
        }
    }

    public void Render(
        World world,
        NetworkEntitiesMap networkEntities,
        UltimaBatcher2D batch,
        GameContext gameCtx,
        Camera camera,
        HuesLoader huesLoader
    )
    {
        var center = Isometric.IsoToScreen(gameCtx.CenterX, gameCtx.CenterY, gameCtx.CenterZ);
        var windowSize = new Vector2(camera.Bounds.Width, camera.Bounds.Height);
        center -= windowSize / 2f;
        center.X += 22f;
        center.Y += 22f;
        center -= gameCtx.CenterOffset;

        const int FONT_SIZE = 18;

        // var backupViewport = batch.GraphicsDevice.Viewport;
        // batch.GraphicsDevice.Viewport = camera.GetViewport();
        //
        batch.Begin();

        var lines = _cuttedTextIndices;

        if (_texture == null)
        {
            _texture = new(batch.GraphicsDevice, 1, 1);
            _texture.SetData(new Color[1] { Color.White });
        }

        foreach (var list in _mainLinkedList)
        {
            if (list.Count == 0)
                continue;

            if (list.First == null)
                continue;

            var ent = networkEntities.Get(world, list.First.Value.Serial);

            if (!ent.ID.IsValid())
                continue;

            ref var worldPos = ref ent.Get<WorldPosition>();
            ref var offset = ref ent.Get<ScreenPositionOffset>();
            var position = Isometric.IsoToScreen(worldPos.X, worldPos.Y, worldPos.Z);

            if (!Unsafe.IsNullRef(ref offset))
                position += offset.Value;
            position -= center;

            position.X += 22f;
            position.Y += 22f;
            position.Y -= Constants.DEFAULT_CHARACTER_HEIGHT * 5;

            (var bounds, var totalLines) = GetBounds(list, FONT_SIZE);

            var lineHeight = bounds.Y / totalLines;
            // if (position.X < camera.Bounds.X)
            //     position.X = camera.Bounds.X;
            // if (position.Y < bounds.Y - lineHeight)
            //     position.Y = bounds.Y - lineHeight;

            position = camera.WorldToScreen(position);


            // batch.DrawRectangle(_texture, (int)(position.X - bounds.X * 0.5f), (int)(position.Y - bounds.Y + lineHeight), (int)bounds.X, (int)bounds.Y, Vector3.UnitZ);
            batch.Draw(_texture, new Rectangle((int)(position.X - bounds.X * 0.5f) - 2, (int)(position.Y - bounds.Y + lineHeight) - 2, (int)bounds.X + 4, (int)bounds.Y + 4),
                new Rectangle(0, 0, 1, 1), ShaderHueTranslator.GetHueVector(1, false, 0.5f));

            var last = list.Last;
            var offsetY = 0f;
            var alpha = IsOverlapped(world, networkEntities, list, FONT_SIZE) ? 0.3f : 1f;

            while (last != null)
            {
                var startPos = position;
                startPos.Y += offsetY;

                var text = last.Value;
                var font = text.MessageType switch
                {
                    MessageType.Spell => FontCache.GetFont(4),
                    _ => FontCache.GetFont(0),
                };

                var dynFont = font.GetFont(FONT_SIZE);

                var textLength = text.Text.Length;
                var currentStart = 0;
                float widthMax = 0f, heightMax = 0f;
                while (currentStart < textLength)
                {
                    var maxSize = 0f;
                    var cutAtIndex = textLength;
                    var lastWhiteSpaceIndex = -1;

                    for (int i = currentStart; i < textLength; i++)
                    {
                        CopyToStringBuilder(text.Text.AsSpan(i, 1));
                        var charSize = dynFont.MeasureString(_sb).X;
                        if (char.IsWhiteSpace(text.Text[i]))
                            lastWhiteSpaceIndex = i;

                        if (maxSize + charSize > MAX_LENGTH)
                        {
                            // Cut at the last whitespace or current index if no whitespace found
                            cutAtIndex = lastWhiteSpaceIndex >= currentStart ? lastWhiteSpaceIndex : i;
                            break;
                        }

                        maxSize += charSize;
                    }

                    // Determine the length of the current line
                    var spanLength = cutAtIndex - currentStart;

                    // Collect the substring that fits in this line
                    lines.Add((currentStart, spanLength));

                    var line = text.Text.AsSpan(currentStart, spanLength);
                    CopyToStringBuilder(line);
                    var size = dynFont.MeasureString(_sb);
                    var pos = position - size / 2f;
                    startPos.X = Math.Min(startPos.X, pos.X);
                    widthMax = Math.Max(widthMax, size.X);
                    heightMax = Math.Max(heightMax, size.Y);

                    // Move to the next segment of the text, skip any whitespace after the cut
                    currentStart = cutAtIndex + (cutAtIndex < textLength - 1 && char.IsWhiteSpace(text.Text[cutAtIndex]) ? 1 : 0);
                }

                if (startPos.X < 0)
                    startPos.X = 0;
                if (startPos.Y < (lines.Count - 1) * heightMax)
                    startPos.Y = (lines.Count - 1) * heightMax;
                if (startPos.X + widthMax > windowSize.X)
                    startPos.X = windowSize.X - widthMax;

                // Now draw the lines in correct order (top-down)
                for (var i = lines.Count - 1; i >= 0; i--)
                {
                    var line = text.Text.AsSpan(lines[i].Item1, lines[i].Item2);
                    CopyToStringBuilder(line);
                    dynFont.DrawText(batch,
                       _sb, startPos,
                       GetColor(huesLoader, alpha, text.Hue),
                       effect: FontStashSharp.FontSystemEffect.Stroked,
                       effectAmount: 1
                    );

                    // // Draw the text
                    // batch.DrawString(font, line, startPos + Vector2.One, ShaderHueTranslator.GetHueVector(1, false, alpha));
                    // batch.DrawString(font, line, startPos, ShaderHueTranslator.GetHueVector(text.Hue, false, alpha));

                    startPos.Y -= heightMax;
                    offsetY -= heightMax;
                }

                last = last.Previous; // Move to the previous sentence in the list

                lines.Clear();
            }
        }

        // batch.SetSampler(null);
        batch.End();
        //
        // batch.GraphicsDevice.Viewport = backupViewport;
    }

    private Color GetColor(HuesLoader huesLoader, float alpha = 1.0f, ushort hue = 0)
    {
        // this might be simplified somehow
        var h = HuesHelper.Color16To32(
            huesLoader.GetColor16(
                 HuesHelper.ColorToHue(Color.White), hue > 0 ? hue : ushort.MinValue)) | 0xFF_00_00_00;

        var c = new Color() { PackedValue = h };
        c.A = (byte)Microsoft.Xna.Framework.MathHelper.Clamp(alpha * 255f, byte.MinValue, byte.MaxValue);

        return c;
    }

    private bool IsOverlapped(
        TinyEcs.World world,
        NetworkEntitiesMap networkEntities,
        LinkedList<TextOverheadEvent> list,
        int fontSize
    )
    {
        (var bounds, var totalLines) = GetBounds(list, fontSize);

        if (list.Count == 0)
            return false;

        if (list.First == null)
            return false;

        var mainEnt = networkEntities.Get(world, list.First.Value.Serial);
        if (!mainEnt.ID.IsValid())
            return false;

        ref var worldPosMain = ref mainEnt.Get<WorldPosition>();
        ref var offsetMain = ref mainEnt.Get<ScreenPositionOffset>();
        var positionMain = Isometric.IsoToScreen(worldPosMain.X, worldPosMain.Y, worldPosMain.Z);

        if (!Unsafe.IsNullRef(ref offsetMain))
            positionMain += offsetMain.Value;

        positionMain.X += 22f;
        positionMain.Y += 22f;
        positionMain.Y -= Constants.DEFAULT_CHARACTER_HEIGHT * 5;

        var lineHeight = bounds.Y / totalLines;
        if (positionMain.X < 0)
            positionMain.X = 0;
        if (positionMain.Y < bounds.Y - lineHeight)
            positionMain.Y = bounds.Y - lineHeight;

        var rectMain = new Rectangle((int)(positionMain.X - bounds.X * 0.5f), (int)(positionMain.Y - bounds.Y + lineHeight), (int)bounds.X, (int)bounds.Y);

        var first = _mainLinkedList.First;
        LinkedListNode<LinkedList<TextOverheadEvent>> current = null;
        while (first != null)
        {
            if (first.Value == list)
            {
                current = first.Next;
                break;
            }
            first = first.Next;
        }

        while (current != null)
        {
            if (current.Value.First == null)
                return false;

            (var bounds2, var totalLines2) = GetBounds(current.Value, fontSize);

            var ent = networkEntities.Get(world, current.Value.First.Value.Serial);
            if (!ent.ID.IsValid())
                continue;

            ref var worldPos = ref ent.Get<WorldPosition>();
            ref var offset = ref ent.Get<ScreenPositionOffset>();
            var position = Isometric.IsoToScreen(worldPos.X, worldPos.Y, worldPos.Z);

            if (!Unsafe.IsNullRef(ref offset))
                position += offset.Value;

            position.X += 22f;
            position.Y += 22f;
            position.Y -= Constants.DEFAULT_CHARACTER_HEIGHT * 5;

            var lineHeight2 = bounds2.Y / totalLines2;
            if (position.X < 0)
                position.X = 0;
            if (position.Y < bounds2.Y - lineHeight2)
                position.Y = bounds2.Y - lineHeight2;

            var rect = new Rectangle((int)(position.X - bounds2.X * 0.5f), (int)(position.Y - bounds2.Y + lineHeight2), (int)bounds2.X, (int)bounds2.Y);

            if (rectMain.Intersects(rect))
            {
                return true;
            }

            current = current.Next;
        }

        return false;
    }

    private (Vector2 bounds, int lines) GetBounds(LinkedList<TextOverheadEvent> list, int fontSize)
    {
        var bounds = Vector2.Zero;
        var last = list.Last;
        var lines = 0;

        while (last != null)
        {
            var text = last.Value;
            var font = text.MessageType switch
            {
                MessageType.Spell => FontCache.GetFont(4),
                _ => FontCache.GetFont(0),
            };

            var dynFont = font.GetFont(fontSize);

            var textLength = text.Text.Length;
            var currentStart = 0;
            float widthMax = 0f, heightMax = 0f;
            var linesCount = 0;

            while (currentStart < textLength)
            {
                var maxSize = 0f;
                var cutAtIndex = textLength;
                var lastWhiteSpaceIndex = -1;

                for (int i = currentStart; i < textLength; i++)
                {
                    CopyToStringBuilder(text.Text.AsSpan(i, 1));
                    var charSize = dynFont.MeasureString(_sb).X;
                    if (char.IsWhiteSpace(text.Text[i]))
                        lastWhiteSpaceIndex = i;

                    if (maxSize + charSize > MAX_LENGTH)
                    {
                        cutAtIndex = lastWhiteSpaceIndex >= currentStart ? lastWhiteSpaceIndex : i;
                        break;
                    }

                    maxSize += charSize;
                }

                var spanLength = cutAtIndex - currentStart;
                var line = text.Text.AsSpan(currentStart, spanLength);
                CopyToStringBuilder(line);
                var size = dynFont.MeasureString(_sb);

                widthMax = Math.Max(widthMax, size.X);
                heightMax = Math.Max(heightMax, size.Y);

                linesCount++;

                currentStart = cutAtIndex + (cutAtIndex < textLength - 1 && char.IsWhiteSpace(text.Text[cutAtIndex]) ? 1 : 0);
            }

            bounds.X = Math.Max(bounds.X, widthMax);
            bounds.Y += heightMax * linesCount;
            last = last.Previous;

            lines += linesCount;
        }

        return (bounds, lines);
    }
}

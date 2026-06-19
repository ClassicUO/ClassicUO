using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

// Port of the legacy EffectManager + GameEffect hierarchy (src/ClassicUO.Client/
// Game/Managers/EffectManager.cs + Game/GameObjects/{Game,Moving,Fixed,Lightning,
// Drag}Effect.cs). Effects are visual-only, non-interactive and short-lived, so
// they live in a flat list on this resource — same shape as AudioState's sound
// list — instead of as ECS entities (no hit-test, no other system queries them,
// and the render pass is already at its query-param budget).
//
// Rendering is in WorldRenderingPlugin.RenderEffects (inside the world batch).
internal sealed class EffectsState
{
    public readonly List<EffectEntry> Effects = new();
    // Explosion effects spawned mid-update (MovingEffect.CreateExplosionEffect);
    // appended after the iteration so the live span isn't reallocated under us.
    public readonly List<EffectEntry> Pending = new();
}

internal enum EffectKind : byte { Fixed, Moving, Drag, Lightning }

internal struct EffectEntry
{
    public EffectKind Kind;
    public ushort Graphic;
    public ushort Hue;
    public GraphicEffectBlendMode Blend;

    public uint SourceSerial, TargetSerial;
    public bool SourceIsEntity, TargetIsEntity;

    // Current source/target world tile. For an entity source/target these are
    // refreshed from the live entity each frame; otherwise they are the fixed
    // coords from the packet.
    public ushort SrcX, SrcY; public sbyte SrcZ;
    public ushort TgtX, TgtY; public sbyte TgtZ;

    public Vector2 Offset;      // screen-space offset (interpolation / drag walk)
    public float AngleToTarget; // moving effects rotate toward the target

    public AnimDataFrame AnimData;
    public byte AnimIndex;
    public ushort AnimationGraphic; // 0xFFFF = not yet resolved
    public float IntervalInMs;
    public float NextChangeFrameTime;
    public float Duration;          // absolute engine ms; < 0 = infinite
    public bool IsEnabled;

    public bool FixedDir;
    public bool CanExplode;
    public float LastMoveTime;      // drag throttle
    public bool Destroyed;
}

internal readonly struct EffectsPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new EffectsState());

        var updateFn = Update;
        app.AddSystem(updateFn).InStage(Stage.Update).Build();

        app.AddSystem((ResMut<EffectsState> fx) =>
        {
            fx.Value.Effects.Clear();
            fx.Value.Pending.Clear();
        })
        .OnExit(GameState.GameScreen).Build();

        app.AddObserver<On<PacketReceived<OnGraphicEffectPacket_0x70>>,
            ResMut<EffectsState>, Res<NetworkEntitiesMap>, Res<UOFileManager>, Res<Time>,
            Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>>>(On70);

        app.AddObserver<On<PacketReceived<OnGraphicEffectC0Packet_0xC0>>,
            ResMut<EffectsState>, Res<NetworkEntitiesMap>, Res<UOFileManager>, Res<Time>,
            Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>>>(OnC0);

        app.AddObserver<On<PacketReceived<OnGraphicEffectC7Packet_0xC7>>,
            ResMut<EffectsState>, Res<NetworkEntitiesMap>, Res<UOFileManager>, Res<Time>,
            Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>>>(OnC7);

#if AGENT_BUILD
        app.AddResource(new DebugEffectQueue());
        var drainFn = DrainDebugEffects;
        app.AddSystem(Stage.First, drainFn);
#endif
    }

#if AGENT_BUILD
    // Drains debug.spawnEffect requests by emitting the same PacketReceived
    // trigger the network fires, so the effect runs through the real 0xC0 parse
    // + observer + update + render path (deterministic harness repro — no spell).
    static void DrainDebugEffects(Commands commands, Res<DebugEffectQueue> q)
    {
        if (q.Value.Pending.Count == 0) return;
        foreach (var r in q.Value.Pending)
            commands.EmitTrigger(new PacketReceived<OnGraphicEffectC0Packet_0xC0>
            {
                Packet = BuildDebugEffectPacket(r),
            });
        q.Value.Pending.Clear();
    }

    static OnGraphicEffectC0Packet_0xC0 BuildDebugEffectPacket(in DebugEffectSpec s)
    {
        // Serialize the 0xC0 wire body (id stripped by the dispatcher) and Fill()
        // — exact network parse path.
        var buf = new List<byte>();
        void U8(int v) => buf.Add((byte)v);
        void U16(int v) { buf.Add((byte)(v >> 8)); buf.Add((byte)v); }
        void U32(uint v) { buf.Add((byte)(v >> 24)); buf.Add((byte)(v >> 16)); buf.Add((byte)(v >> 8)); buf.Add((byte)v); }

        U8(s.Type);
        U32(s.Source);
        U32(s.Target);
        U16(s.Graphic);
        U16(s.SrcX); U16(s.SrcY); U8((byte)s.SrcZ);
        U16(s.TgtX); U16(s.TgtY); U8((byte)s.TgtZ);
        U8(s.Speed);
        U8(s.Duration);
        U16(0);                  // Unknown
        U8(s.FixedDir ? 1 : 0);
        U8(s.Explode ? 1 : 0);
        U32(s.Hue);
        U32(s.Blend);

        var pkt = new OnGraphicEffectC0Packet_0xC0();
        pkt.Fill(new StackDataReader(CollectionsMarshal.AsSpan(buf)));
        return pkt;
    }
#endif

    static void On70(
        On<PacketReceived<OnGraphicEffectPacket_0x70>> trig,
        ResMut<EffectsState> state, Res<NetworkEntitiesMap> map, Res<UOFileManager> fm, Res<Time> time,
        Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>> q)
    {
        var p = trig.Event.Packet;
        Create(state.Value, map.Value, q, fm.Value, time.Value.Total,
            p.EffectType, p.SourceSerial, p.TargetSerial, p.Graphic, 0,
            p.SourceX, p.SourceY, p.SourceZ, p.TargetX, p.TargetY, p.TargetZ,
            p.Speed, p.Duration, p.FixedDirection, p.WillExplode, GraphicEffectBlendMode.Normal);
    }

    static void OnC0(
        On<PacketReceived<OnGraphicEffectC0Packet_0xC0>> trig,
        ResMut<EffectsState> state, Res<NetworkEntitiesMap> map, Res<UOFileManager> fm, Res<Time> time,
        Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>> q)
    {
        var p = trig.Event.Packet;
        Create(state.Value, map.Value, q, fm.Value, time.Value.Total,
            p.EffectType, p.SourceSerial, p.TargetSerial, p.Graphic, (ushort)p.Hue,
            p.SourceX, p.SourceY, p.SourceZ, p.TargetX, p.TargetY, p.TargetZ,
            p.Speed, p.Duration, p.FixedDirection, p.WillExplode, p.BlendMode);
    }

    static void OnC7(
        On<PacketReceived<OnGraphicEffectC7Packet_0xC7>> trig,
        ResMut<EffectsState> state, Res<NetworkEntitiesMap> map, Res<UOFileManager> fm, Res<Time> time,
        Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>> q)
    {
        var p = trig.Event.Packet;
        Create(state.Value, map.Value, q, fm.Value, time.Value.Total,
            p.EffectType, p.SourceSerial, p.TargetSerial, p.Graphic, (ushort)p.Hue,
            p.SourceX, p.SourceY, p.SourceZ, p.TargetX, p.TargetY, p.TargetZ,
            p.Speed, p.Duration, p.FixedDirection, p.WillExplode, p.BlendMode);
    }

    // Port of EffectManager.CreateEffect.
    static void Create(
        EffectsState state, NetworkEntitiesMap map,
        Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>> q,
        UOFileManager fm, float now,
        GraphicEffectType type, uint source, uint target, ushort graphic, ushort hue,
        ushort srcX, ushort srcY, sbyte srcZ, ushort tgtX, ushort tgtY, sbyte tgtZ,
        byte speed, int duration, bool fixedDir, bool doesExplode, GraphicEffectBlendMode blend)
    {
        if (hue != 0)
            hue++;

        duration *= Constants.ITEM_EFFECT_ANIMATION_DELAY;

        var e = new EffectEntry { Blend = blend, AnimationGraphic = 0xFFFF };

        switch (type)
        {
            case GraphicEffectType.Moving:
                if (graphic <= 0) return;
                if (speed == 0) speed++;

                e.Kind = EffectKind.Moving;
                e.FixedDir = fixedDir;
                e.CanExplode = doesExplode;
                InitBase(ref e, fm, now, graphic, hue, 0, speed);

                // Override the frame interval with the move speed (legacy
                // MovingEffect ctor). d uses the original packet speed, not the
                // *10 base value, so it stays in the constructor's scope.
                var d = Constants.ITEM_EFFECT_ANIMATION_DELAY * 2;
                e.IntervalInMs = d + speed * d;
                e.Offset.X += 22; // moving effects want a +22 to the X

                ResolveSource(map, q, ref e, source, srcX, srcY, srcZ);
                ResolveTarget(map, q, ref e, target, tgtX, tgtY, tgtZ);
                break;

            case GraphicEffectType.DragEffect:
                if (graphic <= 0) return;
                if (speed == 0) speed++;

                e.Kind = EffectKind.Drag;
                e.CanExplode = doesExplode;
                InitBase(ref e, fm, now, graphic, hue, duration, speed);
                ResolveSource(map, q, ref e, source, srcX, srcY, srcZ);
                ResolveTarget(map, q, ref e, target, tgtX, tgtY, tgtZ);
                break;

            case GraphicEffectType.Lightning:
                e.Kind = EffectKind.Lightning;
                InitBase(ref e, fm, now, 0x4E20, hue, 400, 0);
                ResolveSource(map, q, ref e, source, srcX, srcY, srcZ);
                break;

            case GraphicEffectType.FixedXYZ:
                if (graphic <= 0) return;

                e.Kind = EffectKind.Fixed;
                InitBase(ref e, fm, now, graphic, hue, duration, 0);
                e.SrcX = srcX; e.SrcY = srcY; e.SrcZ = srcZ; // never follows an entity
                break;

            case GraphicEffectType.FixedFrom:
                if (graphic <= 0) return;

                e.Kind = EffectKind.Fixed;
                InitBase(ref e, fm, now, graphic, hue, duration, 0);
                ResolveSource(map, q, ref e, source, srcX, srcY, srcZ);
                break;

            default: // ScreenFade (unhandled in legacy) / Nothing
                return;
        }

        state.Effects.Add(e);
    }

    // Port of GameEffect ctor.
    static void InitBase(ref EffectEntry e, UOFileManager fm, float now, ushort graphic, ushort hue, int duration, byte speed)
    {
        e.Graphic = graphic;
        e.Hue = hue;
        e.AnimData = fm.AnimData.CalculateCurrentGraphic(graphic);
        e.IsEnabled = true;
        e.AnimIndex = 0;

        // Computed in int: the legacy `speed *= 10` on a byte silently overflows
        // for speed >= 26 (rare). Widening avoids that without changing the
        // common low-speed case.
        int s = speed * 10;
        if (s == 0)
            s = Constants.ITEM_EFFECT_ANIMATION_DELAY;

        e.IntervalInMs = e.AnimData.FrameInterval == 0 ? s : e.AnimData.FrameInterval * s;
        e.Duration = duration > 0 ? now + duration : -1;
    }

    static void ResolveSource(NetworkEntitiesMap map,
        Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>> q,
        ref EffectEntry e, uint serial, ushort x, ushort y, sbyte z)
    {
        e.SourceSerial = serial;
        if (TryPos(map, q, serial, out var ex, out var ey, out var ez, out _))
        {
            e.SourceIsEntity = true;
            e.SrcX = ex; e.SrcY = ey; e.SrcZ = ez;
        }
        else
        {
            e.SrcX = x; e.SrcY = y; e.SrcZ = z;
        }
    }

    static void ResolveTarget(NetworkEntitiesMap map,
        Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>> q,
        ref EffectEntry e, uint serial, ushort x, ushort y, sbyte z)
    {
        e.TargetSerial = serial;
        if (TryPos(map, q, serial, out var ex, out var ey, out var ez, out _))
        {
            e.TargetIsEntity = true;
            e.TgtX = ex; e.TgtY = ey; e.TgtZ = ez;
        }
        else
        {
            e.TgtX = x; e.TgtY = y; e.TgtZ = z;
        }
    }

    static bool TryPos(NetworkEntitiesMap map,
        Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>> q,
        uint serial, out ushort x, out ushort y, out sbyte z, out Vector2 offset)
    {
        x = 0; y = 0; z = 0; offset = Vector2.Zero;
        if (!SerialHelper.IsValid(serial) || !map.TryGet(serial, out var id) || !q.TryGet(id, out var row))
            return false;

        (var wp, var off) = row;
        x = wp.Ref.X; y = wp.Ref.Y; z = wp.Ref.Z;
        if (off.IsValid())
            offset = off.Ref.Value;
        return true;
    }

    static bool EntityAlive(NetworkEntitiesMap map,
        Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>> q,
        uint serial)
        => SerialHelper.IsValid(serial) && map.TryGet(serial, out var id) && q.Contains(id);

    static void Update(
        ResMut<EffectsState> stateRes, Res<Time> time, Res<NetworkEntitiesMap> mapRes, Res<UOFileManager> fm,
        Query<Data<WorldPosition>, With<Player>> playerQ,
        Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>> q)
    {
        var state = stateRes.Value;
        if (state.Effects.Count == 0)
            return;

        var now = time.Value.Total;
        var dt = time.Value.Frame;
        var map = mapRes.Value;

        int px = 0, py = 0, pz = 0;
        foreach (var (_, wp) in playerQ) { px = wp.Ref.X; py = wp.Ref.Y; pz = wp.Ref.Z; break; }

        state.Pending.Clear();

        var span = CollectionsMarshal.AsSpan(state.Effects);
        for (var i = 0; i < span.Length; i++)
        {
            ref var e = ref span[i];

            switch (e.Kind)
            {
                case EffectKind.Lightning:
                    UpdateLightning(ref e, now, map, q);
                    break;

                case EffectKind.Drag:
                    if (e.LastMoveTime > now)
                        break;
                    e.Offset.X += 8;
                    e.Offset.Y += 8;
                    e.LastMoveTime = now + 20;
                    RunBase(ref e, now, map, q);
                    break;

                case EffectKind.Moving:
                    RunBase(ref e, now, map, q);
                    if (!e.Destroyed)
                        UpdateMoving(ref e, dt, px, py, pz, map, q, fm.Value, now, state.Pending);
                    break;

                default: // Fixed
                    RunBase(ref e, now, map, q);
                    if (!e.Destroyed && e.SourceIsEntity &&
                        TryPos(map, q, e.SourceSerial, out var fx, out var fy, out var fz, out var foff))
                    {
                        e.Offset = foff;
                        e.SrcX = fx; e.SrcY = fy; e.SrcZ = fz;
                    }
                    break;
            }
        }

        if (state.Pending.Count != 0)
            state.Effects.AddRange(state.Pending);

        state.Effects.RemoveAll(static x => x.Destroyed);
    }

    // Port of GameEffect.Update (source-destroyed check, lifetime, frame step).
    static void RunBase(ref EffectEntry e, float now, NetworkEntitiesMap map,
        Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>> q)
    {
        if (e.SourceIsEntity && !EntityAlive(map, q, e.SourceSerial))
        {
            e.Destroyed = true;
            return;
        }

        if (e.Destroyed)
            return;

        if (!e.IsEnabled)
        {
            e.AnimationGraphic = e.Graphic;
            return;
        }

        if (e.Duration < now && e.Duration >= 0)
        {
            e.Destroyed = true;
            return;
        }

        if (e.NextChangeFrameTime < now)
        {
            if (e.AnimData.FrameCount != 0)
            {
                e.AnimationGraphic = (ushort)(e.Graphic + e.AnimData.FrameAt(e.AnimIndex));
                e.AnimIndex++;
                if (e.AnimIndex >= e.AnimData.FrameCount)
                    e.AnimIndex = 0;
            }
            else
            {
                e.AnimationGraphic = e.Graphic;
            }

            e.NextChangeFrameTime = now + e.IntervalInMs;
        }
    }

    // Port of LightningEffect.Update.
    static void UpdateLightning(ref EffectEntry e, float now, NetworkEntitiesMap map,
        Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>> q)
    {
        if (e.Destroyed)
            return;

        if (e.AnimIndex >= 10 || (e.Duration < now && e.Duration >= 0))
        {
            e.Destroyed = true;
            return;
        }

        e.AnimationGraphic = (ushort)(e.Graphic + e.AnimIndex);

        if (e.NextChangeFrameTime < now)
        {
            e.AnimIndex++;
            e.NextChangeFrameTime = now + e.IntervalInMs;
        }

        if (e.SourceIsEntity && TryPos(map, q, e.SourceSerial, out var x, out var y, out var z, out _))
        {
            e.SrcX = x; e.SrcY = y; e.SrcZ = z;
        }
    }

    // Port of MovingEffect.UpdateOffset.
    static void UpdateMoving(ref EffectEntry e, float dt, int playerX, int playerY, int playerZ,
        NetworkEntitiesMap map,
        Query<Data<WorldPosition, ScreenPositionOffset>, Filter<Optional<ScreenPositionOffset>>> q,
        UOFileManager fm, float now, List<EffectEntry> pending)
    {
        int tX, tY, tZ;
        if (e.TargetIsEntity && TryPos(map, q, e.TargetSerial, out var ex, out var ey, out var ez, out _))
        {
            tX = ex; tY = ey; tZ = ez;
            e.TgtX = (ushort)ex; e.TgtY = (ushort)ey; e.TgtZ = (sbyte)ez;
        }
        else { tX = e.TgtX; tY = e.TgtY; tZ = e.TgtZ; }

        int sX, sY, sZ;
        if (e.SourceIsEntity && TryPos(map, q, e.SourceSerial, out var sx, out var sy, out var sz, out _))
        { sX = sx; sY = sy; sZ = sz; }
        else { sX = e.SrcX; sY = e.SrcY; sZ = e.SrcZ; }

        int offsetSourceX = sX - playerX, offsetSourceY = sY - playerY, offsetSourceZ = sZ - playerZ;
        int offsetTargetX = tX - playerX, offsetTargetY = tY - playerY, offsetTargetZ = tZ - playerZ;

        var source = new Vector2((offsetSourceX - offsetSourceY) * 22, (offsetSourceX + offsetSourceY) * 22 - offsetSourceZ * 4);
        source.X += e.Offset.X;
        source.Y += e.Offset.Y;

        var target = new Vector2((offsetTargetX - offsetTargetY) * 22, (offsetTargetX + offsetTargetY) * 22 - offsetTargetZ * 4);

        var offset = target - source;
        var distance = offset.Length();
        var frameSpeed = e.IntervalInMs * dt;
        Vector2 s0;

        if (distance > frameSpeed)
        {
            offset.Normalize();
            s0 = offset * frameSpeed;
        }
        else
        {
            s0 = target;
        }

        if (distance <= 22)
        {
            Explode(ref e, fm, now, pending);
            e.Destroyed = true;
            return;
        }

        int newOffsetX = (int)(source.X / 22f);
        int newOffsetY = (int)(source.Y / 22f);

        TileOffsetOnMonitorToXY(ref newOffsetX, ref newOffsetY, out int newCoordX, out int newCoordY);

        int newX = playerX + newCoordX;
        int newY = playerY + newCoordY;

        if (newX == tX && newY == tY)
        {
            Explode(ref e, fm, now, pending);
            e.Destroyed = true;
            return;
        }

        e.AngleToTarget = (float)Math.Atan2(-offset.Y, -offset.X);

        if (newX != sX || newY != sY)
        {
            // Legacy SetSource(newX, newY, sZ) detaches the source entity.
            e.SourceIsEntity = false;
            e.SrcX = (ushort)newX; e.SrcY = (ushort)newY; e.SrcZ = (sbyte)sZ;

            var nextSource = new Vector2((newCoordX - newCoordY) * 22, (newCoordX + newCoordY) * 22 - offsetSourceZ * 4);
            e.Offset.X = source.X - nextSource.X;
            e.Offset.Y = source.Y - nextSource.Y;
        }

        e.Offset.X += s0.X;
        e.Offset.Y += s0.Y;
    }

    // Port of GameEffect.CreateExplosionEffect.
    static void Explode(ref EffectEntry e, UOFileManager fm, float now, List<EffectEntry> pending)
    {
        if (!e.CanExplode)
            return;

        var boom = new EffectEntry
        {
            Kind = EffectKind.Fixed,
            Blend = e.Blend,
            AnimationGraphic = 0xFFFF,
            SrcX = e.TgtX, SrcY = e.TgtY, SrcZ = e.TgtZ,
        };
        InitBase(ref boom, fm, now, 0x36CB, e.Hue, 400, 0);
        pending.Add(boom);
    }

    // Verbatim port of MovingEffect.TileOffsetOnMonitorToXY.
    static void TileOffsetOnMonitorToXY(ref int ofsX, ref int ofsY, out int x, out int y)
    {
        y = 0;

        if (ofsX == 0)
        {
            x = y = ofsY >> 1;
        }
        else if (ofsY == 0)
        {
            x = ofsX >> 1;
            y = -x;
        }
        else
        {
            int absX = Math.Abs(ofsX);
            int absY = Math.Abs(ofsY);
            x = ofsX;

            if (ofsY > ofsX)
            {
                if (ofsX < 0 && ofsY < 0)
                    y = absX - absY;
                else if (ofsX > 0 && ofsY > 0)
                    y = absY - absX;
            }
            else if (ofsX > ofsY)
            {
                if (ofsX < 0 && ofsY < 0)
                    y = -(absY - absX);
                else if (ofsX > 0 && ofsY > 0)
                    y = -(absX - absY);
            }

            if (y == 0 && ofsY != ofsX)
            {
                if (ofsY < 0)
                    y = -(absX + absY);
                else
                    y = absX + absY;
            }

            y /= 2;
            x += y;
        }
    }
}

#if AGENT_BUILD
internal struct DebugEffectSpec
{
    public byte Type;
    public uint Source, Target;
    public ushort Graphic;
    public ushort SrcX, SrcY; public sbyte SrcZ;
    public ushort TgtX, TgtY; public sbyte TgtZ;
    public byte Speed, Duration;
    public bool FixedDir, Explode;
    public uint Hue, Blend;
}

internal sealed class DebugEffectQueue
{
    public readonly List<DebugEffectSpec> Pending = new();
}
#endif

using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

// Server-pushed light levels (0..30; 0 = brightest, 30 = darkest). Only the raw
// clamped values are stored — the effective overall/personal and the resulting
// IsometricLevel are derived per-frame from these + Profile (custom level /
// type), mirroring legacy IsometricLight + PacketHandlers.{Personal,}LightLevel.
internal sealed class WorldLight
{
    public int RealOverall = 9;
    public int RealPersonal = 9;

    public void Reset()
    {
        RealOverall = 9;
        RealPersonal = 9;
    }
}

internal struct LightData
{
    public ushort Color;
    public bool IsHue;
    public byte ID;
    public int DrawX, DrawY;
}

// Dynamic light sources accumulated for the current frame (AccumulateLights),
// consumed + reset by the light pass. Same fixed cap as legacy GameScene.
internal sealed class LightRenderData
{
    public readonly LightData[] Lights = new LightData[LightsLoader.MAX_LIGHTS_DATA_INDEX_COUNT];
    public int Count;
}

// The off-screen target the darkness gradient + light halos are drawn into.
// GuiRenderingPlugin multiplies it over the world image when compositing the
// game window into the UI RT (legacy "composite at present") — keeping the
// world RT a single uninterrupted draw avoids the target round-trip that would
// discard its contents. Active/Alt tell Gui whether and how to composite.
internal sealed class LightRenderTarget
{
    public RenderTarget2D? Rt;
    public bool Active;
    public bool Alt;
}

internal readonly struct LightingPlugin : IPlugin
{
    // Multiply blend: result = dst * srcColor. Darkens the world by the light
    // RT (gray base = ambient level, halos punch brightness back in).
    internal static readonly BlendState DarknessBlend = new()
    {
        ColorSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.SourceColor,
        ColorBlendFunction = BlendFunction.Add
    };

    // Alternative-lights blend: result = dst*src + dst (brighten, no darken).
    internal static readonly BlendState AltLightsBlend = new()
    {
        ColorSourceBlend = Blend.DestinationColor,
        ColorDestinationBlend = Blend.One,
        ColorBlendFunction = BlendFunction.Add
    };

    public void Build(App app)
    {
        app.AddResource(new WorldLight());
        app.AddResource(new LightRenderData());
        app.AddResource(new LightRenderTarget());

        var accumulateFn = AccumulateLights;
        var renderFn = RenderLightTarget;

        app
            .AddSystem(accumulateFn)
            .InStage(Stage.PostUpdate)
            .SingleThreaded()
            .Label("cuo:lighting:accumulate")
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Query<Data<WorldPosition>, With<Player>> q) => q.Count() > 0)
            .Build()

            // Render the light buffer into its own target BEFORE the world RT is
            // bound (cuo:rendering:begin). The world draw then stays a single
            // uninterrupted pass; Gui multiplies this target over it later.
            .AddSystem(renderFn)
            .InStage(Stage.PostUpdate)
            .SingleThreaded()
            .Label("cuo:lighting:render")
            .After("cuo:lighting:accumulate")
            .Before("cuo:rendering:begin")
            .RunIf((Commands cmds) => cmds.HasResource<GraphicsDevice>())
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build();

        // Reset to defaults on leaving the world so a night level doesn't bleed
        // into the next session before the server re-pushes 0x4E/0x4F.
        app.AddSystem((Res<WorldLight> light) => light.Value.Reset())
            .OnExit(GameState.GameScreen)
            .Build();
    }

    // Effective ambient level (0..1) + whether darkness/lights are active this
    // frame. Mirrors legacy IsometricLight.Recalculate + GameScene.UseLights.
    internal static (float level, bool useLights) Compute(WorldLight light, Profile profile)
    {
        int overall, personal;
        if (profile.UseCustomLightLevel)
        {
            overall = profile.LightLevelType == 1
                ? Math.Min(light.RealOverall, profile.LightLevel)
                : profile.LightLevel;
            personal = 0;
        }
        else
        {
            overall = light.RealOverall;
            personal = light.RealPersonal;
        }

        int reverted = 32 - overall;
        float current = personal > reverted ? personal : reverted;
        float level = current * 0.03125f;

        bool useLights = profile.UseCustomLightLevel
            ? personal < overall
            : light.RealPersonal < light.RealOverall;

        return (level, useLights);
    }

    // Legacy GameScene camera-centering (FillGameObjectList/DrawWorld). Must
    // match WorldRenderingPlugin.Rendering exactly so accumulated light coords
    // land on the same pixels as the drawn sprites.
    private static Vector2 CameraCenter(GameContext gameCtx, Camera camera)
    {
        var center = Isometric.IsoToScreen(gameCtx.CenterX, gameCtx.CenterY, gameCtx.CenterZ);
        center.X -= camera.Bounds.Width / 2f;
        center.Y -= camera.Bounds.Height / 2f;
        center.X += 22f;
        center.Y += 22f;
        center -= gameCtx.CenterOffset;
        return center;
    }

    private static void AccumulateLights(
        Res<Camera> camera,
        Res<GameContext> gameCtx,
        Res<Profile> profile,
        Res<WorldLight> worldLight,
        Res<UOFileManager> fileManager,
        ResMut<LightRenderData> lights,
        Query<Data<ScreenPosition, Graphic, NetworkSerial, Facing>,
            Filter<Without<IsTile>, Without<MobAnimation>, Without<ContainedInto>, Optional<NetworkSerial>, Optional<Facing>>> queryStatics)
    {
        lights.Value.Count = 0;

        var (_, useLights) = Compute(worldLight.Value, profile.Value);
        if (!useLights && !profile.Value.UseAlternativeLights)
            return;

        var center = CameraCenter(gameCtx.Value, camera.Value);
        var tileData = fileManager.Value.TileData;
        var useColored = profile.Value.UseColoredLights;

        foreach (var (screenPos, graphic, serial, facing) in queryStatics)
        {
            var g = graphic.Ref.Value;
            ref readonly var data = ref tileData.StaticData[g];

            var isLight = data.IsLight
                || (g >= 0x3E02 && g <= 0x3E0B)
                || (g >= 0x3914 && g <= 0x3929);
            if (!isLight)
                continue;

            // Item light id rides the packet "direction" byte (stored as Facing
            // on world items); statics use their tile-data layer.
            byte id;
            if (serial.IsValid() && facing.IsValid())
                id = (byte)facing.Ref.Value;
            else
                id = data.Layer;

            var x = screenPos.Ref.Value.X - center.X + 22f;
            var y = screenPos.Ref.Value.Y - center.Y + 22f;
            Push(lights.Value, g, id, x, y, useColored);
        }
    }

    // Legacy GameScene.AddLight id/colour resolution (occlusion test omitted —
    // ECS has no per-tile static list to walk; halos are additive so the cost
    // is over-lighting under a few overhangs).
    private static void Push(LightRenderData buf, ushort graphic, byte id, float drawX, float drawY, bool useColored)
    {
        if (buf.Count >= LightsLoader.MAX_LIGHTS_DATA_INDEX_COUNT)
            return;

        if (graphic >= 0x3E02 && graphic <= 0x3E0B
            || graphic >= 0x3914 && graphic <= 0x3929
            || graphic == 0x0B1D)
        {
            id = 2;
        }

        ushort color = 0;
        bool isHue = false;

        if (useColored)
        {
            if (id > 200)
            {
                color = (ushort)(id - 200);
                id = 1;
            }

            if (LightColors.GetHue(graphic, out ushort c, out bool ih))
            {
                color = c;
                isHue = ih;
            }
        }

        if (id >= LightsLoader.MAX_LIGHTS_DATA_INDEX_COUNT)
            return;

        if (color != 0)
            color++;

        ref var l = ref buf.Lights[buf.Count++];
        l.ID = id;
        l.Color = color;
        l.IsHue = isHue;
        l.DrawX = (int)drawX;
        l.DrawY = (int)drawY;
    }

    private static void RenderLightTarget(
        Res<UltimaBatcher2D> batch,
        ResMut<LightRenderTarget> lightRt,
        Res<Camera> camera,
        Res<Profile> profile,
        Res<WorldLight> worldLight,
        ResMut<LightRenderData> lights,
        Res<AssetsServer> assets)
    {
        var (level, useLights) = Compute(worldLight.Value, profile.Value);
        var altLights = profile.Value.UseAlternativeLights;
        if (!useLights && !altLights)
        {
            lightRt.Value.Active = false;
            lights.Value.Count = 0;
            return;
        }

        var device = batch.Value.GraphicsDevice;

        // Sized to the world RT (both = logical camera viewport), so the Gui
        // composite maps it 1:1 over the world image.
        int w = Math.Max(1, camera.Value.Bounds.Width);
        int h = Math.Max(1, camera.Value.Bounds.Height);
        if (lightRt.Value.Rt == null || lightRt.Value.Rt.IsDisposed
            || lightRt.Value.Rt.Width != w || lightRt.Value.Rt.Height != h)
        {
            lightRt.Value.Rt?.Dispose();
            lightRt.Value.Rt = new RenderTarget2D(
                device, w, h, false, SurfaceFormat.Color, DepthFormat.None);
        }
        var rt = lightRt.Value.Rt;

        device.SetRenderTarget(rt);
        if (altLights)
        {
            device.Clear(ClearOptions.Target, Color.Black, 0f, 0);
        }
        else
        {
            var l = level;
            if (profile.Value.UseDarkNights)
                l -= 0.04f;
            device.Clear(ClearOptions.Target, new Vector4(l, l, l, 1f), 0f, 0);
        }

        batch.Value.Begin(null, camera.Value.ViewTransformMatrix);
        batch.Value.SetStencil(DepthStencilState.None);
        batch.Value.SetBlendState(BlendState.Additive);

        var hue = new Vector3(0f, 0f, 1f);
        for (int i = 0; i < lights.Value.Count; i++)
        {
            ref var ld = ref lights.Value.Lights[i];
            ref readonly var info = ref assets.Value.Lights.GetLight(ld.ID);
            if (info.Texture == null)
                continue;

            hue.X = ld.Color;
            hue.Y = hue.X > 1f
                ? (ld.IsHue ? ShaderHueTranslator.SHADER_HUED : ShaderHueTranslator.SHADER_LIGHTS)
                : ShaderHueTranslator.SHADER_NONE;

            batch.Value.Draw(
                info.Texture,
                new Vector2(ld.DrawX - info.UV.Width * 0.5f, ld.DrawY - info.UV.Height * 0.5f),
                info.UV,
                hue,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 1f,
                effects: SpriteEffects.None,
                layerDepth: 0f);
        }

        batch.Value.SetBlendState(null);
        batch.Value.SetStencil(null);
        batch.Value.End();
        device.SetRenderTarget(null);

        lights.Value.Count = 0;
        lightRt.Value.Active = true;
        lightRt.Value.Alt = altLights;
    }
}

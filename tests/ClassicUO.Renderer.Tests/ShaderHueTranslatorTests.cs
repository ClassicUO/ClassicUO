using ClassicUO.Renderer;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.Renderer.Tests
{
    public class ShaderHueTranslatorTests
    {
        // --- Shader type constants ---

        [Fact]
        public void ShaderConstants_HaveExpectedValues()
        {
            ShaderHueTranslator.SHADER_NONE.Should().Be(0);
            ShaderHueTranslator.SHADER_HUED.Should().Be(1);
            ShaderHueTranslator.SHADER_PARTIAL_HUED.Should().Be(2);
            ShaderHueTranslator.SHADER_TEXT_HUE_NO_BLACK.Should().Be(3);
            ShaderHueTranslator.SHADER_TEXT_HUE.Should().Be(4);
            ShaderHueTranslator.SHADER_LAND.Should().Be(5);
            ShaderHueTranslator.SHADER_LAND_HUED.Should().Be(6);
            ShaderHueTranslator.SHADER_SPECTRAL.Should().Be(7);
            ShaderHueTranslator.SHADER_SHADOW.Should().Be(8);
            ShaderHueTranslator.SHADER_LIGHTS.Should().Be(9);
            ShaderHueTranslator.SHADER_EFFECT_HUED.Should().Be(10);
        }

        [Fact]
        public void SelectedHue_HasExpectedValue()
        {
            ShaderHueTranslator.SelectedHue.Should().Be(new Vector3(23, 1, 0));
        }

        [Fact]
        public void SelectedItemHue_HasExpectedValue()
        {
            ShaderHueTranslator.SelectedItemHue.Should().Be(new Vector3(0x0035, 1, 0));
        }

        // --- GetHueVector(int hue) single-arg overload ---

        [Fact]
        public void GetHueVector_ZeroHue_ReturnsNoneShader()
        {
            var result = ShaderHueTranslator.GetHueVector(0);

            result.X.Should().Be(0);
            result.Y.Should().Be(ShaderHueTranslator.SHADER_NONE);
            result.Z.Should().Be(1f);
        }

        [Fact]
        public void GetHueVector_NonZeroHue_ReturnsHuedShader_WithDecrementedHue()
        {
            var result = ShaderHueTranslator.GetHueVector(5);

            result.X.Should().Be(4); // hue - 1
            result.Y.Should().Be(ShaderHueTranslator.SHADER_HUED);
            result.Z.Should().Be(1f);
        }

        // --- GetHueVector full overload ---

        [Fact]
        public void GetHueVector_Partial_ReturnsPartialHuedShader()
        {
            var result = ShaderHueTranslator.GetHueVector(10, partial: true, alpha: 1f);

            result.X.Should().Be(9); // hue - 1
            result.Y.Should().Be(ShaderHueTranslator.SHADER_PARTIAL_HUED);
            result.Z.Should().Be(1f);
        }

        [Fact]
        public void GetHueVector_PartialBitInHue_ForcesPartial()
        {
            // Hue with 0x8000 bit set means partial hue
            int hue = 10 | 0x8000;
            var result = ShaderHueTranslator.GetHueVector(hue, partial: false, alpha: 1f);

            result.X.Should().Be(9); // (10 & 0x7FFF) - 1
            result.Y.Should().Be(ShaderHueTranslator.SHADER_PARTIAL_HUED);
        }

        [Fact]
        public void GetHueVector_ZeroHueWithPartialBit_DisablesPartial()
        {
            // 0x8000 set but actual hue is 0 => partial forced off
            int hue = 0x8000;
            var result = ShaderHueTranslator.GetHueVector(hue, partial: false, alpha: 1f);

            result.X.Should().Be(0);
            result.Y.Should().Be(ShaderHueTranslator.SHADER_NONE);
        }

        [Fact]
        public void GetHueVector_SpectralColor_ReturnsSpectralShader()
        {
            int hue = 0x4000; // SPECTRAL_COLOR_FLAG
            var result = ShaderHueTranslator.GetHueVector(hue, partial: false, alpha: 1f);

            result.Y.Should().Be(ShaderHueTranslator.SHADER_SPECTRAL);
        }

        [Fact]
        public void GetHueVector_Effect_ReturnsEffectHuedShader()
        {
            var result = ShaderHueTranslator.GetHueVector(5, partial: false, alpha: 1f, effect: true);

            result.X.Should().Be(4);
            result.Y.Should().Be(ShaderHueTranslator.SHADER_EFFECT_HUED);
        }

        [Fact]
        public void GetHueVector_EffectWithPartial_ReturnsPartialHued()
        {
            // effect + partial => SHADER_PARTIAL_HUED (partial takes priority)
            var result = ShaderHueTranslator.GetHueVector(5, partial: true, alpha: 1f, effect: true);

            result.Y.Should().Be(ShaderHueTranslator.SHADER_PARTIAL_HUED);
        }

        [Fact]
        public void GetHueVector_Gump_AddsGumpOffset()
        {
            var result = ShaderHueTranslator.GetHueVector(5, partial: false, alpha: 1f, gump: true);

            // SHADER_HUED + GUMP_OFFSET (20) = 21
            result.Y.Should().Be(ShaderHueTranslator.SHADER_HUED + 20);
        }

        [Fact]
        public void GetHueVector_GumpPartial_AddsGumpOffset()
        {
            var result = ShaderHueTranslator.GetHueVector(5, partial: true, alpha: 1f, gump: true);

            // SHADER_PARTIAL_HUED + GUMP_OFFSET (20) = 22
            result.Y.Should().Be(ShaderHueTranslator.SHADER_PARTIAL_HUED + 20);
        }

        [Fact]
        public void GetHueVector_GumpWithEffect_NoGumpOffset()
        {
            // gump + effect => gump offset not applied
            var result = ShaderHueTranslator.GetHueVector(5, partial: false, alpha: 1f, gump: true, effect: true);

            result.Y.Should().Be(ShaderHueTranslator.SHADER_EFFECT_HUED);
        }

        [Fact]
        public void GetHueVector_Alpha_IsStoredInZ()
        {
            var result = ShaderHueTranslator.GetHueVector(0, partial: false, alpha: 0.5f);

            result.Z.Should().Be(0.5f);
        }

        [Fact]
        public void GetHueVector_CircleTrans_AddsOneToAlpha()
        {
            var result = ShaderHueTranslator.GetHueVector(0, partial: false, alpha: 0.75f, circletrans: true);

            result.Z.Should().Be(1.75f);
        }

        [Fact]
        public void GetHueVector_ZeroHue_PartialForcedFalse()
        {
            // Even if partial=true, hue==0 forces partial=false
            var result = ShaderHueTranslator.GetHueVector(0, partial: true, alpha: 1f);

            result.Y.Should().Be(ShaderHueTranslator.SHADER_NONE);
        }
    }
}

using ClassicUO.Game.UI.Controls;
using FluentAssertions;
using System.Linq;
using Xunit;

namespace ClassicUO.Client.Tests.Game.UI
{
    public class ControlTests
    {
        private class TestControl : Control
        {
            public TestControl(int x = 0, int y = 0, int w = 100, int h = 100) : base(null)
            {
                X = x;
                Y = y;
                Width = w;
                Height = h;
            }
        }

        [Fact]
        public void XY_CanBeSetAndRead()
        {
            var ctrl = new TestControl(10, 20);

            ctrl.X.Should().Be(10);
            ctrl.Y.Should().Be(20);
        }

        [Fact]
        public void WidthHeight_CanBeSetAndRead()
        {
            var ctrl = new TestControl(0, 0, 200, 300);

            ctrl.Width.Should().Be(200);
            ctrl.Height.Should().Be(300);
        }

        [Fact]
        public void XY_CanBeModified()
        {
            var ctrl = new TestControl();
            ctrl.X = 42;
            ctrl.Y = 99;

            ctrl.X.Should().Be(42);
            ctrl.Y.Should().Be(99);
        }

        [Fact]
        public void IsVisible_DefaultTrue()
        {
            var ctrl = new TestControl();

            ctrl.IsVisible.Should().BeTrue();
        }

        [Fact]
        public void IsEnabled_DefaultTrue()
        {
            var ctrl = new TestControl();

            ctrl.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void IsDisposed_DefaultFalse()
        {
            var ctrl = new TestControl();

            ctrl.IsDisposed.Should().BeFalse();
        }

        [Fact]
        public void AcceptMouseInput_DefaultTrue()
        {
            var ctrl = new TestControl();

            ctrl.AcceptMouseInput.Should().BeTrue();
        }

        [Fact]
        public void AcceptKeyboardInput_DefaultFalse()
        {
            var ctrl = new TestControl();

            ctrl.AcceptKeyboardInput.Should().BeFalse();
        }

        [Fact]
        public void IsEditable_DefaultFalse()
        {
            var ctrl = new TestControl();

            ctrl.IsEditable.Should().BeFalse();
        }

        [Fact]
        public void LocalSerial_CanBeSetAndRead()
        {
            var ctrl = new TestControl();
            ctrl.LocalSerial = 0x1234;

            ctrl.LocalSerial.Should().Be(0x1234u);
        }

        [Fact]
        public void ServerSerial_CanBeSetAndRead()
        {
            var ctrl = new TestControl();
            ctrl.ServerSerial = 0xABCD;

            ctrl.ServerSerial.Should().Be(0xABCDu);
        }

        [Fact]
        public void Add_Child_SetsParentReference()
        {
            var parent = new TestControl();
            var child = new TestControl();

            parent.Add(child);

            child.Parent.Should().BeSameAs(parent);
        }

        [Fact]
        public void Add_Child_AppearsInChildren()
        {
            var parent = new TestControl();
            var child = new TestControl();

            parent.Add(child);

            parent.Children.Should().Contain(child);
        }

        [Fact]
        public void Children_CanBeEnumerated()
        {
            var parent = new TestControl();
            var child1 = new TestControl(1, 1);
            var child2 = new TestControl(2, 2);
            var child3 = new TestControl(3, 3);

            parent.Add(child1);
            parent.Add(child2);
            parent.Add(child3);

            parent.Children.Count.Should().Be(3);
            parent.Children.Should().ContainInOrder(child1, child2, child3);
        }

        [Fact]
        public void Dispose_SetsIsDisposed()
        {
            var ctrl = new TestControl();

            ctrl.Dispose();

            ctrl.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void Dispose_DisposesChildren()
        {
            var parent = new TestControl();
            var child = new TestControl();
            parent.Add(child);

            parent.Dispose();

            child.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var ctrl = new TestControl();

            ctrl.Dispose();
            var act = () => ctrl.Dispose();

            act.Should().NotThrow();
        }

        [Fact]
        public void Contains_ReturnsTrue_WhenNotDisposed()
        {
            var ctrl = new TestControl(0, 0, 100, 100);

            ctrl.Contains(50, 50).Should().BeTrue();
        }

        [Fact]
        public void Contains_ReturnsFalse_WhenDisposed()
        {
            var ctrl = new TestControl(0, 0, 100, 100);
            ctrl.Dispose();

            ctrl.Contains(50, 50).Should().BeFalse();
        }

        [Fact]
        public void AcceptMouseInput_FalseWhenDisabled()
        {
            var ctrl = new TestControl();
            ctrl.IsEnabled = false;

            ctrl.AcceptMouseInput.Should().BeFalse();
        }

        [Fact]
        public void AcceptMouseInput_FalseWhenNotVisible()
        {
            var ctrl = new TestControl();
            ctrl.IsVisible = false;

            ctrl.AcceptMouseInput.Should().BeFalse();
        }

        [Fact]
        public void AcceptMouseInput_FalseWhenDisposed()
        {
            var ctrl = new TestControl();
            ctrl.Dispose();

            ctrl.AcceptMouseInput.Should().BeFalse();
        }

        [Fact]
        public void AcceptKeyboardInput_TrueWhenExplicitlySet()
        {
            var ctrl = new TestControl();
            ctrl.AcceptKeyboardInput = true;

            ctrl.AcceptKeyboardInput.Should().BeTrue();
        }

        [Fact]
        public void AcceptKeyboardInput_FalseWhenDisabledEvenIfSet()
        {
            var ctrl = new TestControl();
            ctrl.AcceptKeyboardInput = true;
            ctrl.IsEnabled = false;

            ctrl.AcceptKeyboardInput.Should().BeFalse();
        }

        [Fact]
        public void CanMove_DefaultFalse()
        {
            var ctrl = new TestControl();

            ctrl.CanMove.Should().BeFalse();
        }

        [Fact]
        public void CanCloseWithRightClick_DefaultTrue()
        {
            var ctrl = new TestControl();

            ctrl.CanCloseWithRightClick.Should().BeTrue();
        }

        [Fact]
        public void Page_DefaultZero()
        {
            var ctrl = new TestControl();

            ctrl.Page.Should().Be(0);
        }

        [Fact]
        public void Add_SetsChildPage()
        {
            var parent = new TestControl();
            var child = new TestControl();

            parent.Add(child, page: 3);

            child.Page.Should().Be(3);
        }

        [Fact]
        public void Remove_ClearsParent()
        {
            var parent = new TestControl();
            var child = new TestControl();
            parent.Add(child);

            parent.Remove(child);

            child.Parent.Should().BeNull();
        }

        [Fact]
        public void Remove_RemovesFromChildren()
        {
            var parent = new TestControl();
            var child = new TestControl();
            parent.Add(child);

            parent.Remove(child);

            parent.Children.Should().NotContain(child);
        }

        [Fact]
        public void Location_GetSetWorks()
        {
            var ctrl = new TestControl();
            ctrl.Location = new Microsoft.Xna.Framework.Point(15, 25);

            ctrl.X.Should().Be(15);
            ctrl.Y.Should().Be(25);
            ctrl.Location.Should().Be(new Microsoft.Xna.Framework.Point(15, 25));
        }

        [Fact]
        public void Alpha_DefaultOne()
        {
            var ctrl = new TestControl();

            ctrl.Alpha.Should().Be(1.0f);
        }

        [Fact]
        public void IsFromServer_DefaultFalse()
        {
            var ctrl = new TestControl();

            ctrl.IsFromServer.Should().BeFalse();
        }

        [Fact]
        public void SetTooltip_SetsTooltip()
        {
            var ctrl = new TestControl();
            ctrl.SetTooltip("Test tooltip");

            ctrl.HasTooltip.Should().BeTrue();
        }

        [Fact]
        public void ClearTooltip_ClearsTooltip()
        {
            var ctrl = new TestControl();
            ctrl.SetTooltip("Test tooltip");
            ctrl.ClearTooltip();

            ctrl.HasTooltip.Should().BeFalse();
        }

        [Fact]
        public void RootParent_ReturnsNull_WhenNoParent()
        {
            var ctrl = new TestControl();

            ctrl.RootParent.Should().BeNull();
        }

        [Fact]
        public void RootParent_ReturnsTopLevelParent()
        {
            var grandparent = new TestControl();
            var parent = new TestControl();
            var child = new TestControl();
            grandparent.Add(parent);
            parent.Add(child);

            child.RootParent.Should().BeSameAs(grandparent);
        }

        [Fact]
        public void GetControls_ReturnsMatchingType()
        {
            var parent = new TestControl();
            var child1 = new TestControl();
            var child2 = new TestControl();
            parent.Add(child1);
            parent.Add(child2);

            var result = parent.GetControls<TestControl>();

            result.Should().HaveCount(2);
        }

        [Fact]
        public void GetControls_ExcludesDisposed()
        {
            var parent = new TestControl();
            var child1 = new TestControl();
            var child2 = new TestControl();
            parent.Add(child1);
            parent.Add(child2);
            child1.Dispose();

            var result = parent.GetControls<TestControl>();

            result.Should().HaveCount(1);
            result[0].Should().BeSameAs(child2);
        }

        [Fact]
        public void ActivePage_CanBeSetAndRead()
        {
            var ctrl = new TestControl();
            ctrl.ActivePage = 5;

            ctrl.ActivePage.Should().Be(5);
        }

        [Fact]
        public void Tag_CanBeSetAndRead()
        {
            var ctrl = new TestControl();
            ctrl.Tag = "custom data";

            ctrl.Tag.Should().Be("custom data");
        }

        [Fact]
        public void CanCloseWithEsc_DefaultFalse()
        {
            var ctrl = new TestControl();

            ctrl.CanCloseWithEsc.Should().BeFalse();
        }

        [Fact]
        public void IsFocused_DefaultFalse()
        {
            var ctrl = new TestControl();

            ctrl.IsFocused.Should().BeFalse();
        }

        [Fact]
        public void AllowedToDraw_DefaultTrue()
        {
            var ctrl = new TestControl();

            ctrl.AllowedToDraw.Should().BeTrue();
        }

        [Fact]
        public void WantUpdateSize_DefaultTrue()
        {
            var ctrl = new TestControl();

            ctrl.WantUpdateSize.Should().BeTrue();
        }

        [Fact]
        public void IsModal_DefaultFalse()
        {
            var ctrl = new TestControl();

            ctrl.IsModal.Should().BeFalse();
        }

        [Fact]
        public void Children_InitiallyEmpty()
        {
            var ctrl = new TestControl();

            ctrl.Children.Should().BeEmpty();
        }

        [Fact]
        public void SetParentToNull_RemovesFromParentChildren()
        {
            var parent = new TestControl();
            var child = new TestControl();
            parent.Add(child);

            child.Parent = null;

            parent.Children.Should().NotContain(child);
        }

        [Fact]
        public void Reparent_MovesChildToNewParent()
        {
            var parent1 = new TestControl();
            var parent2 = new TestControl();
            var child = new TestControl();
            parent1.Add(child);

            parent2.Add(child);

            parent1.Children.Should().NotContain(child);
            parent2.Children.Should().Contain(child);
            child.Parent.Should().BeSameAs(parent2);
        }
    }
}

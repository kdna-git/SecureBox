using Avalonia.Controls;
using FluentAssertions;
using SecureBox.Config;
using SecureBox.ViewModels;
using Xunit;

namespace SecureBox.UiTest;

public class SettingsViewTests
{
    [Fact]
    public Task ShowsTheCurrentConfiguration() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();

            app.Navigate<SettingsViewModel>();

            app.Visible<TextBox>().Should().Contain(t => t.Text == VaultConfig.Instance.VaultPath);

            var combos = app.Visible<ComboBox>();
            combos.Select(c => c.SelectedItem).Should().Equal("Dark", DataViewMode.TreeView);
            app.Visible<NumericUpDown>().Single().Value.Should().Be(4);
        });

    [Fact]
    public Task SignedOut_ShowsTheCloudSignInForm() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();

            app.Navigate<SettingsViewModel>();

            var buttons = app.Visible<Button>().Select(b => b.Content as string).ToList();
            buttons.Should().Contain(["Sign in", "Sign up"]).And.NotContain("Sign out");
        });

    [Fact]
    public Task SignedOut_DisablesTheCloudButtonsOfTheControlBar() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();

            app.Navigate<SecretsViewModel>();

            var cloudButtons = app.Visible<Button>()
                .Where(b => (ToolTip.GetTip(b) as string)?.Contains("cloud") == true)
                .ToList();
            cloudButtons.Should().HaveCount(3).And.OnlyContain(b => !b.IsEffectivelyEnabled);
        });

    [Fact]
    public Task ChangingTheViewMode_IsUsedTheNextTimeSecretsIsShown() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();
            var settings = app.Navigate<SettingsViewModel>();

            settings.DataView = DataViewMode.MosaicView;
            app.Navigate<SecretsViewModel>();

            app.TreeRows().Should().BeEmpty();
            app.CardNames("DirectoriesList").Should().Equal("Banking", "Work");
        });
}

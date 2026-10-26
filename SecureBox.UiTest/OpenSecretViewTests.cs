using Avalonia.Controls;
using Avalonia.VisualTree;
using FluentAssertions;
using SecureBox.Config;
using SecureBox.ViewModels;
using SecureBox.Views;
using Xunit;

namespace SecureBox.UiTest;

public class OpenSecretViewTests
{
    [Fact]
    public Task OpeningASecret_ShowsItDecrypted() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();
            var vm = app.Navigate<SecretsViewModel>();

            vm.DoubleClickCommand.Execute(vm.AllItems.Single(i => i.Name == "Email"));
            app.Render();

            var view = app.Visible<OpenSecretView>().Should().ContainSingle().Subject;
            var texts = Texts(view);
            texts.Should().Contain(["Email", AppHarness.EmailSecret, "someone@example.com", "personal"]);

            var link = view.GetVisualDescendants().OfType<HyperlinkButton>().Single();
            link.IsEnabled.Should().BeTrue();
            link.NavigateUri.Should().Be(new Uri("https://example.com"));

            view.GetVisualDescendants().OfType<Button>().Should().NotContain(b => Equals(b.Content, "Extract") && b.IsEffectivelyVisible);
        });

    [Fact]
    public Task OpeningAFile_ShowsItsOriginalNameAndTheExtractButton() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync(DataViewMode.MosaicView);
            var vm = app.Navigate<SecretsViewModel>();
            vm.DoubleClickCommand.Execute(vm.Directories.Single(d => d.Name == "Work"));

            vm.DoubleClickCommand.Execute(vm.Files.Single(f => f.Name == "contract"));
            app.Render();

            var view = app.Visible<OpenSecretView>().Should().ContainSingle().Subject;
            Texts(view).Should().Contain("contract.pdf");
            view.GetVisualDescendants()
                .OfType<Button>()
                .Should()
                .Contain(b => Equals(b.Content, "Extract") && b.IsEffectivelyVisible);
        });

    [Fact]
    public Task Close_HidesTheModal() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();
            var vm = app.Navigate<SecretsViewModel>();
            vm.DoubleClickCommand.Execute(vm.AllItems.Single(i => i.Name == "Email"));
            app.Render();

            var open = (OpenSecretViewModel)app.Visible<OpenSecretView>().Single().DataContext!;
            open.CloseCommand.Execute(null);
            app.Render();

            app.Visible<OpenSecretView>().Should().BeEmpty();
        });

    // Text shown by text blocks, and by buttons whose content is plain text
    private static List<string> Texts(Control view) =>
        [
            .. view.GetVisualDescendants()
                .OfType<TextBlock>()
                .Where(t => t.IsEffectivelyVisible && !string.IsNullOrEmpty(t.Text))
                .Select(t => t.Text!),
        ];
}

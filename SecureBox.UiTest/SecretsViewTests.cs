using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using FluentAssertions;
using FluentIcons.Common;
using SecureBox.Components.TreeViewControl;
using SecureBox.Config;
using SecureBox.ViewModels;
using SecureBox.Views.Components;
using Xunit;

namespace SecureBox.UiTest;

public class SecretsViewTests
{
    [Fact]
    public Task Tree_ShowsTheTopLevelItemsOfTheUser() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();

            app.Navigate<SecretsViewModel>();

            app.TreeNames().Should().Equal("Banking", "Work", "Email");
        });

    [Fact]
    public Task Tree_DrawsAnIconForEveryItem() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();

            app.Navigate<SecretsViewModel>();

            app.TreeRows().Should().NotBeEmpty().And.OnlyContain(r => r.HasIcon());
        });

    [Fact]
    public Task Tree_FillsTheStarOfFavoritesOnly() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();

            app.Navigate<SecretsViewModel>();

            app.TreeRow("Banking").Star().Should().Be(IconVariant.Filled);
            app.TreeRow("Work").Star().Should().Be(IconVariant.Regular);
            app.TreeRow("Email").Star().Should().Be(IconVariant.Regular);
        });

    [Fact]
    public Task Tree_ExpandingAFolderShowsItsContent() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();
            app.Navigate<SecretsViewModel>();

            app.TreeRow("Work").IsExpanded = true;
            app.Render();

            app.TreeNames().Should().BeEquivalentTo("Banking", "Work", "VPN", "contract", "Email");
        });

    [Fact]
    public Task Tree_ClickingTheChevronExpandsAndCollapsesTheFolder() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();
            app.Navigate<SecretsViewModel>();

            void ClickChevron()
            {
                var chevron = app.TreeRow("Work")
                    .GetVisualDescendants()
                    .OfType<ChevronSwitch>()
                    .First();
                var point = chevron.TranslatePoint(
                    new(chevron.Bounds.Width / 2, chevron.Bounds.Height / 2),
                    app.Window
                )!.Value;
                app.Window.MouseDown(point, MouseButton.Left);
                app.Window.MouseUp(point, MouseButton.Left);
                app.Render();
            }

            ClickChevron();
            app.TreeNames().Should().Contain(["VPN", "contract"]);

            ClickChevron();
            app.TreeNames().Should().Equal("Banking", "Work", "Email");
        });

    [Fact]
    public Task MosaicMode_HidesTheTreeAndShowsTheMosaic() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync(DataViewMode.MosaicView);

            app.Navigate<SecretsViewModel>();

            app.Visible<TreeViewItemControl>().Should().BeEmpty();
            app.Visible<MosaicView>().Should().ContainSingle();
        });

    [Fact]
    public Task Mosaic_ShowsFoldersAndFilesAsCardsInTheConfiguredColumns() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync(DataViewMode.MosaicView);

            app.Navigate<SecretsViewModel>();

            app.CardNames("DirectoriesList").Should().Equal("Banking", "Work");
            app.CardNames("FilesList").Should().Equal("Email");
            app.Visible<UniformGrid>().Should().OnlyContain(g => g.Columns == 4);
            app.Cards("DirectoriesList").Should().OnlyContain(c => c.HasIcon());
            app.BreadCrumb().Should().Equal(app.UserId);
        });

    [Fact]
    public Task Mosaic_OpeningFoldersUpdatesTheBreadcrumb() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync(DataViewMode.MosaicView);
            var vm = app.Navigate<SecretsViewModel>();

            vm.DoubleClickCommand.Execute(vm.Directories.Single(d => d.Name == "Banking"));
            vm.DoubleClickCommand.Execute(vm.Directories.Single(d => d.Name == "Cards"));
            app.Render();

            app.BreadCrumb().Should().Equal(app.UserId, "Banking", "Cards");
            app.CardNames("DirectoriesList").Should().BeEmpty();
            app.CardNames("FilesList").Should().Equal("Visa");
        });

    [Fact]
    public Task Mosaic_BreadcrumbNavigatesBackToAParentFolder() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync(DataViewMode.MosaicView);
            var vm = app.Navigate<SecretsViewModel>();
            vm.DoubleClickCommand.Execute(vm.Directories.Single(d => d.Name == "Banking"));
            vm.DoubleClickCommand.Execute(vm.Directories.Single(d => d.Name == "Cards"));

            vm.BreadCrumbItems.Single(b => b.DisplayName == "Banking").NavigateCommand!.Execute(null);
            app.Render();

            app.BreadCrumb().Should().Equal(app.UserId, "Banking");
            app.CardNames("DirectoriesList").Should().Equal("Cards");
            app.CardNames("FilesList").Should().Equal("Bank");

            vm.BreadCrumbItems[0].NavigateCommand!.Execute(null);
            app.Render();

            app.BreadCrumb().Should().Equal(app.UserId);
            app.CardNames("DirectoriesList").Should().Equal("Banking", "Work");
        });

    [Fact]
    public Task Mosaic_DoubleClickingAFolderCardOpensIt() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync(DataViewMode.MosaicView);
            app.Navigate<SecretsViewModel>();

            var card = app.Cards("DirectoriesList").Single(c => c.ItemName() == "Work");
            var label = card.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Work");
            var point = label.TranslatePoint(new(label.Bounds.Width / 2, label.Bounds.Height / 2), app.Window)!.Value;

            for (int click = 0; click < 2; click++)
            {
                app.Window.MouseDown(point, MouseButton.Left);
                app.Window.MouseUp(point, MouseButton.Left);
            }
            app.Render();

            app.BreadCrumb().Should().Equal(app.UserId, "Work");
            // The file system decides the order of the files
            app.CardNames("FilesList").Should().BeEquivalentTo("VPN", "contract");
        });

    [Fact]
    public Task Mosaic_FilterHidesTheCardsThatDoNotMatch() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync(DataViewMode.MosaicView);
            var vm = app.Navigate<SecretsViewModel>();

            vm.Filter = "mail";
            app.Render();

            app.CardNames("DirectoriesList").Should().BeEmpty();
            app.CardNames("FilesList").Should().Equal("Email");
        });
}

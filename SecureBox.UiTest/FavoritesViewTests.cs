using Avalonia.Controls;
using Avalonia.VisualTree;
using FluentAssertions;
using FluentIcons.Common;
using Microsoft.Extensions.DependencyInjection;
using SecureBox.Config;
using SecureBox.Stores;
using SecureBox.ViewModels;
using Xunit;

namespace SecureBox.UiTest;

public class FavoritesViewTests
{
    [Fact]
    public Task Tree_ShowsEveryFavoriteAtTheTopLevel() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();

            app.Navigate<FavoritesViewModel>();

            app.TreeNames().Should().Equal("Banking", "VPN");
            app.TreeRows().Should().OnlyContain(r => r.Star() == IconVariant.Filled);
        });

    [Fact]
    public Task Tree_IndentsFavoritesFromTheTopEvenWhenTheyAreNested() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();

            app.Navigate<FavoritesViewModel>();

            // VPN lives inside Work, so the Secrets tree indents it one level
            var name = app.TreeRow("VPN")
                .GetVisualDescendants()
                .OfType<Border>()
                .Single(b => b.Name == "BorderName");
            name.Margin.Left.Should().Be(0);
        });

    [Fact]
    public Task Mosaic_ShowsFavoritesUnderTheirOwnRoot() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync(DataViewMode.MosaicView);

            app.Navigate<FavoritesViewModel>();

            app.BreadCrumb().Should().Equal("Favorites");
            app.CardNames("DirectoriesList").Should().Equal("Banking");
            app.CardNames("FilesList").Should().Equal("VPN");
            app.Cards("FilesList").Should().OnlyContain(c => c.Star() == IconVariant.Filled);
        });

    [Fact]
    public Task StarringAnItemInSecrets_AddsItToFavoritesAndSavesIt() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();
            var secrets = app.Navigate<SecretsViewModel>();

            secrets.AllItems.Single(i => i.Name == "Work").FavoriteCommand.Execute(null);
            app.Render();

            app.TreeRow("Work").Star().Should().Be(IconVariant.Filled);

            app.Navigate<FavoritesViewModel>();
            app.TreeNames().Should().Equal("Banking", "Work");

            app.Services.GetRequiredService<LoggedUserStore>()
                .Favorites.Should()
                .Contain(Path.Combine(app.UserFolder, "Work"));
        });

    [Fact]
    public Task UnstarringAFavorite_RemovesItFromTheFavoritesView() =>
        HeadlessTest.Run(async () =>
        {
            using var app = await AppHarness.StartAsync();
            var favorites = app.Navigate<FavoritesViewModel>();

            favorites.AllItems.Single(i => i.Name == "Banking").FavoriteCommand.Execute(null);
            app.Render();

            app.TreeNames().Should().Equal("VPN");
        });
}

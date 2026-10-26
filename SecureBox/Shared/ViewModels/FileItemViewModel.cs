using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM.Core.ViewModels;
using SecureBox.Config;
using SecureBox.Domain.Models;

namespace SecureBox.ViewModels;

public sealed partial class FileItemViewModel(FileItem fileItem) : ViewModelBase
{
    public string Name => fileItem.Name;
    public string FilePath => fileItem.FilePath;
    // Path shown to the user, relative to the vault, so it starts with the user folder
    public string RelativePath => Path.GetRelativePath(VaultConfig.Instance.Vault, fileItem.FilePath);
    public string Type => fileItem.Type;
    public string Extension => fileItem.Extension;
    public long? Size => fileItem.Size;

    [ObservableProperty]
    public partial bool IsFavorite { get; set; } = fileItem.IsFavorite;
    public event Action<FileItemViewModel>? FavoriteChanged;

    partial void OnIsFavoriteChanged(bool value)
    {
        OnPropertyChanged(nameof(FavoriteDepth));
        FavoriteChanged?.Invoke(this);
    }

    public int Depth => fileItem.Depth;
    public int FavoriteDepth
    {
        get
        {
            if (Parent is null || Parent.FavoriteDepth == -1)
                return IsFavorite ? 0 : -1;

            return Parent.FavoriteDepth + 1;
        }
    }

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;

    public FileItemViewModel? Parent { get; set; }
    public List<FileItemViewModel> SubItems { get; set; } = [];

    [RelayCommand]
    private void Favorite()
    {
        IsFavorite = !IsFavorite;
    }

    public bool Filter(string? filter)
    {
        IsVisible =
            string.IsNullOrEmpty(filter)
            || Name.Contains(filter, StringComparison.OrdinalIgnoreCase);

        if (!IsVisible)
        {
            foreach (FileItemViewModel item in SubItems ?? [])
            {
                IsVisible = item.Filter(filter) || IsVisible;
            }
        }
        else
        {
            ChangeSubItemsVisibility(true);
        }

        return IsVisible;
    }

    private void ChangeSubItemsVisibility(bool visibility)
    {
        if (SubItems is null)
            return;

        foreach (FileItemViewModel item in SubItems ?? [])
        {
            item.IsVisible = visibility;
            item.ChangeSubItemsVisibility(visibility);
        }
    }
}

using MVVM.Core.ViewModels;

namespace MVVM.Core.Stores;

public interface INavigationStore
{
    public ViewModelBase? CurrentViewModel { get; set; }

    public event Action? CurrentViewModelChanged;
}

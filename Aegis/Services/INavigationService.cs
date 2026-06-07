using Aegis.ViewModels;

namespace Aegis.Services;

public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    void NavigateTo<TViewModel>(TViewModel viewModel) where TViewModel : ViewModelBase;
}
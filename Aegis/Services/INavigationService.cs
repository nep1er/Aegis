using Aegis.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aegis.Services
{
    public interface INavigationService
    {
        void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    }
}

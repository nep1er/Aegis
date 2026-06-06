using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aegis.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private string _title = "Главная";

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
    }
}

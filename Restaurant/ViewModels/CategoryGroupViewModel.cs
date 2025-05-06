using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Restaurant.Models;

namespace Restaurant.ViewModels
{
    public class CategoryGroupViewModel
    {
        public string Name { get; }
        public ObservableCollection<DishItemViewModel> Dishes { get; }
        public ObservableCollection<CompositeMenuViewModel> Menus { get; }

        public CategoryGroupViewModel(Category cat, IConfiguration config)
        {
            Name = cat.Name;

            // Dishes
            Dishes = new ObservableCollection<DishItemViewModel>(
                cat.Dishes.Select(d => new DishItemViewModel(d, config))
            );

            // Menus
            Menus = new ObservableCollection<CompositeMenuViewModel>(
                cat.Menus.Select(m => new CompositeMenuViewModel(m, config))
            );
        }
    }

}

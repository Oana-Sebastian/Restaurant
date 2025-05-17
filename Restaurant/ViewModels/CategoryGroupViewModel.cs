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
        public string CategoryName { get; }
        public ObservableCollection<DishItemViewModel> Dishes { get; }
        public ObservableCollection<MenuListItemViewModel> Menus { get; }

        public CategoryGroupViewModel(Category cat, IConfiguration config)
        {
            CategoryName = cat.Name;

            
            Dishes = new ObservableCollection<DishItemViewModel>(
                cat.Dishes.Select(d => new DishItemViewModel(d, config))
            );


            
            Menus = new ObservableCollection<MenuListItemViewModel>(
                cat.Menus.Select(m => new MenuListItemViewModel(m, config))
            );
        }
    }

}

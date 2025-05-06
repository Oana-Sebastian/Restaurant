using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.ViewModels
{
    public class MenuItemDto : INotifyPropertyChanged
    {
        public int DishId { get; }
        public string DishName { get; }

        private int _portionGrams;
        public int PortionGrams
        {
            get => _portionGrams;
            set { _portionGrams = value; OnPropertyChanged(); }
        }

        public MenuItemDto(int dishId, string dishName, int initialPortion)
        {
            DishId = dishId;
            DishName = dishName;
            _portionGrams = initialPortion;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string p = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

}

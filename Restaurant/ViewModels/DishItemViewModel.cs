using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Configuration;
using Restaurant.Models;

namespace Restaurant.ViewModels
{
    public class DishItemViewModel
    {
        public string Name { get; }
        public string PortionDisplay { get; }
        public decimal Price { get; }
        public string AllergensDisplay { get; }
        public string[] ImageUrls { get; }
        public ObservableCollection<BitmapImage> ImageSources { get; }
        public bool IsAvailable { get; }
        public string AvailabilityText => IsAvailable ? "" : "Indisponibil";

        public DishItemViewModel(Dish d, IConfiguration config)
        {

            ImageSources = new ObservableCollection<BitmapImage>();
            Name = d.Name;
            PortionDisplay = $"{d.PortionQuantity}g";         
            Price = d.Price;
            AllergensDisplay = d.DishAllergens != null
            ? string.Join(", ",
                d.DishAllergens
                 .Select(da => da.Allergen?.Name ?? "")
                 .Where(n => !string.IsNullOrEmpty(n))
              )
            : string.Empty;
            ImageUrls = d.Images.Select(i => i.Url).ToArray();  
            IsAvailable = d.TotalQuantity >= d.PortionQuantity;
            if (d.Images != null)
            {
                foreach (var img in d.Images)
                {
                    var outputDir = AppDomain.CurrentDomain.BaseDirectory;
                    var fullPath = Path.Combine(outputDir, "Images", img.Url);

                    if (!File.Exists(fullPath))
                        continue;

                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();

                    ImageSources.Add(bmp);
                }
            }
        }
    }
}

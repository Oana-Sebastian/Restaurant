using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Restaurant.Models;
using Restaurant.ViewModels;

namespace Restaurant.Helpers
{
    public static class SearchService
    {
       
        public static IList<CategoryGroupViewModel> Filter(
            IEnumerable<CategoryGroupViewModel> allGroups,
            string query,
            int minResults = 0)
        {
            if (string.IsNullOrWhiteSpace(query))
                return allGroups
                    .Select(g => Clone(g))
                    .ToList();

            query = query.Trim().ToLowerInvariant();
            var filtered = new List<CategoryGroupViewModel>();

            foreach (var group in allGroups)
            {
                var g2 = new CategoryGroupViewModel(
                    new Category { Name = group.CategoryName, Dishes = Array.Empty<Dish>(), Menus = Array.Empty<Menu>() },
                    null! 
                );
                g2.Dishes.Clear();
                g2.Menus.Clear();

                bool isAllergenQuery =
                    group.Dishes
                        .SelectMany(d => d.AllergensDisplay.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        .Concat(group.Menus
                            .SelectMany(m => m.MenuAllergensDisplay.Split(',', StringSplitOptions.RemoveEmptyEntries)))
                        .Any(a => a.Trim().Equals(query, StringComparison.OrdinalIgnoreCase));

                if (isAllergenQuery)
                {
                   
                    foreach (var d in group.Dishes)
                        if (!d.AllergensDisplay
                              .Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Any(a => a.Trim().Equals(query, StringComparison.OrdinalIgnoreCase)))
                            g2.Dishes.Add(d);

                    foreach (var m in group.Menus)
                        if (!m.MenuAllergensDisplay
                              .Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Any(a => a.Trim().Equals(query, StringComparison.OrdinalIgnoreCase)))
                            g2.Menus.Add(m);
                }
                else
                {

                    foreach (var d in group.Dishes)
                        if (d.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                            g2.Dishes.Add(d);

                    foreach (var m in group.Menus)
                        if (m.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                            g2.Menus.Add(m);
                }

                if (g2.Dishes.Any() || g2.Menus.Any())
                    filtered.Add(g2);
            }

            if (!filtered.Any() && minResults > 0)
            {

                var placeholder = new CategoryGroupViewModel(
                    new Category
                    {
                        Name = "⚠ Niciun rezultat",
                        Dishes = Array.Empty<Dish>(),
                        Menus = Array.Empty<Menu>()
                    },
                    null!);
                filtered.Add(placeholder);
            }

            return filtered;
        }

        private static CategoryGroupViewModel Clone(CategoryGroupViewModel src)
        {
            var copy = new CategoryGroupViewModel(
                new Category
                {
                    Name = src.CategoryName,
                    Dishes = Array.Empty<Dish>(),
                    Menus = Array.Empty<Menu>()
                },
                null!);

            foreach (var d in src.Dishes)
                copy.Dishes.Add(d);
            foreach (var m in src.Menus)
                copy.Menus.Add(m);
            return copy;
        }
    }
}

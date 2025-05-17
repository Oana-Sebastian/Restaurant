using System.Windows;
using System.Windows.Controls;

namespace Restaurant.Helpers
{
    public static class PasswordHelper
    {
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                typeof(string),
                typeof(PasswordHelper),
                new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

        public static string GetBoundPassword(DependencyObject obj)
        {
            return (string)obj.GetValue(BoundPasswordProperty);
        }

        public static void SetBoundPassword(DependencyObject obj, string value)
        {
            obj.SetValue(BoundPasswordProperty, value);
        }

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[PasswordHelper] Attached to PasswordBox");

            if (d is PasswordBox box)
            {
               
                box.PasswordChanged -= HandlePasswordChanged;

               
                if (box.Password != (string)e.NewValue)
                {
                    box.Password = (string)e.NewValue;
                }

                box.PasswordChanged += HandlePasswordChanged;
            }
        }

        private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox box)
            {
                SetBoundPassword(box, box.Password);
                System.Diagnostics.Debug.WriteLine($"[PasswordHelper] PasswordBox changed → {box.Password}");
            }
        }
    }
}

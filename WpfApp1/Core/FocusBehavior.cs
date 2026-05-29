using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace WpfApp1.Core
{
    public class FocusBehavior : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= OnLoaded;
            base.OnDetaching();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Focus();
        }
    }
}

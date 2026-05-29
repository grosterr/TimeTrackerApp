using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1.Core
{
    public class PasswordBindingBehavior : Behavior<PasswordBox>
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register("Password", typeof(string), typeof(PasswordBindingBehavior), new PropertyMetadata(string.Empty, OnPasswordPropertyChanged));

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBindingBehavior behavior)
            {
                if (behavior.AssociatedObject != null && behavior.AssociatedObject.Password != (string)e.NewValue)
                {
                    behavior.AssociatedObject.Password = (string)e.NewValue ?? string.Empty;
                }
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PasswordChanged += OnPasswordChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PasswordChanged -= OnPasswordChanged;
            base.OnDetaching();
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (Password != AssociatedObject.Password)
            {
                Password = AssociatedObject.Password;
            }
        }
    }
}

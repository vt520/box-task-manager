using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Box_Task_Manager {
    public class HideIfNull : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            return value is null ? Visibility.Collapsed : Visibility.Visible;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            if (value is Visibility visibility) return visibility == Visibility.Visible ? nameof(Visibility.Visible) : nameof(Visibility.Collapsed);
            return false;
        }
    }
}

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Box_Task_Manager {
    public class HideIfZero : IValueConverter {
        public virtual object Convert(object value, Type targetType, object parameter, string language) {
            if(value is null) return Visibility.Collapsed;
            if (double.TryParse(value.ToString(), out double result) && result == (double)0) return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            if (value is Visibility visibility) return visibility == Visibility.Visible ? nameof(Visibility.Visible) : nameof(Visibility.Collapsed);
            return false;
        }
    }
}

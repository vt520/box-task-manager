using System;
using System.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Box_Task_Manager {
    public class HideIfEmpty : IValueConverter {
        public virtual object Convert(object value, Type targetType, object parameter, string language) {
            if (value is null) return Visibility.Collapsed;
            if (value is IEnumerable objects) {
                IEnumerator enumerator = objects.GetEnumerator();
                enumerator.Reset();
                if (enumerator.MoveNext() == false) return Visibility.Collapsed;
                return Visibility.Visible;
            }
            if (value.ToString().Trim().Length == 0) return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            if (value is Visibility visibility) return visibility == Visibility.Visible ? nameof(Visibility.Visible) : nameof(Visibility.Collapsed);
            return false;
        }
    }
}

using System;
using System.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Box_Task_Manager {
    public class LastItem : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            object buffer = null;
            if (value is IEnumerable enumeration) {
                IEnumerator enumerator = enumeration.GetEnumerator();
                while (enumerator.MoveNext()) {
                    buffer = enumerator.Current;
                }
            }
            return buffer;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            //if (value is Visibility visibility) return visibility == Visibility.Visible ? nameof(Visibility.Visible) : nameof(Visibility.Collapsed);
            return null;
        }
    }
}

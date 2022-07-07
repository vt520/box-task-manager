using System;
using Windows.UI.Xaml.Data;

namespace Box_Task_Manager {
    public class IsSingularValueConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            long lvalue = long.Parse(value.ToString());
            return lvalue == 1;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            if (value is bool bvalue) return bvalue ? 1 : 0;
            return -1;
        }
    }
}

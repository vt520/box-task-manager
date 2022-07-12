using System;
using System.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Box_Task_Manager {
    public class ShowIfEmpty:  HideIfEmpty {
        public override object Convert(object value, Type targetType, object parameter, string language) {
            Visibility visibility = (Visibility) base.Convert(value, targetType, parameter, language);
            if(visibility == Visibility.Visible) return Visibility.Collapsed;
            return Visibility.Visible;
        }
    }
}

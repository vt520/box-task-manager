using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
namespace Box_Task_Manager
{
    public class NonZeroValueConverter : IValueConverter 
    {
        public object Convert(object value, Type targetType, object parameter, string language) {
            long lvalue = long.Parse(value.ToString());
            return lvalue != 0;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            if (value is bool bvalue) return bvalue ? 0 : 1;
            return -1;
        }
    }
}

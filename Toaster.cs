using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Box_Task_Manager {
    public static class Toaster {
        public struct Templates {
            public static ToastTemplateType Default = ToastTemplateType.ToastImageAndText04;
        }
        public static XmlDocument GetTemplate(string name) {
            switch(name) {
                case nameof(Templates.Default):
                    return ToastNotificationManager.GetTemplateContent(Templates.Default);
                break;
            }
            return null;
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Box_Task_Manager.View {
    public class APIConfiguration : Base, IAPIConfiguration {
        private static APIConfiguration _instance;
        public string Client_ID { get; }
        public string Client_Secret { get; }
        public string Redirect_URL { get; }
        public APIConfiguration() {
            ResourceLoader resources = ResourceLoader.GetForCurrentView("API");
            Client_ID = resources.GetString(nameof(Client_ID));
            Client_Secret = resources.GetString(nameof(Client_Secret));
            Redirect_URL = resources.GetString(nameof(Redirect_URL));
        }
        public static APIConfiguration Instance { 
            get {
                if (_instance is null) _instance = new APIConfiguration();
                return _instance; 
            } 
        }
    }
}

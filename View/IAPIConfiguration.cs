using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Box_Task_Manager.View {
    public interface IAPIConfiguration {
        string Client_ID { get; }
        string Client_Secret { get; }
        string Redirect_URL { get; }
    }
}

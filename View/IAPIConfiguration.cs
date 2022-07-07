
namespace Box_Task_Manager.View {
    public interface IAPIConfiguration {
        string Client_ID { get; }
        string Client_Secret { get; }
        string Redirect_URL { get; }
    }
}

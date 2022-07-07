using Box.V2.Models;

namespace Box_Task_Manager.View.Assignments {
    public class Review : Assignment {
        public async void AcceptTask() {
            await Main.Client.TasksManager.UpdateTaskAssignmentAsync(new BoxTaskAssignmentUpdateRequest {
                Id = BoxTaskAssignment.Id,
                ResolutionState = ResolutionStateType.approved
            });


        }
        public async void RejectTask() {
            await Main.Client.TasksManager.UpdateTaskAssignmentAsync(new BoxTaskAssignmentUpdateRequest {
                Id = BoxTaskAssignment.Id,
                ResolutionState = ResolutionStateType.rejected
            });
        }
    }
}

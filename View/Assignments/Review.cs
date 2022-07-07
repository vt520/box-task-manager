using Box.V2.Models;
using System;
using Windows.UI.Popups;

namespace Box_Task_Manager.View.Assignments {
    public class Review : Assignment {
        public async void AcceptTask() {
            try {
                await Main.Client.TasksManager.UpdateTaskAssignmentAsync(new BoxTaskAssignmentUpdateRequest {
                    Id = BoxTaskAssignment.Id,
                    ResolutionState = ResolutionStateType.approved
                });
            } catch (Exception exception) {
                await (new MessageDialog(exception.Message)).ShowAsync();
                return;
            }

        }
        public async void RejectTask() {
            try {
                await Main.Client.TasksManager.UpdateTaskAssignmentAsync(new BoxTaskAssignmentUpdateRequest {
                    Id = BoxTaskAssignment.Id,
                    ResolutionState = ResolutionStateType.rejected
                });
            } catch (Exception exception) {
                await (new MessageDialog(exception.Message)).ShowAsync();
                return;
            }
        }
    }
}

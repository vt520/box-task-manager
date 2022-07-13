using Box.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace Box_Task_Manager.View.Assignments {
    public class Review : Assignment {
        public async Task AcceptTask() {
            try {
                await Main.Client.TasksManager.UpdateTaskAssignmentAsync(new BoxTaskAssignmentUpdateRequest {
                    Id = BoxTaskAssignment.Id,
                    ResolutionState = ResolutionStateType.approved
                });
                IEnumerable<TaskEntry> existing_tasks = Locator.Instance.Tasks.ToList();
                foreach (TaskEntry task in existing_tasks) {
                    if (task.Assignments.Where(assignment => assignment.Id == BoxTaskAssignment.Id).Any()) {
                        task.Completed = true;
                    }
                }

            } catch (Exception exception) {
                await (new MessageDialog(exception.Message)).ShowAsync();
                return;
            }

        }
        public async Task RejectTask() {
            try {
                await Main.Client.TasksManager.UpdateTaskAssignmentAsync(new BoxTaskAssignmentUpdateRequest {
                    Id = BoxTaskAssignment.Id,
                    ResolutionState = ResolutionStateType.rejected
                });
                IEnumerable<TaskEntry> existing_tasks = Locator.Instance.Tasks.ToList();
                foreach (TaskEntry task in existing_tasks) {
                    if (task.Assignments.Where(assignment => assignment.Id == BoxTaskAssignment.Id).Any()) {
                        task.Completed = true;
                    }
                }

            } catch (Exception exception) {
                await (new MessageDialog(exception.Message)).ShowAsync();
                return;
            }
        }
    }
}

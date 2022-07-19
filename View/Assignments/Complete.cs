using Box.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace Box_Task_Manager.View.Assignments {
    public class Complete : Assignment {
        public async Task CompleteTask() {
            try {
                await Main.Client.TasksManager.UpdateTaskAssignmentAsync(new BoxTaskAssignmentUpdateRequest {
                    Id = BoxTaskAssignment.Id,
                    ResolutionState = ResolutionStateType.completed
                });
                IEnumerable<TaskEntry> existing_tasks = Locator.Instance.Tasks.ToList();
                foreach (TaskEntry task in existing_tasks) {
                    if (task.Assignments.Where(assignment => assignment.Id == BoxTaskAssignment.Id).Any()) {
                        task.Completed = true;
                    }
                }
                if (Locator.Instance.Tasks.Count == 0) await App.Minimize();
            } catch (Exception exception) {
                await (new MessageDialog(exception.Message)).ShowAsync();
                return;
            }
        }
    }
}

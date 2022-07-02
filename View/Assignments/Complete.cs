using Box.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Box_Task_Manager.View.Assignments {
    public class Complete : Assignment {
        public async void CompleteTask() {
            await Main.Client.TasksManager.UpdateTaskAssignmentAsync(new BoxTaskAssignmentUpdateRequest {
                Id = BoxTaskAssignment.Id,
                ResolutionState = ResolutionStateType.completed
            });
        }
    }
}

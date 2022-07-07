using Box.V2.Models;
using Box_Task_Manager.View.Assignments;
using System;
using System.Collections.Generic;

namespace Box_Task_Manager.View {
    public class Assignment : Base {
        private BoxTaskAssignment _BoxTaskAssignment;
        public BoxTaskAssignment BoxTaskAssignment {
            get { return _BoxTaskAssignment; }
            set {
                if (_BoxTaskAssignment == value) return;
                _BoxTaskAssignment = value;
                OnPropertyChangedAsync();
                OnPropertyChangedAsync(nameof(Actions));
            }
        }
        public List<Command> Actions {  
            get {
                if (this is Complete assignment_complete) {
                    return new List<Command> {
                        new Command(async _ => {
                            assignment_complete.CompleteTask();

                        }) {Name = "Complete Task"}
                    };
                } else if (this is Review assignment_review) {
                    return new List<Command> {
                        new Command(async _ => {
                            assignment_review.AcceptTask();
                        }) { Name = "Accept Document" },

                        new Command(async _ => {
                            assignment_review.RejectTask();
                        }) { Name = "Reject Document" }
                    };
                }
                return null;
            } 
        }
        public static Assignment InstanceFor(BoxTaskAssignment assignment, string action = "review") {
            switch(action) {
                case "review": 
                    return new Review() { BoxTaskAssignment = assignment };
                case "complete":
                    return new Complete() { BoxTaskAssignment = assignment };
                default:
                    throw new EntryPointNotFoundException();
            }
        }
    }
}

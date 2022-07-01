using Box.V2.Models;
using Box_Task_Manager.View.Assignments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Box_Task_Manager.View {
    public class Assignment : Base {
        private BoxTaskAssignment _BoxTaskAssignment;
        public BoxTaskAssignment BoxTaskAssignment {
            get { return _BoxTaskAssignment; }
            set {
                if (_BoxTaskAssignment == value) return;
                _BoxTaskAssignment = value;
                OnPropertyChangedAsync();
            }
        }
        public List<Command> Actions {  
            get {
                if(this is Complete assignment_complete) {

                } else if (this is Review assignment_review) {

                }
                return null;
            } 
        }
        public static Assignment InstanceFor(BoxTaskAssignment assignment, string action = "review") {
            switch(action) {
                case "review": 
                    return new Review() { BoxTaskAssignment = assignment };
                case "complete":
                    return new Review() { BoxTaskAssignment = assignment };
                default:
                    throw new EntryPointNotFoundException();
            }
        }
    }
}

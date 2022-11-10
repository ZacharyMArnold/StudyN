﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudyN.Utilities
{
    public class StudynEvent
    {
        public enum StudynEventType
        {
            // Task Events
            AddTask,
            EditTask,
            DeleteTask,
            CompleteTask,
            // Appointment Event
            AppointmentAdd,
            AppointmentEdit,
            AppointmentDelete,
            AppointmentSave
        }

        public StudynEvent(Guid id, StudynEventType eventType)
        {
            Id = id;
            EventType = eventType;
        }


        public Guid Id { get; set; }
        public StudynEventType EventType { get; set; }
    }
}

﻿using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using System;

namespace StudyN.Models
{
    public class CalendarTask
    {
        public CalendarTask(string Text)
        {
            this.Text = Text;
        }

        public bool Completed { get; set; }
        public int Id { get; set; }
        public string Text { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public int TimeNeeded { get; set; }
    }

    public class CalendarTasksData
    {
        void GenerateCalendarTaskss()
        {
            ObservableCollection<CalendarTask> result = new ObservableCollection<CalendarTask>();
            result.Add(
                new CalendarTask("HW: Pitch your Application Idea")
                {
                    Completed = true,
                    Id = 1,
                    Description = "Pitch your appilcation idea...",
                    DueDate = DateTime.Today,
                    TimeNeeded = 3
                }
            );
            result.Add(
                new CalendarTask("HW: Technology Proof of Concept")
                {
                    Completed = false,
                    Id = 2,
                    Description = "Prove your technology works...",
                    DueDate = DateTime.Today,
                    TimeNeeded = 7
                }
            );
            result.Add(
                new CalendarTask("HW: Prototype of Key Features")
                {
                    Completed = false,
                    Id = 3,
                    Description = "Build a prototype of the feature...",
                    DueDate = DateTime.Today,
                    TimeNeeded = 5
                }
            );
            CalendarTasks = result;
        }

        public ObservableCollection<CalendarTask> CalendarTasks { get; private set; }

        public CalendarTasksData()
        {
            GenerateCalendarTaskss();
        }
    }
}

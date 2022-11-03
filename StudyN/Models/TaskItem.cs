﻿namespace StudyN.Models
{
    public class TaskItem
    {
        public TaskItem(string name,
                        string description,
                        DateTime dueTime,
                        int priority,
                        int completionProgress,
                        int totalTimeNeeded,
                        string recur)
        {
            this.Name = name;
            this.Description = description;
            this.DueTime = dueTime;
            this.Priority = priority;
            this.CompletionProgress = completionProgress;
            this.TotalTimeNeeded = totalTimeNeeded;
            this.Recur = recur;
            this.IsRecur = true;
        }

        public bool Completed { get; set; } = false;
        public Guid TaskId { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; } = "";
        public DateTime DueTime { get; set; }
        public int CompletionProgress { get; set; } = 0;
        public int TotalTimeNeeded { get; set; } = 0;
        public int Priority { get; set; } = 3;

        public string Recur { get; set; } = "";

        public bool IsRecur { get; set; } = false;


        //Method for each task %
        public double? Percent
        {
            get
            {
                if (TotalTimeNeeded != 0)
                {
                    double percentage = (double)CompletionProgress / (double)TotalTimeNeeded;
                    if (percentage == Double.NaN)
                        return 0;
                    else
                        return percentage;
                }
                else
                    //If the total time is empty then it does not display %
                    return null;
            }
        }
    }
}



namespace StudyN.Views;

using Java.Security;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Maui.Animations;
using StudyN.Models;
using StudyN.Utilities;
using StudyN.ViewModels;
using static Android.Util.EventLogTags;
using static Android.Provider.Settings;
using Android.Renderscripts;
using DevExpress.CodeParser;

public partial class AddTaskPage : ContentPage
{
    bool editingExistingTask;
    public AddTaskPage()
    {
        InitializeComponent();
        //autoScheduler = new AutoScheduler(GlobalTaskData.TaskManager.TaskList, GlobalAppointmentData.CalendarManager.Appointments);
        AutoScheduler autoScheduler = new AutoScheduler();

        completeButton.IsEnabled = false;
        trashButton.IsEnabled = false;

        //This will check if we are editing an existing task or making a new one. We will know this based on if ToEdit is null or not
        if (GlobalTaskData.ToEdit != null)
        {
            //If we are editing, we need to set the title and load in the values of the task
            Title = "Edit Task";
            LoadValues();
            BindingContext = new EditTaskViewModel();
            completeButton.IsEnabled = true;
            trashButton.IsEnabled = true;
            editingExistingTask = true;
            //CreateDummyTaskTimeLogData();
            TimeListLog.ItemsSource = GlobalTaskData.ToEdit.TimeList;
            this.displayLabel.Text = String.Format("Priority: " + GlobalTaskData.ToEdit.Priority);

        }
        else
        {
            //If we are just creating a new task, we need to set the title and set the time and date so they are not null
            Title = "Add Task";
            completeButton.IsEnabled = false;
            trashButton.IsEnabled = false;
            editingExistingTask = false;
            SetValues();            
        }

        //If we are editing a task, the delete and edit buttons will be visable. If not, then invisable

        //Makes timer button visible
        TimerButton.IsVisible = editingExistingTask;

        //Make task time log visible
        LogTitle.IsVisible = editingExistingTask;
        LogStart.IsVisible = editingExistingTask;
        LogStop.IsVisible = editingExistingTask;
        LogDuration.IsVisible = editingExistingTask;
        TimeListLog.IsVisible = editingExistingTask;
        //checks text of timer button. If it's not being tracked we want to see 
        //track task. Otherwise we want to see stop tracking
        SetTimerButton();
    }   

    void SetTimerButton()
    {
        if (editingExistingTask)
        {
            Guid currenttaskid = GlobalTaskData.ToEdit.TaskId;
            Guid taskbeingtimed = GlobalTaskTimeData.TaskTimeManager.TaskidBeingTimed;
            //Console.WriteLine("ALERT ALERT ALERT ");
            //if no task is being tracked or if the task in edit isn't being tracked
            if (!GlobalTaskTimeData.TaskTimeManager.BeingTimed || currenttaskid != taskbeingtimed)
            {
                TimerButton.Text = "Track Task";
                //Console.WriteLine("ALERT Setting text to track task");
            }
            //if a task is being tracked and this is the task being tracked
            else
            {
                //Console.WriteLine("ALTERT setting text to stop tracking");
                TimerButton.Text = "Stop Tracking";
            }
        }
    }


    async void HandleTimerOnOff(object sender, EventArgs args)
    {
        //gets guid and name of the current task.
        Guid currenttaskid = GlobalTaskData.ToEdit.TaskId;
        String taskname = GlobalTaskData.ToEdit.Name;
        Guid taskidtimed = GlobalTaskTimeData.TaskTimeManager.TaskidBeingTimed;
        bool currentlytiming = GlobalTaskTimeData.TaskTimeManager.BeingTimed;
        //gets the current time
        DateTime gettime = DateTime.Now;

        //Checks if other task is being timed. If it is we want to send an alert to turn off
        //timing of the other task May make popup window have buttons that does this for user
        if (currentlytiming)
        {
            //if the task is being time and the current task id matches the task being timed
            if(currenttaskid == taskidtimed)
            {
                TimerButton.Text = "Track Task";
                GlobalTaskTimeData.TaskTimeManager.StopCurrent(gettime);
                AlertUserOfTimeSpent();
                await Shell.Current.GoToAsync("..");
            }
            //send alert to user that a different task is being tracked
            else
            {
                AlertUserTaskTracking(gettime, currenttaskid);
            }
        } else {
            //update button
            TimerButton.Text = "Stop Tracking";
            //start new timer
            GlobalTaskTimeData.TaskTimeManager.StartNew(gettime, currenttaskid,taskname);
        }
    }


    async void AlertUserOfTimeSpent()
    {
        //gets name of task being timed and time spent on task
        String taskname = GlobalTaskTimeData.TaskTimeManager.TaskName;
        int timespent = GlobalTaskTimeData.TaskTimeManager.taskitemtime.span.Minutes;
        //Informational alert and alert string.
        String alertstr = "You spent " + timespent + " minutes on task " + taskname;
        await DisplayAlert("Great Job!", alertstr, "OK");
    }

    private async void AlertUserTaskTracking(DateTime gettime, Guid currenttaskid)
    {
        //strings for alert
        String tasknameInEdit = GlobalTaskData.ToEdit.Name;
        String tasknameTimed = GlobalTaskTimeData.TaskTimeManager.TaskName;
        //alert currently tracking
        string alertstr = "Would you like to stop tracking task " +  tasknameTimed
        + " and begin tracking " + tasknameInEdit;
        bool tracknew = await DisplayAlert("Task Already Being Tracked", alertstr, "Yes", "No");
        //if user wants to stop tracking old and start tracking new
        if (tracknew)
        {
            TimerButton.Text = "Stop Tracking";
            GlobalTaskTimeData.TaskTimeManager.StopCurrent(gettime);
            AlertUserOfTimeSpent();
            GlobalTaskTimeData.TaskTimeManager.StartNew(gettime, currenttaskid, tasknameInEdit);

        }
    }


    //This function will be used by the delete task button to delete the given task
    private async void HandleDeleteTaskClicked(object sender, EventArgs args)
    {
        //gets variables for check
        bool beingtimed = GlobalTaskTimeData.TaskTimeManager.BeingTimed;
        Guid taskidTimed= GlobalTaskTimeData.TaskTimeManager.TaskidBeingTimed;
        Guid taskidInEdit = GlobalTaskData.ToEdit.TaskId;
        //if task is being timed and task in edit is the one being timed
        if (beingtimed && taskidTimed == taskidInEdit)
        {
            //makes string for alert
            String tasknameTimed = GlobalTaskTimeData.TaskTimeManager.TaskName;
            string alertstr = "Would you like to stop tracking task " + tasknameTimed + " and delete the task?";
            bool deletetracked = await DisplayAlert("Task Is Being Timed!", alertstr, "Yes", "No");
            //If user confirms delete
            if(deletetracked) {
                GlobalTaskTimeData.TaskTimeManager.StopCurrent(DateTime.Now);
                DeleteTask();
                await Shell.Current.GoToAsync("..");
            } 
        } else {
            DeleteTask();
            await Shell.Current.GoToAsync("..");
        }

    }

    //Deletes the task at hand
    private void DeleteTask()
    {
        //The task manager will be told to delete this task, after which we will set ToEdit to null and return to the previous page
        GlobalTaskData.TaskManager.DeleteTask(GlobalTaskData.ToEdit.TaskId);
        GlobalTaskData.ToEdit = null;
    }

    //This function will be used by the complete task button to "complete" a given task
    private async void HandleCompleteTaskClicked(object sender, EventArgs args)
    {
        //gets variables for check
        bool beingtimed = GlobalTaskTimeData.TaskTimeManager.BeingTimed;
        Guid taskidTimed = GlobalTaskTimeData.TaskTimeManager.TaskidBeingTimed;
        Guid taskidInEdit = GlobalTaskData.ToEdit.TaskId;
        //if task is being timed and task in edit is the one being timed
        if (beingtimed && taskidTimed == taskidInEdit)
        {
            //makes string for alert
            String tasknameTimed = GlobalTaskTimeData.TaskTimeManager.TaskName;
            string alertstr = "Would you like to stop tracking task " + tasknameTimed + " and Complete the task?";
            bool deletetracked = await DisplayAlert("Task Is Being Timed!", alertstr, "Yes", "No");
            //If user confirms delete
            if (deletetracked)
            {
                GlobalTaskTimeData.TaskTimeManager.StopCurrent(DateTime.Now);
                CompleteTask();
                await Shell.Current.GoToAsync("..");
            }
        }
        else
        {
            CompleteTask();
            await Shell.Current.GoToAsync("..");
        }

    }

    private void CompleteTask()
    {
        //The task manager will be told to "complete" this task, after which we will set ToEdit to null and return to the previous page
        GlobalTaskData.TaskManager.CompleteTask(GlobalTaskData.ToEdit.TaskId);
        GlobalTaskData.ToEdit = null;
    }

    //This function will be used by the priority slider when its value has changed to set and keep track of the new value
    void HandleSliderValueChanged(object sender, ValueChangedEventArgs args)
    {
        //Stroring the new value and setting the sliders label correctly
        int value = (int)args.NewValue;
        displayLabel.Text = String.Format("Priority: " + value);
    }

    //This function will be used by the add task button to either create a new task or save the changes to an existing one
    private async void HandleAddTaskButton(object sender, EventArgs e)
    {
        // Make sure we aren't storing nulls
        this.name.Text = this.name.Text == null ? "Unnamed Task" : this.name.Text;
        this.description.Text = this.description.Text == null ? "" : this.description.Text;
        int hoursLogged = this.hSpent.Value == null ? 0 : (int)this.hSpent.Value;
        int minutesLogged = this.mSpent.Value == null ? 0 : (int)this.mSpent.Value;
        int totalHours = this.hComplete.Value == null ? 0 : (int)this.hComplete.Value;
        int totalMinutes = this.mComplete.Value == null ? 0 : (int)this.mComplete.Value;
        this.date.Date = this.date.Date == null ? DateTime.Now.AddYears(1) : this.date.Date;
        this.time.Time = this.time.Time == null ? DateTime.Now.AddYears(1) : this.time.Time;
        // Turn logged time and total time into time doubles
        double timeLogged = GlobalTaskData.TaskManager.SumTimes(hoursLogged, minutesLogged);
        double totalTime = GlobalTaskData.TaskManager.SumTimes(totalHours, totalMinutes);

        DateTime dateTime = new DateTime(this.date.Date.Value.Year, this.date.Date.Value.Month, this.date.Date.Value.Day,
            this.time.Time.Value.Hour, this.time.Time.Value.Minute, this.time.Time.Value.Second);

        TaskItem task;

        //Check to see if we are currently editing or adding a task
        if (editingExistingTask)
        {
            //Gets task list
            ObservableCollection<TaskItem> taskList = new ObservableCollection<TaskItem>();
            for (int i = 0; i < taskList.Count; i++)
            {
                //If information is not the same, then it gets saved for all
                if (taskList[i].Description != this.description.Text)
                {
                    taskList[i].Description = this.description.Text;
                }
                if (taskList[i].DueTime != dateTime)
                {
                    taskList[i].DueTime = dateTime;
                }
                if (taskList[i].CompletionProgress != timeLogged)
                {
                    taskList[i].CompletionProgress = timeLogged;
                }
                if (taskList[i].TotalTimeNeeded != totalTime)
                {
                    taskList[i].TotalTimeNeeded = totalTime;
                }
            }
            //Saves the informatiom when editing
            GlobalTaskData.TaskManager.EditTask(
                GlobalTaskData.ToEdit.TaskId,
                this.name.Text,
                this.description.Text,
                dateTime,
                (int)this.priority.Value,
                timeLogged,
                totalTime);
            task = GlobalTaskData.ToEdit;
            GlobalTaskData.ToEdit = null;
            editRecurringTasks(task);
        }
        else
        {
            //If we are not editing, use TaskManager's AddTask function to create and save the task
            task = GlobalTaskData.TaskManager.AddTask(
                    this.name.Text,
                    this.description.Text,
                    dateTime,
                    (int)this.priority.Value,
                    timeLogged,
                    totalTime);
        }



        // Handles recurrence after everything is added into the task
        this.recurrenceDate.Date = this.recurrenceDate.Date == null ? DateTime.Now : this.recurrenceDate.Date;
        //if buttons are checked

        if (dailyRadioButton.IsChecked || weeklyRadioButton.IsChecked || monthlyRadioButton.IsChecked)
        {
            DateTime recurrencedateTime = new DateTime(this.recurrenceDate.Date.Value.Year,
                                           this.recurrenceDate.Date.Value.Month,
                                           this.recurrenceDate.Date.Value.Day);
            //and date for end of recurrence is after this current moment (otherwise recurrence doesn't matter
            if (this.recurrenceDate.Date > DateTime.Now)
            {
                Console.WriteLine(this.recurrenceDate);
                if (dailyRadioButton.IsChecked == true)
                {
                    HandleRecurrenceDay(sender, e, task, recurrencedateTime);
                }
                else if (weeklyRadioButton.IsChecked == true)
                {
                    HandleRecurrenceWeek(sender, e, task, recurrencedateTime);
                }
                else if (monthlyRadioButton.IsChecked == true)
                {
                    HandleRecurrenceMonth(sender, e, task, recurrencedateTime);
                }
            } else { //if recurrence date is null send user alert failure to recurr 
                await DisplayAlert("Recurrance End Date Not Set! ",
               "Sorry you must set a recurrence end date in order " +
               "to schedule recurrence. Please try again.", "OK");
            }

        }



        //Returning to the previous page
        await Shell.Current.GoToAsync("..");

        runAutoScheduler(task.TaskId);
    }

    //This function will load the values held in each field of a task into the respective forms
    void LoadValues()
    {
        this.name.Text = GlobalTaskData.ToEdit.Name;
        this.description.Text = GlobalTaskData.ToEdit.Description;
        this.date.Date = (GlobalTaskData.ToEdit.DueTime.Date);
        this.time.Time = GlobalTaskData.ToEdit.DueTime;
        this.priority.Value = (GlobalTaskData.ToEdit.Priority);
        this.hComplete.Value = (int)GlobalTaskData.ToEdit.TotalTimeNeeded;
        this.mComplete.Value = GlobalTaskData.ToEdit.GetTotalMinutesNeeded();
        this.hSpent.Value = (int)GlobalTaskData.ToEdit.CompletionProgress;
        this.mSpent.Value = GlobalTaskData.ToEdit.GetCompletionProgressMinutes();
    }

    //This function will set the date and time forms to the current time
    void SetValues()
    {
        this.date.Date = null;
        this.time.Time = null;
        this.recurrenceDate.Date = null;
    }

    void runAutoScheduler(Guid taskId)
    {
        GlobalAutoScheduler.AutoScheduler.run(taskId);
        if (GlobalAutoScheduler.AutoScheduler.taskPastDue)
        {
            string tasksString = "";
            foreach (TaskItem task in GlobalAutoScheduler.AutoScheduler.pastDueTasks)
            {
                tasksString += task.Name + ", ";
            } 
            DisplayAlert("The following tasks cannot be completed on-time!", tasksString, "OK");
        }
    }

    //These functions will be used to add recurrence of a selected task for day/week/month
    private void HandleRecurrenceDay(object sender, EventArgs e, TaskItem task, DateTime enddate)
    {
        int hoursLogged = this.hSpent.Value == null ? 0 : (int)this.hSpent.Value;
        int minutesLogged = this.mSpent.Value == null ? 0 : (int)this.mSpent.Value;
        int totalHours = this.hComplete.Value == null ? 0 : (int)this.hComplete.Value;
        int totalMinutes = this.mComplete.Value == null ? 0 : (int)this.mComplete.Value;
        double timeLogged = GlobalTaskData.TaskManager.SumTimes(hoursLogged, minutesLogged);
        double totalTime = GlobalTaskData.TaskManager.SumTimes(totalHours, totalMinutes);
        this.date.Date = this.date.Date == null ? DateTime.MaxValue : this.date.Date;
        this.time.Time = this.time.Time == null ? DateTime.MaxValue : this.time.Time;
        DateTime dateTime = new DateTime(this.date.Date.Value.Year, this.date.Date.Value.Month, this.date.Date.Value.Day,
            this.time.Time.Value.Hour, this.time.Time.Value.Minute, this.time.Time.Value.Second);

        while (dateTime <= enddate)
        {
            dateTime = dateTime.AddDays(1); //every day
            //If we are not editing, use TaskManager's AddTask function to create and save the task
            GlobalTaskData.TaskManager.AddTask(
                this.name.Text,
                this.description.Text,
                dateTime,
                (int)this.priority.Value,
                timeLogged,
                totalTime,
                task.TaskId.ToString());
        }
    }
    private void HandleRecurrenceWeek(object sender, EventArgs e, TaskItem task, DateTime enddate)
    {
        int hoursLogged = this.hSpent.Value == null ? 0 : (int)this.hSpent.Value;
        int minutesLogged = this.mSpent.Value == null ? 0 : (int)this.mSpent.Value;
        int totalhours = this.hComplete.Value == null ? 0 : (int)this.hComplete.Value;
        int totalMinutes = this.mComplete.Value == null ? 0 : (int)this.mComplete.Value;
        double timeLogged = GlobalTaskData.TaskManager.SumTimes(hoursLogged, minutesLogged);
        double totalTime = GlobalTaskData.TaskManager.SumTimes(totalhours, totalMinutes);
        this.date.Date = this.date.Date == null ? DateTime.MaxValue : this.date.Date;
        this.time.Time = this.time.Time == null ? DateTime.MaxValue : this.time.Time;
        DateTime dateTime = new DateTime(this.date.Date.Value.Year, this.date.Date.Value.Month, this.date.Date.Value.Day,
            this.time.Time.Value.Hour, this.time.Time.Value.Minute, this.time.Time.Value.Second);

        while (dateTime <= enddate)
        {
            dateTime = dateTime.AddDays(7); //every week
            //If we are not editing, use TaskManager's AddTask function to create and save the task
            GlobalTaskData.TaskManager.AddTask(
                this.name.Text,
                this.description.Text,
                dateTime,
                (int)this.priority.Value,
                timeLogged,
                totalTime,
                task.TaskId.ToString());
        }

    }
    private void HandleRecurrenceMonth(object sender, EventArgs e, TaskItem task, DateTime enddate)
    {
        int hoursLogged = this.hSpent.Value == null ? 0 : (int)this.hSpent.Value;
        int minutesLogged = this.mSpent.Value == null ? 0 : (int)this.mSpent.Value;
        int totalHours = this.hComplete.Value == null ? 0 : (int)this.hComplete.Value;
        int totalMinutes = this.mComplete.Value == null ? 0 : (int)this.mComplete.Value;
        double timeLogged = GlobalTaskData.TaskManager.SumTimes(hoursLogged, minutesLogged);
        double totalTime = GlobalTaskData.TaskManager.SumTimes(totalHours, totalMinutes);
        this.date.Date = this.date.Date == null ? DateTime.MaxValue : this.date.Date;
        this.time.Time = this.time.Time == null ? DateTime.MaxValue : this.time.Time;
        DateTime dateTime = new DateTime(this.date.Date.Value.Year, this.date.Date.Value.Month, this.date.Date.Value.Day,
            this.time.Time.Value.Hour, this.time.Time.Value.Minute, this.time.Time.Value.Second);
        while (dateTime <= enddate)
        {
            dateTime = dateTime.AddMonths(1); //months
            //If we are not editing, use TaskManager's AddTask function to create and save the task
            GlobalTaskData.TaskManager.AddTask(
                this.name.Text,
                this.description.Text,
                dateTime,
                (int)this.priority.Value,
                timeLogged,
                totalTime,
                task.TaskId.ToString());
        }
        
    }

    private void editRecurringTasks(TaskItem toEdit)
    {
        string toComp;
        if(!toEdit.Recur.Equals(""))
        {
            toComp = toEdit.Recur;
        }
        else
        {
            toComp = toEdit.TaskId.ToString();
        }
        foreach (var task in GlobalTaskData.TaskManager.TaskList)
        {
            if(toComp.Equals(task.Recur) || task.TaskId.ToString().Equals(toComp))
            {
                GlobalTaskData.TaskManager.EditTask(
                task.TaskId,
                toEdit.Name,
                toEdit.Description,
                task.DueTime,
                task.Priority,
                toEdit.CompletionProgress,
                toEdit.TotalTimeNeeded);
            }
        }
    }
}

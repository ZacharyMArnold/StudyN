namespace StudyN.Models;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Android.Accessibilityservice.AccessibilityService;
using DevExpress.CodeParser;
using DevExpress.Utils;
using DevExpress.XtraRichEdit.Model.History;
using StudyN.Models;
using StudyN.Utilities;
using static StudyN.Utilities.StudynEvent;

//Because of the minuteMap, this iteration of the autoScheduler doesn't seem to have a need for blocks
public class AutoScheduler : StudynSubscriber
{
    private DateTime baseTime; //The base time the autoscheduler will use to do all its calculations
    ObservableCollection<Appointment> appts;
    ObservableCollection<TaskItem> tasks;
    private minuteSnapshot[] minuteMap;
    public bool taskPastDue;
    public List<TaskItem> pastDueTasks;

    public AutoScheduler()
    {
        taskPastDue = false;
        tasks = GlobalTaskData.TaskManager.TaskList;
        appts = GlobalAppointmentData.CalendarManager.Appointments;
        pastDueTasks = new List<TaskItem>();
        minuteMap = new minuteSnapshot[40320]; //40320 minutes in 4 weeks. AutoScheduler will only scheduler out 4 weeks.
        for(int i = 0; i < minuteMap.Length; i++) { minuteMap[i] = new minuteSnapshot(); }
        baseTime = DateTime.Now;
    }
   
    //Put appointments from the global appointments list into the minute mapping, for future use in scheduling
    private void MapAppointments()
    {
        Console.WriteLine("autoScheduler.MapAppointments()");
        foreach(Appointment appt in appts)
        {
            if (appt.From == "autoScheduler") //If the appointment is from the autoScheduler, delete it from the calendar so we can reschedule it without duplicating it
            {
                Console.WriteLine("rescheduling appointment from autoScheduler");
                //appts.Remove(appt);
                //GlobalAppointmentData.CalendarManager.DeleteAppointment(minuteMap[i].id); //To be able to delete an appointment, the calnedarManager needs a function to do so
            } 
        }

        foreach (Appointment appt in appts)
        {
            DateTime start, end;
            start = appt.Start; end = appt.End;
            if(end < baseTime.AddMinutes(40320) && end > baseTime) //If the appointment falls wholly within the 4 week autoscheduling time frame
            {
                if(start < baseTime) { start = baseTime; } //If appointment is going on right now, treat it as if it starts right now
                int offset = (int)(start - baseTime).TotalMinutes;
                int span = (int)(end - start).TotalMinutes; 
                for(int i = offset; i < offset + span; i++)
                {
                    minuteMap[i].id = appt.UniqueId; 
                    minuteMap[i].from = "appts";
                    minuteMap[i].name = appt.Subject; 
                }
            }
        }
    }

    private List<BlockContainer> CreateBlockContainers()
    {
        List<BlockContainer> blockContainers = new List<BlockContainer>();   
        IOrderedEnumerable<TaskItem> sortedTasks = sortByWeight(); //Because we sort the tasks first, the containers should also be sorted
        foreach(TaskItem task in sortedTasks)
        {
            if (task.DueTime < baseTime.AddMinutes(40320) && task.DueTime > baseTime) //Task is due within the 4 weeks the scheduler will schedule
            { 
                BlockContainer blockContainer = new BlockContainer();
                blockContainer.task = task;
                blockContainer.blockSize = 60; //60 minute block size. Could change this based on task data. IE different task categories could have different block sizes
                blockContainer.blocks = new Block[(int)(task.TotalTimeNeeded*60 / blockContainer.blockSize)];
                blockContainer.remainder = (int)(task.TotalTimeNeeded*60) % blockContainer.blockSize;
                blockContainers.Add(blockContainer);
            }
;       }

        return blockContainers;
    }

    private bool mapConflict(int start, int end) //return true if there would be a conflict if something were to be inserted into the minuteMap between start and end
    {
        for(int i = start; i < end; i++)
        {
            if(minuteMap[i].id != null) { return true; }
        }
        return false;
    }
    private void MapBlocks(List<BlockContainer> blockContainers)
    {
        foreach(BlockContainer blockContainer in blockContainers)
        {
            int offset = (int)(blockContainer.task.DueTime - baseTime).TotalMinutes - blockContainer.blockSize;
            while (blockContainer.mappedBlocks < blockContainer.blocks.Length && offset >= 0)
            {
                if( !mapConflict(offset, offset + blockContainer.blockSize) ) 
                {
                    //match every minute in this span to the block container (aka task). Then create the task block
                    for(int i = offset; i < offset + blockContainer.blockSize; i++) { minuteMap[i].id = blockContainer.task.TaskId; minuteMap[i].from = "autoScheduler"; minuteMap[i].name = blockContainer.task.Name; }

                    blockContainer.blocks[ blockContainer.mappedBlocks ] = new Block(blockContainer.task.TaskId, new Guid(), offset, offset + blockContainer.blockSize);
                    blockContainer.mappedBlocks++;
                }
                offset -= blockContainer.blockSize;
            }

            if(blockContainer.mappedBlocks < blockContainer.blocks.Length) //not all possible blocks were able to be mapped due to conflicts. So there is more remainder time now
            {
                blockContainer.remainder = blockContainer.remainder + (blockContainer.blocks.Length - blockContainer.mappedBlocks) * blockContainer.blockSize;
            }
        }
    }

    private void MapRemainders(List<BlockContainer> blockContainers)
    {
        foreach (BlockContainer blockContainer in blockContainers)
        {
            int offset = (int)(blockContainer.task.DueTime - baseTime).TotalMinutes;
            while (blockContainer.remainder > 0 && offset >= 0)
            {
                if (minuteMap[offset].id == null) //If nothing has been mapped to this spot yet, put it here
                {
                    minuteMap[offset].id = blockContainer.task.TaskId;
                    minuteMap[offset].from = "autoScheduler";
                    minuteMap[offset].name = blockContainer.task.Name;
                    blockContainer.remainder = blockContainer.remainder - 1;
                }
                offset--;
            }

            if(offset < 0) 
            {
                Console.WriteLine("UNSCHEDUABLE TASK");
                pastDueTasks.Add(blockContainer.task);
                taskPastDue = true;
            }
        }
    }

    private void PullBackBlocks(List<BlockContainer> blockContainers) //Pull blocks apart, so they're not all stacked up against the dueDate. Do this only if possible.
    {
        //containers are sorted by weight. So the more important ones come first, meaning the more important ones will be pulled back first (attempted to be placed earlier on the calendar). Which is good.
        foreach (BlockContainer bc in blockContainers)
        {
            Console.WriteLine(bc.task.Name + " has " + bc.mappedBlocks + " blocks.");
            for(int i = 0; i < bc.mappedBlocks; i++) //For each block in the container
            {
                //Clear block from the minute map, and try to place it earlier
                for(int j = bc.blocks[i].start; j < bc.blocks[i].end; j++)
                {
                    minuteMap[j].id = null;
                    minuteMap[j].from = "";
                    minuteMap[j].name = "";
                }

                int offset = 0;
                while( mapConflict(offset, offset + bc.blockSize) && offset < bc.blocks[i].start) //while we cant place it. After the while loop the block will either be placed earlier, or in the same spot it was in originally.
                {
                    offset++;
                }

                //We shouldn't technically NEED to check if the block will be placed within 40320 minutes, because it was already able to placed within that time frame. Worst case is that it just gets put where it already was.
                /*                if( offset > 0 && bc.task.TaskId == minuteMap[offset - 1].id ) //If the minuteMap just before this block has the same id as this block (IE its apart of the same task), we should try to separate them a bit
                                {
                                    while (offset < bc.blocks[i].start)
                                    {
                                        if (offset + bc.blockSize < bc.blocks[i].start && (minuteMap[offset - 1 + bc.blockSize].id != bc.task.TaskId) && !mapConflict(offset + bc.blockSize, offset + 2 * bc.blockSize)) //If it is possible to push the block forward, push it forward by one block size. IE if where we are trying to put it now is earlier than where it was before.
                                        {
                                            Console.WriteLine("spacing out block");
                                            offset = offset + bc.blockSize;
                                            break;
                                        }
                                        offset++;
                                    }
                                }*/

                //ATTEMPT TO SPACE OUT TOUCHING BLOCKS IF THEY ARE FROM SAME TASK
                if(offset > 0)
                {
                    if (minuteMap[offset - 1].id == bc.task.TaskId)
                    {
                        while(mapConflict(offset + bc.blockSize, offset + 2 * bc.blockSize))
                        {
                            offset++; //We are trying to sort block wise. Perhaps this should be offset += blockSize??????
                            if(!mapConflict(offset + bc.blockSize, offset + 2 * bc.blockSize)) 
                            {
                                if (minuteMap[offset - 1 + bc.blockSize].id == bc.task.TaskId) //If theres not a mapConflict, but the block is touching another block of the same task, attempt to push it forward another block size
                                {
                                    offset = offset + bc.blockSize;
                                }
                            }
                        }
                        offset = offset + bc.blockSize;
                    }
                }
                
                if(offset < bc.blocks[i].start){ bc.blocks[i].start = offset; bc.blocks[i].end = offset + bc.blockSize; } //If the block was succesfully spaced out and STILL placed earlier than where it originally was

                //for (int j = offset; j < offset + bc.blockSize; j++) //Place it in minuteMap
                for(int j = bc.blocks[i].start; j < bc.blocks[i].end; j++) //Place it in minuteMap
                {
                    minuteMap[j].id = bc.task.TaskId;
                    minuteMap[j].from = "autoScheduler";
                    minuteMap[j].name = bc.task.Name;
                }
            }
        }
    }

    private void MapTasks()
    {
        List<BlockContainer> containers = CreateBlockContainers();
        MapBlocks(containers);
        MapRemainders(containers);
        PullBackBlocks(containers);
    }


    //All contiguous minute mappings should be transformed into a continous appointment. IE indexes in the minute mapping that are next to each other and have the same Guid should be combined together.
    private List<Appointment> CoalesceMinuteMapping()
    {
        Console.WriteLine("autoScheduler.CoalesceMinuteMapping()");
        List<Appointment> coalescedAppointments = new List<Appointment>();
        Guid? curGuid = null;
        int guidStart = 0;
        for(int i = 0; i < minuteMap.Length; i++)
        {
            if(curGuid != minuteMap[i].id)
            {
                if(i > 0 && minuteMap[i - 1].id != null) 
                {
                    Appointment appt = new Appointment();
                    appt.Start = baseTime.AddMinutes(guidStart);
                    appt.End = baseTime.AddMinutes(i - 1); //The previous index was the last index with the previous guid
                    appt.Subject = minuteMap[i - 1].name;
                    appt.UniqueId = (Guid)minuteMap[i - 1].id;
                    appt.From = minuteMap[i - 1].from;
                    coalescedAppointments.Add(appt);
                }
                curGuid = minuteMap[i].id;
                guidStart = i;
            }
        }

        return coalescedAppointments;
    }

    private void AddToCalendar(List<Appointment> appts)
    {
        Console.WriteLine("autoScheduler adding to calendar");
        foreach(Appointment appt in appts)
        {
            GlobalAppointmentData.CalendarManager.CreateAppointment(-1, appt.Subject, appt.Start, appt.End - appt.Start, -1, appt.UniqueId, "autoScheduler"); //idk what "room" is for CreateAppointment() method
        }
    }

    private IOrderedEnumerable<TaskItem> sortByWeight()
    {
        List<TaskItem> tempTasks = new List<TaskItem>();
        foreach (TaskItem task in tasks)
        {
            TaskItem tempTask = task;
            tempTask.weight = calculateWeight(tempTask);
            tempTasks.Add(tempTask);
        }

        return tempTasks.OrderByDescending(f => f.weight);  //https://stackoverflow.com/questions/11190220/sort-c-sharp-list-of-objects-by-variable        
    }

    private double? calculateWeight(TaskItem task)
    {
        //Higher weight means item of more importance, IE schedule earlier
        double? weight = 1;
        //double remainingMinutesNeeded = task.TotalTimeNeeded * (1 - task.CompletionProgress / 100);   //Assuming task.TotaltimeNeeded is in minutes,
        //and assuming the numerical value of completion
        //progresses represents a percent
        //IE 85 = 85% complete

        double remainingMinutesNeeded = task.TotalTimeNeeded * 60; //completionProgress seems to be bugged, so currently not using. BUT WE SHOULD USE THE ABOVE LINE IDEALLY


        //Amount of days between (now + estimated time remaining to complete task), and task due date
        double leadtime = (task.DueTime - (DateTime.Now.AddMinutes(remainingMinutesNeeded))).TotalDays;

        Console.WriteLine(task.Name + " leadtime: " + leadtime);
        if (leadtime > 0) //Item is possible to complete BEFORE deadline
        {
            weight = weight / leadtime;   //As leadtime trends to infinity, weight trends to 0. Smaller weights have less scheduling importance.
            weight = weight + task.Priority; //Assuming priority is on a scale from 1 to 10, 1 being not important, 10 being very important.
                                             //If an item has a large dueDistance, it will still have a large weight if it has a high priority
        }                                    //Think of it like this: If an item is super far away in dueDistance, essentially it gets sorted by its priority.

        else //Item is NOT possible to complete before deadline. Give it negative weight so it will be scheduled past its deadline, and be detected as unscheduable.
        {
            //taskPastDue = true;
            //pastDueTasks.Add(task);
            weight = null;
        }

        return weight;
    }


    public void run(Guid id)
    {
        Console.WriteLine("Running autoScheduler");
        refreshData();
        MapAppointments();
        MapTasks();
        AddToCalendar( CoalesceMinuteMapping() );
        Console.WriteLine("Done running autoScheduler");

        for (int i = 0; i < minuteMap.Length; i++)
        {
            if (minuteMap[i].id != null)
            {
                Console.WriteLine("Minute: " + i + "  Name:" + minuteMap[i].name);
            }
        }
    }

    private void refreshData()
    {
        taskPastDue = false;
        pastDueTasks = new List<TaskItem>();
        minuteMap = new minuteSnapshot[40320];
        for (int i = 0; i < minuteMap.Length; i++) { minuteMap[i] = new minuteSnapshot(); }
        baseTime = DateTime.Now;
    }

    public void OnNewStudynEvent(StudynEvent taskEvent)
    {
        Console.WriteLine("autoScheduler.OnNewStudynEvent");
        switch (taskEvent.EventType)
        {
            // On any add, edit, or modify task/appointment, rerun the scheduler
            case StudynEventType.AddTask:
            case StudynEventType.EditTask:
            case StudynEventType.DeleteTask:
                //run(taskEvent.Id);
                break;
            case StudynEventType.AppointmentAdd:
            case StudynEventType.AppointmentEdit:
            case StudynEventType.AppointmentDelete:
            {
                run(taskEvent.Id); 
                break;
            }
            case StudynEventType.CompleteTask:
            {
                // Console Logging just so we can see in the output something is happening
                Console.WriteLine("Scheduler Has CompleteTask Events!");
                GlobalAppointmentData.CalendarManager.TaskCompleted(taskEvent.Id);
                break;
            }
        }
    }

}

internal class minuteSnapshot
{   
    public Guid? id;
    public string name;
    public string from; //Where it is from. IE cretaed by autoScheduler? From calendar? ics file?
    public minuteSnapshot() { id = null; from = ""; name = ""; }
}

internal class BlockContainer
{
    public TaskItem task;
    public int mappedBlocks = 0;
    public int blockSize;
    public Block[] blocks;
    public int remainder; //Minutes remaining after blocks are allocated. IE a task that is 1 hour 15 mins will have 15 minutes remaining (assuming 1 hour block size)
}
internal class Block
{
    public Guid taskID; //equals TaskID
    public Guid blockID;
    public int start; //start on minuteMap
    public int end;   //end on minuteMap

    public Block(Guid tid, Guid bid, int s, int e)
    {
        taskID = tid;
        blockID = bid;
        start = s;
        end = e;
    }
}
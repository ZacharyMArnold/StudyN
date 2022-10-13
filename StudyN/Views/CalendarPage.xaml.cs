﻿using DevExpress.Maui.Scheduler;
using StudyN.Common;
using StudyN.Models; //Calls Calendar Data
using StudyN.ViewModels;
using System.ComponentModel;

namespace StudyN.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]


    public partial class CalendarPage : ContentPage
    {
        readonly CalendarDataView _calendarDataView;
        public CalendarPage()
        {
            InitializeComponent();
            ViewModel = new CalendarViewModel();
            BindingContext = _calendarDataView  = new CalendarDataView(); //Use to pull data of CalendarData under Models

        }

        CalendarViewModel ViewModel { get; }


        void OnDailyClicked(object sender, EventArgs args)
        {
            dayView.IsVisible = true;
            weekView.IsVisible = false;
            monthView.IsVisible = false;

        }

        void OnWeeklyClicked(object sender, EventArgs args)
        {
            dayView.IsVisible = false;
            weekView.IsVisible = true;
            monthView.IsVisible = false;

        }

        void OnMonthlyClicked(object sender, EventArgs args)
        {
            dayView.IsVisible = false;
            weekView.IsVisible = false;
            monthView.IsVisible = true;
        }

        protected override void OnAppearing()
        {
            var notes = weekviewStorage.GetAppointments(new DateTimeRange(DateTime.Now, DateTime.Now.AddDays(7)));
            CalendarDataView.LoadDataForNotification(notes.ToList());
            base.OnAppearing();
            ViewModel.OnAppearing();
        }

        private void Handle_onCalendarTap_FromDayView(object sender, SchedulerGestureEventArgs e)
        {
            if (e.AppointmentInfo == null)
            {
                ShowNewAppointmentEditPage(e.IntervalInfo);
                return;
            }
            AppointmentItem appointment = e.AppointmentInfo.Appointment;
            ShowAppointmentEditPage(appointment);
        }

        private async void Handle_onCalendarHold_FromDayView(object sender, SchedulerGestureEventArgs e)
        {
            if (e.AppointmentInfo != null)
            {
                AppointmentItem appointment = e.AppointmentInfo.Appointment;
                bool answer = await DisplayAlert("Are you sure?",
                    appointment.Subject + " will be deleted.", "Yes", "No");

                if (answer == true)
                {
                    dayviewStorage.RemoveAppointment(appointment);
                }
            }
        }

        private void ShowAppointmentEditPage(AppointmentItem appointment)
        {
            AppointmentEditPage appEditPage = new(appointment, dayviewStorage);
            Navigation.PushAsync(appEditPage);
        }

        private void ShowNewAppointmentEditPage(IntervalInfo info)
        {
            AppointmentEditPage appEditPage = new(info.Start, info.End, info.AllDay, dayviewStorage);
            Navigation.PushAsync(appEditPage);
        }

        // estep: I know there must be a better way to do this, but I just want to try it
        //        since it won't let me use the same storage name for both SchedulerDataStorage objects
        //        (so I have to have this kind of repeated code)
        private void Handle_onCalendarTap_FromWeekView(object sender, SchedulerGestureEventArgs e)
        {
            if (e.AppointmentInfo == null)
            {
                ShowNewAppointmentEditPage_WeekView(e.IntervalInfo);
                //return;
            }
            AppointmentItem appointment = e.AppointmentInfo.Appointment;
            ShowAppointmentEditPage_WeekView(appointment);
        }

        private async void Handle_onCalendarHold_FromWeekView(object sender, SchedulerGestureEventArgs e)
        {
            if (e.AppointmentInfo != null)
            {
                AppointmentItem appointment = e.AppointmentInfo.Appointment;
                bool answer = await DisplayAlert("Are you sure?",
                    appointment.Subject + " will be deleted.", "Yes", "No");

                if (answer == true)
                {
                    weekviewStorage.RemoveAppointment(appointment);
                }
            }
        }

        private void ShowAppointmentEditPage_WeekView(AppointmentItem appointment)
        {
            AppointmentEditPage appEditPage = new(appointment, weekviewStorage);
            Navigation.PushAsync(appEditPage);
        }

        private void ShowNewAppointmentEditPage_WeekView(IntervalInfo info)
        {
            AppointmentEditPage appEditPage = new(info.Start, info.End,
                                                                     info.AllDay, weekviewStorage);
            Navigation.PushAsync(appEditPage);
        }

        private void Handle_onCalendarTap_FromMonthView(object sender, SchedulerGestureEventArgs e)
        {
            //OnDailyClicked(sender, e); // estepanek: not sure if this is causing the devexpress.maui.navigation assembly not found error, because it seems to go away when I comment this out
        }

        //View of events 
        public class CalendarDataView : INotifyPropertyChanged
        {
            readonly AppData data;

            public event PropertyChangedEventHandler PropertyChanged;
            public static DateTime StartDate { get { return AppData.BaseDate; } }
            
            public IReadOnlyList<Appointment> Appointments { get => data.Appointments; } 
            public IReadOnlyList<AppointmentCategory> AppointmentCategories { get => data.AppointmentCategories; }
            public IReadOnlyList<AppointmentStatus> AppointmentStatuses { get => data.AppointmentStatuses; }

            public CalendarDataView()
            {
                data = new AppData();
            }

            public void LoadDataForNotification()
            {
                LoadDataForNotification(Appointments);
            }

            public static void LoadDataForNotification(IReadOnlyList<AppointmentItem> appointments)
            {
                DataAccess.LoadData(appointments);
            }

            protected void RaisePropertyChanged(string name)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                    LoadDataForNotification();
                }
            }
        }

    }
} 
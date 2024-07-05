using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;
using Xceed.Wpf.AvalonDock.Controls;

namespace Calender
{
    public partial class MainWindow : Window
    {
        private readonly string tasksFilePath = "tasks.json";
        private int currentYear;
        public ObservableCollection<TaskItem> Tasks { get; set; }
        private Dictionary<DateTime, ObservableCollection<TaskItem>> tasksByDate;
        private string _taskSummary;

        public string TaskSummary
        {
            get { return _taskSummary; }
            set
            {
                if (_taskSummary != value)
                {
                    _taskSummary = value;
                    OnPropertyChanged(nameof(TaskSummary));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeData();
            InitializeUI();
        }

        private void InitializeData()
        {
            tasksByDate = LoadTasksFromFile() ?? new Dictionary<DateTime, ObservableCollection<TaskItem>>();
            Tasks = new ObservableCollection<TaskItem>();
        }

        private void InitializeUI()
        {
            MainCalendar.DisplayDateChanged += MainCalendar_DisplayDateChanged;
            MainCalendar.SelectedDatesChanged += MainCalendar_SelectedDatesChanged;
            MainCalendar.SelectedDate = DateTime.Today;
            InitializeTasksForDate(DateTime.Today);
            UpdateMonthText();
            UpdateYearButtons(DateTime.Today.Year);
            currentYear = DateTime.Today.Year;
            MainCalendar.DisplayDate = new DateTime(DateTime.Today.Year, MainCalendar.DisplayDate.Month, 1);
            DateTime today = DateTime.Today;
            DayText.Text = today.Day.ToString();
            MonthDayText.Text = today.ToString("MMMM");
            WeekDayText.Text = today.ToString("dddd");
            MainCalendar.SelectedDate = today;
            TaskSummaryText.Loaded += TaskSummaryText_Loaded;
            UpdateTaskSummaryText();
            Closing += MainWindow_Closing;
            CheckAndNotifyForTodayTasks();
        }

        private void CheckAndNotifyForTodayTasks()
        {
            DateTime today = DateTime.Today;
            if (tasksByDate.ContainsKey(today) && tasksByDate[today].Count > 0)
            {
                PlayNotificationSound();
                NotificationWindow notificationWindow = new NotificationWindow("You have tasks for today!");
                notificationWindow.ShowDialog();
            }
        }
        private void PlayNotificationSound()
        {
            try
            {
                // Specify the path to the sound file
                string soundFilePath = "Notify.wav"; // Ensure this file is included in your project and set to copy to output directory
                SoundPlayer player = new SoundPlayer(soundFilePath);
                player.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing sound: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            SaveTasksToFile();
        }

        private void SaveTasksToFile()
        {
            try
            {
                string json = JsonConvert.SerializeObject(tasksByDate);
                File.WriteAllText(tasksFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving tasks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Dictionary<DateTime, ObservableCollection<TaskItem>> LoadTasksFromFile()
        {
            try
            {
                if (File.Exists(tasksFilePath))
                {
                    string json = File.ReadAllText(tasksFilePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        return JsonConvert.DeserializeObject<Dictionary<DateTime, ObservableCollection<TaskItem>>>(json);
                    }
                    else
                    {
                        MessageBox.Show("Tasks file is empty.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tasks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        private void TaskSummaryText_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTaskSummaryText();
        }

        private void UpdateYearButtons(int year)
        {
            YearSelectionPanel.Children.Clear();
            for (int y = year - 5; y <= year + 5; y++)
            {
                Button yearButton = new Button();
                yearButton.Content = y.ToString();
                yearButton.Style = (Style)FindResource("button");
                yearButton.Click += YearButton_Click;
                if (y == DateTime.Today.Year)
                {
                    yearButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3e5ea8"));
                }
                else if (y == currentYear)
                {
                    yearButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3ea85a"));
                }
                YearSelectionPanel.Children.Add(yearButton);
            }
        }

        private void InitializeTasksForDate(DateTime date)
        {
            if (!tasksByDate.ContainsKey(date))
            {
                tasksByDate[date] = new ObservableCollection<TaskItem>();
            }
            SelectedDateTasksListBox.ItemsSource = tasksByDate[date];
        }

        private void YearButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int year;
            if (button != null && int.TryParse(button.Content.ToString(), out year))
            {
                MainCalendar.DisplayDate = new DateTime(year, MainCalendar.DisplayDate.Month, 1);
                currentYear = year;
                UpdateYearButtons(year);
            }
        }

        private void MainCalendar_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            UpdateMonthText();
        }

        private void MainCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainCalendar.SelectedDate.HasValue)
            {
                DateTime selectedDate = MainCalendar.SelectedDate.Value;
                InitializeTasksForDate(selectedDate);
                UpdateDayText(selectedDate);
            }
        }

        private void UpdateMonthText()
        {
            DateTime today = DateTime.Today;
            DateTime displayedDate = MainCalendar.DisplayDate;
            MonthText.Text = displayedDate.ToString("MMMM");
            if (displayedDate.Month == today.Month && displayedDate.Year == today.Year)
            {
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3ea85a"));
            }
            else
            {
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3e5ea8"));
            }

            foreach (Button button in MonthButtons.Children)
            {
                int month;
                if (button != null && int.TryParse(button.Content.ToString(), out month))
                {
                    if (month == today.Month && displayedDate.Year == today.Year)
                    {
                        button.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3e5ea8"));
                    }
                    else if (month == displayedDate.Month)
                    {
                        button.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3ea85a"));
                    }
                    else
                    {
                        button.Foreground = new SolidColorBrush(Colors.Black);
                    }
                }
            }

            foreach (var kvp in tasksByDate)
            {
                DateTime date = kvp.Key;
                if (date.Month == displayedDate.Month && date.Year == displayedDate.Year)
                {
                    foreach (var item in MainCalendar.FindVisualChildren<CalendarDayButton>())
                    {
                        if (item.DataContext is DateTime dt && dt == date)
                        {
                            item.Foreground = kvp.Value.Any() ? new SolidColorBrush(Colors.Green) : item.Foreground;
                        }
                    }
                }
            }
        }

        private void lblNote_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lblNote.Visibility = Visibility.Collapsed;
            txtNote.Visibility = Visibility.Visible;
            txtNote.Focus();
        }

        private void txtNote_TextChanged(object sender, TextChangedEventArgs e)
        {
            lblNote.Visibility = string.IsNullOrWhiteSpace(txtNote.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MonthButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int month;
            if (button != null && int.TryParse(button.Content.ToString(), out month))
            {
                MainCalendar.DisplayDate = new DateTime(MainCalendar.DisplayDate.Year, month, 1);
            }
        }

        private void PreviousDay_Click(object sender, RoutedEventArgs e)
        {
            ChangeDay(-1);
        }

        private void NextDay_Click(object sender, RoutedEventArgs e)
        {
            ChangeDay(1);
        }

        private void ChangeDay(int days)
        {
            DateTime currentDay;
            if (MainCalendar.SelectedDate.HasValue)
            {
                currentDay = MainCalendar.SelectedDate.Value;
            }
            else
            {
                currentDay = DateTime.Today;
            }

            DateTime newDay = currentDay.AddDays(days);
            MainCalendar.SelectedDate = newDay;
            MainCalendar.DisplayDate = newDay;
            UpdateDayText(newDay);
        }

        private void UpdateDayText(DateTime date)
        {
            DayText.Text = date.Day.ToString();
            MonthDayText.Text = date.ToString("MMMM");
            WeekDayText.Text = date.ToString("dddd");
        }

        public class TaskItem
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public string Color { get; set; }
        }

        private void AddNote_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtNote.Text) && MainCalendar.SelectedDate.HasValue)
            {
                DateTime selectedDate = MainCalendar.SelectedDate.Value;
                TaskItem newTask = new TaskItem
                {
                    Title = txtNote.Text,
                    Color = "#3ea85a",
                };

                tasksByDate[selectedDate].Add(newTask);

                txtNote.Clear();
                lblNote.Visibility = Visibility.Visible;

                UpdateTaskSummaryText();
            }
        }

        private void DeleteNote_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            ListBoxItem listBoxItem = FindParent<ListBoxItem>(button);
            if (listBoxItem != null)
            {
                TaskItem taskItem = (TaskItem)listBoxItem.DataContext;
                if (taskItem != null)
                {
                    ObservableCollection<TaskItem> tasks = tasksByDate[MainCalendar.SelectedDate.Value];
                    if (tasks != null)
                    {
                        tasks.Remove(taskItem);
                        UpdateTaskSummaryText();
                    }
                }
            }
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null)
                return null;
            T parent = parentObject as T;
            return parent ?? FindParent<T>(parentObject);
        }

        private void UpdateTaskSummaryText()
        {
            int totalTasks = 0;
            int totalDatesWithTasks = 0;

            foreach (var tasksForDate in tasksByDate.Values)
            {
                totalTasks += tasksForDate.Count;
                if (tasksForDate.Count > 0)
                {
                    totalDatesWithTasks++;
                }
            }

            TaskSummaryText.Text = $"{totalTasks} tasks - {totalDatesWithTasks} dates with tasks";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
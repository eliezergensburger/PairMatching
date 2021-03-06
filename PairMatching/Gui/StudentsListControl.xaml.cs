using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BO;
using LogicLayer;


namespace Gui
{
    /// <summary>
    /// Students list view.
    /// Add, remove student.
    /// Send Emails to singel or many students.
    /// Search Students by their prefix name.
    /// </summary>
    public partial class StudentsListControl : UserControl, INotifyPropertyChanged
    {
        private static readonly ILogicLayer logicLayer = LogicFactory.GetLogicFactory();

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isSimpleView = false;
        /// <summary>
        /// Determines whether this is a view of the list only without actions
        /// </summary>
        public bool IsSimpleView
        {
            get => _isSimpleView; 
            set
            { 
                _isSimpleView = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSimpleView"));
            }
        }


        public StudentsListControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set the item source of the list view to the last filter of the student list
        /// </summary>
        public void SetItemsSource()
        {
            lvStudents.ItemsSource = logicLayer.StudentList;
            tbIsThereResultOfSearcing.Text = string.Empty;
        }

        public void SetItemsSource(Student student)
        {
            if (student.IsFromIsrael)
            {
                logicLayer.StudentListFilter = s => !s.IsFromIsrael;
            }
            else
            {
                logicLayer.StudentListFilter = s => s.IsFromIsrael;
            }
            lvStudents.ItemsSource = logicLayer.StudentList;
            tbIsThereResultOfSearcing.Text = string.Empty;
        }

        private void lvStudents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsSimpleView)
            {
                return;
            }
            var selectedStudent = lvStudents.SelectedItem as Student;
            if (selectedStudent == null)
            {
                return;
            }
            var mainWin = Application.Current.MainWindow as MainWindow;
            mainWin.ShowStudent(selectedStudent);
        }

        private async void manualMatchBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedList = logicLayer.GetAllStudentsBy(s => s.IsSelected);
            if (selectedList.Count() != 2)
            {
                Messages.MessageBoxWarning("בחר 2 תלמידים מהרשימה על מנת לבצע התאמה");
                return;
            }

            try
            {
                var first = selectedList.First();
                var second = selectedList.Last();
                if (Messages.MessageBoxConfirmation($"בטוח שברצונך להתאים את {first.Name} ל- {second.Name}?"))
                {
                    int id = await logicLayer.MatchAsync(first, second);
                    var mainWin = Application.Current.MainWindow as MainWindow;
                    mainWin.RefreshMyStudentsView();
                    mainWin.RefreshMyPairView();
                }
            }
            catch (Exception ex)
            {
                Messages.MessageBoxError(ex.Message);
            }
        }

        private void Search()
        {
            tbIsThereResultOfSearcing.Text = string.Empty;
            if (tbSearch.Text != string.Empty)
            {
                logicLayer.SearchStudents(tbSearch.Text);
                lvStudents.ItemsSource = logicLayer.StudentList;
                if (lvStudents.Items.IsEmpty)
                {
                    tbIsThereResultOfSearcing.Text = "אין תוצאות";
                    return;
                }
                lvStudents.Items.Refresh();
            }
            else
            {
                logicLayer.StudentListFilter = s => !s.IsDeleted;
                lvStudents.ItemsSource = logicLayer.StudentList;
            }
        }

        private void searchBtn_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void tbSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Search();
            }
        }

        private void addStudent_Click(object sender, RoutedEventArgs e)
        {
            new AddStudentWin().Show();
        }

        private void selectAllStudentsCB_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var s in logicLayer.StudentList)
            {
                s.IsSelected = true;
            }
            lvStudents.Items.Refresh();
        }

        private void selectAllStudentsCB_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var s in logicLayer.StudentList)
            {
                s.IsSelected = false;
            }
            lvStudents.Items.Refresh();
        }

        private async void deleteStudent_Click(object sender, RoutedEventArgs e)
        {
            var selectedStudent = (sender as Button).DataContext as Student;
            try
            {
                if (Messages.MessageBoxConfirmation($"האם אתה בטוח שברצונך למחוק את {selectedStudent.Name}?"))
                {
                    await logicLayer.RemoveStudentAsync(selectedStudent.Id);
                    var mainWin = Application.Current.MainWindow as MainWindow;
                    mainWin.RefreshMyStudentsView();
                }
            }
            catch (Exception ex)
            {
                Messages.MessageBoxError(ex.Message);
            }

        }

        private void sendEmaileToStudentsBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedStudent = (sender as Button).DataContext as Student;
            new SendOpenEmail()
            {
                StudentName = selectedStudent.Name,
                Email = selectedStudent.Email
            }.Show();
        }

        private void sendEmaileForAllStudentsBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedStudents = logicLayer.GetAllStudentsBy(s => s.IsSelected);
            if (selectedStudents.Count() == 0)
            {
                Messages.MessageBoxWarning("בחר תלמיד אחד או יותר");
                return;
            }
            new SendOpenEmail()
            {
                StudentName = selectedStudents.Count() > 5 ? "כל המסומנים" :
                                string.Join(", ", from s in selectedStudents
                                                  select s.Name),
                Email = string.Join(", ", from s in selectedStudents
                                          select s.Email)
            }.Show();
        }

        private async void sendStatusEmailForAll_Click(object sender, RoutedEventArgs e)
        {
            var selectedStudents = logicLayer.GetAllStudentsBy(s => s.IsSelected);
            if (selectedStudents.Count() == 0)
            {
                Messages.MessageBoxWarning("בחר תלמיד אחד או יותר");
                return;
            }
            List<Task> tasks = new List<Task>();
            try
            {
                foreach (var s in selectedStudents)
                {
                    tasks.Add(logicLayer.SendEmailToStudentAsync(s, EmailTypes.StatusQuiz));
                }
                await Task.WhenAll(tasks);
                Messages.MessageBoxSimple("המיילים נשלחו בהצלחה!");
            }
            catch (Exception ex)
            {
                Messages.MessageBoxError(ex.Message);
            }

        }

        private void editStudentBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedStudent = (sender as Button).DataContext as Student;
            try
            {
                if (selectedStudent != null)
                {
                    logicLayer.UpdateStudent(selectedStudent);
                    Messages.MessageBoxSimple($"המשתתף {selectedStudent.Name} עודכן בהצלחה");
                }
            }
            catch (Exception ex)
            {
                Messages.MessageBoxError(ex.Message);
            }
        }

        private void filterBtn_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}

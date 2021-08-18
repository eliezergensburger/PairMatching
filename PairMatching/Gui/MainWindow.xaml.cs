﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using LogicLayer;
using BO;
using System.Globalization;

namespace Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly IBL bl = BlFactory.GetBL();

        private ObservableCollection<Pair> pairsList = new ObservableCollection<Pair>();

        private bool isStudentWitoutPairUi;

        public MainWindow()
        {
            // TODO: create data template for the info of student and change that by the country
            // TODO: make a better solution for the Visablity of the panels, maybe using trigers 
            // and data tamplates
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //await bl.ReadDataFromSpredsheetAsync();
            await bl.UpdateAsync();
            await Task.Run(() =>
            {
                var pairsTemp = bl.GetAllPairs();
                if(pairsTemp != null)
                {
                    pairsList = new ObservableCollection<Pair>(pairsTemp);
                }
            });
        }

        private void allStudentBtn_Click(object sender, RoutedEventArgs e)
        {
            isStudentWitoutPairUi = false;
            lvStudents.ItemsSource = bl.StudentList;

            //await bl.SendEmailToStudentAsync(
            //    bl.GetStudent(s => s.Email == "noam8shu@gmail.com"), EmailTypes.StatusQuiz);

            allPairsGrig.Visibility = Visibility.Collapsed;
            tbIsThereResultOfSearcing.Text = string.Empty;
            allStudentGrig.Visibility = Visibility.Visible;
        }

        private void allPairsBtn_Click(object sender, RoutedEventArgs e)
        {
            lvPairs.ItemsSource = pairsList;
            
            allStudentGrig.Visibility = Visibility.Collapsed;
            tbIsThereResultOfSearcing.Text = string.Empty;
            spStudent.Visibility = Visibility.Collapsed;
            spBtnForMatch.Visibility = Visibility.Collapsed;
            lbOpenQuestions.Visibility = Visibility.Collapsed;
            allPairsGrig.Visibility = Visibility.Visible;
        }

        private async void removePairBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedPairs = pairsList.Where(p => p.IsSelected);
            int numOfPairsToRem = selectedPairs.Count();
            if (numOfPairsToRem == 0)
            {
                MessageBoxWarning("בחר אחת או יותר חברותות");
                return;
            }
            try
            {
                if (numOfPairsToRem > 1)
                {
                    if (MessageBoxConfirmation($"האם אתה בטוח שברצונך למחוק את {numOfPairsToRem} החברותות שבחרת?"))
                    {
                        foreach (var p in selectedPairs)
                        {
                            await bl.RemovePairAsync(p);
                        }
                        RefreshMyPairView();
                        RefreshMyStudentsView();
                    }
                }
                else
                {
                    if (MessageBoxConfirmation($"האם אתה בטוח שברצונך למחוק את החברותא {selectedPairs.First()} ?"))
                    {
                        await bl.RemovePairAsync(selectedPairs.First());
                        RefreshMyPairView();
                        RefreshMyStudentsView();
                    }
                }
            }
            catch (BadPairException ex)
            {
                MessageBoxError(ex.Message);
            }
        }

        private void allStudentWithoutPairBtn_Click(object sender, RoutedEventArgs e)
        {
            isStudentWitoutPairUi = true;
            lvStudents.ItemsSource = new ObservableCollection<Student>(bl.GetAllStudentsBy(s => s.MatchTo == 0));
            
            allPairsGrig.Visibility = Visibility.Collapsed;
            tbIsThereResultOfSearcing.Text = string.Empty;
            allStudentGrig.Visibility = Visibility.Visible;
        }

        private async void updateBtn_Click(object sender, RoutedEventArgs e)
        {
            allStudentGrig.Visibility = Visibility.Collapsed;
            spStudent.Visibility = Visibility.Collapsed;
            lbOpenQuestions.Visibility = Visibility.Collapsed;
            allPairsGrig.Visibility = Visibility.Collapsed;
            spBtnForMatch.Visibility = Visibility.Collapsed;
            cbUpdateForEnable.IsChecked = false;
            tbIsThereResultOfSearcing.Text = string.Empty;
            try
            {
                pbUpdate.Visibility = Visibility.Visible;
                await bl.ReadDataFromSpredsheetAsync();
                await bl.UpdateAsync();
            }
            catch (Exception ex)
            {
                MessageBoxError(ex.Message);
            }
            finally
            {
                pbUpdate.Visibility = Visibility.Collapsed;
                cbUpdateForEnable.IsChecked = true;
            }
        }

        private void lvStudents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedStudent = lvStudents.SelectedItem as Student;
            if(selectedStudent == null)
            {
                return;
            }
            spStudent.DataContext = selectedStudent;
            lbOpenQuestions.DataContext = selectedStudent;
            spStudent.Visibility = Visibility.Visible;
            lbOpenQuestions.Visibility = Visibility.Visible;
            spBtnForMatch.Visibility = Visibility.Visible;
        }


        private async void matchBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedStudent = lvStudents.SelectedItem as Student;
            var firstSelectedMatch = cbFirstMatching.SelectedItem as SuggestStudent;
            var secondSelectedMatch = cbSecondeMatching.SelectedItem as SuggestStudent;
            if (firstSelectedMatch == null && secondSelectedMatch == null)
            {
                MessageBoxWarning("בחר תלמיד ממהצעות על מנת להתאים");
                return;
            }
            try
            {
                if (firstSelectedMatch != null)
                {

                    if (MessageBoxConfirmation($"בטוח שברצונך להתאים את {selectedStudent.Name} ל- {firstSelectedMatch.SuggestStudentName}?"))
                    {
                        var first = bl.GetStudent(firstSelectedMatch.SuggestStudentId);
                        await bl.MatchAsync(selectedStudent, first);
                        RefreshMyStudentsView();
                        RefreshMyPairView();
                    }
                    return;
                }
                if (secondSelectedMatch != null)
                {
                    if (MessageBoxConfirmation($"בטוח שברצונך להתאים את {selectedStudent.Name} ל- {secondSelectedMatch.SuggestStudentName}?"))
                    {
                        var seconde = bl.GetStudent(secondSelectedMatch.SuggestStudentId);
                        await bl.MatchAsync(selectedStudent, seconde);
                        RefreshMyStudentsView();
                        RefreshMyPairView();
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBoxError(ex.Message);
            }
        }

        private async void manualMatchBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedList = bl.GetAllStudentsBy(s => s.IsSelected);
            if(selectedList.Count() != 2)
            {
                MessageBoxWarning("בחר 2 תלמידים מהרשימה על מנת לבצע התאמה");
                return;
            }

            try
            {
                var first = selectedList.First();
                var second = selectedList.Last();
                if (MessageBoxConfirmation($"בטוח שברצונך להתאים את {first.Name} ל- {second.Name}?"))
                {
                    await bl.MatchAsync(first, second);
                    RefreshMyStudentsView();
                    RefreshMyPairView();
                }
            }
            catch (Exception ex)
            {
                MessageBoxError(ex.Message);
            }
        }

        private void allStudentFromWorldBtn_Click(object sender, RoutedEventArgs e)
        {
            lvStudents.ItemsSource = new ObservableCollection<Student>(bl.GetAllStudentsBy(s => !s.IsFromIsrael));
            allPairsGrig.Visibility = Visibility.Collapsed;
            tbIsThereResultOfSearcing.Text = string.Empty;
            allStudentGrig.Visibility = Visibility.Visible;
        }

        private void allStudentFromIsraelBtn_Click(object sender, RoutedEventArgs e)
        {
            lvStudents.ItemsSource = new ObservableCollection<Student>(bl.GetAllStudentsBy(s => s.IsFromIsrael));
            allPairsGrig.Visibility = Visibility.Collapsed;
            tbIsThereResultOfSearcing.Text = string.Empty;
            allStudentGrig.Visibility = Visibility.Visible;
        }

        private void RefreshMyPairView()
        {
            pairsList = new ObservableCollection<Pair>(bl.GetAllPairs());
            lvPairs.ItemsSource = pairsList;
            tbIsThereResultOfSearcing.Text = string.Empty;
        }

        private void RefreshMyStudentsView()
        {
            lvStudents.ItemsSource = isStudentWitoutPairUi ?
                new ObservableCollection<Student>(bl.GetAllStudentsBy(s => s.MatchTo == 0)) 
                : bl.StudentList;

            spStudent.Visibility = Visibility.Collapsed;
            lbOpenQuestions.Visibility = Visibility.Collapsed;
            spBtnForMatch.Visibility = Visibility.Collapsed;
            tbIsThereResultOfSearcing.Text = string.Empty;
        }

        private void MessageBoxError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK,
                MessageBoxImage.Error, MessageBoxResult.None,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
        }

        private void MessageBoxWarning(string message)
        {
            MessageBox.Show(message, "Warning", MessageBoxButton.OK,
                MessageBoxImage.Warning, MessageBoxResult.None, 
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
        }

        private bool MessageBoxConfirmation(string message)
        {
            return MessageBox.Show(message, "Confirmation", MessageBoxButton.YesNo,
                        MessageBoxImage.Question, MessageBoxResult.OK,
                        MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading)
                            == MessageBoxResult.Yes;
        }

        private void searchBtn_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void clearCBFirstMatchBtn_Click(object sender, RoutedEventArgs e)
        {
            cbFirstMatching.SelectedItem = null;
        }

        private void clearCBSecondeMatchBtn_Click(object sender, RoutedEventArgs e)
        {
            cbSecondeMatching.SelectedItem = null;
        }

        private void compFirstMatchBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowComparingStudentsWin(cbFirstMatching.SelectedItem);
        }

        private void compSecondeMatchBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowComparingStudentsWin(cbSecondeMatching.SelectedItem);
        }

        private void ShowComparingStudentsWin(object fristStudent)
        {
            if (fristStudent != null && lvStudents.SelectedItem != null)
            {
                var first = bl.GetStudent((fristStudent as SuggestStudent).SuggestStudentId);
                var seconde = lvStudents.SelectedItem as Student;
                if (first.Country == "Israel")
                {
                    new ComparingStudentsWin(first, seconde).Show();
                }
                else
                {
                    new ComparingStudentsWin(seconde, first).Show();
                }
            }
        }

        private void lvPairs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedPair = lvPairs.SelectedItem as Pair;
            if(selectedPair == null)
            {
                return;
            }
            var first = bl.StudentList.FirstOrDefault(s => s.Id == selectedPair.StudentFromIsrael.Id);
            var seconde = bl.StudentList.FirstOrDefault(s => s.Id == selectedPair.StudentFromWorld.Id);
            if (first.Country == "Israel")
            {
                new ComparingStudentsWin(first, seconde).Show();
            }
            else
            {
                new ComparingStudentsWin(seconde, first).Show();
            }
        }

        private  void tbSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                Search();
            }
        }

        private void Search()
        {
            tbIsThereResultOfSearcing.Text = string.Empty;
            if (tbSearch.Text != string.Empty)
            {
                lvStudents.ItemsSource = new ObservableCollection<Student>(bl.SearchStudents(tbSearch.Text));
                if (lvStudents.Items.IsEmpty)
                {
                    tbIsThereResultOfSearcing.Text = "אין תוצאות";
                    return;
                }
                lvStudents.Items.Refresh();
                allPairsGrig.Visibility = Visibility.Collapsed;
                allStudentGrig.Visibility = Visibility.Visible;
            }
        }

        private async void sendEmailToThePairBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedPair = pairsList.Where(p => p.IsSelected);
            int numOfPairs = selectedPair.Count();
            if (numOfPairs == 0 || numOfPairs > 1)
            {
                MessageBoxWarning("בחר חברותא אחת");
                return;
            }

            try
            {
                sendEmailToThePairBtn.IsEnabled = false;
                await bl.SendEmailToPairAsync(selectedPair.First(), EmailTypes.YouGotPair);
                MessageBox.Show("המייל נשלח בהצלחה!");
            }
            catch (Exception ex)
            {
                MessageBoxError(ex.Message);
            }
            finally
            {
                sendEmailToThePairBtn.IsEnabled = true;
            }
        }

        private void selectAllPairCB_Checked(object sender, RoutedEventArgs e)
        {

            foreach(var p in pairsList)
            {
                p.IsSelected = true;
            }
            lvPairs.Items.Refresh();
        }

        private void selectAllPairCB_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var p in pairsList)
            {
                p.IsSelected = false;
            }
            lvPairs.Items.Refresh();
        }
    }

    /// <summary>
    /// Converter class for convert from ListViewItem to number of row 
    /// atending to display row number in the list that displays
    /// </summary>
    public class OrdinalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int ordinal = 0;

            if (value is ListViewItem lvi)
            {
                ListView lv = ItemsControl.ItemsControlFromItemContainer(lvi) as ListView;
                ordinal = lv.ItemContainerGenerator.IndexFromContainer(lvi) + 1;
            }

            return ordinal;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter does not provide conversion back from ordinal position to list view item
            throw new InvalidOperationException();
        }
    }

    public class BooleanAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Any(v => (v is bool b) && b);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}

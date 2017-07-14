using System;
using System.Collections.Generic;
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
using System.Threading;
using System.Collections.ObjectModel;

namespace 自动备份系统
{



    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TaskData = new ObservableCollection<object>();
            TaskData.Add(new { a = "fsdfa" });

            lvwTasks.DataContext = TaskData;
          
        }

        public ObservableCollection<object> TaskData;
        

        private void MainWindowLoadedEventHandler(object sender, RoutedEventArgs e)
        {
            // //new TaskSettings("hello").Show();
            //BackupCore bc= new BackupCore(this);
            //// bc.Backup("hello");
            // Thread t = new Thread(new ParameterizedThreadStart(bc.Backup));
            // t.Start("hello");

           
            //TaskSettings ts = new TaskSettings("hello");
            // ts.Show();

        }
        //public void refreshLog(StringBuilder log)
        //{
        //    txtLogPanel.Text = log.ToString();
        //}
    }
}

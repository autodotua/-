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
using System.Configuration;
using System.Diagnostics;

namespace 自动备份系统
{

    public class TaskInfo
    {
        public int Id { get; internal set; }
        public string Name { get; internal set; }
        public string OriginalDirectories { get; internal set; }
        public string TargetDirectories { get; internal set; }
        public string Interval { get; internal set; }
        public string State { get; internal set; }
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TaskData = new ObservableCollection<TaskInfo>();
            //TaskData.Add(new { Name = "NextTime",NextTime = "fsdfa" });

            lvwTasks.DataContext = TaskData;

        }
        public StringBuilder log = new StringBuilder();
        Thread backupThread;
        int currentTaskIndex;
        public ObservableCollection<TaskInfo> TaskData;
        Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        public List<string> itemsName = new List<string>();
        public List<int> itemsLastTime;
        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        public int CurrentBackupThreads = 0;
        public string CurrentFileCount = "";

        private void MainWindowLoadedEventHandler(object sender, RoutedEventArgs e)
        {

            RefreshListView();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
            //TaskData[0].State = "就绪，剩余" + itemsLastTime[0].ToString() + "秒";

        }

        private void timer_Tick(object sender, EventArgs e)
        {



            //TaskData[i].State = "就绪，剩余"+itemsLastTime[i].ToString()+"秒";

            for (int i = 0; i < itemsName.Count; i++)
            {
                
                if (itemsLastTime[i] == 0)
                {
                    if (CurrentBackupThreads == 0)
                    {
                        // itemsLastTime[i] = int.Parse(TaskData[i].Interval);
                        itemsLastTime[i] = -1;
                        CurrentBackupThreads++;
                        BackupCore bc = new BackupCore(this);
                        backupThread = new Thread(new ParameterizedThreadStart(bc.Backup));
                        backupThread.Start(itemsName[i]);
                        currentTaskIndex = i;
                    }
                    
                }
                else if(itemsLastTime[i]>0)
                {

                    itemsLastTime[i]--;
                    TaskData[i].State = "剩余" + itemsLastTime[i].ToString() + "秒";
                    
                }
                else
                {
                    TaskData[i].State = CurrentFileCount;
                    txtLogPanel.Text = log.ToString();
                    txtLogPanel.ScrollToEnd();
                }
                lvwTasks.Items.Refresh();
            }



        }

        private void RefreshListView()
        {
            TaskData.Clear();//清除绑定的数据
            itemsName.Clear();//清除Task项目列表
            cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);//重新初始化config文件
            if (cfa.AppSettings.Settings["Items"] != null)//如果不是第一次运行的话
            {
                foreach (var i in cfa.AppSettings.Settings["Items"].Value.Split(new string[] { "#Split#" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    itemsName.Add(i);

                }

                foreach (var i in itemsName)
                {
                    TaskData.Add(new TaskInfo
                    {
                        Id = TaskData.Count + 1,
                        Name = i,
                        OriginalDirectories = (cfa.AppSettings.Settings[i + "_White"].Value.Remove(cfa.AppSettings.Settings[i + "_White"].Value.Length - 7, 7) + "，除了" + cfa.AppSettings.Settings[i + "_Black"].Value).Replace("#Split#", "、"),
                        TargetDirectories = cfa.AppSettings.Settings[i + "_TargetDirectory"].Value,
                        Interval = cfa.AppSettings.Settings[i + "_Interval"].Value,
                        State = "就绪"
                    });


                    itemsLastTime = new List<int>();
                    itemsLastTime.Add(int.Parse(cfa.AppSettings.Settings[i + "_Interval"].Value));

                }
            }
            else
            {
                cfa.AppSettings.Settings.Add("Items", "");
                cfa.Save();
            }
        }

        private void NewTaskButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            new TaskSettings("").ShowDialog();
            RefreshListView();
            Debug.WriteLine(TaskData[0].ToString());
        }

        private void StopThreadButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            backupThread.Abort();
            CurrentBackupThreads = 0;
            itemsLastTime[currentTaskIndex] = int.Parse(TaskData[currentTaskIndex].Interval);
        }
        //public void refreshLog(StringBuilder log)
        //{
        //    txtLogPanel.Text = log.ToString();
        //}
    }
}

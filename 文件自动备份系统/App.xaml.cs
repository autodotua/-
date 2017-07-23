using System.Windows;

namespace 自动备份系统
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
          MessageBoxResult mbr= MessageBox.Show("抱歉！程序运行出现没有捕获的异常。错误代码：" + System.Environment.NewLine + e.Exception.Message+ System.Environment.NewLine+ System.Environment.NewLine+"是否继续运行？继续运行可能会出现未知错误", "错误（全局）", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if(mbr==MessageBoxResult.No)
            {
                Current.Shutdown();
            }
        }
    }
}

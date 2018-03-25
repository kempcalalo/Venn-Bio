using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Loader
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Throbber : UserControl
    {
        public Throbber()
        {
            InitializeComponent();
            this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(VisibleChanged);
        }
        void VisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                Application.Current.Dispatcher.InvokeAsync(new Action(() =>
                {
                    this.Focus();
                }), DispatcherPriority.ContextIdle);
            }
        }
    }
}

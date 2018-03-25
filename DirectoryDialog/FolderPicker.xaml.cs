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

namespace DirectoryDialog
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class FolderPicker : UserControl
    {
        public static readonly DependencyProperty DirectoryPathProperty = DependencyProperty.Register("DirectoryPath", typeof(string), typeof(FolderPicker),
            new FrameworkPropertyMetadata((string)string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public string DirectoryPath { get { return (string)GetValue(DirectoryPathProperty) ?? string.Empty; } set { SetValue(DirectoryPathProperty, value); } }


        public FolderPicker()
        {
            InitializeComponent();
        }

        private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (var myDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (!string.IsNullOrEmpty(DirectoryPath))
                {
                    myDialog.SelectedPath = DirectoryPath;
                }
                myDialog.ShowDialog();
                DirectoryPath = myDialog.SelectedPath;
            }
        }
    }
}

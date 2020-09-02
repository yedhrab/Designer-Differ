using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
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
using Task = System.Threading.Tasks.Task;

namespace DesignerDiffer
{
    /// <summary>
    /// Interaction logic for CompareWithHistoryDialog.xaml
    /// </summary>
    public partial class CompareWithHistoryDialog : DialogWindow
    {
        private readonly IAsyncServiceProvider asyncServiceProvider;

        public CompareWithHistoryDialog(IAsyncServiceProvider asyncServiceProvider, string helpTopic) : base(helpTopic)
        {
            this.asyncServiceProvider = asyncServiceProvider;

            InitializeComponent();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void OnCompare(object sender, RoutedEventArgs e)
        {

            string branch = this.BranchTextBox.Text.Trim();
            string commitHash = this.CommitHashTextBox.Text.Trim();

            if (branch == "")
            {
                MessageBox.Show("Gerekli alanlar doldurulmadığı için HEAD ile kıyaslanacak");
                branch = "HEAD";
            }

            DTE2 dte2 = await Utility.GetDTE2Async(asyncServiceProvider);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (Utility.CanFileBeCompared(dte2, out string filePath))
            {
                string solutionDir = System.IO.Path.GetDirectoryName(dte2.Solution.FullName);
                string fileContent = Utility.GetFileHistoryContent(solutionDir, filePath, branch, commitHash);
                string tempFilePath = Utility.CopyContentToTemp(filePath, fileContent);

                ProjectItem selectedProjectItem = dte2.ItemOperations.AddExistingItem(filePath);
                FileCodeModel selectedFileCodeModel = selectedProjectItem.FileCodeModel;
                if (selectedFileCodeModel != null)
                {
                    if (Utility.SortFunctionBodyIfExist(selectedFileCodeModel, Utility.GeneratedFunctionName))
                    {
                        ProjectItem tempProjectItem = dte2.ItemOperations.AddExistingItem(tempFilePath);
                        if (Utility.SortFunctionBodyIfExist(tempProjectItem.FileCodeModel, Utility.GeneratedFunctionName))
                        {
                            tempProjectItem.Save();
                            string oldFilePath = filePath.Replace(selectedProjectItem.Name, tempProjectItem.Name);
                            Utility.DiffFiles(dte2, oldFilePath, filePath);
                        }
                        else
                        {
                            MessageBox.Show("Seçili dosyanın belirtilen commit hash için kaydı git ile bulunamadı");
                        }
                        tempProjectItem.Delete();
                    }
                    else
                    {
                        MessageBox.Show("Seçili dosya designer dosyası değil");
                    }
                }
                else
                {
                    MessageBox.Show("Dosya içeriği desteklenmiyor");
                }
            }
            this.Close();
        }


    }
}

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
                string fileName = filePath.Split('\\').Last();

                string copiedFileContent = System.IO.File.ReadAllText(filePath);
                string copiedFileName = Utility.TempPrefix + fileName;
                string copiedFilePath = Utility.CopyContentToTemp(copiedFileName, copiedFileContent);

                ProjectItem copyProjectItem = dte2.ItemOperations.AddExistingItem(copiedFilePath);
                if (Utility.SortFunctionBodyIfExist(copyProjectItem.FileCodeModel, Utility.GeneratedFunctionName))
                {
                    copyProjectItem.Save();
                    copiedFilePath = filePath.Replace(fileName, copiedFileName);
                    copiedFileContent = System.IO.File.ReadAllText(copiedFilePath);
                    copiedFilePath = Utility.CopyContentToTemp(copiedFileName, copiedFileContent);

                    string oldFileContent = Utility.GetFileHistoryContent(solutionDir, filePath, branch, commitHash);
                    string oldFileName = Utility.TempPrefix + Utility.TempPrefix + fileName;
                    string oldFilePath = Utility.CopyContentToTemp(oldFileName, oldFileContent);

                    ProjectItem oldProjectItem = dte2.ItemOperations.AddExistingItem(oldFilePath);
                    if (Utility.SortFunctionBodyIfExist(oldProjectItem.FileCodeModel, Utility.GeneratedFunctionName))
                    {
                        oldProjectItem.Save();
                        oldFilePath = filePath.Replace(fileName, oldFileName);
                        oldFileContent = System.IO.File.ReadAllText(oldFilePath);
                        oldFilePath = Utility.CopyContentToTemp(oldFileName, oldFileContent);

                        Utility.DiffFiles(dte2, oldFilePath, copiedFilePath);
                    }
                    else
                    {
                        MessageBox.Show("Seçili dosyanın belirtilen commit hash için kaydı git ile bulunamadı");
                    }
                    oldProjectItem.Delete();
                }
                else
                {
                    MessageBox.Show("Seçili dosya designer dosyası değil");
                }
                copyProjectItem.Delete();
            }
            this.Close();
        }
    }
}

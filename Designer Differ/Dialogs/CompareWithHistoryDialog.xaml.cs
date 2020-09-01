using EnvDTE;
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

namespace File_Differ
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

        private async void OnSave(object sender, RoutedEventArgs e)
        {

            string branch = this.BranchTextBox.Text.Trim();
            string commitHash = this.CommitHashTextBox.Text.Trim();

            if (branch == "")
            {
                MessageBox.Show("Gerekli alanlar doldurulmadığı için HEAD ile kıyaslanacak");
                branch = "HEAD";
            }

            var dte2 = await Utility.GetDTE2Async(asyncServiceProvider);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (Utility.CanFileBeCompared(dte2, out string filepath))
            {
                string solutionDir = System.IO.Path.GetDirectoryName(dte2.Solution.FullName);
                string fileContent = Utility.GetFileHistoryContent(solutionDir, filepath, branch, commitHash);
                string tempFilePath = Utility.CopyContentToTemp(filepath, fileContent);

                var projectItem = dte2.ItemOperations.AddExistingItem(filepath);
                if (Utility.IsFuncExistInFileCodeModel(projectItem.FileCodeModel, "InitializeComponent", out CodeFunction cf))
                {
                    string generatedCode = Utility.GetFunctionBodyText(cf);
                    generatedCode = Utility.StripComments(generatedCode);
                    generatedCode = Utility.SortContentBy(generatedCode, '\n');
                    Utility.ReplaceFunctionBodyText(generatedCode, cf);
                }

                Utility.DiffFiles(dte2, tempFilePath, filepath);
            }

            this.Close();
        }


    }
}

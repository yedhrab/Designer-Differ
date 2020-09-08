using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO.Packaging;
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
using System.Xml.Linq;
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
            CommitHashTextBox.Focus();
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

                if (copiedFileName.Split('.').Last() == "cs")
                {
                    ProjectItem copyProjectItem = dte2.ItemOperations.AddExistingItem(copiedFilePath);
                    if (Utility.SortFunctionBodyIfExist(copyProjectItem.FileCodeModel, Utility.GeneratedFunctionName))
                    {
                        copyProjectItem.Save();
                        copiedFileContent = System.IO.File.ReadAllText(filePath.Replace(fileName, copiedFileName));
                        copiedFilePath = Utility.CopyContentToTemp(copiedFileName, copiedFileContent);

                        try
                        {
                            string oldFileContent = Utility.GetFileHistoryContent(solutionDir, filePath, branch, commitHash);
                            string oldFileName = Utility.TempPrefix + copiedFileName;
                            string oldFilePath = Utility.CopyContentToTemp(oldFileName, oldFileContent);

                            ProjectItem oldProjectItem = dte2.ItemOperations.AddExistingItem(oldFilePath);
                            if (Utility.SortFunctionBodyIfExist(oldProjectItem.FileCodeModel, Utility.GeneratedFunctionName))
                            {
                                oldProjectItem.Save();
                                oldFileContent = System.IO.File.ReadAllText(filePath.Replace(fileName, oldFileName));
                                oldFilePath = Utility.CopyContentToTemp(oldFileName, oldFileContent);

                                Utility.DiffFiles(dte2, oldFilePath, copiedFilePath);
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Seçili dosyanın belirtilen commit hash için kaydı git ile bulunamadı");
                            }
                            oldProjectItem.Delete();
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            MessageBox.Show("Git kurulu değil veya projede aktif değil");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Seçili dosya designer dosyası değil");
                    }
                    copyProjectItem.Delete();
                }
                else if (copiedFileName.Split('.').Last() == "resx")
                {
                    XDocument doc = XDocument.Load(copiedFilePath);
                    Utility.SortXMLByName(doc.Root);
                    copiedFilePath = System.IO.Path.GetTempPath() + copiedFileName;
                    doc.Save(copiedFilePath);

                    try
                    {
                        string oldFileName = Utility.TempPrefix + copiedFileName;
                        string oldFileContent = Utility.GetFileHistoryContent(solutionDir, filePath, branch, commitHash);
                        string oldFilePath = Utility.CopyContentToTemp(oldFileName, oldFileContent);

                        doc = XDocument.Load(oldFilePath);
                        Utility.SortXMLByName(doc.Root);
                        doc.Save(oldFilePath);

                        Utility.DiffFiles(dte2, oldFilePath, copiedFilePath);
                        this.Close();
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        MessageBox.Show("Git kurulu değil veya projede aktif değil");
                    }
                }
            }

        }
    }
}

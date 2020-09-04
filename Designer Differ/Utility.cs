using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesignerDiffer
{
    public abstract class Utility
    {

        public const string GeneratedFunctionName = "InitializeComponent";
        public const string TempPrefix = "~";

        public static async Task<DTE2> GetDTE2Async(IAsyncServiceProvider asyncServiceProvider) => await asyncServiceProvider.GetServiceAsync(typeof(DTE)).ConfigureAwait(false) as DTE2 ?? throw new NullReferenceException("DTE alınamadı");

        public static string CopyContentToTemp(string fileName, string fileContent)
        {
            string tempFilepath = System.IO.Path.GetTempPath() + fileName;
            System.IO.File.WriteAllText(tempFilepath, fileContent, Encoding.UTF8);
            return tempFilepath;
        }

        public static string GetFileHistoryContent(string repo, string filepath, string branch, string commitHash)
        {
            string relatedFilePath = filepath.Replace($"{repo}\\", "").Replace("\\", "/");

            string fileContent = "";
            var gitProcess = GitProcess($"show {branch}{commitHash}:{relatedFilePath}", repo);
            gitProcess.Start();
            while (!gitProcess.StandardOutput.EndOfStream)
            {
                string line = gitProcess.StandardOutput.ReadLine();
                fileContent += line + "\n";
            }

            return fileContent;
        }

        public static bool CanFileBeCompared(DTE2 dte, out string filepath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            filepath = GetSelectedFiles(dte).ElementAtOrDefault(0);
            return !string.IsNullOrEmpty(filepath);
        }

        /// <summary>
        /// Dosya yollarının bilgilerini verir
        /// </summary>
        /// <param name="dte">VS için otomasyon objesi</param>
        /// <returns></returns>
        public static IEnumerable<string> GetSelectedFiles(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var items = (Array)dte.ToolWindows.SolutionExplorer.SelectedItems;
            return from item in items.Cast<UIHierarchyItem>()
                   let pi = item.Object as ProjectItem
                   select pi.FileNames[1];
        }

        public static System.Diagnostics.Process GitProcess(string arguments, string workdir) => new System.Diagnostics.Process
        {
            StartInfo = {
                    FileName = "git.exe",
                    WorkingDirectory = workdir,
                    Arguments = $"--no-pager {arguments}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
            EnableRaisingEvents = true
        };

        public static void DiffFiles(DTE2 dte2, string filepath1, string filepath2)
        {
            dte2.ExecuteCommand("Tools.DiffFiles", $"\"{filepath1}\" \"{filepath2}\"");
        }

        public static bool IsFuncExistInCodeElements(CodeElements codeElements, string name, out CodeFunction cf)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (CodeElement element in codeElements)
            {
                if (element is CodeNamespace)
                {
                    CodeNamespace nsp = element as CodeNamespace;

                    foreach (CodeElement subElement in nsp.Children)
                    {
                        if (subElement is CodeClass)
                        {
                            CodeClass c2 = subElement as CodeClass;
                            foreach (CodeElement item in c2.Children)
                            {
                                if (item is CodeFunction)
                                {
                                    CodeFunction _cf = item as CodeFunction;
                                    if (_cf.Name == name)
                                    {
                                        cf = _cf;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            cf = null;
            return false;
        }

        public static bool IsFuncExistInFileCodeModel(FileCodeModel fcm, string name, out CodeFunction cf)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return IsFuncExistInCodeElements(fcm.CodeElements, name, out cf);
        }

        public static bool IsFuncExistInActiveDocument(DTE2 dte, string name, out CodeFunction cf)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            FileCodeModel fcm = dte.ActiveDocument.ProjectItem.FileCodeModel;
            return IsFuncExistInFileCodeModel(fcm, name, out cf);
        }

        public static string StripComments(string code)
        {
            var re = @"(@(?:""[^""]*"")+|""(?:[^""\n\\]+|\\.)*""|'(?:[^'\n\\]+|\\.)*')|//.*|/\*(?s:.*?)\*/";
            return Regex.Replace(code, re, "$1");
        }

        public static string SortContentBy(string content, char delim)
        {
            // TODO: Aynı obje kullanımları bittikten sonra \n koyulabilir
            return string.Join(delim.ToString(), content.Split(delim).OrderBy(p => p)).Trim();
        }

        public static string GetFunctionBodyText(CodeFunction cf)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return cf.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint().GetText(cf.EndPoint);
        }

        public static void ReplaceFunctionBodyText(string text, CodeFunction cf)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            cf.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint().ReplaceText(cf.EndPoint, text, (int)vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);
        }

        public static void SortFunctionBody(CodeFunction cf)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string generatedCode = GetFunctionBodyText(cf);
            generatedCode = StripComments(generatedCode);
            generatedCode = SortContentBy(generatedCode, '\n');
            ReplaceFunctionBodyText(generatedCode, cf);
        }

        public static bool SortFunctionBodyIfExist(FileCodeModel fcm, string funcName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (IsFuncExistInFileCodeModel(fcm, funcName, out CodeFunction cf))
            {
                SortFunctionBody(cf);
                return true;
            }
            return false;
        }

    }
}

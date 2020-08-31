using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace File_Differ
{
    public abstract class Utility
    {

        public static async Task<DTE2> GetDTE2Async(IAsyncServiceProvider asyncServiceProvider) => await asyncServiceProvider.GetServiceAsync(typeof(DTE)).ConfigureAwait(false) as DTE2 ?? throw new NullReferenceException("DTE alınamadı");

        public static string CopyContentToTemp(string filepath, string fileContent)
        {
            string[] splitFilepath = filepath.Split('\\');
            string bareFilename = splitFilepath[splitFilepath.Length - 1];
            string tempFilepath = System.IO.Path.GetTempPath() + bareFilename;
            System.IO.File.WriteAllText(tempFilepath, fileContent, Encoding.UTF8);
            return tempFilepath;
        }

        public static string GetFileHistoryContent(string repo, string filepath, string branch, string commitHash)
        {
            string relatedFilePath = filepath.Replace($"{repo}\\", "").Replace("\\", "/");

            string fileContent = "";
            var gitProcess = Utility.GitProcess($"show {branch}{commitHash}:{relatedFilePath}", repo);
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

        public static Window OpenCodeFile(DTE2 dte2, string filepath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return dte2.ItemOperations.OpenFile(filepath, Constants.vsViewKindCode);
        }

        public static bool IsFuncExistInFileCodeModel(FileCodeModel fcm, string name, out CodeFunction cf)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (CodeElement element in fcm.CodeElements)
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

        public static void SortFunctionBodyInActiveDocument(DTE2 dte, string funcName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Utility.IsFuncExistInActiveDocument(dte, funcName, out CodeFunction cf))
            {
                string generatedCode = Utility.GetFunctionBodyText(cf);
                generatedCode = Utility.StripComments(generatedCode);
                generatedCode = Utility.SortContentBy(generatedCode, '\n');
                Utility.ReplaceFunctionBodyText(generatedCode, cf);
            }
        }
    }
}

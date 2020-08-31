# 6. Gün Staj Notları

## VSIX Eklentisi Proje Yapısı

- `vcst` ve `vsixmanifest` dosyası `sync` edilmeli

![](assets/vsix_project_structure.png)

## Git ile dosya değişikliği algılama

- Temel kullanım `git --no-pager show <commitHash>:<filepath>` şeklindedir
- `--no-pager` komutu ile interactive pencere açılmaz
- `git --no-pager show HEAD~<n>:<filepath>` komutu ile istenen dosyanın istenen sürümdeki halini görebiliriz
    - Örnek kullanım: `git --no-pager show HEAD~1:temp.txt`
    - Verilen numaradaki commit yoksa hata verir
- `git --no-pager show HEAD@{YY-MM-DD}:<filepath>` komutu ile belirli tarih için görürüz
    - Örnek kullanım: `git --no-pager show HEAD@{2013-02-25}:./fileInCurrentDirectory.txt`
    - Verilen tarihte olan bir sürüm yok ise uyarı ile en yakın olanı gösterir
- `git --no-pager show HEAD@{2013-02-25}:./fileInCurrentDirectory.txt > old_fileInCurrentDirectory.txt` eski sürümü dosyaya yazar 
- `git --no-pager show -1 filename.txt` Dosyanın bir eski sürümü ile arasındaki farkları gösterir

> - [How can I view an old version of a file with Git?](https://stackoverflow.com/questions/338436/how-can-i-view-an-old-version-of-a-file-with-git)
> - [How to make git log not prompt to continue?](https://stackoverflow.com/a/7737071)


## VS SDK ile Git Komutu Çalıştırma

### Proje dizinini ve dosya yolunu alma

```c#
string filepath = "...";
string solutionDir = System.IO.Path.GetDirectoryName(dte2.Solution.FullName);
filepath = filepath.Replace($"{solutionDir}\\", "").Replace("\\", "/");
```

> [How do you get the current solution directory from a VSPackage?](https://stackoverflow.com/a/2338796s)

### Git process oluşturma ve okuma

- `arguments` alanına 

```c#
static System.Diagnostics.Process GitProcess(string arguments, string workdir)
{
    return new System.Diagnostics.Process
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
}
```

```c#
string fileContent = "";
gitProcess.Start();
while (!gitProcess.StandardOutput.EndOfStream)
{
    string line = gitProcess.StandardOutput.ReadLine();
    fileContent += line + "\n";
}
```

> - [Process.start: how to get the output?](https://stackoverflow.com/a/4291965)
> - [Is there any async equivalent of Process.Start?](https://stackoverflow.com/a/10789196s)

## VS SDK ile değişken değişiklerini gösterme

- Dosyayı geçici dizine aynı uzantı ve ismle kayıt ediyoruz
- Ardından `Tool.DiffFiles` komutu ile geçici dizindeki ile orjinal dosyayı karşılaştırıyoruz

```c#
string[] splitFilepath = filepath.Split('\\');
string bareFilename = splitFilepath[splitFilepath.Length - 1];
string tempFilepath = System.IO.Path.GetTempPath() + bareFilename;
System.IO.File.WriteAllText(tempFilepath, fileContent, System.Text.Encoding.UTF8);
dte2.ExecuteCommand("Tools.DiffFiles", $"\"{tempFilepath}\" \"{filepath}\"");
```

> - [How can I create a temp file with a specific extension with .NET?](https://stackoverflow.com/a/581574)
> - [How to write to a text file (C# Programming Guide](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/file-system/how-to-write-to-a-text-file)

## VS SDK Guid Otomasyonu

- Aşağıdaki alanlar senkronize olan `vsct` c# dosyasından çekilmelidir

```c#
internal sealed class CompareHistoryCommand
{
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = PackageIds.CompareHistoryCommandId;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = PackageGuids.guidFile_VSPackageCmdSet;
// ...
}
```

## VS SDK Menu ID'leri

- [GUIDs and IDs of Visual Studio menus](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/guids-and-ids-of-visual-studio-menus?view=vs-2019s)
- [IDE-Defined Commands for Extending Project Systems](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/ide-defined-commands-for-extending-project-systems?view=vs-2019s)

## Asıl Sorun için Çözüm Önerileri

- Her iki dosyayı da sırala, değişiklikleri gör
- Sıralamayı geri almak gerekebilir

> [Why does C# designer-generated code (like Form1.designer.cs) play havoc with Subversion?](https://stackoverflow.com/a/500091s)

## Ek Notlar

- Bu iş için benzer bir eklenti isteğinde bunulmuş: [Allow "Compare working copy to historical version" version in git](https://developercommunity.visualstudio.com/idea/370956/allow-compare-working-copy-to-historical-version-v.html)
- Bunun daha da gelişmiş hali [Partial Diff](https://marketplace.visualstudio.com/items?itemName=ryu1kn.partial-diff)
- [Visual Studio 2017 Extension development tutorial, Part 2: Add Menu Item](https://michaelscodingspot.com/visual-studio-2017-extension-development-tutorial-part-2-add-menu-item/)
- [Visual Studio Extension: How to get the line on which Context Menu was called?](https://stackoverflow.com/a/46067206)

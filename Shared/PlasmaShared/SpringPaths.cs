#region using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

#endregion

namespace ZkData
{
    public class SpringPaths
    {
        string springVersion;
        readonly string writableFolderOverride;
        private List<string> dataDirectories;
        public string Cache { get; private set; }
        public ReadOnlyCollection<string> DataDirectories => dataDirectories.AsReadOnly();
        public string DedicatedServer { get; private set; }
        public string Executable { get; private set; }
        public string SpringVersion { get { return springVersion; } }
        public string UnitSyncDirectory { get; private set; }
        public string WritableDirectory { get; private set; }
        public bool SafeMode { get; set; }
        public event EventHandler SpringVersionChanged;
        public string DataDirectoriesJoined => string.Join(Environment.OSVersion.Platform == PlatformID.Unix ? ":" : ";", DataDirectories.Distinct());

        private bool useMultipleDataFolders;

        public SpringPaths(string springPath, string writableFolderOverride = null, bool useMultipleDataFolders = true)
        {
            this.writableFolderOverride = writableFolderOverride;
            this.useMultipleDataFolders = useMultipleDataFolders;

            SetEnginePath(springPath);
        }

        public void OverrideDedicatedServer(string path) {
            DedicatedServer = path;
        }

        public string GetEngineFolderByVersion(string version)
        {
            return Utils.MakePath(WritableDirectory, "engine", version);
        }


        public static string GetMySpringDocPath()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix) 
            {
                var path = Utils.MakePath(Environment.GetEnvironmentVariable("HOME"), ".spring");
                if (!IsDirectoryWritable(path)) {
                    path = Utils.MakePath(Directory.GetCurrentDirectory(), ".spring");
                    IsDirectoryWritable(path);
                }
                return path;
            }
            else
            {
                var dir = Utils.MakePath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Spring");
                if (!IsDirectoryWritable(dir))
                {
                    //if not writable - this should be writable
                    dir = Utils.MakePath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Spring");
                }
                return dir;
            }
        }

        public string GetSpringConfigPath()
        {
            return Utils.MakePath(WritableDirectory, "springsettings.cfg");
        }

        public bool HasEngineVersion(string version)
        {
            var path = GetEngineFolderByVersion(version);
            var exec = Path.Combine(path, Environment.OSVersion.Platform == PlatformID.Unix ? "spring" : "spring.exe");
            if (File.Exists(exec)) return true;
            return false;
        }

        public static bool IsDirectoryWritable(string directory)
        {
            try
            {
                try
                {
                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                }
                catch
                {
                    return false;
                }

                var fullPath = Utils.GetAlternativeFileName(Path.Combine(directory, "test.dat"));
                File.WriteAllText(fullPath, "test");
                if (File.Exists(fullPath))
                {
                    File.ReadAllLines(fullPath);
                    File.Delete(fullPath);
                    return true;
                }
            }
            catch {}
            return false;
        }

        public void MakeFolders()
        {
            CreateFolder(Utils.MakePath(WritableDirectory, "games"));
            CreateFolder(Utils.MakePath(WritableDirectory, "maps"));
            CreateFolder(Utils.MakePath(WritableDirectory, "engine"));
            CreateFolder(Utils.MakePath(WritableDirectory, "packages"));
            CreateFolder(Utils.MakePath(WritableDirectory, "pool"));
            CreateFolder(Utils.MakePath(WritableDirectory, "demos"));
            CreateFolder(Utils.MakePath(WritableDirectory, "temp"));
            Cache = Utils.MakePath(WritableDirectory, "cache");
            CreateFolder(Cache);
        }


        public void SetEnginePath(string springPath) {
            if (springPath == null) springPath = "";


            dataDirectories = useMultipleDataFolders ? new List<string> { GetMySpringDocPath(), springPath } : new List<string>() {springPath};
            if (!string.IsNullOrEmpty(writableFolderOverride))
            {
                if (!Directory.Exists(writableFolderOverride))
                {
                    try
                    {
                        Directory.CreateDirectory(writableFolderOverride);
                    }
                    catch {}
                    ;
                }
                dataDirectories.Insert(0, writableFolderOverride);
            }

            dataDirectories = DataDirectories.Where(Directory.Exists).Distinct().ToList();

            WritableDirectory = DataDirectories.First(IsDirectoryWritable);
            UnitSyncDirectory = springPath;

            Executable = Utils.MakePath(springPath, Environment.OSVersion.Platform == PlatformID.Unix ? "spring" : "spring.exe");
            DedicatedServer = Utils.MakePath(springPath, Environment.OSVersion.Platform == PlatformID.Unix ? "spring-dedicated" : "spring-dedicated.exe");
            Cache = Utils.MakePath(WritableDirectory, "cache");

            var ov = springVersion;
            if (springPath != "") springVersion = GetSpringVersion(Executable);
            else springVersion = null;

            Environment.SetEnvironmentVariable("SPRING_DATADIR", DataDirectoriesJoined, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("SPRING_WRITEDIR", WritableDirectory, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("SPRING_ISOLATED", WritableDirectory, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("SPRING_NOCOLOR", "1", EnvironmentVariableTarget.Process);
            
            if (ov != springVersion  && SpringVersionChanged != null) SpringVersionChanged(this, EventArgs.Empty);
        }




        void CreateFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Unable to create folder {0} : {1}", path, ex.Message);
                }
            }
        }

        
        static string GetSpringVersion(string executablePath)
        {
            var LastPart =
                Path.GetDirectoryName(executablePath).Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            return LastPart;
          
        }
    }
}